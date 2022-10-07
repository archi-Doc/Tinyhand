// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

/*  BenchmarkDotNet, small template code
 *  PM> Install-Package BenchmarkDotNet
 */

using System;
using System.Buffers;
using System.IO;
using System.Linq;
using System.Text.Json;
using BenchmarkDotNet.Attributes;
using ProtoBuf;
// using MemoryPack;
using Tinyhand;
using Tinyhand.IO;

#pragma warning disable SA1401 // Fields should be private

namespace Benchmark.H2HTest;

[ProtoContract]
[MessagePack.MessagePackObject]
[TinyhandObject]
// [MemoryPackable]
public partial class ObjectH2H
{
    public const int ArrayN = 10;

    public ObjectH2H()
    {
        this.B = Enumerable.Range(0, ArrayN).ToArray();
    }

    [ProtoMember(1)]
    [MessagePack.Key(0)]
    [Key(0)]
    public int X { get; set; } = 0;

    [ProtoMember(2)]
    [MessagePack.Key(1)]
    [Key(1)]
    public int Y { get; set; } = 100;

    [ProtoMember(3)]
    [MessagePack.Key(2)]
    [Key(2)]
    public int Z { get; set; } = 10000;

    [ProtoMember(4)]
    [MessagePack.Key(3)]
    [Key(3)]
    public string A { get; set; } = "H2Htest";

    [ProtoMember(9)]
    [MessagePack.Key(8)]
    [Key(8)]
    public int[] B { get; set; } = new int[0];
}

[MessagePack.MessagePackObject(true)]
[TinyhandObject(ImplicitKeyAsName = true)]
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
public class H2HSandbox
{
    ObjectH2H2 h2h2 = default!;
    byte[] utf8 = default!;
    byte[] json = default!;
    TinyhandSerializerOptions simple = default!;

    [GlobalSetup]
    public void Setup()
    {
        this.h2h2 = new ObjectH2H2();
        this.utf8 = TinyhandSerializer.SerializeToUtf8(this.h2h2);
        this.json = JsonSerializer.SerializeToUtf8Bytes(this.h2h2);
        this.simple = TinyhandSerializerOptions.Standard with { Compose = TinyhandComposeOption.Simple, };
    }

    [Benchmark]
    public byte[] SerializeTinyhandStringUtf8()
    {
        return Tinyhand.TinyhandSerializer.SerializeToUtf8(this.h2h2);
    }

    [Benchmark]
    public byte[] SerializeTinyhandStringUtf8Simple()
    {
        return Tinyhand.TinyhandSerializer.SerializeToUtf8(this.h2h2, this.simple);
    }

    [Benchmark]
    public string SerializeMessagePackStringUtf8()
    {
        return MessagePack.MessagePackSerializer.SerializeToJson(this.h2h2);
    }

    [Benchmark]
    public byte[] SerializeJsonStringUtf8()
    {
        return JsonSerializer.SerializeToUtf8Bytes(this.h2h2);
    }

    [Benchmark]
    public ObjectH2H2? DeserializeTinyhandStringUtf8()
    {
        return Tinyhand.TinyhandSerializer.DeserializeFromUtf8<ObjectH2H2>(this.utf8);
    }

    [Benchmark]
    public ObjectH2H2? DeserializeJsonStringUtf8()
    {
        return JsonSerializer.Deserialize<ObjectH2H2>(this.json);
    }
}

[Config(typeof(BenchmarkConfig))]
public class H2HBenchmark
{
    ObjectH2H h2h = default!;
    byte[] data = default!;
    byte[] data3 = default!;
    // byte[] data4 = default!;
    byte[] utf8 = default!;

    ObjectH2H2 h2h2 = default!;
    byte[] data2 = default!;
    byte[] utf8b = default!;
    byte[] json = default!;

    public H2HBenchmark()
    {
    }

    [GlobalSetup]
    public void Setup()
    {
        this.h2h = new ObjectH2H();
        this.data = MessagePack.MessagePackSerializer.Serialize(this.h2h);
        this.utf8 = TinyhandSerializer.SerializeToUtf8(this.h2h);

        this.h2h2 = new ObjectH2H2();
        this.data2 = MessagePack.MessagePackSerializer.Serialize(this.h2h2);
        this.utf8b = TinyhandSerializer.SerializeToUtf8(this.h2h2);
        this.json = JsonSerializer.SerializeToUtf8Bytes(this.h2h2);

        // var c = JsonSerializer.Deserialize<ObjectH2H2>(System.Text.Encoding.UTF8.GetBytes("{ \"X\":0,\"Y\":0,\"Z\":0,\"A\":\"H2Htest\",\"B\":[0,1,2,3,4,5,6,7,8,9]}"));

        this.data3 = this.SerializeProtoBuf();
        // this.data4 = MemoryPackSerializer.Serialize(this.h2h);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
    }

    [Benchmark]
    public byte[] SerializeProtoBuf()
    {
        using (var ms = new MemoryStream())
        {
            ProtoBuf.Serializer.Serialize(ms, this.h2h);
            return ms.ToArray();
        }
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

    /*[Benchmark]
    public byte[] SerializeMemoryPack()
    {
        return MemoryPackSerializer.Serialize(this.h2h);
    }*/

    [Benchmark]
    public byte[] SerializeTinyhandUtf8()
    {
        return Tinyhand.TinyhandSerializer.SerializeToUtf8(this.h2h);
    }

    [Benchmark]
    public ObjectH2H DeserializeProtoBuf()
    {
        return ProtoBuf.Serializer.Deserialize<ObjectH2H>(this.data3.AsSpan());
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
    public ObjectH2H? DeserializeTinyhandUtf8()
    {
        return Tinyhand.TinyhandSerializer.DeserializeFromUtf8<ObjectH2H>(this.utf8);
    }

    /*[Benchmark]
    public ObjectH2H? DeserializeMemoryPack()
    {
        return MemoryPackSerializer.Deserialize<ObjectH2H>(this.data4);
    }*/

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
    public byte[] SerializeTinyhandStringUtf8()
    {
        return Tinyhand.TinyhandSerializer.SerializeToUtf8(this.h2h2);
    }

    [Benchmark]
    public byte[] SerializeJsonStringUtf8()
    {
        return JsonSerializer.SerializeToUtf8Bytes(this.h2h2);
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

    [Benchmark]
    public ObjectH2H2? DeserializeTinyhandStringUtf8()
    {
        return Tinyhand.TinyhandSerializer.DeserializeFromUtf8<ObjectH2H2>(this.utf8b);
    }

    [Benchmark]
    public ObjectH2H2? DeserializeJsonStringUtf8()
    {
        return JsonSerializer.Deserialize<ObjectH2H2>(this.json);
    }
}
