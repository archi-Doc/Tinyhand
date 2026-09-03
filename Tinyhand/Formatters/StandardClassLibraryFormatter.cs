// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Tinyhand.IO;

#pragma warning disable SA1009 // Closing parenthesis should be spaced correctly

namespace Tinyhand.Formatters;

internal sealed class TimeSpanFormatter : ITinyhandFormatter<TimeSpan>
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

    public void Deserialize(ref TinyhandReader reader, ref TimeSpan value, TinyhandSerializerOptions options)
    {
        value = new TimeSpan(reader.ReadInt64());
    }

    public TimeSpan Reconstruct(TinyhandSerializerOptions options)
    {
        return default;
    }

    public TimeSpan Clone(TimeSpan value, TinyhandSerializerOptions options) => value;
}

internal sealed class DateTimeOffsetFormatter : ITinyhandFormatter<DateTimeOffset>
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

    public void Deserialize(ref TinyhandReader reader, ref DateTimeOffset value, TinyhandSerializerOptions options)
    {
        var count = reader.ReadArrayHeader();
        if (count != 2)
        {
            throw new TinyhandException("Invalid DateTimeOffset format.");
        }

        DateTime utc = reader.ReadDateTime();
        var dtOffsetMinutes = reader.ReadInt16();

        value = new DateTimeOffset(utc.Ticks, TimeSpan.FromMinutes(dtOffsetMinutes));
    }

    public DateTimeOffset Reconstruct(TinyhandSerializerOptions options)
    {
        return default;
    }

    public DateTimeOffset Clone(DateTimeOffset value, TinyhandSerializerOptions options) => value;
}

internal sealed class UriFormatter : ITinyhandFormatter<Uri>
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

    public void Deserialize(ref TinyhandReader reader, ref Uri? value, TinyhandSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
        }
        else
        {
            value = new Uri(reader.ReadString() ?? string.Empty, UriKind.RelativeOrAbsolute);
        }
    }

    public Uri Reconstruct(TinyhandSerializerOptions options)
    {
        return new Uri("about:blank");
    }

    public Uri? Clone(Uri? value, TinyhandSerializerOptions options) => value == null ? null : new Uri(value.OriginalString);
}

internal sealed class VersionFormatter : ITinyhandFormatter<Version>
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

    public void Deserialize(ref TinyhandReader reader, ref Version? value, TinyhandSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
        }
        else
        {
            value = new Version(reader.ReadString() ?? string.Empty);
        }
    }

    public Version Reconstruct(TinyhandSerializerOptions options)
    {
        return new Version();
    }

    public Version? Clone(Version? value, TinyhandSerializerOptions options) => value == null ? null : new Version(value.ToString());
}

internal sealed class KeyValuePairFormatter<TKey, TValue> : ITinyhandFormatter<KeyValuePair<TKey, TValue>>
{
    public void Serialize(ref TinyhandWriter writer, KeyValuePair<TKey, TValue> value, TinyhandSerializerOptions options)
    {
        writer.WriteArrayHeader(2); // writer.WriteMapHeader(1);
        IFormatterResolver resolver = options.Resolver;
        resolver.GetFormatter<TKey>().Serialize(ref writer, value.Key, options);
        resolver.GetFormatter<TValue>().Serialize(ref writer, value.Value, options);
        return;
    }

    public void Deserialize(ref TinyhandReader reader, ref KeyValuePair<TKey, TValue> value, TinyhandSerializerOptions options)
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
            var key = resolver.GetFormatter<TKey>().Deserialize(ref reader, options);
            var v = resolver.GetFormatter<TValue>().Deserialize(ref reader, options);
            value = new KeyValuePair<TKey, TValue>(key!, v!);
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

    public KeyValuePair<TKey, TValue> Clone(KeyValuePair<TKey, TValue> value, TinyhandSerializerOptions options)
    {
        var resolver = options.Resolver;
        return new KeyValuePair<TKey, TValue>(resolver.GetFormatter<TKey>().Clone(value.Key, options)!, resolver.GetFormatter<TValue>().Clone(value.Value, options)!);
    }
}

internal sealed class KeyValueListFormatter<TKey, TValue> : ITinyhandFormatter<KeyValueList<TKey, TValue>>
{
    public void Serialize(ref TinyhandWriter writer, KeyValueList<TKey, TValue>? value, TinyhandSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNil();
        }
        else
        {
            var keyFormatter = options.Resolver.GetFormatter<TKey>();
            var valueFormatter = options.Resolver.GetFormatter<TValue>();

            var count = value.Count;
            writer.WriteMapHeader(count);
            for (var i = 0; i < count; i++)
            {
                keyFormatter.Serialize(ref writer, value[i].Key, options);
                valueFormatter.Serialize(ref writer, value[i].Value, options);
            }
        }

        return;
    }

    public void Deserialize(ref TinyhandReader reader, ref KeyValueList<TKey, TValue>? value, TinyhandSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
            return;
        }
        else
        {
            var keyFormatter = options.Resolver.GetFormatter<TKey>();
            var valueFormatter = options.Resolver.GetFormatter<TValue>();

            var count = reader.ReadMapHeader2();
            value ??= new KeyValueList<TKey, TValue>(count);
            options.Security.DepthStep(ref reader);
            try
            {
                for (int i = 0; i < count; i++)
                {
                    value.Add(new(keyFormatter.Deserialize(ref reader, options)!, valueFormatter.Deserialize(ref reader, options)!));
                }
            }
            finally
            {
                reader.Depth--;
            }
        }
    }

    public KeyValueList<TKey, TValue> Reconstruct(TinyhandSerializerOptions options)
    {
        return new();
    }

    public KeyValueList<TKey, TValue>? Clone(KeyValueList<TKey, TValue>? value, TinyhandSerializerOptions options)
    {
        if (value == null)
        {
            return default;
        }
        else
        {
            var keyFormatter = options.Resolver.GetFormatter<TKey>();
            var valueFormatter = options.Resolver.GetFormatter<TValue>();

            var len = value.Count;
            var list = new KeyValueList<TKey, TValue>(len);
            for (var i = 0; i < len; i++)
            {
                list.Add(new(keyFormatter.Clone(value[i].Key, options)!, valueFormatter.Clone(value[i].Value, options)!));
            }

            return list;
        }
    }
}

internal sealed class StringBuilderFormatter : ITinyhandFormatter<StringBuilder>
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

    public void Deserialize(ref TinyhandReader reader, ref StringBuilder? value, TinyhandSerializerOptions options)
    {
        if (!reader.TryReadNil())
        {
            value = new StringBuilder(reader.ReadString());
        }
    }

    public StringBuilder Reconstruct(TinyhandSerializerOptions options)
    {
        return new StringBuilder();
    }

    public StringBuilder? Clone(StringBuilder? value, TinyhandSerializerOptions options) => value == null ? null : new StringBuilder(value.ToString());
}

internal sealed class BitArrayFormatter : ITinyhandFormatter<BitArray>
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

    public void Deserialize(ref TinyhandReader reader, ref BitArray? value, TinyhandSerializerOptions options)
    {
        if (!reader.TryReadNil())
        {
            var len = reader.ReadArrayHeader();

            value = new BitArray(len);
            for (int i = 0; i < len; i++)
            {
                value[i] = reader.ReadBoolean();
            }
        }
    }

    public BitArray Reconstruct(TinyhandSerializerOptions options)
    {
        return new BitArray(0);
    }

    public BitArray? Clone(BitArray? value, TinyhandSerializerOptions options) => value == null ? null : new BitArray(value);
}

internal sealed class BigIntegerFormatter : ITinyhandFormatter<System.Numerics.BigInteger>
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

    public void Deserialize(ref TinyhandReader reader, ref System.Numerics.BigInteger value, TinyhandSerializerOptions options)
    {
        reader.TryReadBytes(out var span);
        value = new System.Numerics.BigInteger(span);
    }

    public System.Numerics.BigInteger Reconstruct(TinyhandSerializerOptions options)
    {
        return default;
    }

    public System.Numerics.BigInteger Clone(System.Numerics.BigInteger value, TinyhandSerializerOptions options) => value;
}

internal sealed class ComplexFormatter : ITinyhandFormatter<System.Numerics.Complex>
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

    public void Deserialize(ref TinyhandReader reader, ref System.Numerics.Complex value, TinyhandSerializerOptions options)
    {
        var count = reader.ReadArrayHeader();
        if (count != 2)
        {
            throw new TinyhandException("Invalid Complex format.");
        }

        var real = reader.ReadDouble();
        var imaginary = reader.ReadDouble();

        value = new System.Numerics.Complex(real, imaginary);
    }

    public System.Numerics.Complex Reconstruct(TinyhandSerializerOptions options)
    {
        return default;
    }

    public System.Numerics.Complex Clone(System.Numerics.Complex value, TinyhandSerializerOptions options) => value;
}

internal sealed class LazyFormatter<T> : ITinyhandFormatter<Lazy<T>>
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

    public void Deserialize(ref TinyhandReader reader, ref Lazy<T>? value, TinyhandSerializerOptions options)
    {
        if (!reader.TryReadNil())
        {
            options.Security.DepthStep(ref reader);
            try
            {
                // deserialize immediately(no delay, because capture byte[] causes memory leak)
                IFormatterResolver resolver = options.Resolver;
                var v = resolver.GetFormatter<T>().Deserialize(ref reader, options);
                if (v != null)
                {
                    value = new Lazy<T>(() => v);
                }
                else
                {
                    value = new Lazy<T>(() => resolver.GetFormatter<T>().Reconstruct(options));
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

    public Lazy<T>? Clone(Lazy<T>? value, TinyhandSerializerOptions options) => value == null ? null : new Lazy<T>(() => options.Resolver.GetFormatter<T>().Clone(value.Value, options)!);
}
