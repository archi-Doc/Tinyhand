﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Arc.IO;

namespace Tinyhand.IO;

public ref struct TinyhandWriter
{
    private ByteBufferWriter writer;

    public TinyhandWriter(IBufferWriter<byte> writer)
    {
        this.writer = new ByteBufferWriter(writer);
    }

    public TinyhandWriter(byte[] initialBuffer)
    {
        this.writer = new ByteBufferWriter(initialBuffer);
    }

    public void Dispose()
    {
        this.writer.Dispose();
    }

    /// <summary>
    /// Gets or sets the cancellation token for this serialization operation.
    /// </summary>
    public CancellationToken CancellationToken { get; set; } = default;

    /// <summary>
    /// Initializes a new instance of the <see cref="TinyhandWriter"/> struct,
    /// with the same settings as this one, but with its own buffer writer.
    /// </summary>
    /// <param name="writer">The writer to use for the new instance.</param>
    /// <returns>The new writer.</returns>
    public TinyhandWriter Clone(IBufferWriter<byte> writer) => new TinyhandWriter(writer)
    {
        CancellationToken = this.CancellationToken,
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="TinyhandWriter"/> struct,
    /// with the same settings as this one, but with its own buffer writer.
    /// </summary>
    /// <param name="initialBuffer">The buffer to use for the new instance.</param>
    /// <returns>The new writer.</returns>
    public TinyhandWriter Clone(byte[] initialBuffer) => new TinyhandWriter(initialBuffer)
    {
        CancellationToken = this.CancellationToken,
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="TinyhandWriter"/> struct,
    /// with the same settings as this one, but with its own buffer writer.
    /// </summary>
    /// <returns>The new writer.</returns>
    public TinyhandWriter Clone() => new TinyhandWriter()
    {
        CancellationToken = this.CancellationToken,
    };

    public byte[] FlushAndGetArray() => this.writer.FlushAndGetArray();

    public void FlushAndGetArray(out byte[] rawArray, out int written)
        => this.writer.FlushAndGetArray(out rawArray, out written);

    public void FlushAndGetMemory(out Memory<byte> memory, out bool useInitialBuffer)
        => this.writer.FlushAndGetMemory(out memory, out useInitialBuffer);

    public ReadOnlySequence<byte> FlushAndGetReadOnlySequence() => this.writer.FlushAndGetReadOnlySequence();

    public ReadOnlySpan<byte> FlushAndGetReadOnlySpan() => this.writer.FlushAndGetReadOnlySpan();

    public void Flush() => this.writer.Flush();

    public Span<byte> GetSpan(int length) => this.writer.GetSpan(length);

    public void Advance(int count) => this.writer.Advance(count);

    public void Ensure(int sizeHint) => this.writer.Ensure(sizeHint);

    public long Written => this.writer.Written;

    /// <summary>
    /// Copies bytes directly into the message pack writer.
    /// </summary>
    /// <param name="span">The span of bytes to copy from.</param>
    public void WriteSpan(ReadOnlySpan<byte> span) => this.writer.Write(span);

    /// <summary>
    /// Copies bytes directly into the message pack writer.
    /// </summary>
    /// <param name="sequence">The span of bytes to copy from.</param>
    public void WriteSequence(scoped in ReadOnlySequence<byte> sequence)
    {
        foreach (ReadOnlyMemory<byte> segment in sequence)
        {
            this.writer.Write(segment.Span);
        }
    }

    public void RawWriteInt8(sbyte value)
    {
        Span<byte> span = this.writer.GetSpan(1);
        span[0] = unchecked((byte)value);
        this.writer.Advance(1);
    }

    public void RawWriteUInt8(byte value)
    {
        Span<byte> span = this.writer.GetSpan(1);
        span[0] = value;
        this.writer.Advance(1);
    }

    public void RawWriteInt16(short value)
    {
        Span<byte> span = this.writer.GetSpan(2);
        WriteBigEndian(value, span);
        this.writer.Advance(2);
    }

    public void RawWriteUInt16(ushort value)
    {
        Span<byte> span = this.writer.GetSpan(2);
        WriteBigEndian(value, span);
        this.writer.Advance(2);
    }

    public void RawWriteInt32(int value)
    {
        Span<byte> span = this.writer.GetSpan(4);
        WriteBigEndian(value, span);
        this.writer.Advance(4);
    }

    public void RawWriteUInt32(uint value)
    {
        Span<byte> span = this.writer.GetSpan(4);
        WriteBigEndian(value, span);
        this.writer.Advance(4);
    }

    public void RawWriteInt64(long value)
    {
        Span<byte> span = this.writer.GetSpan(8);
        WriteBigEndian(value, span);
        this.writer.Advance(8);
    }

    public void RawWriteUInt64(ulong value)
    {
        Span<byte> span = this.writer.GetSpan(8);
        WriteBigEndian(value, span);
        this.writer.Advance(8);
    }

    /// <summary>
    /// Writes a <see cref="MessagePackCode.Nil"/> value.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteNil()
    {
        Span<byte> span = this.writer.GetSpan(1);
        span[0] = MessagePackCode.Nil;
        this.writer.Advance(1);
    }

    /// <summary>
    /// Write the length of the next array to be written in the most compact form of
    /// <see cref="MessagePackCode.MinFixArray"/>,
    /// <see cref="MessagePackCode.Array16"/>, or
    /// <see cref="MessagePackCode.Array32"/>.
    /// </summary>
    /// <param name="count">The number of elements that will be written in the array.</param>
    public void WriteArrayHeader(int count) => this.WriteArrayHeader((uint)count);

    /// <summary>
    /// Write the length of the next array to be written in the most compact form of
    /// <see cref="MessagePackCode.MinFixArray"/>,
    /// <see cref="MessagePackCode.Array16"/>, or
    /// <see cref="MessagePackCode.Array32"/>.
    /// </summary>
    /// <param name="count">The number of elements that will be written in the array.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteArrayHeader(uint count)
    {
        if (count <= MessagePackRange.MaxFixArrayCount)
        {
            Span<byte> span = this.writer.GetSpan(1);
            span[0] = (byte)(MessagePackCode.MinFixArray | count);
            this.writer.Advance(1);
        }
        else if (count <= ushort.MaxValue)
        {
            Span<byte> span = this.writer.GetSpan(3);
            span[0] = MessagePackCode.Array16;
            WriteBigEndian((ushort)count, span.Slice(1));
            this.writer.Advance(3);
        }
        else
        {
            Span<byte> span = this.writer.GetSpan(5);
            span[0] = MessagePackCode.Array32;
            WriteBigEndian(count, span.Slice(1));
            this.writer.Advance(5);
        }
    }

    /// <summary>
    /// Write the length of the next map to be written in the most compact form of
    /// <see cref="MessagePackCode.MinFixMap"/>,
    /// <see cref="MessagePackCode.Map16"/>, or
    /// <see cref="MessagePackCode.Map32"/>.
    /// </summary>
    /// <param name="count">The number of key=value pairs that will be written in the map.</param>
    public void WriteMapHeader(int count) => this.WriteMapHeader((uint)count);

    /// <summary>
    /// Write the length of the next map to be written in the most compact form of
    /// <see cref="MessagePackCode.MinFixMap"/>,
    /// <see cref="MessagePackCode.Map16"/>, or
    /// <see cref="MessagePackCode.Map32"/>.
    /// </summary>
    /// <param name="count">The number of key=value pairs that will be written in the map.</param>
    public void WriteMapHeader(uint count)
    {
        if (count <= MessagePackRange.MaxFixMapCount)
        {
            Span<byte> span = this.writer.GetSpan(1);
            span[0] = (byte)(MessagePackCode.MinFixMap | count);
            this.writer.Advance(1);
        }
        else if (count <= ushort.MaxValue)
        {
            Span<byte> span = this.writer.GetSpan(3);
            span[0] = MessagePackCode.Map16;
            WriteBigEndian((ushort)count, span.Slice(1));
            this.writer.Advance(3);
        }
        else
        {
            Span<byte> span = this.writer.GetSpan(5);
            span[0] = MessagePackCode.Map32;
            WriteBigEndian(count, span.Slice(1));
            this.writer.Advance(5);
        }
    }

    /// <summary>
    /// Writes a <see cref="byte"/> value using a 1-byte code when possible, otherwise as <see cref="MessagePackCode.UInt8"/>.
    /// </summary>
    /// <param name="value">The value.</param>
    public void Write(byte value)
    {
        if (value <= MessagePackCode.MaxFixInt)
        {
            Span<byte> span = this.writer.GetSpan(1);
            span[0] = value;
            this.writer.Advance(1);
        }
        else
        {
            this.WriteUInt8(value);
        }
    }

    /// <summary>
    /// Writes a <see cref="byte"/> value using <see cref="MessagePackCode.UInt8"/>.
    /// </summary>
    /// <param name="value">The value.</param>
    public void WriteUInt8(byte value)
    {
        Span<byte> span = this.writer.GetSpan(2);
        span[0] = MessagePackCode.UInt8;
        span[1] = value;
        this.writer.Advance(2);
    }

    /// <summary>
    /// Writes an 8-bit value using a 1-byte code when possible, otherwise as <see cref="MessagePackCode.Int8"/>.
    /// </summary>
    /// <param name="value">The value.</param>
    public void Write(sbyte value)
    {
        if (value < MessagePackRange.MinFixNegativeInt)
        {
            this.WriteInt8(value);
        }
        else
        {
            Span<byte> span = this.writer.GetSpan(1);
            span[0] = unchecked((byte)value);
            this.writer.Advance(1);
        }
    }

    /// <summary>
    /// Writes an 8-bit value using <see cref="MessagePackCode.Int8"/>.
    /// </summary>
    /// <param name="value">The value.</param>
    public void WriteInt8(sbyte value)
    {
        Span<byte> span = this.writer.GetSpan(2);
        span[0] = MessagePackCode.Int8;
        span[1] = unchecked((byte)value);
        this.writer.Advance(2);
    }

    /// <summary>
    /// Writes a <see cref="ushort"/> value using a 1-byte code when possible, otherwise as <see cref="MessagePackCode.UInt8"/> or <see cref="MessagePackCode.UInt16"/>.
    /// </summary>
    /// <param name="value">The value.</param>
    public void Write(ushort value)
    {
        if (value <= MessagePackRange.MaxFixPositiveInt)
        {
            Span<byte> span = this.writer.GetSpan(1);
            span[0] = unchecked((byte)value);
            this.writer.Advance(1);
        }
        else if (value <= byte.MaxValue)
        {
            Span<byte> span = this.writer.GetSpan(2);
            span[0] = MessagePackCode.UInt8;
            span[1] = unchecked((byte)value);
            this.writer.Advance(2);
        }
        else
        {
            this.WriteUInt16(value);
        }
    }

    /// <summary>
    /// Writes a <see cref="ushort"/> value using <see cref="MessagePackCode.UInt16"/>.
    /// </summary>
    /// <param name="value">The value.</param>
    public void WriteUInt16(ushort value)
    {
        Span<byte> span = this.writer.GetSpan(3);
        span[0] = MessagePackCode.UInt16;
        WriteBigEndian(value, span.Slice(1));
        this.writer.Advance(3);
    }

    /// <summary>
    /// Writes a <see cref="short"/> using a built-in 1-byte code when within specific MessagePack-supported ranges,
    /// or the most compact of
    /// <see cref="MessagePackCode.UInt8"/>,
    /// <see cref="MessagePackCode.UInt16"/>,
    /// <see cref="MessagePackCode.Int8"/>, or
    /// <see cref="MessagePackCode.Int16"/>.
    /// </summary>
    /// <param name="value">The value to write.</param>
    public void Write(short value)
    {
        if (value >= 0)
        {
            this.Write((ushort)value);
        }
        else
        {
            // negative int(use int)
            if (value >= MessagePackRange.MinFixNegativeInt)
            {
                Span<byte> span = this.writer.GetSpan(1);
                span[0] = unchecked((byte)value);
                this.writer.Advance(1);
            }
            else if (value >= sbyte.MinValue)
            {
                Span<byte> span = this.writer.GetSpan(2);
                span[0] = MessagePackCode.Int8;
                span[1] = unchecked((byte)value);
                this.writer.Advance(2);
            }
            else
            {
                this.WriteInt16(value);
            }
        }
    }

    /// <summary>
    /// Writes a <see cref="short"/> using <see cref="MessagePackCode.Int16"/>.
    /// </summary>
    /// <param name="value">The value to write.</param>
    public void WriteInt16(short value)
    {
        Span<byte> span = this.writer.GetSpan(3);
        span[0] = MessagePackCode.Int16;
        WriteBigEndian(value, span.Slice(1));
        this.writer.Advance(3);
    }

    /// <summary>
    /// Writes an <see cref="uint"/> using a built-in 1-byte code when within specific MessagePack-supported ranges,
    /// or the most compact of
    /// <see cref="MessagePackCode.UInt8"/>,
    /// <see cref="MessagePackCode.UInt16"/>, or
    /// <see cref="MessagePackCode.UInt32"/>.
    /// </summary>
    /// <param name="value">The value to write.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(uint value)
    {
        if (value <= MessagePackRange.MaxFixPositiveInt)
        {
            Span<byte> span = this.writer.GetSpan(1);
            span[0] = unchecked((byte)value);
            this.writer.Advance(1);
        }
        else if (value <= byte.MaxValue)
        {
            Span<byte> span = this.writer.GetSpan(2);
            span[0] = MessagePackCode.UInt8;
            span[1] = unchecked((byte)value);
            this.writer.Advance(2);
        }
        else if (value <= ushort.MaxValue)
        {
            Span<byte> span = this.writer.GetSpan(3);
            span[0] = MessagePackCode.UInt16;
            WriteBigEndian((ushort)value, span.Slice(1));
            this.writer.Advance(3);
        }
        else
        {
            this.WriteUInt32(value);
        }
    }

    /// <summary>
    /// Writes an <see cref="uint"/> using <see cref="MessagePackCode.UInt32"/>.
    /// </summary>
    /// <param name="value">The value to write.</param>
    public void WriteUInt32(uint value)
    {
        Span<byte> span = this.writer.GetSpan(5);
        span[0] = MessagePackCode.UInt32;
        WriteBigEndian(value, span.Slice(1));
        this.writer.Advance(5);
    }

    /// <summary>
    /// Writes an <see cref="int"/> using a built-in 1-byte code when within specific MessagePack-supported ranges,
    /// or the most compact of
    /// <see cref="MessagePackCode.UInt8"/>,
    /// <see cref="MessagePackCode.UInt16"/>,
    /// <see cref="MessagePackCode.UInt32"/>,
    /// <see cref="MessagePackCode.Int8"/>,
    /// <see cref="MessagePackCode.Int16"/>,
    /// <see cref="MessagePackCode.Int32"/>.
    /// </summary>
    /// <param name="value">The value to write.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(int value)
    {
        if (value >= 0)
        {
            this.Write((uint)value);
        }
        else
        {
            // negative int(use int)
            if (value >= MessagePackRange.MinFixNegativeInt)
            {
                Span<byte> span = this.writer.GetSpan(1);
                span[0] = unchecked((byte)value);
                this.writer.Advance(1);
            }
            else if (value >= sbyte.MinValue)
            {
                Span<byte> span = this.writer.GetSpan(2);
                span[0] = MessagePackCode.Int8;
                span[1] = unchecked((byte)value);
                this.writer.Advance(2);
            }
            else if (value >= short.MinValue)
            {
                Span<byte> span = this.writer.GetSpan(3);
                span[0] = MessagePackCode.Int16;
                WriteBigEndian((short)value, span.Slice(1));
                this.writer.Advance(3);
            }
            else
            {
                this.WriteInt32(value);
            }
        }
    }

    /// <summary>
    /// Writes an <see cref="int"/> using <see cref="MessagePackCode.Int32"/>.
    /// </summary>
    /// <param name="value">The value to write.</param>
    public void WriteInt32(int value)
    {
        Span<byte> span = this.writer.GetSpan(5);
        span[0] = MessagePackCode.Int32;
        WriteBigEndian(value, span.Slice(1));
        this.writer.Advance(5);
    }

    /// <summary>
    /// Writes an <see cref="ulong"/> using a built-in 1-byte code when within specific MessagePack-supported ranges,
    /// or the most compact of
    /// <see cref="MessagePackCode.UInt8"/>,
    /// <see cref="MessagePackCode.UInt16"/>,
    /// <see cref="MessagePackCode.UInt32"/>,
    /// <see cref="MessagePackCode.Int8"/>,
    /// <see cref="MessagePackCode.Int16"/>,
    /// <see cref="MessagePackCode.Int32"/>.
    /// </summary>
    /// <param name="value">The value to write.</param>
    public void Write(ulong value)
    {
        if (value <= MessagePackRange.MaxFixPositiveInt)
        {
            Span<byte> span = this.writer.GetSpan(1);
            span[0] = unchecked((byte)value);
            this.writer.Advance(1);
        }
        else if (value <= byte.MaxValue)
        {
            Span<byte> span = this.writer.GetSpan(2);
            span[0] = MessagePackCode.UInt8;
            span[1] = unchecked((byte)value);
            this.writer.Advance(2);
        }
        else if (value <= ushort.MaxValue)
        {
            Span<byte> span = this.writer.GetSpan(3);
            span[0] = MessagePackCode.UInt16;
            WriteBigEndian((ushort)value, span.Slice(1));
            this.writer.Advance(3);
        }
        else if (value <= uint.MaxValue)
        {
            Span<byte> span = this.writer.GetSpan(5);
            span[0] = MessagePackCode.UInt32;
            WriteBigEndian((uint)value, span.Slice(1));
            this.writer.Advance(5);
        }
        else
        {
            this.WriteUInt64(value);
        }
    }

    /// <summary>
    /// Writes an <see cref="ulong"/> using <see cref="MessagePackCode.Int32"/>.
    /// </summary>
    /// <param name="value">The value to write.</param>
    public void WriteUInt64(ulong value)
    {
        Span<byte> span = this.writer.GetSpan(9);
        span[0] = MessagePackCode.UInt64;
        WriteBigEndian(value, span.Slice(1));
        this.writer.Advance(9);
    }

    /// <summary>
    /// Writes an <see cref="long"/> using a built-in 1-byte code when within specific MessagePack-supported ranges,
    /// or the most compact of
    /// <see cref="MessagePackCode.UInt8"/>,
    /// <see cref="MessagePackCode.UInt16"/>,
    /// <see cref="MessagePackCode.UInt32"/>,
    /// <see cref="MessagePackCode.UInt64"/>,
    /// <see cref="MessagePackCode.Int8"/>,
    /// <see cref="MessagePackCode.Int16"/>,
    /// <see cref="MessagePackCode.Int32"/>,
    /// <see cref="MessagePackCode.Int64"/>.
    /// </summary>
    /// <param name="value">The value to write.</param>
    public void Write(long value)
    {
        if (value >= 0)
        {
            this.Write((ulong)value);
        }
        else
        {
            // negative int(use int)
            if (value >= MessagePackRange.MinFixNegativeInt)
            {
                Span<byte> span = this.writer.GetSpan(1);
                span[0] = unchecked((byte)value);
                this.writer.Advance(1);
            }
            else if (value >= sbyte.MinValue)
            {
                Span<byte> span = this.writer.GetSpan(2);
                span[0] = MessagePackCode.Int8;
                span[1] = unchecked((byte)value);
                this.writer.Advance(2);
            }
            else if (value >= short.MinValue)
            {
                Span<byte> span = this.writer.GetSpan(3);
                span[0] = MessagePackCode.Int16;
                WriteBigEndian((short)value, span.Slice(1));
                this.writer.Advance(3);
            }
            else if (value >= int.MinValue)
            {
                Span<byte> span = this.writer.GetSpan(5);
                span[0] = MessagePackCode.Int32;
                WriteBigEndian((int)value, span.Slice(1));
                this.writer.Advance(5);
            }
            else
            {
                this.WriteInt64(value);
            }
        }
    }

    /// <summary>
    /// Writes a <see cref="long"/> using <see cref="MessagePackCode.Int64"/>.
    /// </summary>
    /// <param name="value">The value to write.</param>
    public void WriteInt64(long value)
    {
        Span<byte> span = this.writer.GetSpan(9);
        span[0] = MessagePackCode.Int64;
        WriteBigEndian(value, span.Slice(1));
        this.writer.Advance(9);
    }

    /// <summary>
    /// Writes a <see cref="bool"/> value using either <see cref="MessagePackCode.True"/> or <see cref="MessagePackCode.False"/>.
    /// </summary>
    /// <param name="value">The value.</param>
    public void Write(bool value)
    {
        Span<byte> span = this.writer.GetSpan(1);
        span[0] = value ? MessagePackCode.True : MessagePackCode.False;
        this.writer.Advance(1);
    }

    /// <summary>
    /// Writes a <see cref="char"/> value using a 1-byte code when possible, otherwise as <see cref="MessagePackCode.UInt8"/> or <see cref="MessagePackCode.UInt16"/>.
    /// </summary>
    /// <param name="value">The value.</param>
    public void Write(char value) => this.Write((ushort)value);

    /// <summary>
    /// Writes a <see cref="MessagePackCode.Float32"/> value.
    /// </summary>
    /// <param name="value">The value.</param>
    public void Write(float value)
    {
        Span<byte> span = this.writer.GetSpan(5);
        span[0] = MessagePackCode.Float32;
        WriteBigEndian(value, span.Slice(1));
        this.writer.Advance(5);
    }

    /// <summary>
    /// Writes a <see cref="MessagePackCode.Float64"/> value.
    /// </summary>
    /// <param name="value">The value.</param>
    public void Write(double value)
    {
        Span<byte> span = this.writer.GetSpan(9);
        span[0] = MessagePackCode.Float64;
        WriteBigEndian(value, span.Slice(1));
        this.writer.Advance(9);
    }

    /// <summary>
    /// Writes a <see cref="DateTime"/> using the message code <see cref="ReservedMessagePackExtensionTypeCode.DateTime"/>.
    /// </summary>
    /// <param name="dateTime">The value to write.</param>
    public void Write(DateTime dateTime)
    {
        // Timestamp spec
        // https://github.com/msgpack/msgpack/pull/209
        // FixExt4(-1) => seconds |  [1970-01-01 00:00:00 UTC, 2106-02-07 06:28:16 UTC) range
        // FixExt8(-1) => nanoseconds + seconds | [1970-01-01 00:00:00.000000000 UTC, 2514-05-30 01:53:04.000000000 UTC) range
        // Ext8(12,-1) => nanoseconds + seconds | [-584554047284-02-23 16:59:44 UTC, 584554051223-11-09 07:00:16.000000000 UTC) range

        // The spec requires UTC. Convert to UTC if we're sure the value was expressed as Local time.
        // If it's Unspecified, we want to leave it alone since .NET will change the value when we convert
        // and we simply don't know, so we should leave it as-is.
        if (dateTime.Kind == DateTimeKind.Local)
        {
            dateTime = dateTime.ToUniversalTime();
        }

        var secondsSinceBclEpoch = dateTime.Ticks / TimeSpan.TicksPerSecond;
        var seconds = secondsSinceBclEpoch - DateTimeConstants.BclSecondsAtUnixEpoch;
        var nanoseconds = (dateTime.Ticks % TimeSpan.TicksPerSecond) * DateTimeConstants.NanosecondsPerTick;

        if ((seconds >> 34) == 0)
        {
            var data64 = unchecked((ulong)((nanoseconds << 34) | seconds));
            if ((data64 & 0xffffffff00000000L) == 0)
            {
                // timestamp 32(seconds in 32-bit unsigned int)
                var data32 = (uint)data64;
                Span<byte> span = this.writer.GetSpan(6);
                span[0] = MessagePackCode.FixExt4;
                span[1] = unchecked((byte)ReservedMessagePackExtensionTypeCode.DateTime);
                WriteBigEndian(data32, span.Slice(2));
                this.writer.Advance(6);
            }
            else
            {
                // timestamp 64(nanoseconds in 30-bit unsigned int | seconds in 34-bit unsigned int)
                Span<byte> span = this.writer.GetSpan(10);
                span[0] = MessagePackCode.FixExt8;
                span[1] = unchecked((byte)ReservedMessagePackExtensionTypeCode.DateTime);
                WriteBigEndian(data64, span.Slice(2));
                this.writer.Advance(10);
            }
        }
        else
        {
            // timestamp 96( nanoseconds in 32-bit unsigned int | seconds in 64-bit signed int )
            Span<byte> span = this.writer.GetSpan(15);
            span[0] = MessagePackCode.Ext8;
            span[1] = 12;
            span[2] = unchecked((byte)ReservedMessagePackExtensionTypeCode.DateTime);
            WriteBigEndian((uint)nanoseconds, span.Slice(3));
            WriteBigEndian(seconds, span.Slice(7));
            this.writer.Advance(15);
        }
    }

    /// <summary>
    /// Writes a <see cref="byte"/>[], prefixed with a length encoded as the smallest fitting from:
    /// <see cref="MessagePackCode.Bin8"/>,
    /// <see cref="MessagePackCode.Bin16"/>,
    /// <see cref="MessagePackCode.Bin32"/>,
    /// or <see cref="MessagePackCode.Nil"/> if <paramref name="src"/> is <c>null</c>.
    /// </summary>
    /// <param name="src">The array of bytes to write. May be <c>null</c>.</param>
    public void Write(byte[]? src)
    {
        if (src == null)
        {
            this.WriteNil();
        }
        else
        {
            this.Write(src.AsSpan());
        }
    }

    /// <summary>
    /// Writes a span of bytes, prefixed with a length encoded as the smallest fitting from:
    /// <see cref="MessagePackCode.Bin8"/>,
    /// <see cref="MessagePackCode.Bin16"/>, or
    /// <see cref="MessagePackCode.Bin32"/>.
    /// </summary>
    /// <param name="src">The span of bytes to write.</param>
    public void Write(ReadOnlySpan<byte> src)
    {
        int length = (int)src.Length;
        this.WriteBinHeader(length);
        var span = this.writer.GetSpan(length);
        src.CopyTo(span);
        this.writer.Advance(length);
    }

    /// <summary>
    /// Writes a sequence of bytes, prefixed with a length encoded as the smallest fitting from:
    /// <see cref="MessagePackCode.Bin8"/>,
    /// <see cref="MessagePackCode.Bin16"/>, or
    /// <see cref="MessagePackCode.Bin32"/>.
    /// </summary>
    /// <param name="src">The span of bytes to write.</param>
    public void Write(in ReadOnlySequence<byte> src)
    {
        int length = (int)src.Length;
        this.WriteBinHeader(length);
        var span = this.writer.GetSpan(length);
        src.CopyTo(span);
        this.writer.Advance(length);
    }

    /// <summary>
    /// Writes the header that precedes a raw binary sequence with a length encoded as the smallest fitting from:
    /// <see cref="MessagePackCode.Bin8"/>,
    /// <see cref="MessagePackCode.Bin16"/>, or
    /// <see cref="MessagePackCode.Bin32"/>.
    /// </summary>
    /// <param name="length">The length of bytes that will be written next.</param>
    /// <remarks>
    /// <para>
    /// The caller should use <see cref="WriteSequence(in ReadOnlySequence{byte})"/> or <see cref="WriteSpan(ReadOnlySpan{byte})"/>
    /// after calling this method to actually write the content.
    /// Alternatively a single call to <see cref="Write(ReadOnlySpan{byte})"/> or <see cref="Write(in ReadOnlySequence{byte})"/> will take care of the header and content in one call.
    /// </para>
    /// </remarks>
    public void WriteBinHeader(int length)
    {
        // When we write the header, we'll ask for all the space we need for the payload as well
        // as that may help ensure we only allocate a buffer once.
        if (length <= byte.MaxValue)
        {
            var size = length + 2;
            Span<byte> span = this.writer.GetSpan(size);

            span[0] = MessagePackCode.Bin8;
            span[1] = (byte)length;

            this.writer.Advance(2);
        }
        else if (length <= ushort.MaxValue)
        {
            var size = length + 3;
            Span<byte> span = this.writer.GetSpan(size);

            span[0] = MessagePackCode.Bin16;
            WriteBigEndian((ushort)length, span.Slice(1));

            this.writer.Advance(3);
        }
        else
        {
            var size = length + 5;
            Span<byte> span = this.writer.GetSpan(size);

            span[0] = MessagePackCode.Bin32;
            WriteBigEndian(length, span.Slice(1));

            this.writer.Advance(5);
        }
    }

    /// <summary>
    /// Writes out an array of bytes that (may) represent a UTF-8 encoded string, prefixed with the length using one of these message codes:
    /// <see cref="MessagePackCode.MinFixStr"/>,
    /// <see cref="MessagePackCode.Str8"/>,
    /// <see cref="MessagePackCode.Str16"/>, or
    /// <see cref="MessagePackCode.Str32"/>.
    /// </summary>
    /// <param name="utf8stringBytes">The bytes to write.</param>
    public void WriteString(in ReadOnlySequence<byte> utf8stringBytes)
    {
        var length = (int)utf8stringBytes.Length;
        this.WriteStringHeader(length);
        Span<byte> span = this.writer.GetSpan(length);
        utf8stringBytes.CopyTo(span);
        this.writer.Advance(length);
    }

    /// <summary>
    /// Writes out an array of bytes that (may) represent a UTF-8 encoded string, prefixed with the length using one of these message codes:
    /// <see cref="MessagePackCode.MinFixStr"/>,
    /// <see cref="MessagePackCode.Str8"/>,
    /// <see cref="MessagePackCode.Str16"/>, or
    /// <see cref="MessagePackCode.Str32"/>.
    /// </summary>
    /// <param name="utf8stringBytes">The bytes to write.</param>
    public void WriteString(ReadOnlySpan<byte> utf8stringBytes)
    {
        var length = utf8stringBytes.Length;
        this.WriteStringHeader(length);
        Span<byte> span = this.writer.GetSpan(length);
        utf8stringBytes.CopyTo(span);
        this.writer.Advance(length);
    }

    /// <summary>
    /// Writes out the header that may precede a UTF-8 encoded string, prefixed with the length using one of these message codes:
    /// <see cref="MessagePackCode.MinFixStr"/>,
    /// <see cref="MessagePackCode.Str8"/>,
    /// <see cref="MessagePackCode.Str16"/>, or
    /// <see cref="MessagePackCode.Str32"/>.
    /// </summary>
    /// <param name="byteCount">The number of bytes in the string that will follow this header.</param>
    /// <remarks>
    /// The caller should use <see cref="WriteSequence(in ReadOnlySequence{byte})"/> or <see cref="WriteSpan(ReadOnlySpan{byte})"/>
    /// after calling this method to actually write the content.
    /// Alternatively a single call to <see cref="WriteString(ReadOnlySpan{byte})"/> or <see cref="WriteString(in ReadOnlySequence{byte})"/> will take care of the header and content in one call.
    /// </remarks>
    public void WriteStringHeader(int byteCount)
    {
        // When we write the header, we'll ask for all the space we need for the payload as well
        // as that may help ensure we only allocate a buffer once.
        if (byteCount <= MessagePackRange.MaxFixStringLength)
        {
            Span<byte> span = this.writer.GetSpan(byteCount + 1);
            span[0] = (byte)(MessagePackCode.MinFixStr | byteCount);
            this.writer.Advance(1);
        }
        else if (byteCount <= byte.MaxValue)
        {
            Span<byte> span = this.writer.GetSpan(byteCount + 2);
            span[0] = MessagePackCode.Str8;
            span[1] = unchecked((byte)byteCount);
            this.writer.Advance(2);
        }
        else if (byteCount <= ushort.MaxValue)
        {
            Span<byte> span = this.writer.GetSpan(byteCount + 3);
            span[0] = MessagePackCode.Str16;
            WriteBigEndian((ushort)byteCount, span.Slice(1));
            this.writer.Advance(3);
        }
        else
        {
            Span<byte> span = this.writer.GetSpan(byteCount + 5);
            span[0] = MessagePackCode.Str32;
            WriteBigEndian(byteCount, span.Slice(1));
            this.writer.Advance(5);
        }
    }

    /// <summary>
    /// Writes out a <see cref="string"/>, prefixed with the length using one of these message codes:
    /// <see cref="MessagePackCode.MinFixStr"/>,
    /// <see cref="MessagePackCode.Str8"/>,
    /// <see cref="MessagePackCode.Str16"/>,
    /// <see cref="MessagePackCode.Str32"/>,
    /// or <see cref="MessagePackCode.Nil"/> if the <paramref name="value"/> is <c>null</c>.
    /// </summary>
    /// <param name="value">The value to write. May be null.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void Write(string? value)
    {
        if (value == null)
        {
            this.WriteNil();
            return;
        }

        ref byte buffer = ref this.WriteString_PrepareSpan(value.Length, out int bufferSize, out int useOffset);
        fixed (char* pValue = value)
        fixed (byte* pBuffer = &buffer)
        {
            int byteCount = Encoding.UTF8.GetBytes(pValue, value.Length, pBuffer + useOffset, bufferSize);
            this.WriteString_PostEncoding(pBuffer, useOffset, byteCount);
        }
    }

    /// <summary>
    /// Writes out a <see cref="string"/>, prefixed with the length using one of these message codes:
    /// <see cref="MessagePackCode.MinFixStr"/>,
    /// <see cref="MessagePackCode.Str8"/>,
    /// <see cref="MessagePackCode.Str16"/>,
    /// <see cref="MessagePackCode.Str32"/>.
    /// </summary>
    /// <param name="value">The value to write.</param>
    public unsafe void Write(ReadOnlySpan<char> value)
    {
        ref byte buffer = ref this.WriteString_PrepareSpan(value.Length, out int bufferSize, out int useOffset);
        fixed (char* pValue = value)
        fixed (byte* pBuffer = &buffer)
        {
            int byteCount = Encoding.UTF8.GetBytes(pValue, value.Length, pBuffer + useOffset, bufferSize);
            this.WriteString_PostEncoding(pBuffer, useOffset, byteCount);
        }
    }

    /// <summary>
    /// Writes the extension format header, using the smallest one of these codes:
    /// <see cref="MessagePackCode.FixExt1"/>,
    /// <see cref="MessagePackCode.FixExt2"/>,
    /// <see cref="MessagePackCode.FixExt4"/>,
    /// <see cref="MessagePackCode.FixExt8"/>,
    /// <see cref="MessagePackCode.FixExt16"/>,
    /// <see cref="MessagePackCode.Ext8"/>,
    /// <see cref="MessagePackCode.Ext16"/>, or
    /// <see cref="MessagePackCode.Ext32"/>.
    /// </summary>
    /// <param name="extensionHeader">The extension header.</param>
    public void WriteExtensionFormatHeader(ExtensionHeader extensionHeader)
    {
        int dataLength = (int)extensionHeader.Length;
        byte typeCode = unchecked((byte)extensionHeader.TypeCode);
        switch (dataLength)
        {
            case 1:
                Span<byte> span = this.writer.GetSpan(2);
                span[0] = MessagePackCode.FixExt1;
                span[1] = unchecked(typeCode);
                this.writer.Advance(2);
                return;
            case 2:
                span = this.writer.GetSpan(2);
                span[0] = MessagePackCode.FixExt2;
                span[1] = unchecked(typeCode);
                this.writer.Advance(2);
                return;
            case 4:
                span = this.writer.GetSpan(2);
                span[0] = MessagePackCode.FixExt4;
                span[1] = unchecked(typeCode);
                this.writer.Advance(2);
                return;
            case 8:
                span = this.writer.GetSpan(2);
                span[0] = MessagePackCode.FixExt8;
                span[1] = unchecked(typeCode);
                this.writer.Advance(2);
                return;
            case 16:
                span = this.writer.GetSpan(2);
                span[0] = MessagePackCode.FixExt16;
                span[1] = unchecked(typeCode);
                this.writer.Advance(2);
                return;
            default:
                unchecked
                {
                    if (dataLength <= byte.MaxValue)
                    {
                        span = this.writer.GetSpan(dataLength + 3);
                        span[0] = MessagePackCode.Ext8;
                        span[1] = unchecked((byte)dataLength);
                        span[2] = unchecked(typeCode);
                        this.writer.Advance(3);
                    }
                    else if (dataLength <= ushort.MaxValue)
                    {
                        span = this.writer.GetSpan(dataLength + 4);
                        span[0] = MessagePackCode.Ext16;
                        WriteBigEndian((ushort)dataLength, span.Slice(1));
                        span[3] = unchecked(typeCode);
                        this.writer.Advance(4);
                    }
                    else
                    {
                        span = this.writer.GetSpan(dataLength + 6);
                        span[0] = MessagePackCode.Ext32;
                        WriteBigEndian(dataLength, span.Slice(1));
                        span[5] = unchecked(typeCode);
                        this.writer.Advance(6);
                    }
                }

                break;
        }
    }

    /// <summary>
    /// Writes an extension format, using the smallest one of these codes:
    /// <see cref="MessagePackCode.FixExt1"/>,
    /// <see cref="MessagePackCode.FixExt2"/>,
    /// <see cref="MessagePackCode.FixExt4"/>,
    /// <see cref="MessagePackCode.FixExt8"/>,
    /// <see cref="MessagePackCode.FixExt16"/>,
    /// <see cref="MessagePackCode.Ext8"/>,
    /// <see cref="MessagePackCode.Ext16"/>, or
    /// <see cref="MessagePackCode.Ext32"/>.
    /// </summary>
    /// <param name="extensionData">The extension data.</param>
    public void WriteExtensionFormat(ExtensionResult extensionData)
    {
        this.WriteExtensionFormatHeader(extensionData.Header);
        this.WriteSequence(extensionData.Data);
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

    /// <summary>
    /// Estimates the length of the header required for a given string.
    /// </summary>
    /// <param name="characterLength">The length of the string to be written, in characters.</param>
    /// <param name="bufferSize">Receives the guaranteed length of the returned buffer.</param>
    /// <param name="encodedBytesOffset">Receives the offset within the returned buffer to write the encoded string to.</param>
    /// <returns>
    /// A reference to the first byte in the buffer.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ref byte WriteString_PrepareSpan(int characterLength, out int bufferSize, out int encodedBytesOffset)
    {
        // MaxByteCount -> WritePrefix -> GetBytes has some overheads of `MaxByteCount`
        // solves heuristic length check

        // ensure buffer by MaxByteCount(faster than GetByteCount)
        bufferSize = Encoding.UTF8.GetMaxByteCount(characterLength) + 5;
        ref byte buffer = ref this.writer.GetPointer(bufferSize);

        int useOffset;
        if (characterLength <= MessagePackRange.MaxFixStringLength)
        {
            useOffset = 1;
        }
        else if (characterLength <= byte.MaxValue)
        {
            useOffset = 2;
        }
        else if (characterLength <= ushort.MaxValue)
        {
            useOffset = 3;
        }
        else
        {
            useOffset = 5;
        }

        encodedBytesOffset = useOffset;
        return ref buffer;
    }

    /// <summary>
    /// Finalizes an encoding of a string.
    /// </summary>
    /// <param name="pBuffer">A pointer obtained from a prior call to <see cref="WriteString_PrepareSpan"/>.</param>
    /// <param name="estimatedOffset">The offset obtained from a prior call to <see cref="WriteString_PrepareSpan"/>.</param>
    /// <param name="byteCount">The number of bytes used to actually encode the string.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void WriteString_PostEncoding(byte* pBuffer, int estimatedOffset, int byteCount)
    {
        int bufferLength = estimatedOffset + byteCount;

        // move body and write prefix
        if (byteCount <= MessagePackRange.MaxFixStringLength)
        {
            if (estimatedOffset != 1)
            {
                Buffer.MemoryCopy(pBuffer + estimatedOffset, pBuffer + 1, byteCount, byteCount);
            }

            pBuffer[0] = (byte)(MessagePackCode.MinFixStr | byteCount);
            this.writer.Advance(byteCount + 1);
        }
        else if (byteCount <= byte.MaxValue)
        {
            if (estimatedOffset != 2)
            {
                Buffer.MemoryCopy(pBuffer + estimatedOffset, pBuffer + 2, byteCount, byteCount);
            }

            pBuffer[0] = MessagePackCode.Str8;
            pBuffer[1] = unchecked((byte)byteCount);
            this.writer.Advance(byteCount + 2);
        }
        else if (byteCount <= ushort.MaxValue)
        {
            if (estimatedOffset != 3)
            {
                Buffer.MemoryCopy(pBuffer + estimatedOffset, pBuffer + 3, byteCount, byteCount);
            }

            pBuffer[0] = MessagePackCode.Str16;
            WriteBigEndian((ushort)byteCount, pBuffer + 1);
            this.writer.Advance(byteCount + 3);
        }
        else
        {
            if (estimatedOffset != 5)
            {
                Buffer.MemoryCopy(pBuffer + estimatedOffset, pBuffer + 5, byteCount, byteCount);
            }

            pBuffer[0] = MessagePackCode.Str32;
            WriteBigEndian((uint)byteCount, pBuffer + 1);
            this.writer.Advance(byteCount + 5);
        }
    }
}
