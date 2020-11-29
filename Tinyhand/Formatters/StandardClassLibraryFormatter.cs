// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Tinyhand.Internal;
using Tinyhand.IO;

#pragma warning disable SA1649 // File name should match first type name

namespace Tinyhand.Formatters
{
    public sealed class DecimalFormatter : ITinyhandFormatter<decimal>
    {
        public static readonly DecimalFormatter Instance = new DecimalFormatter();

        private DecimalFormatter()
        {
        }

        public void Serialize(ref TinyhandWriter writer, decimal value, TinyhandSerializerOptions options)
        {
            var dest = writer.GetSpan(MessagePackRange.MaxFixStringLength);
            if (System.Buffers.Text.Utf8Formatter.TryFormat(value, dest.Slice(1), out var written))
            {
                // write header
                dest[0] = (byte)(MessagePackCode.MinFixStr | written);
                writer.Advance(written + 1);
            }
            else
            {
                // reset writer's span previously acquired that does not use
                writer.Advance(0);
                writer.Write(value.ToString(CultureInfo.InvariantCulture));
            }
        }

        public decimal Deserialize(ref TinyhandReader reader, object? overwrite, TinyhandSerializerOptions options)
        {
            if (!(reader.ReadStringSequence() is ReadOnlySequence<byte> sequence))
            {
                throw new TinyhandException(string.Format("Unexpected msgpack code {0} ({1}) encountered.", MessagePackCode.Nil, MessagePackCode.ToFormatName(MessagePackCode.Nil)));
            }

            if (sequence.IsSingleSegment)
            {
                var span = sequence.First.Span;
                if (System.Buffers.Text.Utf8Parser.TryParse(span, out decimal result, out var bytesConsumed))
                {
                    if (span.Length != bytesConsumed)
                    {
                        throw new TinyhandException("Unexpected length of string.");
                    }

                    return result;
                }
            }
            else
            {
                // sequence.Length is not free
                var seqLen = (int)sequence.Length;
                if (seqLen < 128)
                {
                    Span<byte> span = stackalloc byte[seqLen];
                    sequence.CopyTo(span);
                    if (System.Buffers.Text.Utf8Parser.TryParse(span, out decimal result, out var bytesConsumed))
                    {
                        if (seqLen != bytesConsumed)
                        {
                            throw new TinyhandException("Unexpected length of string.");
                        }

                        return result;
                    }
                }
                else
                {
                    var rentArray = ArrayPool<byte>.Shared.Rent(seqLen);
                    try
                    {
                        sequence.CopyTo(rentArray);
                        if (System.Buffers.Text.Utf8Parser.TryParse(rentArray.AsSpan(0, seqLen), out decimal result, out var bytesConsumed))
                        {
                            if (seqLen != bytesConsumed)
                            {
                                throw new TinyhandException("Unexpected length of string.");
                            }

                            return result;
                        }
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(rentArray);
                    }
                }
            }

            throw new TinyhandException("Can't parse to decimal, input string was not in a correct format.");
        }

        public decimal Reconstruct(TinyhandSerializerOptions options)
        {
            return default;
        }
    }

    public sealed class TimeSpanFormatter : ITinyhandFormatter<TimeSpan>
    {
        public static readonly ITinyhandFormatter<TimeSpan> Instance = new TimeSpanFormatter();

        private TimeSpanFormatter()
        {
        }

        public void Serialize(ref TinyhandWriter writer, TimeSpan value, TinyhandSerializerOptions options)
        {
            writer.Write(value.Ticks);
            return;
        }

        public TimeSpan Deserialize(ref TinyhandReader reader, object? overwrite, TinyhandSerializerOptions options)
        {
            return new TimeSpan(reader.ReadInt64());
        }

        public TimeSpan Reconstruct(TinyhandSerializerOptions options)
        {
            return default;
        }
    }

    public sealed class DateTimeOffsetFormatter : ITinyhandFormatter<DateTimeOffset>
    {
        public static readonly ITinyhandFormatter<DateTimeOffset> Instance = new DateTimeOffsetFormatter();

        private DateTimeOffsetFormatter()
        {
        }

        public void Serialize(ref TinyhandWriter writer, DateTimeOffset value, TinyhandSerializerOptions options)
        {
            writer.WriteArrayHeader(2);
            writer.Write(new DateTime(value.Ticks, DateTimeKind.Utc)); // current ticks as is
            writer.Write((short)value.Offset.TotalMinutes); // offset is normalized in minutes
            return;
        }

        public DateTimeOffset Deserialize(ref TinyhandReader reader, object? overwrite, TinyhandSerializerOptions options)
        {
            var count = reader.ReadArrayHeader();

            if (count != 2)
            {
                throw new TinyhandException("Invalid DateTimeOffset format.");
            }

            DateTime utc = reader.ReadDateTime();

            var dtOffsetMinutes = reader.ReadInt16();

            return new DateTimeOffset(utc.Ticks, TimeSpan.FromMinutes(dtOffsetMinutes));
        }

        public DateTimeOffset Reconstruct(TinyhandSerializerOptions options)
        {
            return default;
        }
    }

    public sealed class GuidFormatter : ITinyhandFormatter<Guid>
    {
        public static readonly ITinyhandFormatter<Guid> Instance = new GuidFormatter();

        private GuidFormatter()
        {
        }

        public unsafe void Serialize(ref TinyhandWriter writer, Guid value, TinyhandSerializerOptions options)
        {
            byte* pBytes = stackalloc byte[36];
            Span<byte> bytes = new Span<byte>(pBytes, 36);
            new GuidBits(ref value).Write(bytes);
            writer.WriteString(bytes);
        }

        public Guid Deserialize(ref TinyhandReader reader, object? overwrite, TinyhandSerializerOptions options)
        {
            var seq = reader.ReadStringSequence();
            ReadOnlySequence<byte> segment = seq.HasValue ? seq.Value : default;
            if (segment.Length != 36)
            {
                throw new TinyhandException("Unexpected length of string.");
            }

            GuidBits result;
            if (segment.IsSingleSegment)
            {
                result = new GuidBits(segment.First.Span);
            }
            else
            {
                Span<byte> bytes = stackalloc byte[36];
                segment.CopyTo(bytes);
                result = new GuidBits(bytes);
            }

            return result.Value;
        }

        public Guid Reconstruct(TinyhandSerializerOptions options)
        {
            return default;
        }
    }

    public sealed class UriFormatter : ITinyhandFormatter<Uri>
    {
        public static readonly ITinyhandFormatter<Uri> Instance = new UriFormatter();

        private UriFormatter()
        {
        }

        public void Serialize(ref TinyhandWriter writer, Uri? value, TinyhandSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.Write(value.OriginalString);
            }
        }

        public Uri? Deserialize(ref TinyhandReader reader, object? overwrite, TinyhandSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }
            else
            {
                return new Uri(reader.ReadString(), UriKind.RelativeOrAbsolute);
            }
        }

        public Uri Reconstruct(TinyhandSerializerOptions options)
        {
            return new Uri("about:blank");
        }
    }

    public sealed class VersionFormatter : ITinyhandFormatter<Version>
    {
        public static readonly ITinyhandFormatter<Version> Instance = new VersionFormatter();

        private VersionFormatter()
        {
        }

        public void Serialize(ref TinyhandWriter writer, Version? value, TinyhandSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.Write(value.ToString());
            }
        }

        public Version? Deserialize(ref TinyhandReader reader, object? overwrite, TinyhandSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }
            else
            {
                return new Version(reader.ReadString());
            }
        }

        public Version Reconstruct(TinyhandSerializerOptions options)
        {
            return new Version();
        }
    }

    public sealed class KeyValuePairFormatter<TKey, TValue> : ITinyhandFormatter<KeyValuePair<TKey, TValue>>
    {
        public void Serialize(ref TinyhandWriter writer, KeyValuePair<TKey, TValue> value, TinyhandSerializerOptions options)
        {
            writer.WriteArrayHeader(2);
            IFormatterResolver resolver = options.Resolver;
            resolver.GetFormatter<TKey>().Serialize(ref writer, value.Key, options);
            resolver.GetFormatter<TValue>().Serialize(ref writer, value.Value, options);
            return;
        }

        public KeyValuePair<TKey, TValue> Deserialize(ref TinyhandReader reader, object? overwrite, TinyhandSerializerOptions options)
        {
            var count = reader.ReadArrayHeader();

            if (count != 2)
            {
                throw new TinyhandException("Invalid KeyValuePair format.");
            }

            IFormatterResolver resolver = options.Resolver;
            options.Security.DepthStep(ref reader);
            try
            {
                var key = resolver.GetFormatter<TKey>().Deserialize(ref reader, null, options);
                var value = resolver.GetFormatter<TValue>().Deserialize(ref reader, null, options);
                return new KeyValuePair<TKey, TValue>(key!, value!);
            }
            finally
            {
                reader.Depth--;
            }
        }

        public KeyValuePair<TKey, TValue> Reconstruct(TinyhandSerializerOptions options)
        {
            return default;
        }
    }

    public sealed class StringBuilderFormatter : ITinyhandFormatter<StringBuilder>
    {
        public static readonly ITinyhandFormatter<StringBuilder> Instance = new StringBuilderFormatter();

        private StringBuilderFormatter()
        {
        }

        public void Serialize(ref TinyhandWriter writer, StringBuilder? value, TinyhandSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.Write(value.ToString());
            }
        }

        public StringBuilder? Deserialize(ref TinyhandReader reader, object? overwrite, TinyhandSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }
            else
            {
                return new StringBuilder(reader.ReadString());
            }
        }

        public StringBuilder Reconstruct(TinyhandSerializerOptions options)
        {
            return new StringBuilder();
        }
    }

    public sealed class BitArrayFormatter : ITinyhandFormatter<BitArray>
    {
        public static readonly ITinyhandFormatter<BitArray> Instance = new BitArrayFormatter();

        private BitArrayFormatter()
        {
        }

        public void Serialize(ref TinyhandWriter writer, BitArray? value, TinyhandSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                var len = value.Length;
                writer.WriteArrayHeader(len);
                for (int i = 0; i < len; i++)
                {
                    writer.Write(value.Get(i));
                }

                return;
            }
        }

        public BitArray? Deserialize(ref TinyhandReader reader, object? overwrite, TinyhandSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }
            else
            {
                var len = reader.ReadArrayHeader();

                var array = new BitArray(len);
                for (int i = 0; i < len; i++)
                {
                    array[i] = reader.ReadBoolean();
                }

                return array;
            }
        }

        public BitArray Reconstruct(TinyhandSerializerOptions options)
        {
            return new BitArray(0);
        }
    }

    public sealed class BigIntegerFormatter : ITinyhandFormatter<System.Numerics.BigInteger>
    {
        public static readonly ITinyhandFormatter<System.Numerics.BigInteger> Instance = new BigIntegerFormatter();

        private BigIntegerFormatter()
        {
        }

        public void Serialize(ref TinyhandWriter writer, System.Numerics.BigInteger value, TinyhandSerializerOptions options)
        {
            writer.Write(value.ToByteArray());
            return;
        }

        public System.Numerics.BigInteger Deserialize(ref TinyhandReader reader, object? overwrite, TinyhandSerializerOptions options)
        {
            var seq = reader.ReadBytes();
            ReadOnlySequence<byte> bytes = seq.HasValue ? seq.Value : default;
            return new System.Numerics.BigInteger(bytes.ToArray());
        }

        public System.Numerics.BigInteger Reconstruct(TinyhandSerializerOptions options)
        {
            return default;
        }
    }

    public sealed class ComplexFormatter : ITinyhandFormatter<System.Numerics.Complex>
    {
        public static readonly ITinyhandFormatter<System.Numerics.Complex> Instance = new ComplexFormatter();

        private ComplexFormatter()
        {
        }

        public void Serialize(ref TinyhandWriter writer, System.Numerics.Complex value, TinyhandSerializerOptions options)
        {
            writer.WriteArrayHeader(2);
            writer.Write(value.Real);
            writer.Write(value.Imaginary);
            return;
        }

        public System.Numerics.Complex Deserialize(ref TinyhandReader reader, object? overwrite, TinyhandSerializerOptions options)
        {
            var count = reader.ReadArrayHeader();

            if (count != 2)
            {
                throw new TinyhandException("Invalid Complex format.");
            }

            var real = reader.ReadDouble();

            var imaginary = reader.ReadDouble();

            return new System.Numerics.Complex(real, imaginary);
        }

        public System.Numerics.Complex Reconstruct(TinyhandSerializerOptions options)
        {
            return default;
        }
    }

    public sealed class LazyFormatter<T> : ITinyhandFormatter<Lazy<T>>
    {
        public void Serialize(ref TinyhandWriter writer, Lazy<T>? value, TinyhandSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                IFormatterResolver resolver = options.Resolver;
                resolver.GetFormatter<T>().Serialize(ref writer, value.Value, options);
            }
        }

        public Lazy<T>? Deserialize(ref TinyhandReader reader, object? overwrite, TinyhandSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }
            else
            {
                options.Security.DepthStep(ref reader);
                try
                {
                    // deserialize immediately(no delay, because capture byte[] causes memory leak)
                    IFormatterResolver resolver = options.Resolver;
                    T v = resolver.GetFormatter<T>().Deserialize(ref reader, null, options);
                    if (v != null)
                    {
                        return new Lazy<T>(() => v);
                    }
                    else
                    {
                        return new Lazy<T>(() => resolver.GetFormatter<T>().Reconstruct(options));
                    }
                }
                finally
                {
                    reader.Depth--;
                }
            }
        }

        public Lazy<T> Reconstruct(TinyhandSerializerOptions options)
        {
            return new Lazy<T>(() => options.Resolver.GetFormatter<T>().Reconstruct(options));
        }
    }

    /// <summary>
    /// Serializes any instance of <see cref="Type"/> by its <see cref="Type.AssemblyQualifiedName"/> value.
    /// </summary>
    /// <typeparam name="T">The <see cref="Type"/> class itself or a derived type.</typeparam>
    public sealed class TypeFormatter<T> : ITinyhandFormatter<T>
        where T : Type
    {
        public static readonly ITinyhandFormatter<T> Instance = new TypeFormatter<T>();

        private TypeFormatter()
        {
        }

        public void Serialize(ref TinyhandWriter writer, T? value, TinyhandSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.Write(value.AssemblyQualifiedName);
            }
        }

        public T? Deserialize(ref TinyhandReader reader, object? overwrite, TinyhandSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            return (T)Type.GetType(reader.ReadString(), throwOnError: true);
        }

        public T Reconstruct(TinyhandSerializerOptions options)
        {
            return (T)typeof(object);
        }
    }
}
