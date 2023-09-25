// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.Runtime.CompilerServices;

#pragma warning disable SA1202 // Elements should be ordered by access

namespace Arc.IO;

/// <summary>
/// Buffer Writer. Wraps <see cref="IBufferWriter{T}"/> and get byte array.
/// Call Dispose() to return a byte array that is rent by ByteSequence.
/// </summary>
public ref struct ByteBufferWriter
{
    private ByteSequence? byteSequence; // Fast byte sequence class.
    private IBufferWriter<byte> bufferWriter; // IBufferWriter instance.

    private Span<byte> span; // A byte span to be consumed.
    private int spanSize; // The size of the span.
    // private Span<byte> originalSpan; // The original (not sliced) version of the span.
    private long spanWritten; // The size of the written span.
    private byte[]? initialBuffer; // The initial buffer.

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

    public void Dispose()
    {
        if (this.byteSequence != null)
        {
            this.byteSequence.Dispose();
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
    /// Get a span to write to.
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
    /// Notifies the <see cref="IBufferWriter{T}"/>  that count data items were written to the output.
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
            }

            this.spanWritten += this.spanSize;
            this.bufferWriter.Advance(this.spanSize);
            this.span = default;
            this.spanSize = 0;
        }
    }

    /// <summary>
    /// Notifies the <see cref="IBufferWriter{T}"/>  that count data items were written to the output and get a byte array.
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

    /*/// <summary>
    /// Notifies the <see cref="IBufferWriter{T}"/>  that count data items were written to the output and get a byte array.<br/>
    /// Pursue perfection.
    /// </summary>
    /// <param name="rawArray">A byte array containing the written data.</param>
    /// <param name="written">The total number of bytes written by the writer.</param>
    public void FlushAndGetArray(out byte[] rawArray, out int written)
    {
        if (this.bufferWriter == null)
        { // Initial Buffer
            rawArray = this.initialBuffer!;
            written = this.spanSize;
            return;
        }

        this.Flush();

        if (this.byteSequence == null)
        {
            throw new InvalidOperationException("FlushAndGetArray() is not supported for external IBufferWriter<byte>.");
        }

        rawArray = this.byteSequence.ToReadOnlySequence().ToArray();
        written = rawArray.Length;
    }*/

    /// <summary>
    /// Notifies the <see cref="IBufferWriter{T}"/>  that count data items were written to the output and get a <see cref="ReadOnlySequence{T}" />.
    /// </summary>
    /// <returns>A byte array consisting of the written data.</returns>
    public ReadOnlySequence<byte> FlushAndGetReadOnlySequence()
    {
        if (this.bufferWriter == null)
        { // Initial Buffer
            return new ReadOnlySequence<byte>(this.initialBuffer.AsSpan(0, this.spanSize).ToArray());
        }

        this.Flush();

        if (this.byteSequence == null)
        {
            throw new InvalidOperationException("FlushAndGetReadOnlySequence() is not supported for external IBufferWriter<byte>.");
        }

        return this.byteSequence.ToReadOnlySequence();
    }

    /// <summary>
    /// Notifies the <see cref="IBufferWriter{T}"/>  that count data items were written to the output and get a memory region.<br/>
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
    /// Notifies the <see cref="IBufferWriter{T}"/>  that count data items were written to the output and get a <see cref="ReadOnlySpan{T}" />.
    /// </summary>
    /// <param name="span">A byte span consisting of the written data.</param>
    /// <param name="isIinitialBuffer"><see langword="true"/>: The byte span is a part of the initial buffer.</param>
    public void FlushAndGetReadOnlySpan(out ReadOnlySpan<byte> span, out bool isIinitialBuffer)
    {
        if (this.bufferWriter == null)
        { // Initial Buffer
            span = this.initialBuffer.AsSpan(0, this.spanSize);
            isIinitialBuffer = true;
            return;
        }

        this.Flush();

        if (this.byteSequence == null)
        {
            throw new InvalidOperationException("FlushAndGetReadOnlySequence() is not supported for external IBufferWriter<byte>.");
        }

        span = this.byteSequence.ToReadOnlySpan();
        isIinitialBuffer = false;
    }

    /// <summary>
    /// Notifies that data is written to the output span.
    /// </summary>
    /// <param name="count">The number of written data.</param>
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
    /// Write a span of data to the buffer.
    /// </summary>
    /// <param name="source">A source span.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(ReadOnlySpan<byte> source)
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

    private void WriteMultiBuffer(ReadOnlySpan<byte> source)
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
