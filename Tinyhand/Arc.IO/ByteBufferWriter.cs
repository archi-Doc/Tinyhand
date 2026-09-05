// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using Arc.Collections;

#pragma warning disable SA1124
#pragma warning disable SA1202 // Elements should be ordered by access

namespace Arc.IO;

/// <summary>
/// Writes bytes to an external writer or an expandable buffer. Dispose it to release owned pooled buffers.
/// </summary>
public ref struct ByteBufferWriter
{
    [ThreadStatic]
    private static byte[]? primaryBuffer;
    [ThreadStatic]
    private static byte[]? secondaryBuffer;
    [ThreadStatic]
    private static bool primaryInUse;
    [ThreadStatic]
    private static bool secondaryInUse;

    // Nested serialization rents its own buffer instead of overwriting an active writer.
    internal static ByteBufferWriter CreateFromThreadStaticBuffer(bool secondary = false)
    {
        ref var inUse = ref (secondary ? ref secondaryInUse : ref primaryInUse);
        if (inUse)
        {
            return new ByteBufferWriter(BytePool.Default.Rent(Tinyhand.TinyhandSerializer.InitialBufferSize));
        }

        ref var buffer = ref (secondary ? ref secondaryBuffer : ref primaryBuffer);
        buffer ??= new byte[Tinyhand.TinyhandSerializer.InitialBufferSize];
        inUse = true;
        return new ByteBufferWriter(buffer) { threadStaticSlot = secondary ? (byte)2 : (byte)1 };
    }

    public ByteBufferWriter(IBufferWriter<byte> bufferWriter)
    { // Use other IBufferWriter instance (this.bufferWriter != null).
        this.byteSequence = null;
        this.bufferWriter = bufferWriter;
        this.span = this.bufferWriter.GetSpan();
        this.spanSize = 0;
        this.spanWritten = 0;
        this.initialBuffer = null;
    }

    public ByteBufferWriter(byte[] initialBuffer)
    { // Use initial buffer and ByteSequence (this.bufferWriter null -> not null, this.initialBuffer not null -> null).
        this.byteSequence = null;
        this.bufferWriter = null!;
        this.span = initialBuffer.AsSpan();
        this.spanSize = 0;
        this.spanWritten = 0;
        this.initialBuffer = initialBuffer;
    }

    public ByteBufferWriter(BytePool.RentArray array)
    { // Use ByteArrayPool.Owner and ByteSequence (this.bufferWriter null -> not null, this.initialBuffer not null -> null).
        this.byteSequence = null;
        this.bufferWriter = null!;
        this.span = array.AsSpan();
        this.spanSize = 0;
        this.spanWritten = 0;
        this.initialBuffer = array.Array;
        this.array = array;
    }

    #region FieldAndProperty

    private ByteSequence? byteSequence; // Fast byte sequence class.
    private IBufferWriter<byte> bufferWriter; // IBufferWriter instance.

    private Span<byte> span; // A byte span to be consumed.
    private int spanSize; // The size of the span.
    // private Span<byte> originalSpan; // The original (not sliced) version of the span.
    private long spanWritten; // The size of the written span.
    private byte[]? initialBuffer; // The initial buffer.
    private BytePool.RentArray? array;
    private byte threadStaticSlot;

    #endregion

    public void Dispose()
    {
        if (this.threadStaticSlot != 0)
        {
            if (this.threadStaticSlot == 1)
            {
                primaryInUse = false;
            }
            else
            {
                secondaryInUse = false;
            }

            this.threadStaticSlot = 0;
        }

        if (this.byteSequence is not null)
        {
            this.byteSequence.Dispose();
            this.byteSequence = default;
        }

        if (this.array is not null)
        {
            this.array.Return();
            this.array = default;
        }
    }

    /// <summary>
    /// Acquires a new buffer if necessary to ensure that some given number of bytes can be written to a single buffer.
    /// </summary>
    /// <param name="sizeHint">The number of bytes that must be allocated in a single buffer.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)] // Hot path.
    public void Ensure(int sizeHint = 1)
    {
        if (this.span.Length < sizeHint)
        {
            this.Allocate(sizeHint);
        }
    }

    /// <summary>
    /// Acquires a new span to write to, with an optional minimum size.
    /// </summary>
    /// <param name="sizeHint">The minimum size of the requested buffer.</param>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void Allocate(int sizeHint = 1)
    {
        this.Flush();

        if (this.bufferWriter == null)
        { // Create an instance of ByteSequence.
            this.byteSequence = new ByteSequence();
            this.bufferWriter = this.byteSequence;
        }

        var memory = this.bufferWriter.GetMemory(sizeHint);
        this.span = memory.Span; // this.spanSize is already initialized in Flush().
    }

    /// <summary>
    /// Gets a span with at least the requested capacity.
    /// </summary>
    /// <param name="sizeHint">The minimum size of the requested buffer.</param>
    /// <returns>A span to write to.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)] // Hot path.
    public Span<byte> GetSpan(int sizeHint)
    {
        if (this.span.Length < sizeHint)
        {
            this.Allocate(sizeHint);
        }

        return this.span;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref byte GetPointer(int sizeHint)
    {
        if (this.span.Length < sizeHint)
        {
            this.Allocate(sizeHint);
        }

        return ref this.span.GetPinnableReference();
    }

    /// <summary>
    /// Commits pending bytes to the underlying buffer writer.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Flush()
    {
        if (this.spanSize > 0)
        {
            if (this.bufferWriter == null)
            { // Initial buffer to ByteSequence.
                this.byteSequence = new ByteSequence();
                this.bufferWriter = this.byteSequence;
                var span = this.bufferWriter.GetSpan(this.spanSize);
                this.initialBuffer.AsSpan(0, this.spanSize).CopyTo(span);
                this.initialBuffer = default;
                if (this.array is not null)
                {
                    this.array.Return();
                    this.array = default;
                }
            }

            this.spanWritten += this.spanSize;
            this.bufferWriter.Advance(this.spanSize);
            this.span = default;
            this.spanSize = 0;
        }
    }

    /// <summary>
    /// Commits pending bytes and returns the written data as owned pooled memory.
    /// </summary>
    /// <returns>The written data. Return the memory to its pool after use.</returns>
    public BytePool.RentMemory FlushAndGetRentMemory()
    {
        if (this.bufferWriter == null)
        { // Initial Buffer
            if (this.array is { } rentArray)
            {
                this.array = default; // Prevent double return.
                return rentArray.AsMemory(0, this.spanSize);
            }
            else
            {
                return BytePool.RentMemory.CreateFrom(this.initialBuffer.AsSpan(0, this.spanSize).ToArray(), 0, this.spanSize);
            }
        }

        this.Flush();

        if (this.byteSequence == null)
        {
            throw new InvalidOperationException("FlushAndGetMemoryOwner() is not supported for external IBufferWriter<byte>.");
        }

        return this.byteSequence.ToRentMemory();
    }

    /// <summary>
    /// Commits pending bytes and returns the written data as a byte array.
    /// </summary>
    /// <returns>A byte array consisting of the written data.</returns>
    public byte[] FlushAndGetArray()
    {
        if (this.bufferWriter == null)
        { // Initial Buffer
            return this.initialBuffer.AsSpan(0, this.spanSize).ToArray();
        }

        this.Flush();

        if (this.byteSequence == null)
        {
            throw new InvalidOperationException("FlushAndGetArray() is not supported for external IBufferWriter<byte>.");
        }

        return this.byteSequence.ToReadOnlySequence().ToArray();
    }

    /// <summary>
    /// Commits pending bytes and returns the written data as a read-only sequence.
    /// </summary>
    /// <returns>A sequence of the written bytes, valid until the writer is reused or disposed.</returns>
    public ReadOnlySequence<byte> FlushAndGetReadOnlySequence()
    {
        if (this.bufferWriter == null)
        { // Initial Buffer
            return this.spanSize == 0 ? ReadOnlySequence<byte>.Empty : new ReadOnlySequence<byte>(this.initialBuffer!, 0, this.spanSize);
        }

        this.Flush();

        if (this.byteSequence == null)
        {
            throw new InvalidOperationException("FlushAndGetReadOnlySequence() is not supported for external IBufferWriter<byte>.");
        }

        return this.byteSequence.ToReadOnlySequence();
    }

    /// <summary>
    /// Commits pending bytes and returns the written data as a byte array.
    /// </summary>
    /// <param name="array">The byte array containing the written data.</param>
    /// <param name="written">The total number of bytes written by the writer.</param>
    /// <param name="isInitialBuffer"><see langword="true"/> if the byte array is the initial buffer.</param>
    public void FlushAndGetArray(out byte[] array, out int written, out bool isInitialBuffer)
    {
        if (this.bufferWriter == null)
        { // Initial Buffer
            array = this.initialBuffer!;
            written = this.spanSize;
            isInitialBuffer = true;
            return;
        }

        this.Flush();

        if (this.byteSequence == null)
        {
            throw new InvalidOperationException("FlushAndGetArray() is not supported for external IBufferWriter<byte>.");
        }

        array = this.byteSequence.ToReadOnlySequence().ToArray();
        written = array.Length;
        isInitialBuffer = false;
    }

    /// <summary>
    /// Commits pending bytes and returns the written memory region.
    /// </summary>
    /// <param name="memory">The memory region consisting of the written data.</param>
    /// <param name="isInitialBuffer"><see langword="true"/>: The memory region is a part of the initial buffer.</param>
    public void FlushAndGetMemory(out Memory<byte> memory, out bool isInitialBuffer)
    {
        if (this.bufferWriter == null)
        { // Initial Buffer
            memory = this.initialBuffer.AsMemory(0, this.spanSize);
            isInitialBuffer = true;
            return;
        }

        this.Flush();

        if (this.byteSequence == null)
        {
            throw new InvalidOperationException("FlushAndGetMemory() is not supported for external IBufferWriter<byte>.");
        }

        memory = this.byteSequence.ToReadOnlySequence().ToArray().AsMemory();
        isInitialBuffer = false;
    }

    /// <summary>
    /// Commits pending bytes and returns a read-only span of the written data.
    /// </summary>
    /// <param name="span">A byte span consisting of the written data.</param>
    /// <param name="isInitialBuffer"><see langword="true"/>: The byte span is a part of the initial buffer.</param>
    public void FlushAndGetReadOnlySpan(out ReadOnlySpan<byte> span, out bool isInitialBuffer)
    {
        if (this.bufferWriter == null)
        { // Initial Buffer
            span = this.initialBuffer.AsSpan(0, this.spanSize);
            isInitialBuffer = true;
            return;
        }

        this.Flush();

        if (this.byteSequence == null)
        {
            throw new InvalidOperationException("FlushAndGetReadOnlySequence() is not supported for external IBufferWriter<byte>.");
        }

        span = this.byteSequence.ToReadOnlySpan();
        isInitialBuffer = false;
    }

    /// <summary>
    /// Notifies that data is written to the output span.
    /// </summary>
    /// <param name="count">The number of bytes written to the current span.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)] // Hot path.
    public void Advance(int count)
    {
        this.spanSize += count;
        this.span = this.span.Slice(count); // Faster then position++
    }

    /// <summary>
    /// Gets the total number of bytes written by the writer.
    /// </summary>
    public long Written => this.spanWritten + this.spanSize;

    /// <summary>
    /// Writes a span of bytes to the buffer.
    /// </summary>
    /// <param name="source">A source span.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(scoped ReadOnlySpan<byte> source)
    {
        if (this.span.Length >= source.Length)
        {
            source.CopyTo(this.span);
            this.Advance(source.Length);
        }
        else
        {
            this.WriteMultiBuffer(source);
        }
    }

    private void WriteMultiBuffer(scoped ReadOnlySpan<byte> source)
    {
        int copiedBytes = 0;
        int bytesLeftToCopy = source.Length;
        while (bytesLeftToCopy > 0)
        {
            if (this.span.Length == 0)
            {
                this.Allocate();
            }

            var writable = Math.Min(bytesLeftToCopy, this.span.Length);
            source.Slice(copiedBytes, writable).CopyTo(this.span);
            copiedBytes += writable;
            bytesLeftToCopy -= writable;
            this.Advance(writable);
        }
    }
}
