// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using Arc.Unit;

#pragma warning disable SA1124

namespace Arc.IO;

public class ByteSequence : IBufferWriter<byte>, IDisposable
{
    public const int DefaultVaultSize = 32 * 1024;
    private static ArrayPool<byte> arrayPool = ArrayPool<byte>.Create(80 * 1024, 100);

    #region FieldAndProperty

    private ByteVault? firstVault;
    private ByteVault? lastVault;

    #endregion

    public ByteArrayPool.MemoryOwner ToMemoryOwner()
    {
        if (this.firstVault == null)
        {
            return default;
        }
        else if (this.firstVault == this.lastVault)
        {// Single vault
            var memoryOwner = ByteArrayPool.Default.Rent(this.firstVault.Size).ToMemoryOwner(0, this.firstVault.Size);
            this.firstVault.Array.AsSpan(0, this.firstVault.Size).CopyTo(memoryOwner.Memory.Span);
            return memoryOwner;
        }
        else
        {// Multiple vaults
            var size = this.lastVault!.Size;
            var memoryOwner = ByteArrayPool.Default.Rent(size).ToMemoryOwner(0, size);
            var span = memoryOwner.Memory.Span;
            var segment = (ReadOnlySequenceSegment<byte>)this.firstVault;
            while (segment is not null)
            {
                segment.Memory.Span.CopyTo(span);
                span = span.Slice(segment.Memory.Length);
                segment = segment.Next;
            }

            return memoryOwner;
        }
    }

    public ReadOnlySequence<byte> ToReadOnlySequence()
    {
        return this.firstVault == null ?
            ReadOnlySequence<byte>.Empty :
            new ReadOnlySequence<byte>(this.firstVault, 0, this.lastVault!, this.lastVault!.Size);
    }

    public ReadOnlyMemory<byte> ToReadOnlyMemory()
    {
        if (this.firstVault == null)
        {
            return default;
        }
        else if (this.firstVault == this.lastVault)
        {// Single vault
            return new ReadOnlyMemory<byte>(this.firstVault.Array, 0, this.firstVault.Size);
        }
        else
        {// Multiple vaults
            return new ReadOnlySequence<byte>(this.firstVault, 0, this.lastVault!, this.lastVault!.Size).ToArray();
        }
    }

    public ReadOnlySpan<byte> ToReadOnlySpan()
    {
        if (this.firstVault == null)
        {
            return default;
        }
        else if (this.firstVault == this.lastVault)
        {// Single vault
            return new ReadOnlySpan<byte>(this.firstVault.Array, 0, this.firstVault.Size);
        }
        else
        {// Multiple vaults
            return new ReadOnlySequence<byte>(this.firstVault, 0, this.lastVault!, this.lastVault!.Size).ToArray();
        }
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
            // arrayPool.Return(this.Array); // Called by ByteSequence.
            this.Array = null!;
        }
    }
}
