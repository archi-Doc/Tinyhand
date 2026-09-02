// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

/*  BenchmarkDotNet, small template code
 *  PM> Install-Package BenchmarkDotNet
 */

using System.Buffers;
using System.IO;
using System.Linq;
using System.Text.Json;
using BenchmarkDotNet.Attributes;
using ProtoBuf;
using MemoryPack;
using Tinyhand;
using Arc.Collections;
using System;

namespace Benchmark;

#pragma warning disable PBN0020

[Config(typeof(BenchmarkConfig))]
public class Utf8Benchmark
{
    H2HTest.ObjectH2H h2h = default!;
    byte[] data = default!;

    H2HTest.ObjectH2H2 h2h2 = default!;
    byte[] utf8b = default!;

    public Utf8Benchmark()
    {
    }

    [GlobalSetup]
    public void Setup()
    {
        this.h2h = new H2HTest.ObjectH2H();
        this.data = MessagePack.MessagePackSerializer.Serialize(this.h2h);

        this.h2h2 = new H2HTest.ObjectH2H2();
        this.utf8b = TinyhandSerializer.SerializeToUtf8(this.h2h2);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
    }

    [Benchmark]
    public byte[] SerializeTinyhand()
    {
        return TinyhandSerializer.SerializeObject(this.h2h);
    }

    [Benchmark]
    public H2HTest.ObjectH2H? DeserializeTinyhand()
    {
        return TinyhandSerializer.DeserializeObject<H2HTest.ObjectH2H>(this.data);
    }

    [Benchmark]
    public byte[] SerializeTinyhandUtf8()
    {
        return TinyhandSerializer.SerializeToUtf8(this.h2h2);
    }

    [Benchmark]
    public H2HTest.ObjectH2H2? DeserializeTinyhandUtf8()
    {
        return TinyhandSerializer.DeserializeFromUtf8<H2HTest.ObjectH2H2>(this.utf8b);
    }
}
