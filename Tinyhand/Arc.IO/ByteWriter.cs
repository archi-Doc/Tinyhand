// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.Buffers.Text;

namespace Arc.IO;

public ref struct ByteWriter
{
    private ByteBufferWriter writer;

    public ByteWriter(IBufferWriter<byte> writer)
    {
        this.writer = new ByteBufferWriter(writer);
    }

    public ByteWriter(byte[] initialBuffer)
    {
        this.writer = new ByteBufferWriter(initialBuffer);
    }

    public void Dispose()
    {
        this.writer.Dispose();
    }

    public byte[] FlushAndGetArray() => this.writer.FlushAndGetArray();

    public void Flush() => this.writer.Flush();

    public Span<byte> GetSpan(int length) => this.writer.GetSpan(length);

    public void Advance(int count) => this.writer.Advance(count);

    public void Ensure(int sizeHint) => this.writer.Ensure(sizeHint);

    public void WriteSpan(ReadOnlySpan<byte> span) => this.writer.Write(span);

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
                    continue;
            }

            this.WriteSpan(utf8.Slice(from, i - from));
            from = i + 1;
            this.WriteUInt8((byte)'\\');
            this.WriteUInt8(escapeChar);
        }

        if (from != utf8.Length)
        {
            this.WriteSpan(utf8.Slice(from, utf8.Length - from));
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

    public void WriteCRLF() => this.WriteUInt16(0x0D0A);

    public bool WriteStringInt64(long value)
    {
        Span<byte> span = this.writer.GetSpan(20);
        if (Utf8Formatter.TryFormat(value, span, out var written))
        {
            this.WriteSpan(span.Slice(0, written));
            return true;
        }

        return false;
    }

    public bool WriteStringDouble(double value)
    {
        Span<byte> span = this.writer.GetSpan(32);
        if (Utf8Formatter.TryFormat(value, span, out var written))
        {
            this.WriteSpan(span.Slice(0, written));
            return true;
        }

        return false;
    }

    public void MessagePackInt32(int value)
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

    private static unsafe void WriteBigEndian(ushort value, byte* span)
    {
        unchecked
        {
            span[0] = (byte)(value >> 8);
            span[1] = (byte)value;
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

    private static unsafe void WriteBigEndian(uint value, byte* span)
    {
        unchecked
        {
            span[0] = (byte)(value >> 24);
            span[1] = (byte)(value >> 16);
            span[2] = (byte)(value >> 8);
            span[3] = (byte)value;
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

    private static unsafe void WriteBigEndian(float value, Span<byte> span) => WriteBigEndian(*(int*)&value, span);

    private static unsafe void WriteBigEndian(double value, Span<byte> span) => WriteBigEndian(*(long*)&value, span);
}
