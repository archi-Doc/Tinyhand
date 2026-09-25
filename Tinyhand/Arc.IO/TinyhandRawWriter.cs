// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.Buffers.Text;
using Arc.Collections;
using Tinyhand;
using Tinyhand.IO;

namespace Arc.IO;

/// <summary>
/// Writes Tinyhand text tokens and raw bytes to a buffer. Dispose it to release owned pooled buffers.
/// </summary>
public ref struct TinyhandRawWriter
{
    public static TinyhandRawWriter CreateFromBytePool(int initialBufferSize = TinyhandSerializer.InitialBufferSize)
        => new(BytePool.Default.Rent(initialBufferSize));

    internal static TinyhandRawWriter CreateFromThreadStaticBuffer()
        => new() { writer = ByteBufferWriter.CreateFromThreadStaticBuffer() };

    private ByteBufferWriter writer;

    public TinyhandRawWriter(IBufferWriter<byte> writer)
    {
        this.writer = new ByteBufferWriter(writer);
    }

    public TinyhandRawWriter(byte[] initialBuffer)
    {
        this.writer = new ByteBufferWriter(initialBuffer);
    }

    public TinyhandRawWriter(BytePool.RentedArray array)
    {
        this.writer = new ByteBufferWriter(array);
    }

    public void Dispose()
    {
        this.writer.Dispose();
    }

    public byte[] FlushAndGetArray()
        => this.writer.FlushAndGetArray();

    public BytePool.RentedMemory FlushAndGetRentMemory()
        => this.writer.FlushAndGetRentMemory();

    public void FlushAndGetReadOnlySpan(out ReadOnlySpan<byte> span, out bool isInitialBuffer)
        => this.writer.FlushAndGetReadOnlySpan(out span, out isInitialBuffer);

    public void Flush()
        => this.writer.Flush();

    public Span<byte> GetSpan(int length)
        => this.writer.GetSpan(length);

    public void Advance(int count)
        => this.writer.Advance(count);

    public void Ensure(int sizeHint)
        => this.writer.Ensure(sizeHint);

    public void WriteRaw(scoped ReadOnlySpan<byte> span)
        => this.writer.Write(span);

    public void WriteEscapedUtf8(ReadOnlySpan<byte> utf8)
    {
        var from = 0;
        // for JIT Optimization, for-loop i < str.Length
        for (int i = 0; i < utf8.Length; i++)
        {
            byte escapeChar;
            switch (utf8[i])
            {
                case (byte)'"': // 0x22
                    escapeChar = (byte)'"';
                    break;
                case (byte)'\\': // 0x5C
                    escapeChar = (byte)'\\';
                    break;
                case (byte)'\b': // 0x08
                    escapeChar = (byte)'b';
                    break;
                case (byte)'\f': // 0xC
                    escapeChar = (byte)'f';
                    break;
                case (byte)'\n': // 0x0A
                    escapeChar = (byte)'n';
                    break;
                case (byte)'\r': // 0x0D
                    escapeChar = (byte)'r';
                    break;
                case (byte)'\t': // 0x09
                    escapeChar = (byte)'t';
                    break;

                default:
                    if (utf8[i] < 0x20)
                    {
                        this.WriteRaw(utf8.Slice(from, i - from));
                        from = i + 1;
                        var escaped = this.writer.GetSpan(6);
                        "\\u00"u8.CopyTo(escaped);
                        escaped[4] = "0123456789abcdef"u8[utf8[i] >> 4];
                        escaped[5] = "0123456789abcdef"u8[utf8[i] & 0xf];
                        this.writer.Advance(6);
                    }

                    continue;
            }

            this.WriteRaw(utf8.Slice(from, i - from));
            from = i + 1;
            this.WriteUInt8((byte)'\\');
            this.WriteUInt8(escapeChar);
        }

        if (from != utf8.Length)
        {
            this.WriteRaw(utf8.Slice(from, utf8.Length - from));
        }
    }

    public void WriteInt8(sbyte value)
    {
        Span<byte> span = this.writer.GetSpan(1);
        span[0] = unchecked((byte)value);
        this.writer.Advance(1);
    }

    public void WriteUInt8(byte value)
    {
        Span<byte> span = this.writer.GetSpan(1);
        span[0] = value;
        this.writer.Advance(1);
    }

    public void WriteInt16(short value)
    {
        Span<byte> span = this.writer.GetSpan(2);
        WriteBigEndian(value, span);
        this.writer.Advance(2);
    }

    public void WriteUInt16(ushort value)
    {
        Span<byte> span = this.writer.GetSpan(2);
        WriteBigEndian(value, span);
        this.writer.Advance(2);
    }

    public void WriteInt32(int value)
    {
        Span<byte> span = this.writer.GetSpan(4);
        WriteBigEndian(value, span);
        this.writer.Advance(4);
    }

    public void WriteUInt32(uint value)
    {
        Span<byte> span = this.writer.GetSpan(4);
        WriteBigEndian(value, span);
        this.writer.Advance(4);
    }

    public void WriteInt64(long value)
    {
        Span<byte> span = this.writer.GetSpan(8);
        WriteBigEndian(value, span);
        this.writer.Advance(8);
    }

    public void WriteUInt64(ulong value)
    {
        Span<byte> span = this.writer.GetSpan(8);
        WriteBigEndian(value, span);
        this.writer.Advance(8);
    }

    public void WriteLineFeed() => this.WriteUInt8(0x0A);

    public bool TryWriteInt64Text(long value)
    {
        Span<byte> span = this.writer.GetSpan(20);
        if (Utf8Formatter.TryFormat(value, span, out var written))
        {
            // TryFormat wrote directly into the writer buffer, so just commit it.
            this.writer.Advance(written);
            return true;
        }

        return false;
    }

    public bool TryWriteUInt64Text(ulong value)
    {
        Span<byte> span = this.writer.GetSpan(20);
        if (Utf8Formatter.TryFormat(value, span, out var written))
        {
            // TryFormat wrote directly into the writer buffer, so just commit it.
            this.writer.Advance(written);
            return true;
        }

        return false;
    }

    public bool TryWriteSingleText(float value)
    {
        if (float.IsNaN(value))
        {
            this.WriteRaw(TinyhandConstants.DoubleNaNSpan);
            return true;
        }
        else if (float.IsPositiveInfinity(value))
        {
            this.WriteRaw(TinyhandConstants.DoublePositiveInfinitySpan);
            return true;
        }
        else if (float.IsNegativeInfinity(value))
        {
            this.WriteRaw(TinyhandConstants.DoubleNegativeInfinitySpan);
            return true;
        }
        else if (value == 0 && float.IsNegative(value))
        {// "-0" would be read back as the integer 0.
            this.WriteRaw(TinyhandConstants.NegativeZeroSpan);
            return true;
        }

        Span<byte> span = this.writer.GetSpan(32);
        if (Utf8Formatter.TryFormat(value, span, out var written))
        {
            // TryFormat wrote directly into the writer buffer, so just commit it.
            this.writer.Advance(written);
            return true;
        }

        return false;
    }

    public bool TryWriteDoubleText(double value)
    {
        if (double.IsNaN(value))
        {
            this.WriteRaw(TinyhandConstants.DoubleNaNSpan);
            return true;
        }
        else if (double.IsPositiveInfinity(value))
        {
            this.WriteRaw(TinyhandConstants.DoublePositiveInfinitySpan);
            return true;
        }
        else if (double.IsNegativeInfinity(value))
        {
            this.WriteRaw(TinyhandConstants.DoubleNegativeInfinitySpan);
            return true;
        }
        else if (value == 0 && double.IsNegative(value))
        {// "-0" would be read back as the integer 0.
            this.WriteRaw(TinyhandConstants.NegativeZeroSpan);
            return true;
        }

        Span<byte> span = this.writer.GetSpan(32);
        if (Utf8Formatter.TryFormat(value, span, out var written))
        {
            // TryFormat wrote directly into the writer buffer, so just commit it.
            this.writer.Advance(written);
            return true;
        }

        return false;
    }

    public void WriteMessagePackInt32(int value)
    {
        Span<byte> span = this.writer.GetSpan(5);
        span[0] = 0xd2;
        WriteBigEndian(value, span.Slice(1));
        this.writer.Advance(5);
    }

    private static void WriteBigEndian(short value, Span<byte> span) => WriteBigEndian(unchecked((ushort)value), span);

    private static void WriteBigEndian(int value, Span<byte> span) => WriteBigEndian(unchecked((uint)value), span);

    private static void WriteBigEndian(long value, Span<byte> span) => WriteBigEndian(unchecked((ulong)value), span);

    private static void WriteBigEndian(ushort value, Span<byte> span)
    {
        unchecked
        {
            // Write to highest index first so the JIT skips bounds checks on subsequent writes.
            span[1] = (byte)value;
            span[0] = (byte)(value >> 8);
        }
    }

    private static void WriteBigEndian(uint value, Span<byte> span)
    {
        unchecked
        {
            // Write to highest index first so the JIT skips bounds checks on subsequent writes.
            span[3] = (byte)value;
            span[2] = (byte)(value >> 8);
            span[1] = (byte)(value >> 16);
            span[0] = (byte)(value >> 24);
        }
    }

    private static void WriteBigEndian(ulong value, Span<byte> span)
    {
        unchecked
        {
            // Write to highest index first so the JIT skips bounds checks on subsequent writes.
            span[7] = (byte)value;
            span[6] = (byte)(value >> 8);
            span[5] = (byte)(value >> 16);
            span[4] = (byte)(value >> 24);
            span[3] = (byte)(value >> 32);
            span[2] = (byte)(value >> 40);
            span[1] = (byte)(value >> 48);
            span[0] = (byte)(value >> 56);
        }
    }
}
