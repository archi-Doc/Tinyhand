// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Tinyhand;

#pragma warning disable SA1124 // Do not use regions

[StructLayout(LayoutKind.Explicit)]
public readonly partial struct Struct256 : IEquatable<Struct256>, IComparable<Struct256>
{
    public const string Name = "Struct256";
    public const int Length = 32;

    public static readonly Struct256 Zero = default;

    public static readonly Struct256 One = new(1);

    public static readonly Struct256 Two = new(2);

    public static readonly Struct256 Three = new(3);

    #region FieldAndProperty

    [FieldOffset(0)]
    public readonly long Long0;
    [FieldOffset(8)]
    public readonly long Long1;
    [FieldOffset(16)]
    public readonly long Long2;
    [FieldOffset(24)]
    public readonly long Long3;

    [FieldOffset(0)]
    public readonly int Int0;
    [FieldOffset(4)]
    public readonly int Int1;
    [FieldOffset(8)]
    public readonly int Int2;
    [FieldOffset(12)]
    public readonly int Int3;
    [FieldOffset(16)]
    public readonly int Int4;
    [FieldOffset(20)]
    public readonly int Int5;
    [FieldOffset(24)]
    public readonly int Int6;
    [FieldOffset(28)]
    public readonly int Int7;

    #endregion

    public Struct256(int int0)
    {
        this.Long0 = (int)int0;
        this.Long1 = 0;
        this.Long2 = 0;
        this.Long3 = 0;
    }

    public Struct256(long long0)
    {
        this.Long0 = long0;
        this.Long1 = 0;
        this.Long2 = 0;
        this.Long3 = 0;
    }

    public Struct256(long long0, long long1, long long2, long long3)
    {
        this.Long0 = long0;
        this.Long1 = long1;
        this.Long2 = long2;
        this.Long3 = long3;
    }

    public Struct256(ref Struct256 struct128)
    {
        // struct128.AsSpan().CopyTo(this.UnsafeAsSpan());
        this.Long0 = struct128.Long0;
        this.Long1 = struct128.Long1;
        this.Long2 = struct128.Long2;
        this.Long3 = struct128.Long3;
    }

    public Struct256(ReadOnlySpan<byte> span)
    {
        if (span.Length < Length)
        {
            throw new ArgumentException($"Length of a byte array must be at least {Length}");
        }

        this.Long0 = BitConverter.ToInt64(span);
        span = span.Slice(8);
        this.Long1 = BitConverter.ToInt64(span);
        span = span.Slice(8);
        this.Long2 = BitConverter.ToInt64(span);
        span = span.Slice(8);
        this.Long3 = BitConverter.ToInt64(span);
    }

    public bool TryWriteBytes(Span<byte> destination)
    {
        if (destination.Length < Length)
        {
            throw new ArgumentException($"Length of a byte array must be at least {Length}");
        }

        var d = destination;
        BitConverter.TryWriteBytes(d, this.Long0);
        d = d.Slice(8);
        BitConverter.TryWriteBytes(d, this.Long1);
        d = d.Slice(8);
        BitConverter.TryWriteBytes(d, this.Long2);
        d = d.Slice(8);
        BitConverter.TryWriteBytes(d, this.Long3);
        return true;
    }

    [UnscopedRef]
    public ReadOnlySpan<byte> AsSpan()
        => MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref Unsafe.AsRef(in this), 1));

    public bool IsZero
        => this.Long0 == 0 && this.Long1 == 0 && this.Long2 == 0 && this.Long3 == 0;

    public bool Equals(Struct256 other)
        => this.Long0 == other.Long0 && this.Long1 == other.Long1 && this.Long2 == other.Long2 && this.Long3 == other.Long3;

    public override bool Equals(object? obj)
        => obj is Struct256 other && this.Equals(other);

    public override int GetHashCode()
    {
        // return (int)Arc.Crypto.XxHash3.Hash64(this.AsSpan());
        // return (((((((((((((this.Int0 * 397) ^ this.Int1) * 397) ^ this.Int2) * 397) ^ this.Int3) * 397) ^ this.Int4) * 397) ^ this.Int5) * 397) ^ this.Int6) * 397) ^ this.Int7; // Fast, but...
        return HashCode.Combine(this.Long0, this.Long1, this.Long2, this.Long3);
    }

    public override string ToString()
    {
        if (this.Long1 == 0 && this.Long2 == 0 && this.Long3 == 0)
        {
            if (this.Long0 == 0)
            {
                return $"{Name}: 0";
            }
            else if (this.Long0 == 1)
            {
                return $"{Name}: 1";
            }
            else if (this.Long0 == 2)
            {
                return $"{Name}: 2";
            }
        }

        return $"{Name}: {this.Long0:D4}";
    }

    public int CompareTo(Struct256 other)
    {
        if (this.Long0 > other.Long0)
        {
            return 1;
        }
        else if (this.Long0 < other.Long0)
        {
            return -1;
        }

        if (this.Long1 > other.Long1)
        {
            return 1;
        }
        else if (this.Long1 < other.Long1)
        {
            return -1;
        }

        if (this.Long2 > other.Long2)
        {
            return 1;
        }
        else if (this.Long2 < other.Long2)
        {
            return -1;
        }

        if (this.Long3 > other.Long3)
        {
            return 1;
        }
        else if (this.Long3 < other.Long3)
        {
            return -1;
        }

        return 0;
    }

    public static bool operator ==(Struct256 left, Struct256 right)
        => left.Equals(right);

    public static bool operator !=(Struct256 left, Struct256 right)
        => !left.Equals(right);

    /*[UnscopedRef]
    private Span<byte> UnsafeAsSpan()
        => MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref Unsafe.AsRef(in this), 1));*/
}
