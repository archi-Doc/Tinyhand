// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;

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

/*[StructLayout(LayoutKind.Explicit)]
public readonly struct PrimitiveValue
{
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
            var tag = TagObject.ToTag(this.tagOrString);
            if (tag < 0)
            {
                return PrimitiveValueKind.String;
            }

            return (PrimitiveValueKind)tag;
            var tagOrString = this.tagOrString;

            if (tagOrString is null)
            {
                return LimitedValueKind.Bool;
            }
            else if (ReferenceEquals(tagOrString, I64Tag))
            {
                return LimitedValueKind.I64;
            }
            else if (ReferenceEquals(tagOrString, DoubleTag))
            {
                return LimitedValueKind.Double;
            }

            return LimitedValueKind.Text;
        }
    }

    public LimitedValue(bool value)
    {
        this.tagOrString = null;
        this.Bool = value;
    }

    public LimitedValue(long value)
    {
        this.tagOrString = I64Tag;
        this.I64 = value;
    }

    public LimitedValue(double value)
    {
        this.tagOrString = DoubleTag;
        this.F64 = value;
    }

    public LimitedValue(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        this.tagOrString = value;
    }

    public bool Equals(LimitedValue other)
    {
        var tagOrText = this.tagOrString;

        if (tagOrText is null)
        {
            return other.tagOrText is null && this.Bool == other.Bool;
        }
        else if (ReferenceEquals(tagOrText, I64Tag))
        {
            return ReferenceEquals(other.tagOrText, I64Tag) &&
                this.I64 == other.I64;
        }
        else if (ReferenceEquals(tagOrText, DoubleTag))
        {
            return ReferenceEquals(other.tagOrText, DoubleTag) &&
                this.F64.Equals(other.Double);
        }

        return other.tagOrText is string otherText &&
            string.Equals((string)tagOrText, otherText, StringComparison.Ordinal);
    }

    public override bool Equals(object? obj)
        => obj is LimitedValue other && this.Equals(other);

    public override int GetHashCode()
    {
        var tagOrText = this.tagOrString;

        if (tagOrText is null)
        {
            return HashCode.Combine(LimitedValueKind.Bool, this.Bool);
        }
        else if (ReferenceEquals(tagOrText, I64Tag))
        {
            return HashCode.Combine(LimitedValueKind.I64, this.I64);
        }
        else if (ReferenceEquals(tagOrText, DoubleTag))
        {
            return HashCode.Combine(LimitedValueKind.Double, this.F64);
        }

        return HashCode.Combine(LimitedValueKind.Text, StringComparer.Ordinal.GetHashCode((string)tagOrText));
    }

    public static bool operator ==(LimitedValue left, LimitedValue right)
        => left.Equals(right);

    public static bool operator !=(LimitedValue left, LimitedValue right)
        => !left.Equals(right);

    public override string ToString() => this.Kind switch
    {
        LimitedValueKind.Bool => this.Bool.ToString(),
        LimitedValueKind.I64 => this.I64.ToString(),
        LimitedValueKind.Double => this.F64.ToString(),
        LimitedValueKind.Text => this.String,
        _ => string.Empty,
    };
}

/// <summary>
/// Represents the kind of primitive value.
/// </summary>
public enum PrimitiveValueKind
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
    ISize,

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
}*/
