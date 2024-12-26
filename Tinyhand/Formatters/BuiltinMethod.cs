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
    public static byte[]? DeserializeUInt8Array(ref TinyhandReader reader)
    {
        if (reader.TryReadNil())
        {
            return null; // new byte[0];
        }
        else
        {
            var len = reader.ReadArrayHeader();
            var array = new byte[len];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = reader.ReadUInt8();
            }

            return array;
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
    public static List<byte>? DeserializeUInt8List(ref TinyhandReader reader)
    {
        if (reader.TryReadNil())
        {
            return null; // new List<byte>();
        }
        else
        {
            var len = reader.ReadArrayHeader();
            var list = new List<byte>(len);
            for (int i = 0; i < len; i++)
            {
                list.Add(reader.ReadUInt8());
            }

            return list;
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
    public static sbyte[]? DeserializeInt8Array(ref TinyhandReader reader)
    {
        if (reader.TryReadNil())
        {
            return null; // new sbyte[0];
        }
        else
        {
            var len = reader.ReadArrayHeader();
            var array = new sbyte[len];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = reader.ReadInt8();
            }

            return array;
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
    public static List<sbyte>? DeserializeInt8List(ref TinyhandReader reader)
    {
        if (reader.TryReadNil())
        {
            return null; // new List<sbyte>();
        }
        else
        {
            var len = reader.ReadArrayHeader();
            var list = new List<sbyte>(len);
            for (int i = 0; i < len; i++)
            {
                list.Add(reader.ReadInt8());
            }

            return list;
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
    public static ushort[]? DeserializeUInt16Array(ref TinyhandReader reader)
    {
        if (reader.TryReadNil())
        {
            return null; // new ushort[0];
        }
        else
        {
            var len = reader.ReadArrayHeader();
            var array = new ushort[len];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = reader.ReadUInt16();
            }

            return array;
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
    public static List<ushort>? DeserializeUInt16List(ref TinyhandReader reader)
    {
        if (reader.TryReadNil())
        {
            return null; // new List<ushort>();
        }
        else
        {
            var len = reader.ReadArrayHeader();
            var list = new List<ushort>(len);
            for (int i = 0; i < len; i++)
            {
                list.Add(reader.ReadUInt16());
            }

            return list;
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
    public static short[]? DeserializeInt16Array(ref TinyhandReader reader)
    {
        if (reader.TryReadNil())
        {
            return null; // new short[0];
        }
        else
        {
            var len = reader.ReadArrayHeader();
            var array = new short[len];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = reader.ReadInt16();
            }

            return array;
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
    public static List<short>? DeserializeInt16List(ref TinyhandReader reader)
    {
        if (reader.TryReadNil())
        {
            return null; // new List<short>();
        }
        else
        {
            var len = reader.ReadArrayHeader();
            var list = new List<short>(len);
            for (int i = 0; i < len; i++)
            {
                list.Add(reader.ReadInt16());
            }

            return list;
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
    public static uint[]? DeserializeUInt32Array(ref TinyhandReader reader)
    {
        if (reader.TryReadNil())
        {
            return null; // new uint[0];
        }
        else
        {
            var len = reader.ReadArrayHeader();
            var array = new uint[len];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = reader.ReadUInt32();
            }

            return array;
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
    public static List<uint>? DeserializeUInt32List(ref TinyhandReader reader)
    {
        if (reader.TryReadNil())
        {
            return null; // new List<uint>();
        }
        else
        {
            var len = reader.ReadArrayHeader();
            var list = new List<uint>(len);
            for (int i = 0; i < len; i++)
            {
                list.Add(reader.ReadUInt32());
            }

            return list;
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
    public static int[]? DeserializeInt32Array(ref TinyhandReader reader)
    {
        if (reader.TryReadNil())
        {
            return null; // new int[0];
        }
        else
        {
            var len = reader.ReadArrayHeader();
            var array = new int[len];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = reader.ReadInt32();
            }

            return array;
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
    public static List<int>? DeserializeInt32List(ref TinyhandReader reader)
    {
        if (reader.TryReadNil())
        {
            return null; // new List<int>();
        }
        else
        {
            var len = reader.ReadArrayHeader();
            var list = new List<int>(len);
            for (int i = 0; i < len; i++)
            {
                list.Add(reader.ReadInt32());
            }

            return list;
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
    public static ulong[]? DeserializeUInt64Array(ref TinyhandReader reader)
    {
        if (reader.TryReadNil())
        {
            return null; // new ulong[0];
        }
        else
        {
            var len = reader.ReadArrayHeader();
            var array = new ulong[len];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = reader.ReadUInt64();
            }

            return array;
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
    public static List<ulong>? DeserializeUInt64List(ref TinyhandReader reader)
    {
        if (reader.TryReadNil())
        {
            return null; // new List<ulong>();
        }
        else
        {
            var len = reader.ReadArrayHeader();
            var list = new List<ulong>(len);
            for (int i = 0; i < len; i++)
            {
                list.Add(reader.ReadUInt64());
            }

            return list;
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
    public static long[]? DeserializeInt64Array(ref TinyhandReader reader)
    {
        if (reader.TryReadNil())
        {
            return null; // new long[0];
        }
        else
        {
            var len = reader.ReadArrayHeader();
            var array = new long[len];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = reader.ReadInt64();
            }

            return array;
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
    public static List<long>? DeserializeInt64List(ref TinyhandReader reader)
    {
        if (reader.TryReadNil())
        {
            return null; // new List<long>();
        }
        else
        {
            var len = reader.ReadArrayHeader();
            var list = new List<long>(len);
            for (int i = 0; i < len; i++)
            {
                list.Add(reader.ReadInt64());
            }

            return list;
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
    public static float[]? DeserializeSingleArray(ref TinyhandReader reader)
    {
        if (reader.TryReadNil())
        {
            return null; // new float[0];
        }
        else
        {
            var len = reader.ReadArrayHeader();
            var array = new float[len];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = reader.ReadSingle();
            }

            return array;
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
    public static List<float>? DeserializeSingleList(ref TinyhandReader reader)
    {
        if (reader.TryReadNil())
        {
            return null; // new List<float>();
        }
        else
        {
            var len = reader.ReadArrayHeader();
            var list = new List<float>(len);
            for (int i = 0; i < len; i++)
            {
                list.Add(reader.ReadSingle());
            }

            return list;
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
    public static double[]? DeserializeDoubleArray(ref TinyhandReader reader)
    {
        if (reader.TryReadNil())
        {
            return null; // new double[0];
        }
        else
        {
            var len = reader.ReadArrayHeader();
            var array = new double[len];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = reader.ReadDouble();
            }

            return array;
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
    public static List<double>? DeserializeDoubleList(ref TinyhandReader reader)
    {
        if (reader.TryReadNil())
        {
            return null; // new List<double>();
        }
        else
        {
            var len = reader.ReadArrayHeader();
            var list = new List<double>(len);
            for (int i = 0; i < len; i++)
            {
                list.Add(reader.ReadDouble());
            }

            return list;
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
    public static bool[]? DeserializeBooleanArray(ref TinyhandReader reader)
    {
        if (reader.TryReadNil())
        {
            return null; // new bool[0];
        }
        else
        {
            var len = reader.ReadArrayHeader();
            var array = new bool[len];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = reader.ReadBoolean();
            }

            return array;
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
    public static List<bool>? DeserializeBooleanList(ref TinyhandReader reader)
    {
        if (reader.TryReadNil())
        {
            return null; // new List<bool>();
        }
        else
        {
            var len = reader.ReadArrayHeader();
            var list = new List<bool>(len);
            for (int i = 0; i < len; i++)
            {
                list.Add(reader.ReadBoolean());
            }

            return list;
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
    public static char[]? DeserializeCharArray(ref TinyhandReader reader)
    {
        if (reader.TryReadNil())
        {
            return null; // new char[0];
        }
        else
        {
            var len = reader.ReadArrayHeader();
            var array = new char[len];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = reader.ReadChar();
            }

            return array;
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
    public static List<char>? DeserializeCharList(ref TinyhandReader reader)
    {
        if (reader.TryReadNil())
        {
            return null; // new List<char>();
        }
        else
        {
            var len = reader.ReadArrayHeader();
            var list = new List<char>(len);
            for (int i = 0; i < len; i++)
            {
                list.Add(reader.ReadChar());
            }

            return list;
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
    public static DateTime[]? DeserializeDateTimeArray(ref TinyhandReader reader)
    {
        if (reader.TryReadNil())
        {
            return null; // new DateTime[0];
        }
        else
        {
            var len = reader.ReadArrayHeader();
            var array = new DateTime[len];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = reader.ReadDateTime();
            }

            return array;
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
    public static List<DateTime>? DeserializeDateTimeList(ref TinyhandReader reader)
    {
        if (reader.TryReadNil())
        {
            return null; // new List<DateTime>();
        }
        else
        {
            var len = reader.ReadArrayHeader();
            var list = new List<DateTime>(len);
            for (int i = 0; i < len; i++)
            {
                list.Add(reader.ReadDateTime());
            }

            return list;
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
    public static Int128[]? DeserializeInt128Array(ref TinyhandReader reader)
    {
        if (reader.TryReadNil())
        {
            return null; // new Int128[0];
        }
        else
        {
            var len = reader.ReadArrayHeader();
            var array = new Int128[len];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = reader.ReadInt128();
            }

            return array;
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
    public static List<Int128>? DeserializeInt128List(ref TinyhandReader reader)
    {
        if (reader.TryReadNil())
        {
            return null; // new List<Int128>();
        }
        else
        {
            var len = reader.ReadArrayHeader();
            var list = new List<Int128>(len);
            for (int i = 0; i < len; i++)
            {
                list.Add(reader.ReadInt128());
            }

            return list;
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
    public static UInt128[]? DeserializeUInt128Array(ref TinyhandReader reader)
    {
        if (reader.TryReadNil())
        {
            return null; // new UInt128[0];
        }
        else
        {
            var len = reader.ReadArrayHeader();
            var array = new UInt128[len];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = reader.ReadUInt128();
            }

            return array;
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
    public static List<UInt128>? DeserializeUInt128List(ref TinyhandReader reader)
    {
        if (reader.TryReadNil())
        {
            return null; // new List<UInt128>();
        }
        else
        {
            var len = reader.ReadArrayHeader();
            var list = new List<UInt128>(len);
            for (int i = 0; i < len; i++)
            {
                list.Add(reader.ReadUInt128());
            }

            return list;
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
