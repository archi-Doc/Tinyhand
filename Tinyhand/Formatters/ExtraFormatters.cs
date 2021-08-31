// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.Net;
using Tinyhand.IO;

#pragma warning disable SA1649 // File name should match first type name

namespace Tinyhand.Formatters
{
    /// <summary>
    /// Serialize by .NET native DateTime binary format.
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

            /*Span<byte> span = stackalloc byte[16];
            value.TryWriteBytes(span, out var written);
            writer.Write(span);*/
        }

        public IPAddress? Deserialize(ref TinyhandReader reader, TinyhandSerializerOptions options)
        {
            var seq = reader.ReadBytes();
            if (seq.HasValue)
            {
                return new IPAddress(seq.Value.ToArray());
            }
            else
            {
                return null;
            }
        }

        public IPAddress Reconstruct(TinyhandSerializerOptions options)
        {
            return IPAddress.None;
        }

        public IPAddress? Clone(IPAddress? value, TinyhandSerializerOptions options) => value == null ? null : new IPAddress(value.GetAddressBytes());
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
}
