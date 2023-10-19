// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Linq;
using System.Text;

namespace Tinyhand;

/// <summary>
///  An experimental structure for representing a utf8 string in a type distinct from a byte sequence.
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
        this.Value = new byte[utf8.Value.Length];
        Array.Copy(utf8.Value, this.Value, utf8.Value.Length);
    }

    public Utf8String(ReadOnlySpan<byte> utf8)
    {
        this.Value = utf8.ToArray();
    }

    public readonly byte[] Value;

    public bool Equals(Utf8String other)
        => this.Value.SequenceEqual(other.Value);

    public unsafe override int GetHashCode()
    {// (int)FarmHash.Hash64(this.Value);
        var length = this.Value.Length;
        if (length == 0)
        {
            return HashCode.Combine(length);
        }
        else if (length == 1)
        {
            int i = this.Value[0];
            return HashCode.Combine(length, i);
        }
        else if (length == 2)
        {
            int i = (this.Value[1] << 8) | this.Value[0];
            return HashCode.Combine(length, i);
        }
        else if (length == 3)
        {
            int i = (this.Value[2] << 16) | (this.Value[1] << 8) | this.Value[0];
            return HashCode.Combine(length, i);
        }
        else
        {
            fixed (byte* b = this.Value)
            {
                int* first = (int*)b;
                int* last = (int*)(b + length - 4);

                return HashCode.Combine(length, first[0], last[0]);
            }
        }
    }

    public override string ToString()
    {
        try
        {
            return Encoding.UTF8.GetString(this.Value);
        }
        catch
        {
            return string.Empty;
        }
    }
}
