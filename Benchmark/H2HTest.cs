﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

/*  BenchmarkDotNet, small template code
 *  PM> Install-Package BenchmarkDotNet
 */

using System.Linq;
using BenchmarkDotNet.Attributes;
using Tinyhand;
using Tinyhand.IO;

#pragma warning disable SA1401 // Fields should be private

namespace Benchmark.H2HTest
{
    [MessagePack.MessagePackObject]
    [TinyhandObject]
    public partial class ObjectH2H
    {
        public const int ArrayN = 10;

        public ObjectH2H()
        {
            this.B = Enumerable.Range(0, ArrayN).ToArray();
        }

        [MessagePack.Key(0)]
        [Key(0)]
        public int X { get; set; }

        [MessagePack.Key(1)]
        [Key(1)]
        public int Y { get; set; }

        [MessagePack.Key(2)]
        [Key(2)]
        public int Z { get; set; }

        [MessagePack.Key(3)]
        [Key(3)]
        public string A { get; set; } = "H2Htest";

        [MessagePack.Key(8)]
        [Key(8)]
        public int[] B { get; set; } = new int[0];
    }

    [MessagePack.MessagePackObject(true)]
    [TinyhandObject(KeyAsPropertyName = true)]
    public partial class ObjectH2H2
    {
        public const int ArrayN = 10;

        public ObjectH2H2()
        {
            this.B = Enumerable.Range(0, ArrayN).ToArray();
        }

        public int X { get; set; }

        public int Y { get; set; }

        public int Z { get; set; }

        public string A { get; set; } = "H2Htest";

        public int[] B { get; set; } = new int[0];
    }

    [Config(typeof(BenchmarkConfig))]
    public class H2HBenchmark
    {
        ObjectH2H h2h = default!;
        byte[] data = default!;

        ObjectH2H2 h2h2 = default!;
        byte[] data2 = default!;

        public H2HBenchmark()
        {
        }

        [GlobalSetup]
        public void Setup()
        {
            this.h2h = new ObjectH2H();
            this.data = MessagePack.MessagePackSerializer.Serialize(this.h2h);

            this.h2h2 = new ObjectH2H2();
            this.data2 = MessagePack.MessagePackSerializer.Serialize(this.h2h2);
        }

        [GlobalCleanup]
        public void Cleanup()
        {
        }

        [Benchmark]
        public byte[] SerializeMessagePack()
        {
            return MessagePack.MessagePackSerializer.Serialize(this.h2h);
        }

        [Benchmark]
        public byte[] SerializeTinyhand()
        {
            return Tinyhand.TinyhandSerializer.Serialize(this.h2h);
        }

        [Benchmark]
        public ObjectH2H DeserializeMessagePack()
        {
            return MessagePack.MessagePackSerializer.Deserialize<ObjectH2H>(this.data);
        }

        [Benchmark]
        public ObjectH2H? DeserializeTinyhand()
        {
            return Tinyhand.TinyhandSerializer.Deserialize<ObjectH2H>(this.data);
        }

        [Benchmark]
        public byte[] SerializeMessagePackString()
        {
            return MessagePack.MessagePackSerializer.Serialize(this.h2h2);
        }

        [Benchmark]
        public byte[] SerializeTinyhandString()
        {
            return Tinyhand.TinyhandSerializer.Serialize(this.h2h2);
        }

        [Benchmark]
        public ObjectH2H2 DeserializeMessagePackString()
        {
            return MessagePack.MessagePackSerializer.Deserialize<ObjectH2H2>(this.data2);
        }

        [Benchmark]
        public ObjectH2H2? DeserializeTinyhandString()
        {
            return Tinyhand.TinyhandSerializer.Deserialize<ObjectH2H2>(this.data2);
        }
    }
}
