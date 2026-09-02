// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace Tinyhand.IO;

public ref partial struct TinyhandReader
{
    /// <summary>
    /// Reads a MessagePack integer as a <see cref="byte"/>.
    /// </summary>
    /// <returns>The decoded value.</returns>
    /// <exception cref="OverflowException">The value is outside the target type's range.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte ReadUInt8()
    {
        var code = this.ReadUnsafe<byte>();
        if (code <= MessagePackCode.MaxFixInt)
        {
            return code;
        }

        return this.ReadUInt8Slow(code);
    }

    /// <summary>
    /// Reads a MessagePack integer as a <see cref="sbyte"/>.
    /// </summary>
    /// <returns>The decoded value.</returns>
    /// <exception cref="OverflowException">The value is outside the target type's range.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public sbyte ReadInt8()
    {
        var code = this.ReadUnsafe<byte>();
        // A signed comparison recognizes both positive and negative fixints.
        var fixint = unchecked((sbyte)code);
        if (fixint >= MessagePackRange.MinFixNegativeInt)
        {
            return fixint;
        }

        return this.ReadInt8Slow(code);
    }

    /// <summary>
    /// Reads a MessagePack integer as a <see cref="ushort"/>.
    /// </summary>
    /// <returns>The decoded value.</returns>
    /// <exception cref="OverflowException">The value is outside the target type's range.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort ReadUInt16()
    {
        var code = this.ReadUnsafe<byte>();
        if (code <= MessagePackCode.MaxFixInt)
        {
            return code;
        }

        return this.ReadUInt16Slow(code);
    }

    /// <summary>
    /// Reads a MessagePack integer as a <see cref="short"/>.
    /// </summary>
    /// <returns>The decoded value.</returns>
    /// <exception cref="OverflowException">The value is outside the target type's range.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public short ReadInt16()
    {
        var code = this.ReadUnsafe<byte>();
        // A signed comparison recognizes both positive and negative fixints.
        var fixint = unchecked((sbyte)code);
        if (fixint >= MessagePackRange.MinFixNegativeInt)
        {
            return fixint;
        }

        return this.ReadInt16Slow(code);
    }

    /// <summary>
    /// Reads a MessagePack integer as a <see cref="uint"/>.
    /// </summary>
    /// <returns>The decoded value.</returns>
    /// <exception cref="OverflowException">The value is outside the target type's range.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint ReadUInt32()
    {
        var code = this.ReadUnsafe<byte>();
        if (code <= MessagePackCode.MaxFixInt)
        {
            return code;
        }

        return this.ReadUInt32Slow(code);
    }

    /// <summary>
    /// Reads a MessagePack integer as a <see cref="int"/>.
    /// </summary>
    /// <returns>The decoded value.</returns>
    /// <exception cref="OverflowException">The value is outside the target type's range.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReadInt32()
    {
        var code = this.ReadUnsafe<byte>();
        // A signed comparison recognizes both positive and negative fixints.
        var fixint = unchecked((sbyte)code);
        if (fixint >= MessagePackRange.MinFixNegativeInt)
        {
            return fixint;
        }

        return this.ReadInt32Slow(code);
    }

    /// <summary>
    /// Reads a MessagePack integer as a <see cref="ulong"/>.
    /// </summary>
    /// <returns>The decoded value.</returns>
    /// <exception cref="OverflowException">The value is outside the target type's range.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong ReadUInt64()
    {
        var code = this.ReadUnsafe<byte>();
        if (code <= MessagePackCode.MaxFixInt)
        {
            return code;
        }

        return this.ReadUInt64Slow(code);
    }

    /// <summary>
    /// Reads a MessagePack integer as a <see cref="long"/>.
    /// </summary>
    /// <returns>The decoded value.</returns>
    /// <exception cref="OverflowException">The value is outside the target type's range.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long ReadInt64()
    {
        var code = this.ReadUnsafe<byte>();
        // A signed comparison recognizes both positive and negative fixints.
        var fixint = unchecked((sbyte)code);
        if (fixint >= MessagePackRange.MinFixNegativeInt)
        {
            return fixint;
        }

        return this.ReadInt64Slow(code);
    }

    /// <summary>
    /// Tries to read an unsigned integer without advancing on failure.
    /// </summary>
    /// <param name="value">The value, or zero on failure.</param>
    /// <returns>Whether a complete, nonnegative integer was read.</returns>
    public bool TryReadUInt64(out ulong value)
    {
        var reader = this;
        value = default;
        if (!reader.TryRead(out byte code))
        {
            return false;
        }

        if (code <= MessagePackCode.MaxFixInt)
        {
            value = code;
        }
        else
        {
            switch (code)
            {
                case MessagePackCode.UInt8:
                    if (!reader.TryRead(out byte u8))
                    {
                        return false;
                    }

                    value = u8;
                    break;
                case MessagePackCode.UInt16:
                    if (!reader.TryReadBigEndian(out ushort u16))
                    {
                        return false;
                    }

                    value = u16;
                    break;
                case MessagePackCode.UInt32:
                    if (!reader.TryReadBigEndian(out uint u32))
                    {
                        return false;
                    }

                    value = u32;
                    break;
                case MessagePackCode.UInt64:
                    if (!reader.TryReadBigEndian(out value))
                    {
                        return false;
                    }

                    break;
                case MessagePackCode.Int8:
                    if (!reader.TryRead(out sbyte i8) || i8 < 0)
                    {
                        return false;
                    }

                    value = (ulong)i8;
                    break;
                case MessagePackCode.Int16:
                    if (!reader.TryReadBigEndian(out short i16) || i16 < 0)
                    {
                        return false;
                    }

                    value = (ulong)i16;
                    break;
                case MessagePackCode.Int32:
                    if (!reader.TryReadBigEndian(out int i32) || i32 < 0)
                    {
                        return false;
                    }

                    value = (ulong)i32;
                    break;
                case MessagePackCode.Int64:
                    if (!reader.TryReadBigEndian(out long i64) || i64 < 0)
                    {
                        return false;
                    }

                    value = (ulong)i64;
                    break;
                default:
                    return false;
            }
        }

        this = reader;
        return true;
    }

    private byte ReadUInt8Slow(byte code)
    {
        // The eight consecutive integer codes form a dense jump table.
        switch (code)
        {
            case MessagePackCode.UInt8:
                return checked((byte)this.ReadUnsafe<byte>());
            case MessagePackCode.UInt16:
                return checked((byte)this.ReadIntegerUInt16());
            case MessagePackCode.UInt32:
                return checked((byte)this.ReadIntegerUInt32());
            case MessagePackCode.UInt64:
                return checked((byte)this.ReadIntegerUInt64());
            case MessagePackCode.Int8:
                return checked((byte)this.ReadUnsafe<sbyte>());
            case MessagePackCode.Int16:
                return checked((byte)this.ReadIntegerInt16());
            case MessagePackCode.Int32:
                return checked((byte)this.ReadIntegerInt32());
            case MessagePackCode.Int64:
                return checked((byte)this.ReadIntegerInt64());
            default:
                if (code >= MessagePackCode.MinNegativeFixInt)
                {
                    throw new OverflowException();
                }

                throw ThrowInvalidCode(code, MessagePackType.Integer);
        }
    }

    private sbyte ReadInt8Slow(byte code)
    {
        // The eight consecutive integer codes form a dense jump table.
        switch (code)
        {
            case MessagePackCode.UInt8:
                return checked((sbyte)this.ReadUnsafe<byte>());
            case MessagePackCode.UInt16:
                return checked((sbyte)this.ReadIntegerUInt16());
            case MessagePackCode.UInt32:
                return checked((sbyte)this.ReadIntegerUInt32());
            case MessagePackCode.UInt64:
                return checked((sbyte)this.ReadIntegerUInt64());
            case MessagePackCode.Int8:
                return checked((sbyte)this.ReadUnsafe<sbyte>());
            case MessagePackCode.Int16:
                return checked((sbyte)this.ReadIntegerInt16());
            case MessagePackCode.Int32:
                return checked((sbyte)this.ReadIntegerInt32());
            case MessagePackCode.Int64:
                return checked((sbyte)this.ReadIntegerInt64());
            default:
                throw ThrowInvalidCode(code, MessagePackType.Integer);
        }
    }

    private ushort ReadUInt16Slow(byte code)
    {
        // The eight consecutive integer codes form a dense jump table.
        switch (code)
        {
            case MessagePackCode.UInt8:
                return checked((ushort)this.ReadUnsafe<byte>());
            case MessagePackCode.UInt16:
                return checked((ushort)this.ReadIntegerUInt16());
            case MessagePackCode.UInt32:
                return checked((ushort)this.ReadIntegerUInt32());
            case MessagePackCode.UInt64:
                return checked((ushort)this.ReadIntegerUInt64());
            case MessagePackCode.Int8:
                return checked((ushort)this.ReadUnsafe<sbyte>());
            case MessagePackCode.Int16:
                return checked((ushort)this.ReadIntegerInt16());
            case MessagePackCode.Int32:
                return checked((ushort)this.ReadIntegerInt32());
            case MessagePackCode.Int64:
                return checked((ushort)this.ReadIntegerInt64());
            default:
                if (code >= MessagePackCode.MinNegativeFixInt)
                {
                    throw new OverflowException();
                }

                throw ThrowInvalidCode(code, MessagePackType.Integer);
        }
    }

    private short ReadInt16Slow(byte code)
    {
        // The eight consecutive integer codes form a dense jump table.
        switch (code)
        {
            case MessagePackCode.UInt8:
                return checked((short)this.ReadUnsafe<byte>());
            case MessagePackCode.UInt16:
                return checked((short)this.ReadIntegerUInt16());
            case MessagePackCode.UInt32:
                return checked((short)this.ReadIntegerUInt32());
            case MessagePackCode.UInt64:
                return checked((short)this.ReadIntegerUInt64());
            case MessagePackCode.Int8:
                return checked((short)this.ReadUnsafe<sbyte>());
            case MessagePackCode.Int16:
                return checked((short)this.ReadIntegerInt16());
            case MessagePackCode.Int32:
                return checked((short)this.ReadIntegerInt32());
            case MessagePackCode.Int64:
                return checked((short)this.ReadIntegerInt64());
            default:
                throw ThrowInvalidCode(code, MessagePackType.Integer);
        }
    }

    private uint ReadUInt32Slow(byte code)
    {
        // The eight consecutive integer codes form a dense jump table.
        switch (code)
        {
            case MessagePackCode.UInt8:
                return checked((uint)this.ReadUnsafe<byte>());
            case MessagePackCode.UInt16:
                return checked((uint)this.ReadIntegerUInt16());
            case MessagePackCode.UInt32:
                return checked((uint)this.ReadIntegerUInt32());
            case MessagePackCode.UInt64:
                return checked((uint)this.ReadIntegerUInt64());
            case MessagePackCode.Int8:
                return checked((uint)this.ReadUnsafe<sbyte>());
            case MessagePackCode.Int16:
                return checked((uint)this.ReadIntegerInt16());
            case MessagePackCode.Int32:
                return checked((uint)this.ReadIntegerInt32());
            case MessagePackCode.Int64:
                return checked((uint)this.ReadIntegerInt64());
            default:
                if (code >= MessagePackCode.MinNegativeFixInt)
                {
                    throw new OverflowException();
                }

                throw ThrowInvalidCode(code, MessagePackType.Integer);
        }
    }

    private int ReadInt32Slow(byte code)
    {
        // The eight consecutive integer codes form a dense jump table.
        switch (code)
        {
            case MessagePackCode.UInt8:
                return checked((int)this.ReadUnsafe<byte>());
            case MessagePackCode.UInt16:
                return checked((int)this.ReadIntegerUInt16());
            case MessagePackCode.UInt32:
                return checked((int)this.ReadIntegerUInt32());
            case MessagePackCode.UInt64:
                return checked((int)this.ReadIntegerUInt64());
            case MessagePackCode.Int8:
                return checked((int)this.ReadUnsafe<sbyte>());
            case MessagePackCode.Int16:
                return checked((int)this.ReadIntegerInt16());
            case MessagePackCode.Int32:
                return checked((int)this.ReadIntegerInt32());
            case MessagePackCode.Int64:
                return checked((int)this.ReadIntegerInt64());
            default:
                throw ThrowInvalidCode(code, MessagePackType.Integer);
        }
    }

    private ulong ReadUInt64Slow(byte code)
    {
        // The eight consecutive integer codes form a dense jump table.
        switch (code)
        {
            case MessagePackCode.UInt8:
                return checked((ulong)this.ReadUnsafe<byte>());
            case MessagePackCode.UInt16:
                return checked((ulong)this.ReadIntegerUInt16());
            case MessagePackCode.UInt32:
                return checked((ulong)this.ReadIntegerUInt32());
            case MessagePackCode.UInt64:
                return checked((ulong)this.ReadIntegerUInt64());
            case MessagePackCode.Int8:
                return checked((ulong)this.ReadUnsafe<sbyte>());
            case MessagePackCode.Int16:
                return checked((ulong)this.ReadIntegerInt16());
            case MessagePackCode.Int32:
                return checked((ulong)this.ReadIntegerInt32());
            case MessagePackCode.Int64:
                return checked((ulong)this.ReadIntegerInt64());
            default:
                if (code >= MessagePackCode.MinNegativeFixInt)
                {
                    throw new OverflowException();
                }

                throw ThrowInvalidCode(code, MessagePackType.Integer);
        }
    }

    private long ReadInt64Slow(byte code)
    {
        // The eight consecutive integer codes form a dense jump table.
        switch (code)
        {
            case MessagePackCode.UInt8:
                return checked((long)this.ReadUnsafe<byte>());
            case MessagePackCode.UInt16:
                return checked((long)this.ReadIntegerUInt16());
            case MessagePackCode.UInt32:
                return checked((long)this.ReadIntegerUInt32());
            case MessagePackCode.UInt64:
                return checked((long)this.ReadIntegerUInt64());
            case MessagePackCode.Int8:
                return checked((long)this.ReadUnsafe<sbyte>());
            case MessagePackCode.Int16:
                return checked((long)this.ReadIntegerInt16());
            case MessagePackCode.Int32:
                return checked((long)this.ReadIntegerInt32());
            case MessagePackCode.Int64:
                return checked((long)this.ReadIntegerInt64());
            default:
                throw ThrowInvalidCode(code, MessagePackType.Integer);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ushort ReadIntegerUInt16()
    {
        var value = this.ReadUnsafe<ushort>();
        return BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(value) : value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private short ReadIntegerInt16()
    {
        var value = this.ReadUnsafe<short>();
        return BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(value) : value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private uint ReadIntegerUInt32()
    {
        var value = this.ReadUnsafe<uint>();
        return BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(value) : value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int ReadIntegerInt32()
    {
        var value = this.ReadUnsafe<int>();
        return BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(value) : value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ulong ReadIntegerUInt64()
    {
        var value = this.ReadUnsafe<ulong>();
        return BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(value) : value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private long ReadIntegerInt64()
    {
        var value = this.ReadUnsafe<long>();
        return BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(value) : value;
    }
}
