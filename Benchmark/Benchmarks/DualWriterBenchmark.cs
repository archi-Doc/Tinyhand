// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using Arc.IO;
using Tinyhand;
using Tinyhand.IO;
using System.Runtime.CompilerServices;

namespace Benchmark.DualWriter
{
    public ref struct DualWriter
    {
        private byte[] dualBuffer;
        private int dualSize;
        // private Span<byte> originalSpan;
        private ByteBufferWriter writer;

        private int DualRemaining => (dualBuffer.Length - this.dualSize);

        public DualWriter(byte[] dualBuffer)
        {
            this.dualBuffer = dualBuffer;
            this.dualSize = 0;
            this.writer = new();
        }

        public unsafe void WriteInt32(int value)
        {// Pure byte[] writer is fast, but
            fixed (byte* p = &this.dualBuffer[this.dualSize])
            {
                p[0] = MessagePackCode.Int32;
                WriteBigEndianUnsafe(value, p);
            }

            this.dualSize += 5;
        }

        public unsafe void WriteInt32B(int value)
        {// Adding some features reduces the performance to almost the same level of current TinyhandWriter...
            if (this.CheckDualBuffer(5))
            {
                fixed (byte* p = &this.dualBuffer![this.dualSize])
                {
                    p[0] = MessagePackCode.Int32;
                    WriteBigEndianUnsafe(value, p);
                }

                this.dualSize += 5;
            }
            else
            {
                Span<byte> span = this.writer.GetSpan(5);
                span[0] = MessagePackCode.Int32;
                WriteBigEndian(value, span.Slice(1));
                this.writer.Advance(5);
            }
        }

        public byte[] FlushAndGetArray()
        {
            if (this.dualBuffer != null)
            {
                return this.dualBuffer.AsSpan(0, this.dualSize).ToArray();
            }
            else
            {
                return this.writer.FlushAndGetArray();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool CheckDualBuffer(int size)
        {
            if (this.dualBuffer == null)
            {
                return false;
            }
            else if ((this.dualBuffer.Length - this.dualSize) < size)
            {
                this.writer = new();
                this.writer.Write(this.dualBuffer.AsSpan(0, this.dualSize));
                this.dualBuffer = null!;
                return false;
            }
            else
            {
                return true;
            }
        }

        private static unsafe void WriteBigEndianUnsafe(int value, byte* p) => WriteBigEndianUnsafe(unchecked((uint)value), p);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void WriteBigEndianUnsafe(uint value, byte* p)
        {
            unchecked
            {
                p[3] = (byte)value;
                p[2] = (byte)(value >> 8);
                p[1] = (byte)(value >> 16);
                p[1] = (byte)(value >> 24);
            }
        }

        private static void WriteBigEndian(int value, Span<byte> span) => WriteBigEndian(unchecked((uint)value), span);

        private static void WriteBigEndian(uint value, Span<byte> span)
        {
            unchecked
            {
                span[3] = (byte)value;
                span[2] = (byte)(value >> 8);
                span[1] = (byte)(value >> 16);
                span[0] = (byte)(value >> 24);
            }
        }
    }

    [Config(typeof(BenchmarkConfig))]
    public class DualWriterBenchmark
    {
        public byte[] InitialBuffer { get; set; } = new byte[1024];

        public DualWriterBenchmark()
        {
        }

        [GlobalSetup]
        public void Setup()
        {
        }

        [Benchmark]
        public byte[] Original_Write()
        {
            var w = new TinyhandWriter(InitialBuffer);

            for (var n = 0; n < 10; n++)
            {
                w.WriteInt32(n);
            }

            return w.FlushAndGetArray();
        }

        [Benchmark]
        public byte[] Dual_Write()
        {
            var w = new DualWriter(InitialBuffer);

            for (var n = 0; n < 10; n++)
            {
                w.WriteInt32(n);
            }

            return w.FlushAndGetArray();
        }

        [Benchmark]
        public byte[] Dual_WriteB()
        {
            var w = new DualWriter(InitialBuffer);

            for (var n = 0; n < 10; n++)
            {
                w.WriteInt32B(n);
            }

            return w.FlushAndGetArray();
        }
    }
}
