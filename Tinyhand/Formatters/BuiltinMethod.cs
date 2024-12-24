// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

/* THIS (.cs) FILE IS GENERATED. DO NOT CHANGE IT.
 * CHANGE THE .tt FILE INSTEAD. */

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Tinyhand.IO;

#pragma warning disable SA1649 // File name should match first type name

namespace Tinyhand.Formatters;

public static partial class Builtin
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void SerializeUInt8Array(ref TinyhandWriter writer, byte[]? value)
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
    public static void DeserializeUInt8Array(ref TinyhandReader reader, ref byte[]? value)
    {
        if (reader.TryReadNil())
        {
        }
        else
        {
            var len = reader.ReadArrayHeader();
            value = new byte[len];
            for (int i = 0; i < value.Length; i++)
            {
                value[i] = reader.ReadUInt8();
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void SerializeUInt8List(ref TinyhandWriter writer, List<byte>? value)
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
    public static void DeserializeUInt8List(ref TinyhandReader reader, ref List<byte>? value)
    {
        if (reader.TryReadNil())
        {
        }
        else
        {
            var len = reader.ReadArrayHeader();
            value ??= new List<byte>(len);
            for (int i = 0; i < len; i++)
            {
                value.Add(reader.ReadUInt8());
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static byte[]? CloneUInt8Array(byte[]? value)
    {
        if (value == null)
        {
            return null;
        }
        else
        {
            var array = new byte[value.Length];
            Array.Copy(value, array, value.Length);
            return array;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void SerializeInt8Array(ref TinyhandWriter writer, sbyte[]? value)
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
    public static void DeserializeInt8Array(ref TinyhandReader reader, ref sbyte[]? value)
    {
        if (reader.TryReadNil())
        {
        }
        else
        {
            var len = reader.ReadArrayHeader();
            value = new sbyte[len];
            for (int i = 0; i < value.Length; i++)
            {
                value[i] = reader.ReadInt8();
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void SerializeInt8List(ref TinyhandWriter writer, List<sbyte>? value)
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
    public static void DeserializeInt8List(ref TinyhandReader reader, ref List<sbyte>? value)
    {
        if (reader.TryReadNil())
        {
        }
        else
        {
            var len = reader.ReadArrayHeader();
            value ??= new List<sbyte>(len);
            for (int i = 0; i < len; i++)
            {
                value.Add(reader.ReadInt8());
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static sbyte[]? CloneInt8Array(sbyte[]? value)
    {
        if (value == null)
        {
            return null;
        }
        else
        {
            var array = new sbyte[value.Length];
            Array.Copy(value, array, value.Length);
            return array;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void SerializeUInt16Array(ref TinyhandWriter writer, ushort[]? value)
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
    public static void DeserializeUInt16Array(ref TinyhandReader reader, ref ushort[]? value)
    {
        if (reader.TryReadNil())
        {
        }
        else
        {
            var len = reader.ReadArrayHeader();
            value = new ushort[len];
            for (int i = 0; i < value.Length; i++)
            {
                value[i] = reader.ReadUInt16();
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void SerializeUInt16List(ref TinyhandWriter writer, List<ushort>? value)
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
    public static void DeserializeUInt16List(ref TinyhandReader reader, ref List<ushort>? value)
    {
        if (reader.TryReadNil())
        {
        }
        else
        {
            var len = reader.ReadArrayHeader();
            value ??= new List<ushort>(len);
            for (int i = 0; i < len; i++)
            {
                value.Add(reader.ReadUInt16());
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ushort[]? CloneUInt16Array(ushort[]? value)
    {
        if (value == null)
        {
            return null;
        }
        else
        {
            var array = new ushort[value.Length];
            Array.Copy(value, array, value.Length);
            return array;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void SerializeInt16Array(ref TinyhandWriter writer, short[]? value)
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
    public static void DeserializeInt16Array(ref TinyhandReader reader, ref short[]? value)
    {
        if (reader.TryReadNil())
        {
        }
        else
        {
            var len = reader.ReadArrayHeader();
            value = new short[len];
            for (int i = 0; i < value.Length; i++)
            {
                value[i] = reader.ReadInt16();
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void SerializeInt16List(ref TinyhandWriter writer, List<short>? value)
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
    public static void DeserializeInt16List(ref TinyhandReader reader, ref List<short>? value)
    {
        if (reader.TryReadNil())
        {
        }
        else
        {
            var len = reader.ReadArrayHeader();
            value ??= new List<short>(len);
            for (int i = 0; i < len; i++)
            {
                value.Add(reader.ReadInt16());
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static short[]? CloneInt16Array(short[]? value)
    {
        if (value == null)
        {
            return null;
        }
        else
        {
            var array = new short[value.Length];
            Array.Copy(value, array, value.Length);
            return array;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void SerializeUInt32Array(ref TinyhandWriter writer, uint[]? value)
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
    public static void DeserializeUInt32Array(ref TinyhandReader reader, ref uint[]? value)
    {
        if (reader.TryReadNil())
        {
        }
        else
        {
            var len = reader.ReadArrayHeader();
            value = new uint[len];
            for (int i = 0; i < value.Length; i++)
            {
                value[i] = reader.ReadUInt32();
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void SerializeUInt32List(ref TinyhandWriter writer, List<uint>? value)
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
    public static void DeserializeUInt32List(ref TinyhandReader reader, ref List<uint>? value)
    {
        if (reader.TryReadNil())
        {
        }
        else
        {
            var len = reader.ReadArrayHeader();
            value ??= new List<uint>(len);
            for (int i = 0; i < len; i++)
            {
                value.Add(reader.ReadUInt32());
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static uint[]? CloneUInt32Array(uint[]? value)
    {
        if (value == null)
        {
            return null;
        }
        else
        {
            var array = new uint[value.Length];
            Array.Copy(value, array, value.Length);
            return array;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void SerializeInt32Array(ref TinyhandWriter writer, int[]? value)
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
    public static void DeserializeInt32Array(ref TinyhandReader reader, ref int[]? value)
    {
        if (reader.TryReadNil())
        {
        }
        else
        {
            var len = reader.ReadArrayHeader();
            value = new int[len];
            for (int i = 0; i < value.Length; i++)
            {
                value[i] = reader.ReadInt32();
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void SerializeInt32List(ref TinyhandWriter writer, List<int>? value)
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
    public static void DeserializeInt32List(ref TinyhandReader reader, ref List<int>? value)
    {
        if (reader.TryReadNil())
        {
        }
        else
        {
            var len = reader.ReadArrayHeader();
            value ??= new List<int>(len);
            for (int i = 0; i < len; i++)
            {
                value.Add(reader.ReadInt32());
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int[]? CloneInt32Array(int[]? value)
    {
        if (value == null)
        {
            return null;
        }
        else
        {
            var array = new int[value.Length];
            Array.Copy(value, array, value.Length);
            return array;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void SerializeUInt64Array(ref TinyhandWriter writer, ulong[]? value)
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
    public static void DeserializeUInt64Array(ref TinyhandReader reader, ref ulong[]? value)
    {
        if (reader.TryReadNil())
        {
        }
        else
        {
            var len = reader.ReadArrayHeader();
            value = new ulong[len];
            for (int i = 0; i < value.Length; i++)
            {
                value[i] = reader.ReadUInt64();
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void SerializeUInt64List(ref TinyhandWriter writer, List<ulong>? value)
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
    public static void DeserializeUInt64List(ref TinyhandReader reader, ref List<ulong>? value)
    {
        if (reader.TryReadNil())
        {
        }
        else
        {
            var len = reader.ReadArrayHeader();
            value ??= new List<ulong>(len);
            for (int i = 0; i < len; i++)
            {
                value.Add(reader.ReadUInt64());
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ulong[]? CloneUInt64Array(ulong[]? value)
    {
        if (value == null)
        {
            return null;
        }
        else
        {
            var array = new ulong[value.Length];
            Array.Copy(value, array, value.Length);
            return array;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void SerializeInt64Array(ref TinyhandWriter writer, long[]? value)
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
    public static void DeserializeInt64Array(ref TinyhandReader reader, ref long[]? value)
    {
        if (reader.TryReadNil())
        {
        }
        else
        {
            var len = reader.ReadArrayHeader();
            value = new long[len];
            for (int i = 0; i < value.Length; i++)
            {
                value[i] = reader.ReadInt64();
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void SerializeInt64List(ref TinyhandWriter writer, List<long>? value)
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
    public static void DeserializeInt64List(ref TinyhandReader reader, ref List<long>? value)
    {
        if (reader.TryReadNil())
        {
        }
        else
        {
            var len = reader.ReadArrayHeader();
            value ??= new List<long>(len);
            for (int i = 0; i < len; i++)
            {
                value.Add(reader.ReadInt64());
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long[]? CloneInt64Array(long[]? value)
    {
        if (value == null)
        {
            return null;
        }
        else
        {
            var array = new long[value.Length];
            Array.Copy(value, array, value.Length);
            return array;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void SerializeSingleArray(ref TinyhandWriter writer, float[]? value)
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
    public static void DeserializeSingleArray(ref TinyhandReader reader, ref float[]? value)
    {
        if (reader.TryReadNil())
        {
        }
        else
        {
            var len = reader.ReadArrayHeader();
            value = new float[len];
            for (int i = 0; i < value.Length; i++)
            {
                value[i] = reader.ReadSingle();
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void SerializeSingleList(ref TinyhandWriter writer, List<float>? value)
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
    public static void DeserializeSingleList(ref TinyhandReader reader, ref List<float>? value)
    {
        if (reader.TryReadNil())
        {
        }
        else
        {
            var len = reader.ReadArrayHeader();
            value ??= new List<float>(len);
            for (int i = 0; i < len; i++)
            {
                value.Add(reader.ReadSingle());
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static float[]? CloneSingleArray(float[]? value)
    {
        if (value == null)
        {
            return null;
        }
        else
        {
            var array = new float[value.Length];
            Array.Copy(value, array, value.Length);
            return array;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void SerializeDoubleArray(ref TinyhandWriter writer, double[]? value)
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
    public static void DeserializeDoubleArray(ref TinyhandReader reader, ref double[]? value)
    {
        if (reader.TryReadNil())
        {
        }
        else
        {
            var len = reader.ReadArrayHeader();
            value = new double[len];
            for (int i = 0; i < value.Length; i++)
            {
                value[i] = reader.ReadDouble();
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void SerializeDoubleList(ref TinyhandWriter writer, List<double>? value)
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
    public static void DeserializeDoubleList(ref TinyhandReader reader, ref List<double>? value)
    {
        if (reader.TryReadNil())
        {
        }
        else
        {
            var len = reader.ReadArrayHeader();
            value ??= new List<double>(len);
            for (int i = 0; i < len; i++)
            {
                value.Add(reader.ReadDouble());
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static double[]? CloneDoubleArray(double[]? value)
    {
        if (value == null)
        {
            return null;
        }
        else
        {
            var array = new double[value.Length];
            Array.Copy(value, array, value.Length);
            return array;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void SerializeBooleanArray(ref TinyhandWriter writer, bool[]? value)
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
    public static void DeserializeBooleanArray(ref TinyhandReader reader, ref bool[]? value)
    {
        if (reader.TryReadNil())
        {
        }
        else
        {
            var len = reader.ReadArrayHeader();
            value = new bool[len];
            for (int i = 0; i < value.Length; i++)
            {
                value[i] = reader.ReadBoolean();
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void SerializeBooleanList(ref TinyhandWriter writer, List<bool>? value)
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
    public static void DeserializeBooleanList(ref TinyhandReader reader, ref List<bool>? value)
    {
        if (reader.TryReadNil())
        {
        }
        else
        {
            var len = reader.ReadArrayHeader();
            value ??= new List<bool>(len);
            for (int i = 0; i < len; i++)
            {
                value.Add(reader.ReadBoolean());
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool[]? CloneBooleanArray(bool[]? value)
    {
        if (value == null)
        {
            return null;
        }
        else
        {
            var array = new bool[value.Length];
            Array.Copy(value, array, value.Length);
            return array;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void SerializeCharArray(ref TinyhandWriter writer, char[]? value)
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
    public static void DeserializeCharArray(ref TinyhandReader reader, ref char[]? value)
    {
        if (reader.TryReadNil())
        {
        }
        else
        {
            var len = reader.ReadArrayHeader();
            value = new char[len];
            for (int i = 0; i < value.Length; i++)
            {
                value[i] = reader.ReadChar();
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void SerializeCharList(ref TinyhandWriter writer, List<char>? value)
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
    public static void DeserializeCharList(ref TinyhandReader reader, ref List<char>? value)
    {
        if (reader.TryReadNil())
        {
        }
        else
        {
            var len = reader.ReadArrayHeader();
            value ??= new List<char>(len);
            for (int i = 0; i < len; i++)
            {
                value.Add(reader.ReadChar());
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static char[]? CloneCharArray(char[]? value)
    {
        if (value == null)
        {
            return null;
        }
        else
        {
            var array = new char[value.Length];
            Array.Copy(value, array, value.Length);
            return array;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void SerializeDateTimeArray(ref TinyhandWriter writer, DateTime[]? value)
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
    public static void DeserializeDateTimeArray(ref TinyhandReader reader, ref DateTime[]? value)
    {
        if (reader.TryReadNil())
        {
        }
        else
        {
            var len = reader.ReadArrayHeader();
            value = new DateTime[len];
            for (int i = 0; i < value.Length; i++)
            {
                value[i] = reader.ReadDateTime();
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void SerializeDateTimeList(ref TinyhandWriter writer, List<DateTime>? value)
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
    public static void DeserializeDateTimeList(ref TinyhandReader reader, ref List<DateTime>? value)
    {
        if (reader.TryReadNil())
        {
        }
        else
        {
            var len = reader.ReadArrayHeader();
            value ??= new List<DateTime>(len);
            for (int i = 0; i < len; i++)
            {
                value.Add(reader.ReadDateTime());
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static DateTime[]? CloneDateTimeArray(DateTime[]? value)
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

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void SerializeInt128Array(ref TinyhandWriter writer, Int128[]? value)
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
    public static void DeserializeInt128Array(ref TinyhandReader reader, ref Int128[]? value)
    {
        if (reader.TryReadNil())
        {
        }
        else
        {
            var len = reader.ReadArrayHeader();
            value = new Int128[len];
            for (int i = 0; i < value.Length; i++)
            {
                value[i] = reader.ReadInt128();
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void SerializeInt128List(ref TinyhandWriter writer, List<Int128>? value)
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
    public static void DeserializeInt128List(ref TinyhandReader reader, ref List<Int128>? value)
    {
        if (reader.TryReadNil())
        {
        }
        else
        {
            var len = reader.ReadArrayHeader();
            value ??= new List<Int128>(len);
            for (int i = 0; i < len; i++)
            {
                value.Add(reader.ReadInt128());
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Int128[]? CloneInt128Array(Int128[]? value)
    {
        if (value == null)
        {
            return null;
        }
        else
        {
            var array = new Int128[value.Length];
            Array.Copy(value, array, value.Length);
            return array;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void SerializeUInt128Array(ref TinyhandWriter writer, UInt128[]? value)
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
    public static void DeserializeUInt128Array(ref TinyhandReader reader, ref UInt128[]? value)
    {
        if (reader.TryReadNil())
        {
        }
        else
        {
            var len = reader.ReadArrayHeader();
            value = new UInt128[len];
            for (int i = 0; i < value.Length; i++)
            {
                value[i] = reader.ReadUInt128();
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void SerializeUInt128List(ref TinyhandWriter writer, List<UInt128>? value)
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
    public static void DeserializeUInt128List(ref TinyhandReader reader, ref List<UInt128>? value)
    {
        if (reader.TryReadNil())
        {
        }
        else
        {
            var len = reader.ReadArrayHeader();
            value ??= new List<UInt128>(len);
            for (int i = 0; i < len; i++)
            {
                value.Add(reader.ReadUInt128());
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static UInt128[]? CloneUInt128Array(UInt128[]? value)
    {
        if (value == null)
        {
            return null;
        }
        else
        {
            var array = new UInt128[value.Length];
            Array.Copy(value, array, value.Length);
            return array;
        }
    }
}
