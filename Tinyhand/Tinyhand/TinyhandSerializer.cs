﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Arc;
using Arc.Collections;
using Arc.IO;
using MessagePack.LZ4;
using Tinyhand.IO;

[module: System.Runtime.CompilerServices.SkipLocalsInit]

#pragma warning disable SA1618 // Generic type parameters should be documented

namespace Tinyhand;

public static partial class TinyhandSerializer
{
    #region Base

    public const int InitialBufferSize = 32 * 1024;
    private const int MaxHintSize = 1024 * 1024;

    /// <summary>
    /// A thread-local, recyclable array that may be used for short bursts of code.
    /// </summary>
    [ThreadStatic]
    private static byte[]? threadStaticBuffer;
    [ThreadStatic]
    private static byte[]? threadStaticBuffer2;

    /// <summary>
    /// Gets or sets <see cref="IServiceProvider"/> that is used to create an instance with  <see cref="TinyhandObjectAttribute.UseServiceProvider"/> set to true.
    /// </summary>
    public static IServiceProvider ServiceProvider
    {
        get
        {
            if (serviceProvider == null)
            {
                throw new TinyhandException("Set TinyhandSerializer.ServiceProvider before use.");
            }

            return serviceProvider;
        }

        set
        {
            serviceProvider = value;
        }
    }

    private static IServiceProvider? serviceProvider;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object GetService(Type type)
    {
        var instance = ServiceProvider.GetService(type);
        if (instance == null)
        {
            TinyhandHelper.ThrowNoServiceException(type);
        }

        return instance;
    }

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

    #endregion

    #region Object

    /// <summary>
    /// Calculates the XXHash3 hash value for the specified value.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value to calculate the hash for.</param>
    /// <returns>The XXHash3 hash value.</returns>
    public static ulong GetXxHash3<T>(in T? value)
        where T : ITinyhandSerializable<T>
    {
        var writer = TinyhandWriter.CreateFromThreadStaticBuffer();
        try
        {
            T.Serialize(ref writer, ref Unsafe.AsRef(in value), DefaultOptions);
            writer.FlushAndGetReadOnlySpan(out var span, out _);
            return Arc.Crypto.XxHash3.Hash64(span);
        }
        catch
        {
            return 0;
        }
        finally
        {
            writer.Dispose();
        }
    }

    public static byte[] SerializeObject<T>(in T? value)
        where T : ITinyhandSerializable<T>
    {
        var writer = TinyhandWriter.CreateFromThreadStaticBuffer();
        try
        {
            T.Serialize(ref writer, ref Unsafe.AsRef(in value), DefaultOptions);
            return writer.FlushAndGetArray();
        }
        catch (Exception ex)
        {
            throw new TinyhandException($"Failed to serialize {typeof(T).FullName} value.", ex);
        }
        finally
        {
            writer.Dispose();
        }
    }

    public static byte[] SerializeObject<T>(in T? value, TinyhandSerializerOptions? options)
        where T : ITinyhandSerializable<T>
    {
        options = options ?? DefaultOptions;
        var writer = TinyhandWriter.CreateFromThreadStaticBuffer();
        try
        {
            if (!options.HasLz4CompressFlag)
            {
                try
                {
                    T.Serialize(ref writer, ref Unsafe.AsRef(in value), options);
                }
                catch (Exception ex)
                {
                    throw new TinyhandException($"Failed to serialize {typeof(T).FullName} value.", ex);
                }
            }
            else
            {
                Serialize(ref writer, value, options);
            }

            return writer.FlushAndGetArray();
        }
        finally
        {
            writer.Dispose();
        }
    }

    public static void SerializeObject<T>(ref TinyhandWriter writer, in T? value, TinyhandSerializerOptions? options = null)
        where T : ITinyhandSerializable<T>
    {
        options = options ?? TinyhandSerializer.DefaultOptions;
        T.Serialize(ref writer, ref Unsafe.AsRef(in value), options);
    }

    public static BytePool.RentMemory SerializeObjectToRentMemory<T>(in T? value, TinyhandSerializerOptions? options = null)
        where T : ITinyhandSerializable<T>
    {
        options = options ?? TinyhandSerializer.DefaultOptions;
        var writer = TinyhandWriter.CreateFromBytePool();
        try
        {
            T.Serialize(ref writer, ref Unsafe.AsRef(in value), options);
            return writer.FlushAndGetRentMemory();
        }
        catch (Exception ex)
        {
            throw new TinyhandException($"Failed to serialize {typeof(T).FullName} value.", ex);
        }
        finally
        {
            writer.Dispose();
        }
    }

    public static void ReadStringConvertibleOrDeserializeObject<T>(ref TinyhandReader reader, scoped ref T? value, TinyhandSerializerOptions? options = null)
        where T : ITinyhandSerializable<T>, IStringConvertible<T>
    {
        var st = reader.TryReadString();
        if (st is not null)
        {
            T.TryParse(st, out value, out _);
        }
        else
        {
            DeserializeObject(ref reader, ref value, options);
        }
    }

    public static void ReadStringConvertibleOrDeserializeObject2<T>(ref TinyhandReader reader, scoped ref T value, TinyhandSerializerOptions? options = null)
        where T : ITinyhandSerializable<T>, ITinyhandReconstructable<T>, IStringConvertible<T>
    {
        var st = reader.TryReadString();
        if (st is not null)
        {
            T.TryParse(st, out value!, out _);
        }
        else
        {
            DeserializeObject(ref reader, ref value!, options);
        }

        if (value is null)
        {
            ReconstructObject<T>(ref value, options);
        }
    }

    public static T? DeserializeObject<T>(ReadOnlySpan<byte> data)
        where T : ITinyhandSerializable<T>
    {
        var reader = new TinyhandReader(data);

        try
        {
            var value = default(T);
            T.Deserialize(ref reader, ref value, DefaultOptions);
            return value;
        }
        catch (Exception ex)
        {
            throw new TinyhandException($"Failed to deserialize {typeof(T).FullName} value.", ex);
        }
    }

    public static void DeserializeObject<T>(ReadOnlySpan<byte> data, ref T? value)
        where T : ITinyhandSerializable<T>
    {
        var reader = new TinyhandReader(data);

        try
        {
            T.Deserialize(ref reader, ref value, DefaultOptions);
        }
        catch (Exception ex)
        {
            throw new TinyhandException($"Failed to deserialize {typeof(T).FullName} value.", ex);
        }
    }

    public static void DeserializeObject<T>(ref TinyhandReader reader, scoped ref T? value, TinyhandSerializerOptions? options = null)
        where T : ITinyhandSerializable<T>
    {
        options = options ?? DefaultOptions;
        T.Deserialize(ref reader, ref value, options);
    }

    public static void DeserializeAndReconstructObject<T>(ref TinyhandReader reader, [NotNull] scoped ref T? value, TinyhandSerializerOptions? options = null)
        where T : ITinyhandSerializable<T>, ITinyhandReconstructable<T>
    {
        options = options ?? DefaultOptions;
        T.Deserialize(ref reader, ref value, options);
        if (value is null)
        {
            T.Reconstruct(ref value, options);
            // value = options.Resolver.GetFormatter<T>().Reconstruct(options);
        }
    }

    public static T? DeserializeObject<T>(ref TinyhandReader reader, TinyhandSerializerOptions? options = null)
        where T : ITinyhandSerializable<T>
    {
        options = options ?? DefaultOptions;
        var value = default(T);
        T.Deserialize(ref reader, ref value, options);
        return value;
    }

    public static T DeserializeAndReconstructObject<T>(ref TinyhandReader reader, TinyhandSerializerOptions? options = null)
        where T : ITinyhandSerializable<T>, ITinyhandReconstructable<T>
    {
        options = options ?? DefaultOptions;
        var value = default(T);
        T.Deserialize(ref reader, ref value, options);
        if (value == null)
        {
            T.Reconstruct(ref value, options);
        }

        return value;
        // return value ?? options.Resolver.GetFormatter<T>().Reconstruct(options);
    }

    public static bool TryDeserializeObject<T>(ReadOnlySpan<byte> data, [MaybeNullWhen(false)] out T value)
        where T : ITinyhandSerializable<T>
    {
        var reader = new TinyhandReader(data);
        try
        {
            value = default;
            T.Deserialize(ref reader, ref value, DefaultOptions);
            return value is not null;
        }
        catch
        {
            value = default;
            return false;
        }
    }

    public static bool TryDeserializeObject<T>(ref TinyhandReader reader, scoped ref T? value, TinyhandSerializerOptions? options = null)
        where T : ITinyhandSerializable<T>
    {
        options = options ?? DefaultOptions;
        try
        {
            T.Deserialize(ref reader, ref value, DefaultOptions);
            return value is not null;
        }
        catch
        {
            value = default;
            return false;
        }
    }

    /* /// <summary>
    /// Gets the type identifier (FarmHash.Hash64(Type.FullName)).
    /// </summary>
    /// <typeparam name="T">The type to get the identifier for.</typeparam>
    /// <returns>The type identifier.</returns>
    public static ulong GetTypeIdentifierObject<T>()
        where T : ITinyhandSerializable<T>
        => T.GetTypeIdentifier(); // GetTypeIdentifierCode */

    /// <summary>
    /// Creates a new object and sets valid values to the object members.
    /// </summary>
    /// <param name="obj">The object to reconstruct.</param>
    /// <param name="options">The options. Set <see langword="null"/> to use default options.</param>
    public static void ReconstructObject<T>([NotNull] scoped ref T? obj, TinyhandSerializerOptions? options = null)
        where T : ITinyhandReconstructable<T>
    {
        options = options ?? DefaultOptions;
        T.Reconstruct(ref obj, options);
    }

    public static T ReconstructObject<T>(TinyhandSerializerOptions? options = null)
        where T : ITinyhandReconstructable<T>
    {
        var obj = default(T);
        options = options ?? DefaultOptions;
        T.Reconstruct(ref obj, options);
        return obj;
    }

    /// <summary>
    /// Creates a deep copy of the object.
    /// </summary>
    /// <param name="obj">The object to clone.</param>
    /// <param name="options">The options. Set <see langword="null"/> to use default options.</param>
    /// <returns>The new object.</returns>
    /// <exception cref="TinyhandException">Thrown when any error occurs during serialization.</exception>
    [return: NotNullIfNotNull(nameof(obj))]
    public static T? CloneObject<T>(in T? obj, TinyhandSerializerOptions? options = null)
        where T : ITinyhandCloneable<T>
    {
        options = options ?? DefaultOptions;
        return T.Clone(ref Unsafe.AsRef(in obj), options);
    }

    #endregion

    /// <summary>
    /// Create a new instance of the given type.
    /// </summary>
    /// <typeparam name="T">The type of value to reconstruct.</typeparam>
    /// <param name="options">The options. Set <see langword="null"/> to use default options.</param>
    /// <returns>The created instance.</returns>
    /// <exception cref="TinyhandException">Thrown when any error occurs during reconstruction.</exception>
    public static T Reconstruct<T>(TinyhandSerializerOptions? options = null)
    {
        options = options ?? DefaultOptions;
        return options.Resolver.GetFormatter<T>().Reconstruct(options);
    }

    /// <summary>
    /// Creates a deep copy of the object.
    /// </summary>
    /// <param name="obj">The object to clone.</param>
    /// <param name="options">The options. Set <see langword="null"/> to use default options.</param>
    /// <returns>The new object.</returns>
    /// <exception cref="TinyhandException">Thrown when any error occurs during serialization.</exception>
    [return: NotNullIfNotNull(nameof(obj))]
    public static T? Clone<T>(T? obj, TinyhandSerializerOptions? options = null)
    {
        options = options ?? DefaultOptions;
        return options.Resolver.GetFormatter<T>().Clone(obj, options);
    }

    /// <summary>
    /// Serializes a given value with the specified buffer writer.
    /// </summary>
    /// <param name="writer">The buffer writer to serialize with.</param>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">The options. Set <see langword="null"/> to use default options.</param>
    /// <exception cref="TinyhandException">Thrown when any error occurs during serialization.</exception>
    public static void Serialize<T>(IBufferWriter<byte> writer, T value, TinyhandSerializerOptions? options = null)
    {
        var w = new TinyhandWriter(writer);
        try
        {
            options = options ?? DefaultOptions;
            if (!options.HasLz4CompressFlag)
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
    /// Serializes a given value to a byte array.
    /// </summary>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">The options. Set <see langword="null"/> to use default options.</param>
    /// <returns>A byte array with the serialized value.</returns>
    /// <exception cref="TinyhandException">Thrown when any error occurs during serialization.</exception>
    public static byte[] Serialize<T>(T value, TinyhandSerializerOptions? options = null)
    {
        options = options ?? DefaultOptions;
        var writer = TinyhandWriter.CreateFromThreadStaticBuffer();
        try
        {
            if (!options.HasLz4CompressFlag)
            {
                try
                {
                    options.Resolver.GetFormatter<T>().Serialize(ref writer, value, options);
                }
                catch (Exception ex)
                {
                    throw new TinyhandException($"Failed to serialize {typeof(T).FullName} value.", ex);
                }
            }
            else
            {
                Serialize(ref writer, value, options);
            }

            return writer.FlushAndGetArray();
        }
        finally
        {
            writer.Dispose();
        }
    }

    /// <summary>
    /// Serializes a given value to a <see cref="BytePool.RentMemory"/>.
    /// </summary>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">The options. Set <see langword="null"/> to use default options.</param>
    /// <returns>A byte array with the serialized value.</returns>
    /// <exception cref="TinyhandException">Thrown when any error occurs during serialization.</exception>
    public static BytePool.RentMemory SerializeToRentMemory<T>(T value, TinyhandSerializerOptions? options = null)
    {
        options = options ?? DefaultOptions;
        var writer = TinyhandWriter.CreateFromBytePool();
        try
        {
            if (!options.HasLz4CompressFlag)
            {
                try
                {
                    options.Resolver.GetFormatter<T>().Serialize(ref writer, value, options);
                }
                catch (Exception ex)
                {
                    throw new TinyhandException($"Failed to serialize {typeof(T).FullName} value.", ex);
                }
            }
            else
            {
                Serialize(ref writer, value, options);
            }

            return writer.FlushAndGetRentMemory();
        }
        finally
        {
            writer.Dispose();
        }
    }

    /// <summary>
    /// Serializes a given value in signature mode.
    /// </summary>
    /// <param name="value">The value to serialize.</param>
    /// <param name="level">The level for serialization (members with this level or lower will be serialized).</param>
    /// <returns>A byte array with the serialized value.</returns>
    /// <exception cref="TinyhandException">Thrown when any error occurs during serialization.</exception>
    public static byte[] SerializeSignature<T>(T value, int level)
    {
        var writer = TinyhandWriter.CreateFromThreadStaticBuffer();
        writer.Level = level;
        try
        {
            var options = TinyhandSerializerOptions.Signature;
            try
            {
                options.Resolver.GetFormatter<T>().Serialize(ref writer, value, options);
            }
            catch (Exception ex)
            {
                throw new TinyhandException($"Failed to serialize {typeof(T).FullName} value.", ex);
            }

            return writer.FlushAndGetArray();
        }
        finally
        {
            writer.Dispose();
        }
    }

    /// <summary>
    /// Serializes a given value to the specified stream.
    /// </summary>
    /// <param name="stream">The stream to serialize to.</param>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">The options. Use <c>null</c> to use default options.</param>
    /// <exception cref="TinyhandException">Thrown when any error occurs during serialization.</exception>
    public static void Serialize<T>(Stream stream, T value, TinyhandSerializerOptions? options = null)
    {
        var writer = TinyhandWriter.CreateFromThreadStaticBuffer();
        try
        {
            Serialize(ref writer, value, options);

            try
            {
                foreach (var segment in writer.FlushAndGetReadOnlySequence())
                {
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
            writer.Dispose();
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
            if (options.HasLz4CompressFlag && !PrimitiveChecker<T>.IsTinyhandFixedSizePrimitive)
            {
                var w = writer.Clone(GetThreadStaticBuffer2());
                try
                {
                    options.Resolver.GetFormatter<T>().Serialize(ref w, value, options);
                    ToLZ4BinaryCore(w.FlushAndGetReadOnlySequence(), ref writer);
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
    /// Serializes a given value to the specified stream.
    /// </summary>
    /// <param name="stream">The stream to serialize to.</param>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">The options. Use <c>null</c> to use default options.</param>
    /// <returns>A task that completes with the result of the async serialization operation.</returns>
    /// <exception cref="TinyhandException">Thrown when any error occurs during serialization.</exception>
    public static async Task SerializeAsync<T>(Stream stream, T value, TinyhandSerializerOptions? options = null)
    {
        var byteSequence = new ByteSequence();
        try
        {
            Serialize<T>(byteSequence, value, options);

            try
            {
                foreach (var segment in byteSequence.ToReadOnlySequence())
                {
                    await stream.WriteAsync(segment).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                throw new TinyhandException("Error occurred while writing the serialized data to the stream.", ex);
            }
        }
        finally
        {
            byteSequence.Dispose();
        }
    }

    /// <summary>
    /// Deserializes a value of a given type from a sequence of bytes.
    /// </summary>
    /// <typeparam name="T">The type of value to deserialize.</typeparam>
    /// <param name="buffer">The buffer to deserialize from.</param>
    /// <param name="options">The options. Use <c>null</c> to use default options.</param>
    /// <returns>The deserialized value.</returns>
    /// <exception cref="TinyhandException">Thrown when any error occurs during deserialization.</exception>
    public static T? Deserialize<T>(ReadOnlySpan<byte> buffer, TinyhandSerializerOptions? options = null)
    {
        var reader = new TinyhandReader(buffer);
        return Deserialize<T>(ref reader, options);
    }

    /// <summary>
    /// Attempts to deserialize a value of a given type from a sequence of bytes.
    /// </summary>
    /// <typeparam name="T">The type of value to deserialize.</typeparam>
    /// <param name="buffer">The buffer to deserialize from.</param>
    /// <param name="value">.</param>
    /// <param name="options">The options. Use <c>null</c> to use default options.</param>
    /// <returns><see langword="true"/> if the deserialization is successfully done; otherwise, <see langword="false"/>.</returns>
    public static bool TryDeserialize<T>(ReadOnlySpan<byte> buffer, [MaybeNullWhen(false)] out T value, TinyhandSerializerOptions? options = null)
    {
        try
        {
            value = Deserialize<T>(buffer, options);
            return value != null;
        }
        catch
        {
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Deserializes a value of a given type from a sequence of bytes.
    /// </summary>
    /// <typeparam name="T">The type of value to deserialize.</typeparam>
    /// <param name="buffer">The buffer to deserialize from.</param>
    /// <returns>The deserialized value.</returns>
    /// <exception cref="TinyhandException">Thrown when any error occurs during deserialization.</exception>
    public static T? Deserialize<T>(ReadOnlySpan<byte> buffer)
    {
        var reader = new TinyhandReader(buffer);

        try
        {
            return DefaultOptions.Resolver.GetFormatter<T>().Deserialize(ref reader, DefaultOptions);
        }
        catch (Exception ex)
        {
            throw new TinyhandException($"Failed to deserialize {typeof(T).FullName} value.", ex);
        }
    }

    /// <summary>
    /// Deserializes a value of a given type from a sequence of bytes.
    /// </summary>
    /// <typeparam name="T">The type of value to deserialize.</typeparam>
    /// <param name="buffer">The memory to deserialize from.</param>
    /// <param name="bytesRead">The number of bytes read.</param>
    /// <param name="options">The options. Use <c>null</c> to use default options.</param>
    /// <returns>The deserialized value.</returns>
    /// <exception cref="TinyhandException">Thrown when any error occurs during deserialization.</exception>
    public static T? Deserialize<T>(ReadOnlySpan<byte> buffer, out int bytesRead, TinyhandSerializerOptions? options)
    {
        var reader = new TinyhandReader(buffer);
        var result = Deserialize<T>(ref reader, options);
        bytesRead = (int)reader.Consumed; // buffer.Slice(0, (int)reader.Consumed).Length;
        return result;
    }

    /// <summary>
    /// Attempts to deserialize a value of a given type from a sequence of bytes.
    /// </summary>
    /// <typeparam name="T">The type of value to deserialize.</typeparam>
    /// <param name="buffer">The buffer to deserialize from.</param>
    /// <param name="value">.</param>
    /// <param name="bytesRead">The number of bytes read.</param>
    /// <param name="options">The options. Use <c>null</c> to use default options.</param>
    /// <returns><see langword="true"/> if the deserialization is successfully done; otherwise, <see langword="false"/>.</returns>
    public static bool TryDeserialize<T>(ReadOnlySpan<byte> buffer, [MaybeNullWhen(false)] out T value, out int bytesRead, TinyhandSerializerOptions? options = null)
    {
        try
        {
            value = Deserialize<T>(buffer, out bytesRead, options);
            return value != null;
        }
        catch
        {
        }

        value = default;
        bytesRead = 0;
        return false;
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
            if (options.HasLz4CompressFlag)
            {
                var byteSequence = new ByteSequence();
                try
                {
                    if (TryDecompress(ref reader, byteSequence))
                    {
                        var r = reader.Clone(byteSequence.ToReadOnlySpan());
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
    /// <returns>The deserialized value.</returns>
    /// <exception cref="TinyhandException">Thrown when any error occurs during deserialization.</exception>
    public static T? Deserialize<T>(Stream stream, TinyhandSerializerOptions? options = null)
    {
        if (TryDeserializeFromMemoryStream(stream, options, out T? result))
        {
            return result;
        }

        var byteSequence = new ByteSequence();
        try
        {
            int bytesRead;
            do
            {
                var span = byteSequence.GetSpan(stream.CanSeek ? (int)Math.Min(MaxHintSize, stream.Length - stream.Position) : 0);
                bytesRead = stream.Read(span);
                byteSequence.Advance(bytesRead);
            }
            while (bytesRead > 0);

            return DeserializeFromSequenceAndRewindStreamIfPossible<T>(stream, options, byteSequence);
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
    /// <returns>The deserialized value.</returns>
    /// <exception cref="TinyhandException">Thrown when any error occurs during deserialization.</exception>
    public static async ValueTask<T?> DeserializeAsync<T>(Stream stream, TinyhandSerializerOptions? options = null)
    {
        if (TryDeserializeFromMemoryStream(stream, options, out T? result))
        {
            return result;
        }

        var byteSequence = new ByteSequence();
        try
        {
            int bytesRead;
            do
            {
                var memory = byteSequence.GetMemory(stream.CanSeek ? (int)Math.Min(MaxHintSize, stream.Length - stream.Position) : 0);
                bytesRead = await stream.ReadAsync(memory).ConfigureAwait(false);
                byteSequence.Advance(bytesRead);
            }
            while (bytesRead > 0);

            return DeserializeFromSequenceAndRewindStreamIfPossible<T>(stream, options, byteSequence);
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

    internal static bool TryDecompress(ref TinyhandReader reader, IBufferWriter<byte> writer)
    {
        if (reader.End)
        {
            return false;
        }

        if (reader.NextMessagePackType != MessagePackType.Array)
        {
            return false;
        }

        var peekReader = reader.Fork();
        var arrayLength = peekReader.ReadArrayHeader();
        if (arrayLength != 0 && peekReader.NextMessagePackType == MessagePackType.Extension)
        {
            ExtensionHeader header = peekReader.ReadExtensionFormatHeader();
            if (header.TypeCode == Tinyhand.MessagePackExtensionCodes.Lz4BlockArray)
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
                        reader.TryReadBytes(out var span);

                        var uncompressedSpan = writer.GetSpan(uncompressedLength).Slice(0, uncompressedLength);
                        var actualUncompressedLength = LZ4Codec.Decode(span, uncompressedSpan);
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

        return false;
    }

    internal static void ToLZ4BinaryCore(scoped in ReadOnlySequence<byte> msgpackUncompressedData, ref TinyhandWriter writer)
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
        writer.WriteExtensionFormatHeader(new ExtensionHeader(Tinyhand.MessagePackExtensionCodes.Lz4BlockArray, extHeaderSize));
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

    /// <summary>
    /// Gets a thread-static buffer with a size of <see cref="InitialBufferSize"/>.
    /// </summary>
    /// <returns>A byte array that can be used as a buffer.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static byte[] GetThreadStaticBuffer()
        => threadStaticBuffer ??= new byte[InitialBufferSize];

    /// <summary>
    /// Gets a thread-static buffer2 with a size of <see cref="InitialBufferSize"/>.
    /// </summary>
    /// <returns>A byte array that can be used as a buffer.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static byte[] GetThreadStaticBuffer2()
        => threadStaticBuffer2 ??= new byte[InitialBufferSize];

    private static bool TryDeserializeFromMemoryStream<T>(Stream stream, TinyhandSerializerOptions? options, out T? result)
    {
        if (stream is MemoryStream ms && ms.TryGetBuffer(out ArraySegment<byte> streamBuffer))
        {
            result = Deserialize<T>(streamBuffer.AsSpan(checked((int)ms.Position)), out int bytesRead, options);

            // Emulate that we had actually "read" from the stream.
            ms.Seek(bytesRead, SeekOrigin.Current);
            return true;
        }

        result = default;
        return false;
    }

    private static T? DeserializeFromSequenceAndRewindStreamIfPossible<T>(Stream streamToRewind, TinyhandSerializerOptions? options, ByteSequence sequence)
    {
        if (streamToRewind is null)
        {
            throw new ArgumentNullException(nameof(streamToRewind));
        }

        var reader = new TinyhandReader(sequence.ToReadOnlySpan());
        var result = Deserialize<T>(ref reader, options);

        if (streamToRewind.CanSeek && !reader.End)
        {
            // Reverse the stream as many bytes as we left unread.
            int bytesNotRead = reader.Remaining;
            streamToRewind.Seek(-bytesNotRead, SeekOrigin.Current);
        }

        return result;
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
