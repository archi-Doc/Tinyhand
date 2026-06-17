// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Tinyhand;

namespace Benchmark;

[Config(typeof(BenchmarkConfig))]
public class CharBufferBenchmark
{
    private readonly char[] buffer = Enumerable.Range(0, 32).Select(x => (char)x).ToArray();

    public CharBufferBenchmark()
    {
    }

    [GlobalSetup]
    public void Setup()
    {
    }

    [Benchmark]
    public byte[] SerializeArray()
    {
        return TinyhandSerializer.Serialize(this.buffer);
    }

    [Benchmark]
    public byte[] SerializeMemory()
    {
        return TinyhandSerializer.Serialize(this.buffer.AsMemory());
    }

    [Benchmark]
    public byte[] SerializeReadOnlyMemory()
    {
        return TinyhandSerializer.Serialize(new ReadOnlyMemory<char>(this.buffer));
    }
}
