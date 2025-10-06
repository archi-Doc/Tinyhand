// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

/*  BenchmarkDotNet, small template code
 *  PM> Install-Package BenchmarkDotNet
 */

using Arc.Collections;
using BenchmarkDotNet.Attributes;
using Tinyhand;

namespace Benchmark.H2HTest;

[TinyhandObject(ImplicitMemberNameAsKey = true)]
[MessagePack.MessagePackObject(true)]
public partial class LargeDataClass
{
    public const int M = 456;
    public const int N = 1_234_567;

    public void Prepare()
    {
        this.Int = new int[N];
        for (var n = 0; n < N; n++)
        {
            this.Int[n] = n;
        }

        this.String = new string[M];
        this.String[0] = "test";
        for (var n = 1; n < M; n++)
        {
            this.String[n] = this.String[n - 1] + n.ToString();
        }
    }

    public int[] Int { get; set; } = default!;

    public string[] String { get; set; } = default!;
}

[Config(typeof(BenchmarkConfig))]
public class H2HLarge
{
    LargeDataClass h2h = default!;
    byte[] data = default!;
    byte[] dataMp = default!;
    byte[] dataTh = default!;

    public H2HLarge()
    {
    }

    [GlobalSetup]
    public void Setup()
    {
        this.h2h = new LargeDataClass();
        this.h2h.Prepare();
        this.data = MessagePack.MessagePackSerializer.Serialize(this.h2h);
        this.dataMp = MessagePack.MessagePackSerializer.Serialize(this.h2h, MessagePack.MessagePackSerializerOptions.Standard.WithCompression(MessagePack.MessagePackCompression.Lz4BlockArray));
        this.dataTh = TinyhandSerializer.Serialize(this.h2h, TinyhandSerializerOptions.Lz4);
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
        return TinyhandSerializer.Serialize(this.h2h);
    }

    [Benchmark]
    public BytePool.RentMemory SerializeTinyhand2()
    {
        var rentMemory = TinyhandSerializer.SerializeToRentMemory(this.h2h);
        rentMemory.Return();
        return rentMemory;
    }

    /*[Benchmark]
    public LargeDataClass DeserializeMessagePack()
    {
        return MessagePack.MessagePackSerializer.Deserialize<LargeDataClass>(this.data);
    }

    [Benchmark]
    public LargeDataClass? DeserializeTinyhand()
    {
        return Tinyhand.TinyhandSerializer.Deserialize<LargeDataClass>(this.data);
    }

    [Benchmark]
    public byte[] SerializeMessagePackLz4()
    {
        return MessagePack.MessagePackSerializer.Serialize(this.h2h, MessagePack.MessagePackSerializerOptions.Standard.WithCompression(MessagePack.MessagePackCompression.Lz4BlockArray));
    }

    [Benchmark]
    public byte[] SerializeTinyhandLz4()
    {
        return Tinyhand.TinyhandSerializer.Serialize(this.h2h, TinyhandSerializerOptions.Lz4);
    }

    [Benchmark]
    public LargeDataClass DeserializeMessagePackLz4()
    {
        return MessagePack.MessagePackSerializer.Deserialize<LargeDataClass>(this.dataMp, MessagePack.MessagePackSerializerOptions.Standard.WithCompression(MessagePack.MessagePackCompression.Lz4BlockArray));
    }

    [Benchmark]
    public LargeDataClass? DeserializeTinyhandLz4()
    {
        return Tinyhand.TinyhandSerializer.Deserialize<LargeDataClass>(this.dataTh, TinyhandSerializerOptions.Lz4);
    }*/
}
