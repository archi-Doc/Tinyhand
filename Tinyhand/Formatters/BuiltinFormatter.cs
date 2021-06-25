// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using Tinyhand.IO;

#pragma warning disable SA1204 // Static elements should appear before instance elements
#pragma warning disable SA1649 // File name should match first type name

namespace Tinyhand.Formatters
{
    public sealed class StringFormatter : ITinyhandFormatter<string>
    {
        public static readonly StringFormatter Instance = new();

        private StringFormatter()
        {
        }

        public void Serialize(ref TinyhandWriter writer, string? value, TinyhandSerializerOptions options)
        {
            writer.Write(value);
        }

        public string? Deserialize(ref TinyhandReader reader, TinyhandSerializerOptions options)
        {
            return reader.ReadString(); // ?? string.Empty;
        }

        public string Reconstruct(TinyhandSerializerOptions options)
        {
            return string.Empty;
        }

        public string? Clone(string? value, TinyhandSerializerOptions options)
        {
            return value;
        }
    }

    public sealed class StringArrayFormatter : ITinyhandFormatter<string[]>
    {
        public static readonly StringArrayFormatter Instance = new();

        private StringArrayFormatter()
        {
        }

        public void Serialize(ref TinyhandWriter writer, string[]? value, TinyhandSerializerOptions options) => Tinyhand.Formatters.Builtin.SerializeStringArray(ref writer, value);

        public string[]? Deserialize(ref TinyhandReader reader, TinyhandSerializerOptions options) => Tinyhand.Formatters.Builtin.DeserializeStringArray(ref reader);

        public string[] Reconstruct(TinyhandSerializerOptions options)
        {
            return new string[0];
        }

        public string[]? Clone(string[]? value, TinyhandSerializerOptions options) => Tinyhand.Formatters.Builtin.CloneStringArray(value);
    }

    public sealed class StringListFormatter : ITinyhandFormatter<List<string>>
    {
        public static readonly StringListFormatter Instance = new();

        private StringListFormatter()
        {
        }

        public void Serialize(ref TinyhandWriter writer, List<string>? value, TinyhandSerializerOptions options) => Tinyhand.Formatters.Builtin.SerializeStringList(ref writer, value);

        public List<string>? Deserialize(ref TinyhandReader reader, TinyhandSerializerOptions options) => Tinyhand.Formatters.Builtin.DeserializeStringList(ref reader);

        public List<string> Reconstruct(TinyhandSerializerOptions options)
        {
            return new List<string>();
        }

        public List<string>? Clone(List<string>? value, TinyhandSerializerOptions options) => value == null ? null : new List<string>(value);
    }

    public sealed class ByteArrayFormatter : ITinyhandFormatter<byte[]>
    {
        public static readonly ByteArrayFormatter Instance = new();

        private ByteArrayFormatter()
        {
        }

        public void Serialize(ref TinyhandWriter writer, byte[]? value, TinyhandSerializerOptions options)
        {
            writer.Write(value);
        }

        public byte[]? Deserialize(ref TinyhandReader reader, TinyhandSerializerOptions options)
        {
            return reader.ReadBytes()?.ToArray(); // ?? new byte[0];
        }

        public byte[] Reconstruct(TinyhandSerializerOptions options)
        {
            return new byte[0];
        }

        public byte[]? Clone(byte[]? value, TinyhandSerializerOptions options) => Tinyhand.Formatters.Builtin.CloneUInt8Array(value);
    }

    public sealed class ByteListFormatter : ITinyhandFormatter<List<byte>>
    {
        public static readonly ByteListFormatter Instance = new();

        private ByteListFormatter()
        {
        }

        public void Serialize(ref TinyhandWriter writer, List<byte>? value, TinyhandSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.Write(value.ToArray().AsSpan());
            }
        }

        public List<byte>? Deserialize(ref TinyhandReader reader, TinyhandSerializerOptions options)
        {
            var array = reader.ReadBytes()?.ToArray(); // ?? new byte[0];
            return array == null ? null : new List<byte>(array);
        }

        public List<byte> Reconstruct(TinyhandSerializerOptions options)
        {
            return new List<byte>();
        }

        public List<byte>? Clone(List<byte>? value, TinyhandSerializerOptions options) => value == null ? null : new List<byte>(value);
    }

    public static partial class Builtin
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void SerializeStringArray(ref TinyhandWriter writer, string[]? value)
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
                    writer.Write(value[i]);
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string[]? DeserializeStringArray(ref TinyhandReader reader)
        {
            if (reader.TryReadNil())
            {
                return null; // new string[0];
            }
            else
            {
                var len = reader.ReadArrayHeader();
                var array = new string[len];
                for (int i = 0; i < array.Length; i++)
                {
                    array[i] = reader.ReadString() ?? string.Empty;
                }

                return array;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string[]? CloneStringArray(string?[]? value)
        {
            if (value == null)
            {
                return null;
            }
            else
            {
                var array = new string[value.Length];
                Array.Copy(value, array, value.Length);
                return array;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void SerializeNullableStringArray(ref TinyhandWriter writer, string?[]? value)
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
                    writer.Write(value[i]);
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string[]? DeserializeNullableStringArray(ref TinyhandReader reader)
        {
            if (reader.TryReadNil())
            {
                return null; // new string[0];
            }
            else
            {
                var len = reader.ReadArrayHeader();
                var array = new string[len];
                for (int i = 0; i < array.Length; i++)
                {
                    array[i] = reader.ReadString() ?? string.Empty;
                }

                return array;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void SerializeStringList(ref TinyhandWriter writer, List<string>? value)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.WriteArrayHeader(value.Count);
                for (int i = 0; i < value.Count; i++)
                {
                    writer.Write(value[i]);
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static List<string>? DeserializeStringList(ref TinyhandReader reader)
        {
            if (reader.TryReadNil())
            {
                return null; // new List<string>();
            }
            else
            {
                var len = reader.ReadArrayHeader();
                var list = new List<string>(len);
                for (int i = 0; i < len; i++)
                {
                    list.Add(reader.ReadString() ?? string.Empty);
                }

                return list;
            }
        }
    }
}
