// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Tinyhand;

namespace Sandbox;

[TinyhandObject]
public readonly partial struct PublicKey : IEquatable<PublicKey>
{
    public const int PublicKeyLength = 32;

    public PublicKey(byte keyType, byte[] x)
    {
        this.keyType = keyType;
        this.x = x;
    }

    [Key(0)]
    private readonly byte keyType;

    [Key(1)]
    private readonly byte[] x = Array.Empty<byte>();

    public uint KeyType => (uint)(this.keyType & ~1);

    public uint YTilde => (uint)(this.keyType & 1);

    public void Test(byte[] x)
    {
        Unsafe.AsRef(this.x) = x;
    }

    public bool Validate()
    {
        if (this.KeyType == 0)
        {// secp256r1
            if (this.x.Length == PublicKeyLength)
            {
                return true;
            }
        }

        return false;
    }

    public override int GetHashCode()
    {
        if (this.x.Length >= sizeof(int))
        {
            return BitConverter.ToInt32(this.x.AsSpan(1));
        }
        else
        {
            return 0;
        }
    }

    public bool Equals(PublicKey other)
        => this.x.SequenceEqual(other.x);
}
