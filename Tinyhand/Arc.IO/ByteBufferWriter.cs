// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.Runtime.CompilerServices;

#pragma warning disable SA1202 // Elements should be ordered by access

namespace Arc.IO
{
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
        private Span<byte> originalSpan; // The original (not sliced) version of the span.

        public ByteBufferWriter(IBufferWriter<byte> bufferWriter)
        { // Use other IBufferWriter instance.
            this.byteSequence = null;
            this.bufferWriter = bufferWriter;
            this.span = this.bufferWriter.GetSpan();
            this.spanSize = 0;
            this.originalSpan = this.span;
        }

        public ByteBufferWriter(byte[] initialBuffer)
        { // Use initial buffer and ByteSequence.
            this.byteSequence = null;
            this.bufferWriter = null!;
            this.span = initialBuffer.AsSpan();
            this.spanSize = 0;
            this.originalSpan = this.span;
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
            this.originalSpan = this.span;
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
                { // Initial Buffer to ByteSequence.
                    this.byteSequence = new ByteSequence();
                    this.bufferWriter = this.byteSequence;
                    var span = this.bufferWriter.GetSpan(this.spanSize);
                    this.originalSpan.Slice(0, this.spanSize).CopyTo(span);
                }

                this.bufferWriter.Advance(this.spanSize);
                this.span = default;
                this.spanSize = 0;
                this.originalSpan = default;
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
                return this.originalSpan.Slice(0, this.spanSize).ToArray();
            }

            this.Flush();

            if (this.byteSequence == null)
            {
                throw new InvalidOperationException("FlushAndGetArray() is not supported for external IBufferWriter<byte>.");
            }

            return this.byteSequence.GetReadOnlySequence().ToArray();
        }

        /// <summary>
        /// Notifies the <see cref="IBufferWriter{T}"/>  that count data items were written to the output and get a <see cref="ReadOnlySequence{T}" />.
        /// </summary>
        /// <returns>A byte array consisting of the written data.</returns>
        public ReadOnlySequence<byte> FlushAndGetReadOnlySequence()
        {
            if (this.bufferWriter == null)
            { // Initial Buffer
                return new ReadOnlySequence<byte>(this.originalSpan.Slice(0, this.spanSize).ToArray());
            }

            this.Flush();

            if (this.byteSequence == null)
            {
                throw new InvalidOperationException("FlushAndGetReadOnlySequence() is not supported for external IBufferWriter<byte>.");
            }

            return this.byteSequence.GetReadOnlySequence();
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

    public class ByteSequence : IBufferWriter<byte>, IDisposable
    {
        public const int DefaultVaultSize = 32 * 1024;
        private static ArrayPool<byte> arrayPool = ArrayPool<byte>.Create(80 * 1024, 100);

        private ByteVault? firstVault;
        private ByteVault? lastVault;

        public ReadOnlySequence<byte> GetReadOnlySequence()
        {
            return this.firstVault == null ?
                ReadOnlySequence<byte>.Empty :
                new ReadOnlySequence<byte>(this.firstVault, 0, this.lastVault!, this.lastVault!.Size);
        }

        public void Advance(int count)
        {
            if (this.lastVault == null)
            {
                throw new InvalidOperationException("Cannot advance before acquiring memory.");
            }

            this.lastVault.Advance(count);
        }

        public void Dispose()
        {
            var current = this.firstVault;
            while (current != null)
            {
                var next = (ByteVault?)current.Next;

                arrayPool.Return(current.Array);
                current.Clear();

                current = next;
            }

            this.firstVault = this.lastVault = null;
        }

        public Memory<byte> GetMemory(int sizeHint = 0) => this.GetVault(sizeHint).RemainingMemory;

        public Span<byte> GetSpan(int sizeHint = 0) => this.GetVault(sizeHint).RemainingSpan;

        private ByteVault GetVault(int sizeHint)
        {
            int bufferSizeToAllocate = 0;

            if (sizeHint == 0)
            {
                if (this.lastVault == null || this.lastVault.Remaining == 0)
                {
                    bufferSizeToAllocate = DefaultVaultSize;
                }
            }
            else
            {
                if (this.lastVault == null || this.lastVault.Remaining < sizeHint)
                {
                    bufferSizeToAllocate = Math.Max(sizeHint, DefaultVaultSize);
                }
            }

            if (bufferSizeToAllocate > 0)
            {
                var vault = new ByteVault(arrayPool.Rent(bufferSizeToAllocate));
                this.AddVault(vault);
            }

            return this.lastVault!;
        }

        private void AddVault(ByteVault vault)
        {
            if (this.lastVault == null)
            {
                this.firstVault = this.lastVault = vault;
            }
            else
            {
                if (this.lastVault.Size > 0)
                {// Add a new block.
                    this.lastVault.SetNext(vault);
                }
                else
                {// The last block is completely unused. Replace it instead of appending to it.
                    var current = this.firstVault!;
                    if (this.firstVault == this.lastVault)
                    { // Only one vault.
                        this.firstVault = vault;
                    }
                    else
                    {
                        while (current.Next != this.lastVault)
                        {
                            current = (ByteVault)current.Next!;
                        }
                    }

                    arrayPool.Return(this.lastVault.Array);
                    this.lastVault.Clear();

                    current.SetNext(vault);
                }

                this.lastVault = vault;
            }
        }

        private class ByteVault : ReadOnlySequenceSegment<byte>
        {
            public ByteVault(byte[] array)
            {
                this.Array = array;
                this.Memory = array;
            }

            internal byte[] Array { get; set; }

            internal int Size { get; set; }

            internal int Remaining => this.Array.Length - this.Size;

            internal Memory<byte> RemainingMemory => this.Array.AsMemory().Slice(this.Size);

            internal Span<byte> RemainingSpan => this.Array.AsSpan().Slice(this.Size);

            internal void Advance(int count)
            {
                this.Size += count;
                if (count < 0 || this.Size > this.Array.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(count));
                }
            }

            internal void SetNext(ByteVault next)
            {
                this.Next = next;
                next.RunningIndex = this.RunningIndex + this.Size;
                this.Memory = this.Memory.Slice(0, this.Size);
            }

            internal void Clear()
            {
                this.Memory = default;
                this.Next = null;
                this.RunningIndex = 0;
                this.Size = 0;
                arrayPool.Return(this.Array);
                this.Array = null!;
            }
        }
    }
}
