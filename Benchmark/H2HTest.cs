// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

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

    [Config(typeof(BenchmarkConfig))]
    public class H2HBenchmark
    {
        ObjectH2H h2h = default!;
        byte[] data = default!;

        public H2HBenchmark()
        {
        }

        [GlobalSetup]
        public void Setup()
        {
            this.h2h = new ObjectH2H();
            this.data = MessagePack.MessagePackSerializer.Serialize(this.h2h);
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
    }
}
