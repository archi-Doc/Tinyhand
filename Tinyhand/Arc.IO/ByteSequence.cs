// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using Arc.Collections;

#pragma warning disable SA1124

namespace Arc.IO;

/// <summary>
/// Stores bytes in pooled segments and exposes them as a buffer writer or read-only sequence. Dispose it after use.
/// </summary>
public class ByteSequence : IBufferWriter<byte>, IDisposable
{
    public const int DefaultVaultSize = 32 * 1024;

    #region FieldAndProperty

    private ByteVault? firstVault;
    private ByteVault? lastVault;

    #endregion

    public BytePool.RentedMemory ToRentMemory()
    {
        if (this.firstVault == null)
        {
            return default;
        }
        else if (this.firstVault == this.lastVault)
        {// Single vault
            var memory = BytePool.Default.Rent(this.firstVault.Size).AsMemory(0, this.firstVault.Size);
            this.firstVault.RentArray.Array.AsSpan(0, this.firstVault.Size).CopyTo(memory.Span);
            return memory;
        }
        else
        {// Multiple vaults
            var size = (int)this.lastVault!.RunningIndex + this.lastVault!.Size;
            var memory = BytePool.Default.Rent(size).AsMemory(0, size);
            var span = memory.Span;
            var vault = this.firstVault;
            while (vault is not null)
            {
                vault.Memory.Slice(0, vault.Size).Span.CopyTo(span);
                span = span.Slice(vault.Size);
                vault = vault.Next as ByteVault;
            }

            return memory;
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
            return new ReadOnlyMemory<byte>(this.firstVault.RentArray.Array, 0, this.firstVault.Size);
        }
        else
        {// Multiple vaults
            return new ReadOnlySequence<byte>(this.firstVault, 0, this.lastVault!, this.lastVault!.Size).ToArray();
        }
    }

    public ReadOnlySpan<byte> ToReadOnlySpan() => this.ToReadOnlySpan(0);

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

            current.RentArray.Return();
            current.Clear();

            current = next;
        }

        this.firstVault = this.lastVault = null;
    }

    public Memory<byte> GetMemory(int sizeHint = 0) => this.GetVault(sizeHint).RemainingMemory;

    public Span<byte> GetSpan(int sizeHint = 0) => this.GetVault(sizeHint).RemainingSpan;

    /// <summary>
    /// Gets the written data followed by <paramref name="pending"/> bytes that were written to the last vault but not advanced yet.
    /// </summary>
    /// <param name="pending">The number of bytes written after the last <see cref="Advance(int)"/>.</param>
    /// <returns>The data; a single vault is returned without copying.</returns>
    internal ReadOnlySpan<byte> ToReadOnlySpan(int pending)
    {
        if (this.firstVault == null)
        {
            return default;
        }
        else if (this.firstVault == this.lastVault)
        {// Single vault
            return new ReadOnlySpan<byte>(this.firstVault.RentArray.Array, 0, this.firstVault.Size + pending);
        }
        else
        {// Multiple vaults
            var lastVault = this.lastVault!;
            var array = new byte[(int)lastVault.RunningIndex + lastVault.Size + pending];
            var span = array.AsSpan();
            for (var vault = this.firstVault; vault != lastVault; vault = (ByteVault)vault.Next!)
            {
                vault.RentArray.Array.AsSpan(0, vault.Size).CopyTo(span);
                span = span.Slice(vault.Size);
            }

            lastVault.RentArray.Array.AsSpan(0, lastVault.Size + pending).CopyTo(span);
            return array;
        }
    }

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
            var vault = new ByteVault(BytePool.Default.Rent(bufferSizeToAllocate));
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

                this.lastVault.RentArray.Return();
                this.lastVault.Clear();

                current.SetNext(vault);
            }

            this.lastVault = vault;
        }
    }

    private class ByteVault : ReadOnlySequenceSegment<byte>
    {
        public ByteVault(BytePool.RentedArray rentArray)
        {
            this.RentArray = rentArray;
            this.Memory = rentArray.Array;
        }

        internal BytePool.RentedArray RentArray { get; set; }

        internal int Size { get; set; }

        internal int Remaining => this.RentArray.Array.Length - this.Size;

        internal Memory<byte> RemainingMemory => this.RentArray.Array.AsMemory(this.Size);

        internal Span<byte> RemainingSpan => this.RentArray.Array.AsSpan(this.Size);

        internal void Advance(int count)
        {
            if ((uint)count > (uint)this.Remaining)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            this.Size += count;
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
            this.RentArray = null!;
        }
    }
}
