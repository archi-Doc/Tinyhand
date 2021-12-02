// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.Runtime.CompilerServices;

#pragma warning disable SA1202 // Elements should be ordered by access

namespace Arc.IO
{
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
                // arrayPool.Return(this.Array); // Called by ByteSequence.
                this.Array = null!;
            }
        }
    }
}
