// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Net;
using System.Runtime.InteropServices;
using Tinyhand.IO;

namespace Tinyhand.Formatters;

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
/// Serialize IPAddress.
/// </summary>
public sealed class IPEndPointFormatter : ITinyhandFormatter<IPEndPoint>
{
    public static readonly IPEndPointFormatter Instance = new IPEndPointFormatter();

    public void Serialize(ref TinyhandWriter writer, IPEndPoint? value, TinyhandSerializerOptions options)
    {// Nil or Bin8(Address, RawInt)
        if (value == null)
        {
            writer.WriteNil();
            return;
        }

        var span = writer.GetSpan(32);
        if (value.Address.TryWriteBytes(span.Slice(2), out var written))
        {
            span[0] = MessagePackCode.Bin8;
            span[1] = (byte)(written + 4);
            MemoryMarshal.Write(span.Slice(2 + written), value.Port);
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

        var port = MemoryMarshal.Read<int>(span.Slice(span.Length - 4));
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
