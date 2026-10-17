// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Tinyhand;

/// <summary>
/// SipHash-2-4, specialized for the 8- and 16-byte keys supported by <see cref="TinyhandSecurity"/>.
/// </summary>
/// <remarks>
/// Follows the authors' reference algorithm: https://github.com/veorq/SipHash/blob/master/siphash.c.
/// A process-local random 128-bit key prevents precomputed hash-flooding inputs. This is only a
/// collection hash; its truncated output must not be used for authentication or persisted.
/// </remarks>
internal readonly struct SipHash
{
    private static readonly SipHash ProcessHasher = CreateRandom();

    private readonly ulong initial0;
    private readonly ulong initial1;
    private readonly ulong initial2;
    private readonly ulong initial3;

    internal SipHash(ulong key0, ulong key1)
    {
        this.initial0 = 0x736f6d6570736575UL ^ key0;
        this.initial1 = 0x646f72616e646f6dUL ^ key1;
        this.initial2 = 0x6c7967656e657261UL ^ key0;
        this.initial3 = 0x7465646279746573UL ^ key1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int Hash(ulong value) => unchecked((int)ProcessHasher.Hash64(value));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int Hash(ulong first, ulong second) => unchecked((int)ProcessHasher.Hash64(first, second));

    // The words represent little-endian message bytes, as in the reference algorithm.
    internal ulong Hash64(ulong value)
    {
        var v0 = this.initial0;
        var v1 = this.initial1;
        var v2 = this.initial2;
        var v3 = this.initial3;
        Compress(value, ref v0, ref v1, ref v2, ref v3);
        return Finalize(8UL << 56, ref v0, ref v1, ref v2, ref v3);
    }

    internal ulong Hash64(ulong first, ulong second)
    {
        var v0 = this.initial0;
        var v1 = this.initial1;
        var v2 = this.initial2;
        var v3 = this.initial3;
        Compress(first, ref v0, ref v1, ref v2, ref v3);
        Compress(second, ref v0, ref v1, ref v2, ref v3);
        return Finalize(16UL << 56, ref v0, ref v1, ref v2, ref v3);
    }

    private static SipHash CreateRandom()
    {
        Span<byte> key = stackalloc byte[16];
        RandomNumberGenerator.Fill(key);
        return new SipHash(BinaryPrimitives.ReadUInt64LittleEndian(key), BinaryPrimitives.ReadUInt64LittleEndian(key[8..]));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Compress(ulong value, ref ulong v0, ref ulong v1, ref ulong v2, ref ulong v3)
    {
        v3 ^= value;
        Round(ref v0, ref v1, ref v2, ref v3);
        Round(ref v0, ref v1, ref v2, ref v3);
        v0 ^= value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong Finalize(ulong lengthBlock, ref ulong v0, ref ulong v1, ref ulong v2, ref ulong v3)
    {
        Compress(lengthBlock, ref v0, ref v1, ref v2, ref v3);
        v2 ^= 0xff;
        Round(ref v0, ref v1, ref v2, ref v3);
        Round(ref v0, ref v1, ref v2, ref v3);
        Round(ref v0, ref v1, ref v2, ref v3);
        Round(ref v0, ref v1, ref v2, ref v3);
        return v0 ^ v1 ^ v2 ^ v3;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Round(ref ulong v0, ref ulong v1, ref ulong v2, ref ulong v3)
    {
        unchecked
        {
            v0 += v1;
            v1 = BitOperations.RotateLeft(v1, 13);
            v1 ^= v0;
            v0 = BitOperations.RotateLeft(v0, 32);
            v2 += v3;
            v3 = BitOperations.RotateLeft(v3, 16);
            v3 ^= v2;
            v0 += v3;
            v3 = BitOperations.RotateLeft(v3, 21);
            v3 ^= v0;
            v2 += v1;
            v1 = BitOperations.RotateLeft(v1, 17);
            v1 ^= v2;
            v2 = BitOperations.RotateLeft(v2, 32);
        }
    }
}
