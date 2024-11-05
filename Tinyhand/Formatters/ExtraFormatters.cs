// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Net;
using Arc.Collections;
using Tinyhand.IO;

namespace Tinyhand.Formatters;

/// <summary>
/// Struct128 formatter.
/// </summary>
public sealed class Struct128Formatter : ITinyhandFormatter<Struct128>
{
    public static readonly Struct128Formatter Instance = new();

    public static Struct128 DeserializeValue(ref TinyhandReader reader, TinyhandSerializerOptions options)
    {
        if (!reader.TryReadBytes(out var span) ||
            span.Length < Struct128.Length)
        {
            return default;
        }

        return new(span);
    }

    public void Serialize(ref TinyhandWriter writer, Struct128 value, TinyhandSerializerOptions options)
        => writer.Write(value.AsSpan());

    public Struct128 Deserialize(ref TinyhandReader reader, TinyhandSerializerOptions options)
        => DeserializeValue(ref reader, options);

    public Struct128 Reconstruct(TinyhandSerializerOptions options)
        => default;

    public Struct128 Clone(Struct128 value, TinyhandSerializerOptions options)
        => value;
}

/// <summary>
/// Struct256 formatter.
/// </summary>
public sealed class Struct256Formatter : ITinyhandFormatter<Struct256>
{
    public static readonly Struct256Formatter Instance = new();

    public static Struct256 DeserializeValue(ref TinyhandReader reader, TinyhandSerializerOptions options)
    {
        if (!reader.TryReadBytes(out var span) ||
            span.Length < Struct256.Length)
        {
            return default;
        }

        return new(span);
    }

    public void Serialize(ref TinyhandWriter writer, Struct256 value, TinyhandSerializerOptions options)
        => writer.Write(value.AsSpan());

    public Struct256 Deserialize(ref TinyhandReader reader, TinyhandSerializerOptions options)
        => DeserializeValue(ref reader, options);

    public Struct256 Reconstruct(TinyhandSerializerOptions options)
        => default;

    public Struct256 Clone(Struct256 value, TinyhandSerializerOptions options)
        => value;
}

/// <summary>
/// BytePool.RentMemory formatter.
/// </summary>
public sealed class RentMemoryFormatter : ITinyhandFormatter<BytePool.RentMemory>
{
    public static readonly RentMemoryFormatter Instance = new();

    private RentMemoryFormatter()
    {
    }

    public void Serialize(ref TinyhandWriter writer, BytePool.RentMemory value, TinyhandSerializerOptions options)
    {
        writer.Write(value.Span);
    }

    public BytePool.RentMemory Deserialize(ref TinyhandReader reader, TinyhandSerializerOptions options)
    {
        return reader.ReadBytesToRentMemory();
    }

    public BytePool.RentMemory Reconstruct(TinyhandSerializerOptions options)
    {
        return default;
    }

    public BytePool.RentMemory Clone(BytePool.RentMemory value, TinyhandSerializerOptions options)
    {
        var rentMemory = BytePool.Default.Rent(value.Length).AsMemory();
        value.Span.CopyTo(rentMemory.Span);
        return rentMemory;
    }
}

/// <summary>
/// BytePool.RentReadOnlyMemory formatter.
/// </summary>
public sealed class RentReadOnlyMemoryFormatter : ITinyhandFormatter<BytePool.RentReadOnlyMemory>
{
    public static readonly RentReadOnlyMemoryFormatter Instance = new();

    private RentReadOnlyMemoryFormatter()
    {
    }

    public void Serialize(ref TinyhandWriter writer, BytePool.RentReadOnlyMemory value, TinyhandSerializerOptions options)
    {
        writer.Write(value.Span);
    }

    public BytePool.RentReadOnlyMemory Deserialize(ref TinyhandReader reader, TinyhandSerializerOptions options)
    {
        return reader.ReadBytesToRentMemory().ReadOnly;
    }

    public BytePool.RentReadOnlyMemory Reconstruct(TinyhandSerializerOptions options)
    {
        return default;
    }

    public BytePool.RentReadOnlyMemory Clone(BytePool.RentReadOnlyMemory value, TinyhandSerializerOptions options)
    {
        var rentMemory = BytePool.Default.Rent(value.Length).AsMemory();
        value.Span.CopyTo(rentMemory.Span);
        return rentMemory.ReadOnly;
    }
}

/// <summary>
/// Serialize IPAddress.
/// </summary>
public sealed class IPAddressFormatter : ITinyhandFormatter<IPAddress>
{
    public static readonly IPAddressFormatter Instance = new IPAddressFormatter();

    public void Serialize(ref TinyhandWriter writer, IPAddress? value, TinyhandSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNil();
            return;
        }

        var span = writer.GetSpan(32);
        if (value.TryWriteBytes(span.Slice(2), out var written))
        {
            span[0] = MessagePackCode.Bin8;
            span[1] = (byte)written;
            writer.Advance(2 + written);
        }
        else
        {
            writer.WriteNil();
            return;
        }
    }

    public IPAddress? Deserialize(ref TinyhandReader reader, TinyhandSerializerOptions options)
    {
        if (!reader.TryReadBytes(out var span))
        {
            return null;
        }

        return new IPAddress(span);
    }

    public IPAddress Reconstruct(TinyhandSerializerOptions options)
    {
        return IPAddress.None;
    }

    public IPAddress? Clone(IPAddress? value, TinyhandSerializerOptions options) => value == null ? null : new IPAddress(value.GetAddressBytes());
}

/// <summary>
/// Serialize IPEndPoint.
/// </summary>
public sealed class IPEndPointFormatter : ITinyhandFormatter<IPEndPoint>
{
    public static readonly IPEndPointFormatter Instance = new IPEndPointFormatter();

    public void Serialize(ref TinyhandWriter writer, IPEndPoint? value, TinyhandSerializerOptions options)
    {// Nil or Bin8(Address, Port(4))
        if (value == null)
        {
            writer.WriteNil();
            return;
        }

        var span = writer.GetSpan(32);
        if (value.Address.TryWriteBytes(span.Slice(2), out var written))
        {
            span[0] = MessagePackCode.Bin8;
            span[1] = (byte)(written + 4); // Address + Port(4)
            BitConverter.TryWriteBytes(span.Slice(2 + written), value.Port);
            writer.Advance(2 + written + 4);
        }
        else
        {
            writer.WriteNil();
            return;
        }
    }

    public IPEndPoint? Deserialize(ref TinyhandReader reader, TinyhandSerializerOptions options)
    {
        if (!reader.TryReadBytes(out var span) ||
            span.Length < 4)
        {
            return null;
        }

        var port = BitConverter.ToInt32(span.Slice(span.Length - 4));
        return new IPEndPoint(new IPAddress(span.Slice(0, span.Length - 4)), port);
    }

    public IPEndPoint Reconstruct(TinyhandSerializerOptions options)
    {
        return new IPEndPoint(IPAddress.None, 0);
    }

    public IPEndPoint? Clone(IPEndPoint? value, TinyhandSerializerOptions options) => value == null ? null : new IPEndPoint(new IPAddress(value.Address.GetAddressBytes()), value.Port);
}

/*public sealed class NativeDateTimeArrayFormatter : ITinyhandFormatter<DateTime[]>
{
    public static readonly NativeDateTimeArrayFormatter Instance = new NativeDateTimeArrayFormatter();

    public void Serialize(ref TinyhandWriter writer, DateTime[]? value, TinyhandSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNil();
        }
        else
        {
            writer.WriteArrayHeader(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                writer.Write(value[i].ToBinary());
            }
        }
    }

    public DateTime[]? Deserialize(ref TinyhandReader reader, TinyhandSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
            return null;
        }

        var len = reader.ReadArrayHeader();
        if (len == 0)
        {
            return Array.Empty<DateTime>();
        }

        var array = new DateTime[len];
        for (int i = 0; i < array.Length; i++)
        {
            var dateData = reader.ReadInt64();
            array[i] = DateTime.FromBinary(dateData);
        }

        return array;
    }

    public DateTime[] Reconstruct(TinyhandSerializerOptions options)
    {
        return new DateTime[0];
    }

    public DateTime[]? Clone(DateTime[]? value, TinyhandSerializerOptions options)
    {
        if (value == null)
        {
            return null;
        }
        else
        {
            var array = new DateTime[value.Length];
            Array.Copy(value, array, value.Length);
            return array;
        }
    }
}*/
