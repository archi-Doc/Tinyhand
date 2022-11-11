﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

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
using MemoryPack;
using Tinyhand;
using Tinyhand.IO;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Options;

#pragma warning disable SA1401 // Fields should be private

namespace Benchmark.H2HTest;

public class TinyhandObjectFormatter<T> : ITinyhandFormatter<T>
    where T : ITinyhandObject<T>
{
    public void Serialize(ref TinyhandWriter writer, T? v, TinyhandSerializerOptions options)
        => T.Serialize(ref writer, ref v, options);

    public T? Deserialize(ref TinyhandReader reader, TinyhandSerializerOptions options)
    {
        var v = default(T);
        T.Deserialize(ref reader, ref v, options);
        return v;
    }

    public T Reconstruct(TinyhandSerializerOptions options)
        => T.Reconstruct(options);

    public T? Clone(T? value, TinyhandSerializerOptions options)
        => T.Clone(ref value, options);
}

[ProtoContract]
[MessagePack.MessagePackObject]
[TinyhandObject]
[MemoryPackable]
public partial class ObjectH2H : ITinyhandObject<ObjectH2H>
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

    static void ITinyhandObject<ObjectH2H>.Serialize(ref TinyhandWriter writer, scoped ref ObjectH2H? value, TinyhandSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNil();
            return;
        }

        if (!options.IsSignatureMode) writer.WriteArrayHeader(9);
        writer.Write(value.X);
        writer.Write(value.Y);
        writer.Write(value.Z);
        writer.Write(value.A);
        writer.WriteNil();
        writer.WriteNil();
        writer.WriteNil();
        writer.WriteNil();
        SerializeInt32Array(ref writer, value.B);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void SerializeInt32Array(ref TinyhandWriter writer, int[]? value)
    {
        if (value == null)
        {
            writer.WriteNil();
        }
        else
        {
            writer.WriteArrayHeader(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                writer.Write(value[i]);
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int[]? DeserializeInt32Array(ref TinyhandReader reader)
    {
        if (reader.TryReadNil())
        {
            return null; // new int[0];
        }
        else
        {
            var len = reader.ReadArrayHeader();
            var array = new int[len];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = reader.ReadInt32();
            }

            return array;
        }
    }

    static void ITinyhandObject<ObjectH2H>.Deserialize(ref TinyhandReader reader, scoped ref ObjectH2H? value, TinyhandSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
            return;
        }

        value ??= new();
        var numberOfData = reader.ReadArrayHeader();
        if (numberOfData-- > 0 && !reader.TryReadNil())
        {
            value.X = reader.ReadInt32();
        }
        if (numberOfData-- > 0 && !reader.TryReadNil())
        {
            value.Y = reader.ReadInt32();
        }
        if (numberOfData-- > 0 && !reader.TryReadNil())
        {
            value.Z = reader.ReadInt32();
        }
        if (numberOfData-- > 0 && !reader.TryReadNil())
        {
            // reader.Skip();
            value.A = reader.ReadString() ?? string.Empty;
        }
        else
        {
            value.A = string.Empty;
        }
        if (numberOfData-- > 0) reader.Skip();
        if (numberOfData-- > 0) reader.Skip();
        if (numberOfData-- > 0) reader.Skip();
        if (numberOfData-- > 0) reader.Skip();
        if (numberOfData-- > 0 && !reader.TryReadNil())
        {
            value.B = DeserializeInt32Array(ref reader) ?? new int[0];
        }
        else
        {
            value.B = new int[0];
        }
        while (numberOfData-- > 0) reader.Skip();
    }

    static ObjectH2H ITinyhandObject<ObjectH2H>.Reconstruct(TinyhandSerializerOptions options)
    {
        var v = new ObjectH2H();
        return v;
    }

    public static ObjectH2H? Clone(ref ObjectH2H? value, TinyhandSerializerOptions options)
    {
        return value;
    }
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
    byte[] initialBuffer = default!;
    byte[] data = default!;
    byte[] data3 = default!;
    byte[] data4 = default!;
    byte[] data5 = default!;
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
        this.initialBuffer = new byte[1024];
        this.data = MessagePack.MessagePackSerializer.Serialize(this.h2h);
        this.utf8 = TinyhandSerializer.SerializeToUtf8(this.h2h);

        this.h2h2 = new ObjectH2H2();
        this.data2 = MessagePack.MessagePackSerializer.Serialize(this.h2h2);
        this.utf8b = TinyhandSerializer.SerializeToUtf8(this.h2h2);
        this.json = JsonSerializer.SerializeToUtf8Bytes(this.h2h2);

        // var c = JsonSerializer.Deserialize<ObjectH2H2>(System.Text.Encoding.UTF8.GetBytes("{ \"X\":0,\"Y\":0,\"Z\":0,\"A\":\"H2Htest\",\"B\":[0,1,2,3,4,5,6,7,8,9]}"));

        using (var ms = new MemoryStream())
        {
            ProtoBuf.Serializer.Serialize(ms, this.h2h);
            this.data3 = ms.ToArray();
        }

        this.data4 = MemoryPackSerializer.Serialize(this.h2h);
        this.data5 = TinyhandSerializer.SerializeB(this.h2h);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
    }

    /*[Benchmark]
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
    public byte[] SerializeMemoryPack()
    {
        return MemoryPackSerializer.Serialize(this.h2h);
    }

    [Benchmark]
    public byte[] SerializeTinyhand()
    {
        return Tinyhand.TinyhandSerializer.Serialize(this.h2h);
    }

    [Benchmark]
    public byte[] SerializeTinyhandB()
    {
        return Tinyhand.TinyhandSerializer.SerializeB(this.h2h);
    }

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
    }*/

    // [Benchmark]
    public ObjectH2H? DeserializeMemoryPack()
    {
        return MemoryPackSerializer.Deserialize<ObjectH2H>(this.data4);
    }

    [Benchmark]
    public ObjectH2H? DeserializeTinyhand0()
    {
        /*var reader = new TinyhandReader(this.data);
        var formatter = TinyhandSerializerOptions.Standard.Resolver.GetFormatter<ObjectH2H>();
        return formatter.Deserialize(ref reader, TinyhandSerializerOptions.Standard);*/

        var reader = new TinyhandReader(this.data);
        var v = new ObjectH2H();
        v.Deserialize(ref reader, TinyhandSerializerOptions.Standard);
        return v;
    }

    [Benchmark]
    public ObjectH2H? DeserializeTinyhand()
    {
        /*var reader = new TinyhandReader(this.data);
        var formatter = TinyhandSerializerOptions.Standard.Resolver.GetFormatter<ObjectH2H>();
        return formatter.Deserialize(ref reader, TinyhandSerializerOptions.Standard);*/

        /*var reader = new TinyhandReader(this.data);
        var v = new ObjectH2H();
        v.Deserialize(ref reader, TinyhandSerializerOptions.Standard);
        return v;*/

        return Tinyhand.TinyhandSerializer.Deserialize<ObjectH2H>(this.data);
    }

    [Benchmark]
    public ObjectH2H? DeserializeTinyhandB()
    {
        ObjectH2H? value = default;
        Tinyhand.TinyhandSerializer.DeserializeB<ObjectH2H>(this.data5, ref value);
        return value;
    }

    /*[Benchmark]
    public ObjectH2H? DeserializeTinyhandUtf8()
    {
        return Tinyhand.TinyhandSerializer.DeserializeFromUtf8<ObjectH2H>(this.utf8);
    }*/

    /*[Benchmark]
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
    }*/
}
