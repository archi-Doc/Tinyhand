// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Arc.IO;
using MessagePack;
using MessagePack.LZ4;
using Tinyhand.IO;

#pragma warning disable SA1618 // Generic type parameters should be documented

namespace Tinyhand
{
    public static class TinyhandSerializer
    {
        private const int InitialBufferSize = 32 * 1024;
        private const int MaxHintSize = 1024 * 1024;

        /// <summary>
        /// A thread-local, recyclable array that may be used for short bursts of code.
        /// </summary>
        [ThreadStatic]
        private static byte[]? initialBuffer;
        [ThreadStatic]
        private static byte[]? initialBuffer2;

        /// <summary>
        /// Gets or sets the default set of options to use when not explicitly specified for a method call.
        /// </summary>
        /// <value>The default value is <see cref="TinyhandSerializerOptions.Standard"/>.</value>
        /// <remarks>
        /// This is an AppDomain or process-wide setting.
        /// If you're writing a library, you should NOT set or rely on this property but should instead pass
        /// in <see cref="TinyhandSerializerOptions.Standard"/> (or the required options) explicitly to every method call
        /// to guarantee appropriate behavior in any application.
        /// If you are an app author, realize that setting this property impacts the entire application so it should only be
        /// set once, and before any use of <see cref="TinyhandSerializer"/> occurs.
        /// </remarks>
        public static TinyhandSerializerOptions DefaultOptions { get; set; } = TinyhandSerializerOptions.Standard;

        /// <summary>
        /// Serializes a given value with the specified buffer writer.
        /// </summary>
        /// <param name="writer">The buffer writer to serialize with.</param>
        /// <param name="value">The value to serialize.</param>
        /// <param name="options">The options. Use <c>null</c> to use default options.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <exception cref="TinyhandException">Thrown when any error occurs during serialization.</exception>
        public static void Serialize<T>(IBufferWriter<byte> writer, T value, TinyhandSerializerOptions? options = null, CancellationToken cancellationToken = default)
        {
            var w = new TinyhandWriter(writer) { CancellationToken = cancellationToken };
            try
            {
                options = options ?? DefaultOptions;
                if (options.Compression == TinyhandCompression.None)
                {
                    try
                    {
                        options.Resolver.GetFormatter<T>().Serialize(ref w, value, options);
                    }
                    catch (Exception ex)
                    {
                        throw new TinyhandException($"Failed to serialize {typeof(T).FullName} value.", ex);
                    }
                }
                else
                {
                    Serialize(ref w, value, options);
                }

                w.Flush();
            }
            finally
            {
                w.Dispose();
            }
        }

        /// <summary>
        /// Serializes a given value with the specified buffer writer.
        /// </summary>
        /// <param name="value">The value to serialize.</param>
        /// <param name="options">The options. Use <c>null</c> to use default options.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A byte array with the serialized value.</returns>
        /// <exception cref="TinyhandException">Thrown when any error occurs during serialization.</exception>
        public static byte[] Serialize<T>(T value, TinyhandSerializerOptions? options = null, CancellationToken cancellationToken = default)
        {
            if (initialBuffer == null)
            {
                initialBuffer = new byte[InitialBufferSize];
            }

            var w = new TinyhandWriter(initialBuffer) { CancellationToken = cancellationToken };
            try
            {
                options = options ?? DefaultOptions;
                if (options.Compression == TinyhandCompression.None)
                {
                    try
                    {
                        options.Resolver.GetFormatter<T>().Serialize(ref w, value, options);
                    }
                    catch (Exception ex)
                    {
                        throw new TinyhandException($"Failed to serialize {typeof(T).FullName} value.", ex);
                    }
                }
                else
                {
                    Serialize(ref w, value, options);
                }

                return w.FlushAndGetArray();
            }
            finally
            {
                w.Dispose();
            }
        }

        /// <summary>
        /// Serializes a given value to the specified stream.
        /// </summary>
        /// <param name="stream">The stream to serialize to.</param>
        /// <param name="value">The value to serialize.</param>
        /// <param name="options">The options. Use <c>null</c> to use default options.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <exception cref="TinyhandException">Thrown when any error occurs during serialization.</exception>
        public static void Serialize<T>(Stream stream, T value, TinyhandSerializerOptions? options = null, CancellationToken cancellationToken = default)
        {
            if (initialBuffer == null)
            {
                initialBuffer = new byte[InitialBufferSize];
            }

            var w = new TinyhandWriter(initialBuffer) { CancellationToken = cancellationToken };
            try
            {
                Serialize(ref w, value, options);

                try
                {
                    foreach (var segment in w.FlushAndGetReadOnlySequence())
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        stream.Write(segment.Span);
                    }
                }
                catch (Exception ex)
                {
                    throw new TinyhandException("Error occurred while writing the serialized data to the stream.", ex);
                }
            }
            finally
            {
                w.Dispose();
            }
        }

        /// <summary>
        /// Serializes a given value with the specified buffer writer.
        /// </summary>
        /// <param name="writer">The buffer writer to serialize with.</param>
        /// <param name="value">The value to serialize.</param>
        /// <param name="options">The options. Use <c>null</c> to use default options.</param>
        /// <exception cref="TinyhandException">Thrown when any error occurs during serialization.</exception>
        public static void Serialize<T>(ref TinyhandWriter writer, T value, TinyhandSerializerOptions? options = null)
        {
            options = options ?? DefaultOptions;

            try
            {
                if (options.Compression != TinyhandCompression.None && !PrimitiveChecker<T>.IsTinyhandFixedSizePrimitive)
                {
                    if (initialBuffer2 == null)
                    {
                        initialBuffer2 = new byte[InitialBufferSize];
                    }

                    var w = writer.Clone(initialBuffer2);
                    try
                    {
                        options.Resolver.GetFormatter<T>().Serialize(ref w, value, options);
                        w.Flush();
                        ToLZ4BinaryCore(w.FlushAndGetReadOnlySequence(), ref writer, options.Compression);
                    }
                    finally
                    {
                        w.Dispose();
                    }
                }
                else
                {
                    options.Resolver.GetFormatter<T>().Serialize(ref writer, value, options);
                }
            }
            catch (Exception ex)
            {
                throw new TinyhandException($"Failed to serialize {typeof(T).FullName} value.", ex);
            }
        }

        /// <summary>
        /// Create a new instance of a given type.
        /// </summary>
        /// <typeparam name="T">The type of value to reconstruct.</typeparam>
        /// <param name="options">The options. Use <c>null</c> to use default options.</param>
        /// <returns>The created instance.</returns>
        /// <exception cref="TinyhandException">Thrown when any error occurs during reconstruction.</exception>
        public static T Reconstruct<T>(TinyhandSerializerOptions? options = null)
        {
            options = options ?? DefaultOptions;
            return options.Resolver.GetFormatter<T>().Reconstruct(options);
        }

        /// <summary>
        /// Deserializes a value of a given type from a sequence of bytes.
        /// </summary>
        /// <typeparam name="T">The type of value to deserialize.</typeparam>
        /// <param name="byteSequence">The sequence to deserialize from.</param>
        /// <param name="options">The options. Use <c>null</c> to use default options.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The deserialized value.</returns>
        /// <exception cref="TinyhandException">Thrown when any error occurs during deserialization.</exception>
        public static T? Deserialize<T>(in ReadOnlySequence<byte> byteSequence, TinyhandSerializerOptions? options = null, CancellationToken cancellationToken = default)
        {
            var reader = new TinyhandReader(byteSequence)
            {
                CancellationToken = cancellationToken,
            };

            return Deserialize<T>(ref reader, options);
        }

        /// <summary>
        /// Deserializes a value of a given type from a sequence of bytes.
        /// </summary>
        /// <typeparam name="T">The type of value to deserialize.</typeparam>
        /// <param name="buffer">The buffer to deserialize from.</param>
        /// <param name="options">The options. Use <c>null</c> to use default options.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The deserialized value.</returns>
        /// <exception cref="TinyhandException">Thrown when any error occurs during deserialization.</exception>
        public static T? Deserialize<T>(ReadOnlyMemory<byte> buffer, TinyhandSerializerOptions? options = null, CancellationToken cancellationToken = default)
        {
            var reader = new TinyhandReader(buffer)
            {
                CancellationToken = cancellationToken,
            };

            return Deserialize<T>(ref reader, options);
        }

        /// <summary>
        /// Deserializes a value of a given type from a sequence of bytes.
        /// </summary>
        /// <typeparam name="T">The type of value to deserialize.</typeparam>
        /// <param name="buffer">The buffer to deserialize from.</param>
        /// <param name="options">The options. Use <c>null</c> to use default options.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The deserialized value.</returns>
        /// <exception cref="TinyhandException">Thrown when any error occurs during deserialization.</exception>
        public static T? Deserialize<T>(byte[] buffer, TinyhandSerializerOptions? options = null, CancellationToken cancellationToken = default)
        {
            var reader = new TinyhandReader(buffer)
            {
                CancellationToken = cancellationToken,
            };

            return Deserialize<T>(ref reader, options);
        }

        /// <summary>
        /// Deserializes a value of a given type from a sequence of bytes.
        /// </summary>
        /// <typeparam name="T">The type of value to deserialize.</typeparam>
        /// <param name="buffer">The memory to deserialize from.</param>
        /// <param name="options">The options. Use <c>null</c> to use default options.</param>
        /// <param name="bytesRead">The number of bytes read.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The deserialized value.</returns>
        /// <exception cref="TinyhandException">Thrown when any error occurs during deserialization.</exception>
        public static T? Deserialize<T>(ReadOnlyMemory<byte> buffer, TinyhandSerializerOptions? options, out int bytesRead, CancellationToken cancellationToken = default)
        {
            var reader = new TinyhandReader(buffer)
            {
                CancellationToken = cancellationToken,
            };

            T result = Deserialize<T>(ref reader, options);
            bytesRead = buffer.Slice(0, (int)reader.Consumed).Length;
            return result;
        }

        /// <summary>
        /// Deserializes a value of a given type from a sequence of bytes.
        /// </summary>
        /// <typeparam name="T">The type of value to deserialize.</typeparam>
        /// <param name="reader">The reader to deserialize from.</param>
        /// <param name="options">The options. Use <c>null</c> to use default options.</param>
        /// <returns>The deserialized value.</returns>
        /// <exception cref="TinyhandException">Thrown when any error occurs during deserialization.</exception>
        public static T? Deserialize<T>(ref TinyhandReader reader, TinyhandSerializerOptions? options = null)
        {
            options = options ?? DefaultOptions;

            try
            {
                if (options.Compression != TinyhandCompression.None)
                {
                    var byteSequence = new ByteSequence();
                    try
                    {
                        if (TryDecompress(ref reader, byteSequence))
                        {
                            var r = reader.Clone(byteSequence.GetReadOnlySequence());
                            return options.Resolver.GetFormatter<T>().Deserialize(ref r, options);
                        }
                        else
                        {
                            return options.Resolver.GetFormatter<T>().Deserialize(ref reader, options);
                        }
                    }
                    finally
                    {
                        byteSequence.Dispose();
                    }
                }
                else
                {
                    return options.Resolver.GetFormatter<T>().Deserialize(ref reader, options);
                }
            }
            catch (Exception ex)
            {
                throw new TinyhandException($"Failed to deserialize {typeof(T).FullName} value.", ex);
            }
#if DEBUG
            finally
            {
                Debug.Assert(reader.Depth == 0, "reader.Depth should be 0.");
            }
#endif
        }

        /// <summary>
        /// Deserializes the entire content of a <see cref="Stream"/>.
        /// </summary>
        /// <typeparam name="T">The type of value to deserialize.</typeparam>
        /// <param name="stream">
        /// The stream to deserialize from.
        /// The entire stream will be read, and the first msgpack token deserialized will be returned.
        /// If <see cref="Stream.CanSeek"/> is true on the stream, its position will be set to just after the last deserialized byte.
        /// </param>
        /// <param name="options">The options. Use <c>null</c> to use default options.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The deserialized value.</returns>
        /// <exception cref="TinyhandException">Thrown when any error occurs during deserialization.</exception>
        /// <remarks>
        /// If multiple top-level msgpack data structures are expected on the stream, use <see cref="TinyhandReader"/> instead.
        /// </remarks>
        public static T? Deserialize<T>(Stream stream, TinyhandSerializerOptions? options = null, CancellationToken cancellationToken = default)
        {
            if (TryDeserializeFromMemoryStream(stream, options, cancellationToken, out T result))
            {
                return result;
            }

            var byteSequence = new ByteSequence();
            try
            {
                int bytesRead;
                do
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    Span<byte> span = byteSequence.GetSpan(stream.CanSeek ? (int)Math.Min(MaxHintSize, stream.Length - stream.Position) : 0);
                    bytesRead = stream.Read(span);
                    byteSequence.Advance(bytesRead);
                }
                while (bytesRead > 0);

                return DeserializeFromSequenceAndRewindStreamIfPossible<T>(stream, options, byteSequence.GetReadOnlySequence(), cancellationToken);
            }
            catch (Exception ex)
            {
                throw new TinyhandException("Error occurred while reading from the stream.", ex);
            }
            finally
            {
                byteSequence.Dispose();
            }
        }

        private static bool TryDeserializeFromMemoryStream<T>(Stream stream, TinyhandSerializerOptions? options, CancellationToken cancellationToken, out T? result)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (stream is MemoryStream ms && ms.TryGetBuffer(out ArraySegment<byte> streamBuffer))
            {
                result = Deserialize<T>(streamBuffer.AsMemory(checked((int)ms.Position)), options, out int bytesRead, cancellationToken);

                // Emulate that we had actually "read" from the stream.
                ms.Seek(bytesRead, SeekOrigin.Current);
                return true;
            }

            result = default;
            return false;
        }

        private static T? DeserializeFromSequenceAndRewindStreamIfPossible<T>(Stream streamToRewind, TinyhandSerializerOptions? options, ReadOnlySequence<byte> sequence, CancellationToken cancellationToken)
        {
            if (streamToRewind is null)
            {
                throw new ArgumentNullException(nameof(streamToRewind));
            }

            var reader = new TinyhandReader(sequence)
            {
                CancellationToken = cancellationToken,
            };

            var result = Deserialize<T>(ref reader, options);

            if (streamToRewind.CanSeek && !reader.End)
            {
                // Reverse the stream as many bytes as we left unread.
                int bytesNotRead = checked((int)reader.Sequence.Slice(reader.Position).Length);
                streamToRewind.Seek(-bytesNotRead, SeekOrigin.Current);
            }

            return result;
        }

        private static bool TryDecompress(ref TinyhandReader reader, IBufferWriter<byte> writer)
        {
            if (!reader.End)
            {
                // Try to find LZ4BlockArray
                if (reader.NextMessagePackType == MessagePackType.Array)
                {
                    var peekReader = reader.CreatePeekReader();
                    var arrayLength = peekReader.ReadArrayHeader();
                    if (arrayLength != 0 && peekReader.NextMessagePackType == MessagePackType.Extension)
                    {
                        ExtensionHeader header = peekReader.ReadExtensionFormatHeader();
                        if (header.TypeCode == MessagePack.MessagePackExtensionCodes.Lz4BlockArray)
                        {
                            // switch peekReader as original reader.
                            reader = peekReader;

                            // Read from [Ext(98:int,int...), bin,bin,bin...]
                            var sequenceCount = arrayLength - 1;
                            var uncompressedLengths = ArrayPool<int>.Shared.Rent(sequenceCount);
                            try
                            {
                                for (int i = 0; i < sequenceCount; i++)
                                {
                                    uncompressedLengths[i] = reader.ReadInt32();
                                }

                                for (int i = 0; i < sequenceCount; i++)
                                {
                                    var uncompressedLength = uncompressedLengths[i];
                                    var lz4Block = reader.ReadBytes();
                                    if (lz4Block == null)
                                    {
                                        continue;
                                    }

                                    var uncompressedSpan = writer.GetSpan(uncompressedLength).Slice(0, uncompressedLength);
                                    var actualUncompressedLength = LZ4Operation(lz4Block.Value, uncompressedSpan, LZ4Codec.Decode);
                                    Debug.Assert(actualUncompressedLength == uncompressedLength, "Unexpected length of uncompressed data.");
                                    writer.Advance(actualUncompressedLength);
                                }

                                return true;
                            }
                            finally
                            {
                                ArrayPool<int>.Shared.Return(uncompressedLengths);
                            }
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Performs LZ4 compression or decompression.
        /// </summary>
        /// <param name="input">The input for the operation.</param>
        /// <param name="output">The buffer to write the result of the operation.</param>
        /// <param name="lz4Operation">The LZ4 codec transformation.</param>
        /// <returns>The number of bytes written to the <paramref name="output"/>.</returns>
        private static int LZ4Operation(in ReadOnlySequence<byte> input, Span<byte> output, LZ4Transform lz4Operation)
        {
            ReadOnlySpan<byte> inputSpan;
            byte[]? rentedInputArray = null;
            if (input.IsSingleSegment)
            {
                inputSpan = input.First.Span;
            }
            else
            {
                rentedInputArray = ArrayPool<byte>.Shared.Rent((int)input.Length);
                input.CopyTo(rentedInputArray);
                inputSpan = rentedInputArray.AsSpan(0, (int)input.Length);
            }

            try
            {
                return lz4Operation(inputSpan, output);
            }
            finally
            {
                if (rentedInputArray != null)
                {
                    ArrayPool<byte>.Shared.Return(rentedInputArray);
                }
            }
        }

        private static void ToLZ4BinaryCore(in ReadOnlySequence<byte> msgpackUncompressedData, ref TinyhandWriter writer, TinyhandCompression compression)
        {
            if (compression == TinyhandCompression.Lz4)
            {
                // Write to [Ext(98:int,int...), bin,bin,bin...]
                var sequenceCount = 0;
                var extHeaderSize = 0;
                foreach (var item in msgpackUncompressedData)
                {
                    sequenceCount++;
                    extHeaderSize += GetUInt32WriteSize((uint)item.Length);
                }

                writer.WriteArrayHeader(sequenceCount + 1);
                writer.WriteExtensionFormatHeader(new ExtensionHeader(MessagePack.MessagePackExtensionCodes.Lz4BlockArray, extHeaderSize));
                foreach (var item in msgpackUncompressedData)
                {
                    writer.Write(item.Length);
                }

                foreach (var item in msgpackUncompressedData)
                {
                    var maxCompressedLength = MessagePack.LZ4.LZ4Codec.MaximumOutputLength(item.Length);
                    var lz4Span = writer.GetSpan(maxCompressedLength + 5);
                    int lz4Length = MessagePack.LZ4.LZ4Codec.Encode(item.Span, lz4Span.Slice(5, lz4Span.Length - 5));
                    WriteBin32Header((uint)lz4Length, lz4Span);
                    writer.Advance(lz4Length + 5);
                }
            }
            else
            {
                throw new ArgumentException("Invalid TinyhandCompression Code. Code:" + compression);
            }
        }

        private static int GetUInt32WriteSize(uint value)
        {
            if (value <= MessagePackRange.MaxFixPositiveInt)
            {
                return 1;
            }
            else if (value <= byte.MaxValue)
            {
                return 2;
            }
            else if (value <= ushort.MaxValue)
            {
                return 3;
            }
            else
            {
                return 5;
            }
        }

        private static void WriteBin32Header(uint value, Span<byte> span)
        {
            unchecked
            {
                span[0] = MessagePackCode.Bin32;

                // Write to highest index first so the JIT skips bounds checks on subsequent writes.
                span[4] = (byte)value;
                span[3] = (byte)(value >> 8);
                span[2] = (byte)(value >> 16);
                span[1] = (byte)(value >> 24);
            }
        }

        private static class PrimitiveChecker<T>
        {
            public static readonly bool IsTinyhandFixedSizePrimitive;

            static PrimitiveChecker()
            {
                IsTinyhandFixedSizePrimitive = IsTinyhandFixedSizePrimitiveTypeHelper(typeof(T));
            }
        }

        private static bool IsTinyhandFixedSizePrimitiveTypeHelper(Type type)
        {
            return type == typeof(short)
                || type == typeof(int)
                || type == typeof(long)
                || type == typeof(ushort)
                || type == typeof(uint)
                || type == typeof(ulong)
                || type == typeof(float)
                || type == typeof(double)
                || type == typeof(bool)
                || type == typeof(byte)
                || type == typeof(sbyte)
                || type == typeof(char);
        }
    }
}
