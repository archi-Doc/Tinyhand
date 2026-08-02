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
    private readonly struct Table
    {
        public readonly Func<PrimitiveValue, string> toString;
        public readonly Func<PrimitiveValue, int> getHashCode;

        public Table(Func<PrimitiveValue, string> toString, Func<PrimitiveValue, int> getHashCode)
        {
            this.toString = toString;
            this.getHashCode = getHashCode;
        }
    }

    private static readonly Table[] TagTable;

    static PrimitiveValue()
    {

        TagTable = new Table[(int)PrimitiveValueKind.F64 + 1];
        TagTable[0] = new(v => string.Empty, v => 0); // Invalid
        TagTable[1] = new(v => v.Bool.ToString(), v => v.Bool.GetHashCode()); // String
        TagTable[2] = new(v => v.Bool.ToString(), v => v.Bool.GetHashCode()); // Bool
        TagTable[3] = new(v => v.I128.ToString(), v => v.I128.GetHashCode()); // Integer
        TagTable[4] = new(v => v.I8.ToString(), v => v.I8.GetHashCode()); // I8
        TagTable[5] = new(v => v.I16.ToString(), v => v.I16.GetHashCode()); // I16
        TagTable[6] = new(v => v.I32.ToString(), v => v.I32.GetHashCode()); // I32
        TagTable[7] = new(v => v.I64.ToString(), v => v.I64.GetHashCode()); // I64
        TagTable[8] = new(v => v.I128.ToString(), v => v.I128.GetHashCode()); // I128
        TagTable[9] = new(v => v.I128.ToString(), v => v.I128.GetHashCode()); // Isize
        TagTable[10] = new(v => v.U8.ToString(), v => v.U8.GetHashCode()); // U8
        TagTable[11] = new(v => v.U16.ToString(), v => v.U16.GetHashCode()); // U16
        TagTable[12] = new(v => v.U32.ToString(), v => v.U32.GetHashCode()); // U32
        TagTable[13] = new(v => v.U64.ToString(), v => v.U64.GetHashCode()); // U64
        TagTable[14] = new(v => v.U128.ToString(), v => v.U128.GetHashCode()); // U128
        TagTable[15] = new(v => v.U128.ToString(), v => v.U128.GetHashCode()); // Usize
        TagTable[16] = new(v => v.F64.ToString(), v => v.F64.GetHashCode()); // Floag
        TagTable[17] = new(v => v.F32.ToString(), v => v.F32.GetHashCode()); // F32
        TagTable[18] = new(v => v.F64.ToString(), v => v.F64.GetHashCode()); // F64
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
        else if (tag == (int)PrimitiveValueKind.F64)
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
        => this.GetTable().toString(this);

    public override int GetHashCode()
    {
        if (this.tagOrString is string st)
        {
            return st.GetHashCode();
        }
        else
        {
            return this.I128.GetHashCode();
        }
    }

    public int GetHashCode2()
        => this.GetTable().getHashCode(this);

    public static bool operator ==(PrimitiveValue left, PrimitiveValue right)
        => left.Equals(right);

    public static bool operator !=(PrimitiveValue left, PrimitiveValue right)
        => !left.Equals(right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Table GetTable()
        => TagTable[TagObject.ToTag(this.tagOrString)];

    public static void Serialize(ref TinyhandWriter writer, scoped ref PrimitiveValue value, TinyhandSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    public static void Deserialize(ref TinyhandReader reader, scoped ref PrimitiveValue value, TinyhandSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    public static void Reconstruct([NotNull] scoped ref PrimitiveValue value, TinyhandSerializerOptions options)
    {
    }

    public static PrimitiveValue Clone(scoped ref PrimitiveValue value, TinyhandSerializerOptions options)
    {
        throw new NotImplementedException();
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
    /// A signed pointer-sized integer.
    /// </summary>
    Isize,

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
    /// An unsigned pointer-sized integer.
    /// </summary>
    USize,

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
