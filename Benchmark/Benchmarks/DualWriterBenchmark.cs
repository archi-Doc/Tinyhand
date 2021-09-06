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
        private byte[]? dualBuffer;
        private int dualSize;
        // private Span<byte> originalSpan;
        private ByteBufferWriter writer;

        public DualWriter(byte[] dualBuffer)
        {
            this.dualBuffer = dualBuffer;
            this.dualSize = 0;
            this.writer = new();
        }

        public void WriteInt32(int value)
        {
            // if (this.CheckDualBuffer(5))
            {
                this.dualBuffer[this.dualSize] = MessagePackCode.Int32;
                TryWriteInt32(this.dualBuffer, this.dualSize, 0, value);
                this.dualSize += 5;
            }
            /*else
            {
                Span<byte> span = this.writer.GetSpan(5);
                span[0] = MessagePackCode.Int32;
                WriteBigEndian(value, span.Slice(1));
                this.writer.Advance(5);
            }*/
        }

        public byte[] FlushAndGetArray()
        {
            return this.dualBuffer.AsSpan(0, this.dualSize).ToArray();
            /*if (this.dualBuffer != null)
            {
                return this.dualBuffer.AsSpan(0, this.dualSpan.Length).ToArray();
            }
            else
            {
                return this.writer.FlushAndGetArray();
            }*/
        }

        /*private bool CheckDualBuffer(int size)
        {
            if (this.dualBuffer == null)
            {
                return false;
            }
            else if ((this.dualBuffer.Length - this.dualPosition) < size)
            {
                this.writer = new();
                this.writer.Write(this.dualBuffer);
                this.dualBuffer = null!;
                return false;
            }
            else
            {
                return true;
            }
        }*/

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe bool TryWriteInt32(byte[] buffer, int offset, int count, int value)
        {
            unchecked
            {
                fixed (byte* p = &buffer[offset])
                {
                    p[4] = (byte)value;
                    p[3] = (byte)(value >> 8);
                    p[2] = (byte)(value >> 16);
                    p[1] = (byte)(value >> 24);
                    p[0] = MessagePackCode.Int32;
                }
            }

            return true;
        }

        private static void WriteBigEndian(int value, Span<byte> span) => WriteBigEndian(unchecked((uint)value), span);

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
    }
}
