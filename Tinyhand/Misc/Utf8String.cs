// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Tinyhand;

/// <summary>
/// Represents UTF-8 bytes as a distinct string value with bytewise equality.
/// </summary>
public readonly struct Utf8String : IEquatable<Utf8String>
{
    public Utf8String()
    {
        this.Value = Array.Empty<byte>();
    }

    public Utf8String(byte[] utf8)
    {
        this.Value = utf8;
    }

    public Utf8String(Utf8String utf8)
    {
        this.Value = utf8.Span.ToArray();
    }

    public Utf8String(ReadOnlySpan<byte> utf8)
    {
        this.Value = utf8.ToArray();
    }

    public readonly byte[] Value;

    /// <summary>
    /// Gets the utf8 sequence. A <see langword="default"/> instance is treated as an empty sequence.
    /// </summary>
    public ReadOnlySpan<byte> Span => this.Value; // A null array becomes an empty span.

    public static bool operator ==(Utf8String left, Utf8String right) => left.Equals(right);

    public static bool operator !=(Utf8String left, Utf8String right) => !left.Equals(right);

    public bool Equals(Utf8String other)
        => this.Span.SequenceEqual(other.Span);

    public override bool Equals(object? obj)
        => obj is Utf8String other && this.Equals(other);

    public override int GetHashCode()
    {// (int)FarmHash.Hash64(this.Value);
        var span = this.Span;
        var length = span.Length;
        if (length == 0)
        {
            return HashCode.Combine(length);
        }
        else if (length == 1)
        {
            int i = span[0];
            return HashCode.Combine(length, i);
        }
        else if (length == 2)
        {
            int i = (span[1] << 8) | span[0];
            return HashCode.Combine(length, i);
        }
        else if (length == 3)
        {
            int i = (span[2] << 16) | (span[1] << 8) | span[0];
            return HashCode.Combine(length, i);
        }
        else
        {
            ref var b = ref MemoryMarshal.GetReference(span);
            var first = Unsafe.ReadUnaligned<int>(ref b);
            var last = Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref b, length - 4));

            return HashCode.Combine(length, first, last);
        }
    }

    public override string ToString()
    {
        try
        {
            return Encoding.UTF8.GetString(this.Span);
        }
        catch
        {
            return string.Empty;
        }
    }
}
