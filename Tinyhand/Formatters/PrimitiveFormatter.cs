// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

/* THIS (.cs) FILE IS GENERATED. DO NOT CHANGE IT.
 * CHANGE THE .tt FILE INSTEAD. */

using System;
using System.Collections.Generic;
using Tinyhand.IO;

#pragma warning disable SA1649 // File name should match first type name

namespace Tinyhand.Formatters
{
    public sealed class UInt8Formatter : ITinyhandFormatter<byte>
    {
        public static readonly UInt8Formatter Instance = new ();

        private UInt8Formatter()
        {
        }

        public void Serialize(ref TinyhandWriter writer, byte value, TinyhandSerializerOptions options)
        {
            writer.Write(value);
        }

        public byte Deserialize(ref TinyhandReader reader, object? overwrite, TinyhandSerializerOptions options)
        {
            return reader.ReadUInt8();
        }

        public byte Reconstruct(TinyhandSerializerOptions options)
        {
            return default;
        }
    }

    public sealed class NullableUInt8Formatter : ITinyhandFormatter<byte?>
    {
        public static readonly NullableUInt8Formatter Instance = new ();

        private NullableUInt8Formatter()
        {
        }

        public void Serialize(ref TinyhandWriter writer, byte? value, TinyhandSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.Write(value.Value);
            }
        }

        public byte? Deserialize(ref TinyhandReader reader, object? overwrite, TinyhandSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return default;
            }
            else
            {
                return reader.ReadUInt8();
            }
        }

        public byte? Reconstruct(TinyhandSerializerOptions options)
        {
            return default(byte);
        }
    }

    public sealed class Int8Formatter : ITinyhandFormatter<sbyte>
    {
        public static readonly Int8Formatter Instance = new ();

        private Int8Formatter()
        {
        }

        public void Serialize(ref TinyhandWriter writer, sbyte value, TinyhandSerializerOptions options)
        {
            writer.Write(value);
        }

        public sbyte Deserialize(ref TinyhandReader reader, object? overwrite, TinyhandSerializerOptions options)
        {
            return reader.ReadInt8();
        }

        public sbyte Reconstruct(TinyhandSerializerOptions options)
        {
            return default;
        }
    }

    public sealed class NullableInt8Formatter : ITinyhandFormatter<sbyte?>
    {
        public static readonly NullableInt8Formatter Instance = new ();

        private NullableInt8Formatter()
        {
        }

        public void Serialize(ref TinyhandWriter writer, sbyte? value, TinyhandSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.Write(value.Value);
            }
        }

        public sbyte? Deserialize(ref TinyhandReader reader, object? overwrite, TinyhandSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return default;
            }
            else
            {
                return reader.ReadInt8();
            }
        }

        public sbyte? Reconstruct(TinyhandSerializerOptions options)
        {
            return default(sbyte);
        }
    }

    public sealed class Int8ArrayFormatter : ITinyhandFormatter<sbyte[]>
    {
        public static readonly Int8ArrayFormatter Instance = new ();

        private Int8ArrayFormatter()
        {
        }

        public void Serialize(ref TinyhandWriter writer, sbyte[]? value, TinyhandSerializerOptions options) => Tinyhand.Formatters.Builtin.SerializeInt8Array(ref writer, value);

        public sbyte[]? Deserialize(ref TinyhandReader reader, object? overwrite, TinyhandSerializerOptions options) => Tinyhand.Formatters.Builtin.DeserializeInt8Array(ref reader);

        public sbyte[] Reconstruct(TinyhandSerializerOptions options)
        {
            return new sbyte[0];
        }
    }

    public sealed class Int8ListFormatter : ITinyhandFormatter<List<sbyte>>
    {
        public static readonly Int8ListFormatter Instance = new ();

        private Int8ListFormatter()
        {
        }

        public void Serialize(ref TinyhandWriter writer, List<sbyte>? value, TinyhandSerializerOptions options) => Tinyhand.Formatters.Builtin.SerializeInt8List(ref writer, value);

        public List<sbyte>? Deserialize(ref TinyhandReader reader, object? overwrite, TinyhandSerializerOptions options) => Tinyhand.Formatters.Builtin.DeserializeInt8List(ref reader);

        public List<sbyte> Reconstruct(TinyhandSerializerOptions options)
        {
            return new List<sbyte>();
        }
    }

    public sealed class UInt16Formatter : ITinyhandFormatter<ushort>
    {
        public static readonly UInt16Formatter Instance = new ();

        private UInt16Formatter()
        {
        }

        public void Serialize(ref TinyhandWriter writer, ushort value, TinyhandSerializerOptions options)
        {
            writer.Write(value);
        }

        public ushort Deserialize(ref TinyhandReader reader, object? overwrite, TinyhandSerializerOptions options)
        {
            return reader.ReadUInt16();
        }

        public ushort Reconstruct(TinyhandSerializerOptions options)
        {
            return default;
        }
    }

    public sealed class NullableUInt16Formatter : ITinyhandFormatter<ushort?>
    {
        public static readonly NullableUInt16Formatter Instance = new ();

        private NullableUInt16Formatter()
        {
        }

        public void Serialize(ref TinyhandWriter writer, ushort? value, TinyhandSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.Write(value.Value);
            }
        }

        public ushort? Deserialize(ref TinyhandReader reader, object? overwrite, TinyhandSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return default;
            }
            else
            {
                return reader.ReadUInt16();
            }
        }

        public ushort? Reconstruct(TinyhandSerializerOptions options)
        {
            return default(ushort);
        }
    }

    public sealed class UInt16ArrayFormatter : ITinyhandFormatter<ushort[]>
    {
        public static readonly UInt16ArrayFormatter Instance = new ();

        private UInt16ArrayFormatter()
        {
        }

        public void Serialize(ref TinyhandWriter writer, ushort[]? value, TinyhandSerializerOptions options) => Tinyhand.Formatters.Builtin.SerializeUInt16Array(ref writer, value);

        public ushort[]? Deserialize(ref TinyhandReader reader, object? overwrite, TinyhandSerializerOptions options) => Tinyhand.Formatters.Builtin.DeserializeUInt16Array(ref reader);

        public ushort[] Reconstruct(TinyhandSerializerOptions options)
        {
            return new ushort[0];
        }
    }

    public sealed class UInt16ListFormatter : ITinyhandFormatter<List<ushort>>
    {
        public static readonly UInt16ListFormatter Instance = new ();

        private UInt16ListFormatter()
        {
        }

        public void Serialize(ref TinyhandWriter writer, List<ushort>? value, TinyhandSerializerOptions options) => Tinyhand.Formatters.Builtin.SerializeUInt16List(ref writer, value);

        public List<ushort>? Deserialize(ref TinyhandReader reader, object? overwrite, TinyhandSerializerOptions options) => Tinyhand.Formatters.Builtin.DeserializeUInt16List(ref reader);

        public List<ushort> Reconstruct(TinyhandSerializerOptions options)
        {
            return new List<ushort>();
        }
    }

    public sealed class Int16Formatter : ITinyhandFormatter<short>
    {
        public static readonly Int16Formatter Instance = new ();

        private Int16Formatter()
        {
        }

        public void Serialize(ref TinyhandWriter writer, short value, TinyhandSerializerOptions options)
        {
            writer.Write(value);
        }

        public short Deserialize(ref TinyhandReader reader, object? overwrite, TinyhandSerializerOptions options)
        {
            return reader.ReadInt16();
        }

        public short Reconstruct(TinyhandSerializerOptions options)
        {
            return default;
        }
    }

    public sealed class NullableInt16Formatter : ITinyhandFormatter<short?>
    {
        public static readonly NullableInt16Formatter Instance = new ();

        private NullableInt16Formatter()
        {
        }

        public void Serialize(ref TinyhandWriter writer, short? value, TinyhandSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.Write(value.Value);
            }
        }

        public short? Deserialize(ref TinyhandReader reader, object? overwrite, TinyhandSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return default;
            }
            else
            {
                return reader.ReadInt16();
            }
        }

        public short? Reconstruct(TinyhandSerializerOptions options)
        {
            return default(short);
        }
    }

    public sealed class Int16ArrayFormatter : ITinyhandFormatter<short[]>
    {
        public static readonly Int16ArrayFormatter Instance = new ();

        private Int16ArrayFormatter()
        {
        }

        public void Serialize(ref TinyhandWriter writer, short[]? value, TinyhandSerializerOptions options) => Tinyhand.Formatters.Builtin.SerializeInt16Array(ref writer, value);

        public short[]? Deserialize(ref TinyhandReader reader, object? overwrite, TinyhandSerializerOptions options) => Tinyhand.Formatters.Builtin.DeserializeInt16Array(ref reader);

        public short[] Reconstruct(TinyhandSerializerOptions options)
        {
            return new short[0];
        }
    }

    public sealed class Int16ListFormatter : ITinyhandFormatter<List<short>>
    {
        public static readonly Int16ListFormatter Instance = new ();

        private Int16ListFormatter()
        {
        }

        public void Serialize(ref TinyhandWriter writer, List<short>? value, TinyhandSerializerOptions options) => Tinyhand.Formatters.Builtin.SerializeInt16List(ref writer, value);

        public List<short>? Deserialize(ref TinyhandReader reader, object? overwrite, TinyhandSerializerOptions options) => Tinyhand.Formatters.Builtin.DeserializeInt16List(ref reader);

        public List<short> Reconstruct(TinyhandSerializerOptions options)
        {
            return new List<short>();
        }
    }

    public sealed class UInt32Formatter : ITinyhandFormatter<uint>
    {
        public static readonly UInt32Formatter Instance = new ();

        private UInt32Formatter()
        {
        }

        public void Serialize(ref TinyhandWriter writer, uint value, TinyhandSerializerOptions options)
        {
            writer.Write(value);
        }

        public uint Deserialize(ref TinyhandReader reader, object? overwrite, TinyhandSerializerOptions options)
        {
            return reader.ReadUInt32();
        }

        public uint Reconstruct(TinyhandSerializerOptions options)
        {
            return default;
        }
    }

    public sealed class NullableUInt32Formatter : ITinyhandFormatter<uint?>
    {
        public static readonly NullableUInt32Formatter Instance = new ();

        private NullableUInt32Formatter()
        {
        }

        public void Serialize(ref TinyhandWriter writer, uint? value, TinyhandSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.Write(value.Value);
            }
        }

        public uint? Deserialize(ref TinyhandReader reader, object? overwrite, TinyhandSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return default;
            }
            else
            {
                return reader.ReadUInt32();
            }
        }

        public uint? Reconstruct(TinyhandSerializerOptions options)
        {
            return default(uint);
        }
    }

    public sealed class UInt32ArrayFormatter : ITinyhandFormatter<uint[]>
    {
        public static readonly UInt32ArrayFormatter Instance = new ();

        private UInt32ArrayFormatter()
        {
        }

        public void Serialize(ref TinyhandWriter writer, uint[]? value, TinyhandSerializerOptions options) => Tinyhand.Formatters.Builtin.SerializeUInt32Array(ref writer, value);

        public uint[]? Deserialize(ref TinyhandReader reader, object? overwrite, TinyhandSerializerOptions options) => Tinyhand.Formatters.Builtin.DeserializeUInt32Array(ref reader);

        public uint[] Reconstruct(TinyhandSerializerOptions options)
        {
            return new uint[0];
        }
    }

    public sealed class UInt32ListFormatter : ITinyhandFormatter<List<uint>>
    {
        public static readonly UInt32ListFormatter Instance = new ();

        private UInt32ListFormatter()
        {
        }

        public void Serialize(ref TinyhandWriter writer, List<uint>? value, TinyhandSerializerOptions options) => Tinyhand.Formatters.Builtin.SerializeUInt32List(ref writer, value);

        public List<uint>? Deserialize(ref TinyhandReader reader, object? overwrite, TinyhandSerializerOptions options) => Tinyhand.Formatters.Builtin.DeserializeUInt32List(ref reader);

        public List<uint> Reconstruct(TinyhandSerializerOptions options)
        {
            return new List<uint>();
        }
    }

    public sealed class Int32Formatter : ITinyhandFormatter<int>
    {
        public static readonly Int32Formatter Instance = new ();

        private Int32Formatter()
        {
        }

        public void Serialize(ref TinyhandWriter writer, int value, TinyhandSerializerOptions options)
        {
            writer.Write(value);
        }

        public int Deserialize(ref TinyhandReader reader, object? overwrite, TinyhandSerializerOptions options)
        {
            return reader.ReadInt32();
        }

        public int Reconstruct(TinyhandSerializerOptions options)
        {
            return default;
        }
    }

    public sealed class NullableInt32Formatter : ITinyhandFormatter<int?>
    {
        public static readonly NullableInt32Formatter Instance = new ();

        private NullableInt32Formatter()
        {
        }

        public void Serialize(ref TinyhandWriter writer, int? value, TinyhandSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.Write(value.Value);
            }
        }

        public int? Deserialize(ref TinyhandReader reader, object? overwrite, TinyhandSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return default;
            }
            else
            {
                return reader.ReadInt32();
            }
        }

        public int? Reconstruct(TinyhandSerializerOptions options)
        {
            return default(int);
        }
    }

    public sealed class Int32ArrayFormatter : ITinyhandFormatter<int[]>
    {
        public static readonly Int32ArrayFormatter Instance = new ();

        private Int32ArrayFormatter()
        {
        }

        public void Serialize(ref TinyhandWriter writer, int[]? value, TinyhandSerializerOptions options) => Tinyhand.Formatters.Builtin.SerializeInt32Array(ref writer, value);

        public int[]? Deserialize(ref TinyhandReader reader, object? overwrite, TinyhandSerializerOptions options) => Tinyhand.Formatters.Builtin.DeserializeInt32Array(ref reader);

        public int[] Reconstruct(TinyhandSerializerOptions options)
        {
            return new int[0];
        }
    }

    public sealed class Int32ListFormatter : ITinyhandFormatter<List<int>>
    {
        public static readonly Int32ListFormatter Instance = new ();

        private Int32ListFormatter()
        {
        }

        public void Serialize(ref TinyhandWriter writer, List<int>? value, TinyhandSerializerOptions options) => Tinyhand.Formatters.Builtin.SerializeInt32List(ref writer, value);

        public List<int>? Deserialize(ref TinyhandReader reader, object? overwrite, TinyhandSerializerOptions options) => Tinyhand.Formatters.Builtin.DeserializeInt32List(ref reader);

        public List<int> Reconstruct(TinyhandSerializerOptions options)
        {
            return new List<int>();
        }
    }

    public sealed class UInt64Formatter : ITinyhandFormatter<ulong>
    {
        public static readonly UInt64Formatter Instance = new ();

        private UInt64Formatter()
        {
        }

        public void Serialize(ref TinyhandWriter writer, ulong value, TinyhandSerializerOptions options)
        {
            writer.Write(value);
        }

        public ulong Deserialize(ref TinyhandReader reader, object? overwrite, TinyhandSerializerOptions options)
        {
            return reader.ReadUInt64();
        }

        public ulong Reconstruct(TinyhandSerializerOptions options)
        {
            return default;
        }
    }

    public sealed class NullableUInt64Formatter : ITinyhandFormatter<ulong?>
    {
        public static readonly NullableUInt64Formatter Instance = new ();

        private NullableUInt64Formatter()
        {
        }

        public void Serialize(ref TinyhandWriter writer, ulong? value, TinyhandSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.Write(value.Value);
            }
        }

        public ulong? Deserialize(ref TinyhandReader reader, object? overwrite, TinyhandSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return default;
            }
            else
            {
                return reader.ReadUInt64();
            }
        }

        public ulong? Reconstruct(TinyhandSerializerOptions options)
        {
            return default(ulong);
        }
    }

    public sealed class UInt64ArrayFormatter : ITinyhandFormatter<ulong[]>
    {
        public static readonly UInt64ArrayFormatter Instance = new ();

        private UInt64ArrayFormatter()
        {
        }

        public void Serialize(ref TinyhandWriter writer, ulong[]? value, TinyhandSerializerOptions options) => Tinyhand.Formatters.Builtin.SerializeUInt64Array(ref writer, value);

        public ulong[]? Deserialize(ref TinyhandReader reader, object? overwrite, TinyhandSerializerOptions options) => Tinyhand.Formatters.Builtin.DeserializeUInt64Array(ref reader);

        public ulong[] Reconstruct(TinyhandSerializerOptions options)
        {
            return new ulong[0];
        }
    }

    public sealed class UInt64ListFormatter : ITinyhandFormatter<List<ulong>>
    {
        public static readonly UInt64ListFormatter Instance = new ();

        private UInt64ListFormatter()
        {
        }

        public void Serialize(ref TinyhandWriter writer, List<ulong>? value, TinyhandSerializerOptions options) => Tinyhand.Formatters.Builtin.SerializeUInt64List(ref writer, value);

        public List<ulong>? Deserialize(ref TinyhandReader reader, object? overwrite, TinyhandSerializerOptions options) => Tinyhand.Formatters.Builtin.DeserializeUInt64List(ref reader);

        public List<ulong> Reconstruct(TinyhandSerializerOptions options)
        {
            return new List<ulong>();
        }
    }

    public sealed class Int64Formatter : ITinyhandFormatter<long>
    {
        public static readonly Int64Formatter Instance = new ();

        private Int64Formatter()
        {
        }

        public void Serialize(ref TinyhandWriter writer, long value, TinyhandSerializerOptions options)
        {
            writer.Write(value);
        }

        public long Deserialize(ref TinyhandReader reader, object? overwrite, TinyhandSerializerOptions options)
        {
            return reader.ReadInt64();
        }

        public long Reconstruct(TinyhandSerializerOptions options)
        {
            return default;
        }
    }

    public sealed class NullableInt64Formatter : ITinyhandFormatter<long?>
    {
        public static readonly NullableInt64Formatter Instance = new ();

        private NullableInt64Formatter()
        {
        }

        public void Serialize(ref TinyhandWriter writer, long? value, TinyhandSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.Write(value.Value);
            }
        }

        public long? Deserialize(ref TinyhandReader reader, object? overwrite, TinyhandSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return default;
            }
            else
            {
                return reader.ReadInt64();
            }
        }

        public long? Reconstruct(TinyhandSerializerOptions options)
        {
            return default(long);
        }
    }

    public sealed class Int64ArrayFormatter : ITinyhandFormatter<long[]>
    {
        public static readonly Int64ArrayFormatter Instance = new ();

        private Int64ArrayFormatter()
        {
        }

        public void Serialize(ref TinyhandWriter writer, long[]? value, TinyhandSerializerOptions options) => Tinyhand.Formatters.Builtin.SerializeInt64Array(ref writer, value);

        public long[]? Deserialize(ref TinyhandReader reader, object? overwrite, TinyhandSerializerOptions options) => Tinyhand.Formatters.Builtin.DeserializeInt64Array(ref reader);

        public long[] Reconstruct(TinyhandSerializerOptions options)
        {
            return new long[0];
        }
    }

    public sealed class Int64ListFormatter : ITinyhandFormatter<List<long>>
    {
        public static readonly Int64ListFormatter Instance = new ();

        private Int64ListFormatter()
        {
        }

        public void Serialize(ref TinyhandWriter writer, List<long>? value, TinyhandSerializerOptions options) => Tinyhand.Formatters.Builtin.SerializeInt64List(ref writer, value);

        public List<long>? Deserialize(ref TinyhandReader reader, object? overwrite, TinyhandSerializerOptions options) => Tinyhand.Formatters.Builtin.DeserializeInt64List(ref reader);

        public List<long> Reconstruct(TinyhandSerializerOptions options)
        {
            return new List<long>();
        }
    }

    public sealed class SingleFormatter : ITinyhandFormatter<float>
    {
        public static readonly SingleFormatter Instance = new ();

        private SingleFormatter()
        {
        }

        public void Serialize(ref TinyhandWriter writer, float value, TinyhandSerializerOptions options)
        {
            writer.Write(value);
        }

        public float Deserialize(ref TinyhandReader reader, object? overwrite, TinyhandSerializerOptions options)
        {
            return reader.ReadSingle();
        }

        public float Reconstruct(TinyhandSerializerOptions options)
        {
            return default;
        }
    }

    public sealed class NullableSingleFormatter : ITinyhandFormatter<float?>
    {
        public static readonly NullableSingleFormatter Instance = new ();

        private NullableSingleFormatter()
        {
        }

        public void Serialize(ref TinyhandWriter writer, float? value, TinyhandSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.Write(value.Value);
            }
        }

        public float? Deserialize(ref TinyhandReader reader, object? overwrite, TinyhandSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return default;
            }
            else
            {
                return reader.ReadSingle();
            }
        }

        public float? Reconstruct(TinyhandSerializerOptions options)
        {
            return default(float);
        }
    }

    public sealed class SingleArrayFormatter : ITinyhandFormatter<float[]>
    {
        public static readonly SingleArrayFormatter Instance = new ();

        private SingleArrayFormatter()
        {
        }

        public void Serialize(ref TinyhandWriter writer, float[]? value, TinyhandSerializerOptions options) => Tinyhand.Formatters.Builtin.SerializeSingleArray(ref writer, value);

        public float[]? Deserialize(ref TinyhandReader reader, object? overwrite, TinyhandSerializerOptions options) => Tinyhand.Formatters.Builtin.DeserializeSingleArray(ref reader);

        public float[] Reconstruct(TinyhandSerializerOptions options)
        {
            return new float[0];
        }
    }

    public sealed class SingleListFormatter : ITinyhandFormatter<List<float>>
    {
        public static readonly SingleListFormatter Instance = new ();

        private SingleListFormatter()
        {
        }

        public void Serialize(ref TinyhandWriter writer, List<float>? value, TinyhandSerializerOptions options) => Tinyhand.Formatters.Builtin.SerializeSingleList(ref writer, value);

        public List<float>? Deserialize(ref TinyhandReader reader, object? overwrite, TinyhandSerializerOptions options) => Tinyhand.Formatters.Builtin.DeserializeSingleList(ref reader);

        public List<float> Reconstruct(TinyhandSerializerOptions options)
        {
            return new List<float>();
        }
    }

    public sealed class DoubleFormatter : ITinyhandFormatter<double>
    {
        public static readonly DoubleFormatter Instance = new ();

        private DoubleFormatter()
        {
        }

        public void Serialize(ref TinyhandWriter writer, double value, TinyhandSerializerOptions options)
        {
            writer.Write(value);
        }

        public double Deserialize(ref TinyhandReader reader, object? overwrite, TinyhandSerializerOptions options)
        {
            return reader.ReadDouble();
        }

        public double Reconstruct(TinyhandSerializerOptions options)
        {
            return default;
        }
    }

    public sealed class NullableDoubleFormatter : ITinyhandFormatter<double?>
    {
        public static readonly NullableDoubleFormatter Instance = new ();

        private NullableDoubleFormatter()
        {
        }

        public void Serialize(ref TinyhandWriter writer, double? value, TinyhandSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.Write(value.Value);
            }
        }

        public double? Deserialize(ref TinyhandReader reader, object? overwrite, TinyhandSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return default;
            }
            else
            {
                return reader.ReadDouble();
            }
        }

        public double? Reconstruct(TinyhandSerializerOptions options)
        {
            return default(double);
        }
    }

    public sealed class DoubleArrayFormatter : ITinyhandFormatter<double[]>
    {
        public static readonly DoubleArrayFormatter Instance = new ();

        private DoubleArrayFormatter()
        {
        }

        public void Serialize(ref TinyhandWriter writer, double[]? value, TinyhandSerializerOptions options) => Tinyhand.Formatters.Builtin.SerializeDoubleArray(ref writer, value);

        public double[]? Deserialize(ref TinyhandReader reader, object? overwrite, TinyhandSerializerOptions options) => Tinyhand.Formatters.Builtin.DeserializeDoubleArray(ref reader);

        public double[] Reconstruct(TinyhandSerializerOptions options)
        {
            return new double[0];
        }
    }

    public sealed class DoubleListFormatter : ITinyhandFormatter<List<double>>
    {
        public static readonly DoubleListFormatter Instance = new ();

        private DoubleListFormatter()
        {
        }

        public void Serialize(ref TinyhandWriter writer, List<double>? value, TinyhandSerializerOptions options) => Tinyhand.Formatters.Builtin.SerializeDoubleList(ref writer, value);

        public List<double>? Deserialize(ref TinyhandReader reader, object? overwrite, TinyhandSerializerOptions options) => Tinyhand.Formatters.Builtin.DeserializeDoubleList(ref reader);

        public List<double> Reconstruct(TinyhandSerializerOptions options)
        {
            return new List<double>();
        }
    }

    public sealed class BooleanFormatter : ITinyhandFormatter<bool>
    {
        public static readonly BooleanFormatter Instance = new ();

        private BooleanFormatter()
        {
        }

        public void Serialize(ref TinyhandWriter writer, bool value, TinyhandSerializerOptions options)
        {
            writer.Write(value);
        }

        public bool Deserialize(ref TinyhandReader reader, object? overwrite, TinyhandSerializerOptions options)
        {
            return reader.ReadBoolean();
        }

        public bool Reconstruct(TinyhandSerializerOptions options)
        {
            return default;
        }
    }

    public sealed class NullableBooleanFormatter : ITinyhandFormatter<bool?>
    {
        public static readonly NullableBooleanFormatter Instance = new ();

        private NullableBooleanFormatter()
        {
        }

        public void Serialize(ref TinyhandWriter writer, bool? value, TinyhandSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.Write(value.Value);
            }
        }

        public bool? Deserialize(ref TinyhandReader reader, object? overwrite, TinyhandSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return default;
            }
            else
            {
                return reader.ReadBoolean();
            }
        }

        public bool? Reconstruct(TinyhandSerializerOptions options)
        {
            return default(bool);
        }
    }

    public sealed class BooleanArrayFormatter : ITinyhandFormatter<bool[]>
    {
        public static readonly BooleanArrayFormatter Instance = new ();

        private BooleanArrayFormatter()
        {
        }

        public void Serialize(ref TinyhandWriter writer, bool[]? value, TinyhandSerializerOptions options) => Tinyhand.Formatters.Builtin.SerializeBooleanArray(ref writer, value);

        public bool[]? Deserialize(ref TinyhandReader reader, object? overwrite, TinyhandSerializerOptions options) => Tinyhand.Formatters.Builtin.DeserializeBooleanArray(ref reader);

        public bool[] Reconstruct(TinyhandSerializerOptions options)
        {
            return new bool[0];
        }
    }

    public sealed class BooleanListFormatter : ITinyhandFormatter<List<bool>>
    {
        public static readonly BooleanListFormatter Instance = new ();

        private BooleanListFormatter()
        {
        }

        public void Serialize(ref TinyhandWriter writer, List<bool>? value, TinyhandSerializerOptions options) => Tinyhand.Formatters.Builtin.SerializeBooleanList(ref writer, value);

        public List<bool>? Deserialize(ref TinyhandReader reader, object? overwrite, TinyhandSerializerOptions options) => Tinyhand.Formatters.Builtin.DeserializeBooleanList(ref reader);

        public List<bool> Reconstruct(TinyhandSerializerOptions options)
        {
            return new List<bool>();
        }
    }

    public sealed class CharFormatter : ITinyhandFormatter<char>
    {
        public static readonly CharFormatter Instance = new ();

        private CharFormatter()
        {
        }

        public void Serialize(ref TinyhandWriter writer, char value, TinyhandSerializerOptions options)
        {
            writer.Write(value);
        }

        public char Deserialize(ref TinyhandReader reader, object? overwrite, TinyhandSerializerOptions options)
        {
            return reader.ReadChar();
        }

        public char Reconstruct(TinyhandSerializerOptions options)
        {
            return default;
        }
    }

    public sealed class NullableCharFormatter : ITinyhandFormatter<char?>
    {
        public static readonly NullableCharFormatter Instance = new ();

        private NullableCharFormatter()
        {
        }

        public void Serialize(ref TinyhandWriter writer, char? value, TinyhandSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.Write(value.Value);
            }
        }

        public char? Deserialize(ref TinyhandReader reader, object? overwrite, TinyhandSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return default;
            }
            else
            {
                return reader.ReadChar();
            }
        }

        public char? Reconstruct(TinyhandSerializerOptions options)
        {
            return default(char);
        }
    }

    public sealed class CharArrayFormatter : ITinyhandFormatter<char[]>
    {
        public static readonly CharArrayFormatter Instance = new ();

        private CharArrayFormatter()
        {
        }

        public void Serialize(ref TinyhandWriter writer, char[]? value, TinyhandSerializerOptions options) => Tinyhand.Formatters.Builtin.SerializeCharArray(ref writer, value);

        public char[]? Deserialize(ref TinyhandReader reader, object? overwrite, TinyhandSerializerOptions options) => Tinyhand.Formatters.Builtin.DeserializeCharArray(ref reader);

        public char[] Reconstruct(TinyhandSerializerOptions options)
        {
            return new char[0];
        }
    }

    public sealed class CharListFormatter : ITinyhandFormatter<List<char>>
    {
        public static readonly CharListFormatter Instance = new ();

        private CharListFormatter()
        {
        }

        public void Serialize(ref TinyhandWriter writer, List<char>? value, TinyhandSerializerOptions options) => Tinyhand.Formatters.Builtin.SerializeCharList(ref writer, value);

        public List<char>? Deserialize(ref TinyhandReader reader, object? overwrite, TinyhandSerializerOptions options) => Tinyhand.Formatters.Builtin.DeserializeCharList(ref reader);

        public List<char> Reconstruct(TinyhandSerializerOptions options)
        {
            return new List<char>();
        }
    }

    public sealed class DateTimeFormatter : ITinyhandFormatter<DateTime>
    {
        public static readonly DateTimeFormatter Instance = new ();

        private DateTimeFormatter()
        {
        }

        public void Serialize(ref TinyhandWriter writer, DateTime value, TinyhandSerializerOptions options)
        {
            writer.Write(value);
        }

        public DateTime Deserialize(ref TinyhandReader reader, object? overwrite, TinyhandSerializerOptions options)
        {
            return reader.ReadDateTime();
        }

        public DateTime Reconstruct(TinyhandSerializerOptions options)
        {
            return default;
        }
    }

    public sealed class NullableDateTimeFormatter : ITinyhandFormatter<DateTime?>
    {
        public static readonly NullableDateTimeFormatter Instance = new ();

        private NullableDateTimeFormatter()
        {
        }

        public void Serialize(ref TinyhandWriter writer, DateTime? value, TinyhandSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.Write(value.Value);
            }
        }

        public DateTime? Deserialize(ref TinyhandReader reader, object? overwrite, TinyhandSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return default;
            }
            else
            {
                return reader.ReadDateTime();
            }
        }

        public DateTime? Reconstruct(TinyhandSerializerOptions options)
        {
            return default(DateTime);
        }
    }

    public sealed class DateTimeArrayFormatter : ITinyhandFormatter<DateTime[]>
    {
        public static readonly DateTimeArrayFormatter Instance = new ();

        private DateTimeArrayFormatter()
        {
        }

        public void Serialize(ref TinyhandWriter writer, DateTime[]? value, TinyhandSerializerOptions options) => Tinyhand.Formatters.Builtin.SerializeDateTimeArray(ref writer, value);

        public DateTime[]? Deserialize(ref TinyhandReader reader, object? overwrite, TinyhandSerializerOptions options) => Tinyhand.Formatters.Builtin.DeserializeDateTimeArray(ref reader);

        public DateTime[] Reconstruct(TinyhandSerializerOptions options)
        {
            return new DateTime[0];
        }
    }

    public sealed class DateTimeListFormatter : ITinyhandFormatter<List<DateTime>>
    {
        public static readonly DateTimeListFormatter Instance = new ();

        private DateTimeListFormatter()
        {
        }

        public void Serialize(ref TinyhandWriter writer, List<DateTime>? value, TinyhandSerializerOptions options) => Tinyhand.Formatters.Builtin.SerializeDateTimeList(ref writer, value);

        public List<DateTime>? Deserialize(ref TinyhandReader reader, object? overwrite, TinyhandSerializerOptions options) => Tinyhand.Formatters.Builtin.DeserializeDateTimeList(ref reader);

        public List<DateTime> Reconstruct(TinyhandSerializerOptions options)
        {
            return new List<DateTime>();
        }
    }
}
