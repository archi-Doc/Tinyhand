// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Runtime.InteropServices;
using Tinyhand.IO;

#pragma warning disable SA1121 // Use built-in type alias

namespace Tinyhand.Formatters;

public sealed class NativeGuidFormatter : ITinyhandFormatter<Guid>
{
    /// <summary>
    /// Unsafe binary Guid formatter. this is only allowed on LittleEndian environment.
    /// </summary>
    public static readonly ITinyhandFormatter<Guid> Instance = new NativeGuidFormatter();

    private NativeGuidFormatter()
    {
    }

    /* Guid's underlying _a,...,_k field is sequential and same layout as .NET Framework and Mono(Unity).
     * But target machines must be same endian so restrict only for little endian. */

    public unsafe void Serialize(ref TinyhandWriter writer, Guid value, TinyhandSerializerOptions options)
    {
        if (!BitConverter.IsLittleEndian)
        {
            throw new InvalidOperationException("NativeGuidFormatter only allows on little endian env.");
        }

        var valueSpan = new ReadOnlySpan<byte>(&value, sizeof(Guid));
        writer.Write(valueSpan);
    }

    public unsafe void Deserialize(ref TinyhandReader reader, ref Guid value, TinyhandSerializerOptions options)
    {
        if (!BitConverter.IsLittleEndian)
        {
            throw new InvalidOperationException("NativeGuidFormatter only allows on little endian env.");
        }

        reader.TryReadBytes(out var span);
        if (span.Length != sizeof(Guid))
        {
            throw new TinyhandException("Invalid Guid Size.");
        }

        span.CopyTo(MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref value, 1)));
    }

    public Guid Reconstruct(TinyhandSerializerOptions options)
    {
        return default;
    }

    public Guid Clone(Guid value, TinyhandSerializerOptions options) => value;
}

public sealed class NativeDecimalFormatter : ITinyhandFormatter<Decimal>
{
    /// <summary>
    /// Unsafe binary Decimal formatter. this is only allows on LittleEndian environment.
    /// </summary>
    public static readonly ITinyhandFormatter<Decimal> Instance = new NativeDecimalFormatter();

    private NativeDecimalFormatter()
    {
    }

    /* decimal underlying "flags, hi, lo, mid" fields are sequential and same layuout with .NET Framework and Mono(Unity)
     * But target machines must be same endian so restrict only for little endian. */

    public unsafe void Serialize(ref TinyhandWriter writer, Decimal value, TinyhandSerializerOptions options)
    {
        if (!BitConverter.IsLittleEndian)
        {
            throw new InvalidOperationException("NativeDecimalFormatter only allows on little endian env.");
        }

        var valueSpan = new ReadOnlySpan<byte>(&value, sizeof(Decimal));
        writer.Write(valueSpan);
    }

    public unsafe void Deserialize(ref TinyhandReader reader, ref Decimal value, TinyhandSerializerOptions options)
    {
        if (!BitConverter.IsLittleEndian)
        {
            throw new InvalidOperationException("NativeDecimalFormatter only allows on little endian env.");
        }

        reader.TryReadBytes(out var span);
        if (span.Length != sizeof(decimal))
        {
            throw new TinyhandException("Invalid decimal Size.");
        }

        span.CopyTo(MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref value, 1)));
    }

    public Decimal Reconstruct(TinyhandSerializerOptions options)
    {
        return default;
    }

    public Decimal Clone(Decimal value, TinyhandSerializerOptions options) => value;
}
