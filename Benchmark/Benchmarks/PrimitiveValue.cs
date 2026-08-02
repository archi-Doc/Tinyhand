// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using Tinyhand.IO;

namespace Tinyhand;

/*public interface ITagObject
{
    int Tag { get; }
}*/

public sealed class TagObject
{
    public const int MaxTag = 256;

    private static readonly TagObject[] tagObjects;

    static TagObject()
    {
        tagObjects = new TagObject[MaxTag];
        for (var i = 0; i < MaxTag; i++)
        {
            tagObjects[i] = new(i);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TagObject FromTag(int tag)
        => tagObjects[tag];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ToTag(object? obj)
        => obj is TagObject tagObject ? tagObject.Tag : -1;

    public int Tag { get; private set; }

    private TagObject(int tag)
    {
        this.Tag = tag;
    }
}

[TinyhandObject]
[StructLayout(LayoutKind.Explicit)]
public readonly partial struct PrimitiveValue : ITinyhandSerializable<PrimitiveValue>, ITinyhandReconstructable<PrimitiveValue>, ITinyhandCloneable<PrimitiveValue>
{// 32
    private delegate void SerializeAction(ref TinyhandWriter writer, PrimitiveValue value);

    private static readonly Func<PrimitiveValue, string>[] toStringTable;
    private static readonly Func<PrimitiveValue, int>[] getHashCodeTable;
    private static readonly SerializeAction[] serializeTable;

    static PrimitiveValue()
    {
        var length = (int)PrimitiveValueKind.F64 + 1;
        getHashCodeTable = new Func<PrimitiveValue, int>[length];
        getHashCodeTable[0] = static v => 0; // Invalid
        getHashCodeTable[1] = static v => v.String.GetHashCode(); // String
        getHashCodeTable[2] = static v => v.Bool.GetHashCode(); // Bool
        getHashCodeTable[3] = static v => v.I128.GetHashCode(); // Integer
        getHashCodeTable[4] = static v => v.I8.GetHashCode(); // I8
        getHashCodeTable[5] = static v => v.I16.GetHashCode(); // I16
        getHashCodeTable[6] = static v => v.I32.GetHashCode(); // I32
        getHashCodeTable[7] = static v => v.I64.GetHashCode(); // I64
        getHashCodeTable[8] = static v => v.I128.GetHashCode(); // I128
        getHashCodeTable[9] = static v => v.U8.GetHashCode(); // U8
        getHashCodeTable[10] = static v => v.U16.GetHashCode(); // U16
        getHashCodeTable[11] = static v => v.U32.GetHashCode(); // U32
        getHashCodeTable[12] = static v => v.U64.GetHashCode(); // U64
        getHashCodeTable[13] = static v => v.U128.GetHashCode(); // U128
        getHashCodeTable[14] = static v => v.F64.GetHashCode(); // Float
        getHashCodeTable[15] = static v => v.F32.GetHashCode(); // F32
        getHashCodeTable[16] = static v => v.F64.GetHashCode(); // F64

        toStringTable = new Func<PrimitiveValue, string>[length];
        toStringTable[0] = static v => string.Empty; // Invalid
        toStringTable[1] = static v => v.String; // String
        toStringTable[2] = static v => v.Bool.ToString(); // Bool
        toStringTable[3] = static v => v.I128.ToString(); // Integer
        toStringTable[4] = static v => v.I8.ToString(); // I8
        toStringTable[5] = static v => v.I16.ToString(); // I16
        toStringTable[6] = static v => v.I32.ToString(); // I32
        toStringTable[7] = static v => v.I64.ToString(); // I64
        toStringTable[8] = static v => v.I128.ToString(); // I128
        toStringTable[9] = static v => v.U8.ToString(); // U8
        toStringTable[10] = static v => v.U16.ToString(); // U16
        toStringTable[11] = static v => v.U32.ToString(); // U32
        toStringTable[12] = static v => v.U64.ToString(); // U64
        toStringTable[13] = static v => v.U128.ToString(); // U128
        toStringTable[14] = static v => v.F64.ToString(); // Float
        toStringTable[15] = static v => v.F32.ToString(); // F32
        toStringTable[16] = static v => v.F64.ToString(); // F64

        serializeTable = new SerializeAction[length];
        serializeTable[0] = static (ref x, v) => x.WriteNil(); // Invalid
        serializeTable[1] = static (ref x, v) => x.Write(v.String); // String
        serializeTable[2] = static (ref x, v) => x.Write(v.Bool); // Bool
        serializeTable[3] = static (ref x, v) => x.Write(v.I128); // Integer
        serializeTable[4] = static (ref x, v) => x.Write(v.I8); // I8
        serializeTable[5] = static (ref x, v) => x.Write(v.I16); // I16
        serializeTable[6] = static (ref x, v) => x.Write(v.I32); // I32
        serializeTable[7] = static (ref x, v) => x.Write(v.I64); // I64
        serializeTable[8] = static (ref x, v) => x.Write(v.I128); // I128
        serializeTable[9] = static (ref x, v) => x.Write(v.U8); // U8
        serializeTable[10] = static (ref x, v) => x.Write(v.U16); // U16
        serializeTable[11] = static (ref x, v) => x.Write(v.U32); // U32
        serializeTable[12] = static (ref x, v) => x.Write(v.U64); // U64
        serializeTable[13] = static (ref x, v) => x.Write(v.U128); // U128
        serializeTable[14] = static (ref x, v) => x.Write(v.F64); // Float
        serializeTable[15] = static (ref x, v) => x.Write(v.F32); // F32
        serializeTable[16] = static (ref x, v) => x.Write(v.F64); // F64
    }

    [FieldOffset(0)]
    private readonly object? tagOrString;

    [FieldOffset(8)]
    public readonly bool Bool;

    [FieldOffset(8)]
    public readonly sbyte I8;

    [FieldOffset(8)]
    public readonly short I16;

    [FieldOffset(8)]
    public readonly int I32;

    [FieldOffset(8)]
    public readonly long I64;

    [FieldOffset(8)]
    public readonly Int128 I128;

    [FieldOffset(8)]
    public readonly byte U8;

    [FieldOffset(8)]
    public readonly ushort U16;

    [FieldOffset(8)]
    public readonly uint U32;

    [FieldOffset(8)]
    public readonly ulong U64;

    [FieldOffset(8)]
    public readonly UInt128 U128;

    [FieldOffset(8)]
    public readonly float F32;

    [FieldOffset(8)]
    public readonly double F64;

    public string String => (this.tagOrString as string) ?? string.Empty;

    public PrimitiveValueKind Kind
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if (this.tagOrString is TagObject tagObject)
            {
                return (PrimitiveValueKind)tagObject.Tag;
            }
            else if (this.tagOrString is string)
            {
                return PrimitiveValueKind.String;
            }

            return PrimitiveValueKind.Invalid;
        }
    }

    public PrimitiveValue(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        this.tagOrString = value;
    }

    public PrimitiveValue(bool value)
    {
        this.tagOrString = TagObject.FromTag((int)PrimitiveValueKind.Bool);
        this.Bool = value;
    }

    public PrimitiveValue(sbyte value)
    {
        this.tagOrString = TagObject.FromTag((int)PrimitiveValueKind.I8);
        this.I8 = value;
    }

    public PrimitiveValue(short value)
    {
        this.tagOrString = TagObject.FromTag((int)PrimitiveValueKind.I16);
        this.I16 = value;
    }

    public PrimitiveValue(int value)
    {
        this.tagOrString = TagObject.FromTag((int)PrimitiveValueKind.I32);
        this.I32 = value;
    }

    public PrimitiveValue(long value)
    {
        this.tagOrString = TagObject.FromTag((int)PrimitiveValueKind.I64);
        this.I64 = value;
    }

    public PrimitiveValue(Int128 value)
    {
        this.tagOrString = TagObject.FromTag((int)PrimitiveValueKind.I128);
        this.I128 = value;
    }

    public PrimitiveValue(byte value)
    {
        this.tagOrString = TagObject.FromTag((int)PrimitiveValueKind.U8);
        this.U8 = value;
    }

    public PrimitiveValue(ushort value)
    {
        this.tagOrString = TagObject.FromTag((int)PrimitiveValueKind.U16);
        this.U16 = value;
    }

    public PrimitiveValue(uint value)
    {
        this.tagOrString = TagObject.FromTag((int)PrimitiveValueKind.U32);
        this.U32 = value;
    }

    public PrimitiveValue(ulong value)
    {
        this.tagOrString = TagObject.FromTag((int)PrimitiveValueKind.U64);
        this.U64 = value;
    }

    public PrimitiveValue(UInt128 value)
    {
        this.tagOrString = TagObject.FromTag((int)PrimitiveValueKind.U128);
        this.U128 = value;
    }

    public PrimitiveValue(float value)
    {
        this.tagOrString = TagObject.FromTag((int)PrimitiveValueKind.F32);
        this.F32 = value;
    }

    public PrimitiveValue(double value)
    {
        this.tagOrString = TagObject.FromTag((int)PrimitiveValueKind.F64);
        this.F64 = value;
    }

    public PrimitiveValue(PrimitiveValue value)
    {
        this.tagOrString = value.tagOrString;
        this.I128 = value.I128;
    }

    public bool Equals(PrimitiveValue other)
    {
        var tag = TagObject.ToTag(this.tagOrString);
        var tag2 = TagObject.ToTag(other.tagOrString);
        if (tag != tag2)
        {
            return false;
        }

        if (tag == (int)PrimitiveValueKind.F32)
        {
            return float.Equals(this.F32, other.F32);
        }
        else if (tag == (int)PrimitiveValueKind.Float ||
            tag == (int)PrimitiveValueKind.F64)
        {
            return double.Equals(this.F64, other.F64);
        }
        else
        {
            return this.I128 == other.I128;
        }
    }

    public override bool Equals(object? obj)
        => obj is PrimitiveValue other && this.Equals(other);

    public override string ToString()
        => toStringTable[TagObject.ToTag(this.tagOrString)](this);

    public override int GetHashCode()
    {
        return getHashCodeTable[TagObject.ToTag(this.tagOrString)](this);

        // return this.GetTable().getHashCode(this);

        /*if (this.tagOrString is string st)
        {
            return st.GetHashCode();
        }
        else
        {
            return this.I128.GetHashCode();
        }*/
    }

    public static bool operator ==(PrimitiveValue left, PrimitiveValue right)
        => left.Equals(right);

    public static bool operator !=(PrimitiveValue left, PrimitiveValue right)
        => !left.Equals(right);

    public static void Serialize(ref TinyhandWriter writer, scoped ref PrimitiveValue value, TinyhandSerializerOptions options)
    {
        serializeTable[TagObject.ToTag(value.tagOrString)](ref writer, value);
    }

    public static void Deserialize(ref TinyhandReader reader, scoped ref PrimitiveValue value, TinyhandSerializerOptions options)
    {
        reader.TryRead(out byte code);
        switch (code)
        {
            case MessagePackCode.Int8:
                {
                    reader.TryRead(out sbyte v);
                    value = new(v);
                }
                break;

            case MessagePackCode.Int16:
                {
                    reader.TryReadBigEndian(out short v);
                    value = new(v);
                }
                break;

            case MessagePackCode.Int32:
                {
                    reader.TryReadBigEndian(out int v);
                    value = new(v);
                }
                break;

            case MessagePackCode.Int64:
                {
                    reader.TryReadBigEndian(out long v);
                    value = new(v);
                }
                break;

            case MessagePackExtensionCodes.Int128:
                {
                    reader.TryReadBigEndian(out Int128 v);
                    value = new(v);
                }
                break;
        }
    }

    public static void Reconstruct([NotNull] scoped ref PrimitiveValue value, TinyhandSerializerOptions options)
    {
    }

    public unsafe static PrimitiveValue Clone(scoped ref PrimitiveValue value, TinyhandSerializerOptions options)
    {
        return new(value);
    }
}

/// <summary>
/// Represents the kind of primitive value.
/// </summary>
public enum PrimitiveValueKind : byte
{
    /// <summary>
    /// The value is invalid or could not be classified.
    /// </summary>
    Invalid,

    /// <summary>
    /// A string value.
    /// </summary>
    String,

    Bool,

    /// <summary>
    /// An integer without an explicit type suffix.
    /// </summary>
    Integer,

    /// <summary>
    /// A signed 8-bit integer.
    /// </summary>
    I8,

    /// <summary>
    /// A signed 16-bit integer.
    /// </summary>
    I16,

    /// <summary>
    /// A signed 32-bit integer.
    /// </summary>
    I32,

    /// <summary>
    /// A signed 64-bit integer.
    /// </summary>
    I64,

    /// <summary>
    /// A signed 128-bit integer.
    /// </summary>
    I128,

    /// <summary>
    /// An unsigned 8-bit integer.
    /// </summary>
    U8,

    /// <summary>
    /// An unsigned 16-bit integer.
    /// </summary>
    U16,

    /// <summary>
    /// An unsigned 32-bit integer.
    /// </summary>
    U32,

    /// <summary>
    /// An unsigned 64-bit integer.
    /// </summary>
    U64,

    /// <summary>
    /// An unsigned 128-bit integer.
    /// </summary>
    U128,

    /// <summary>
    /// A floating-point without an explicit type suffix.
    /// </summary>
    Float,

    /// <summary>
    /// A 32-bit floating-point.
    /// </summary>
    F32,

    /// <summary>
    /// A 64-bit floating-point.
    /// </summary>
    F64,
}
