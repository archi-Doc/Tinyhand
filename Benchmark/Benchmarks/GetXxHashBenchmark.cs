// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Benchmark.H2HTest;
using Tinyhand;
using System.Diagnostics;

namespace Benchmark;

[TinyhandObject]
public partial class ObjectH2HArray
{
    private const int N = 3000;

    public ObjectH2HArray()
    {
        this.array = new ObjectH2H[N];
        for (var i = 0; i < N; i++)
        {
            this.array[i] = new();
            this.array[i].X = i;
        }
    }

    [Key(0)]
    private ObjectH2H[] array;
}

[Config(typeof(BenchmarkConfig))]
public class GetXxHashBenchmark
{

    private ObjectH2H objectH2H = new();
    private ObjectH2HArray array = new();

    public GetXxHashBenchmark()
    {
        var bin = TinyhandSerializer.Serialize(this.objectH2H);
        bin = TinyhandSerializer.Serialize(this.array);

        Debug.Assert(this.GetXxHash3() == this.GetXxHash3b());
        Debug.Assert(this.GetXxHash3() == this.GetXxHash3c());
        Debug.Assert(this.GetXxHash3_Array() == this.GetXxHash3b_Array());
        Debug.Assert(this.GetXxHash3_Array() == this.GetXxHash3c_Array());
    }

    [GlobalSetup]
    public void Setup()
    {
    }

    [Benchmark]
    public ulong GetXxHash3()
        => TinyhandSerializer.GetXxHash3(this.objectH2H);

    [Benchmark]
    public ulong GetXxHash3b()
        => TinyhandSerializer.GetXxHash3b(this.objectH2H);

    [Benchmark]
    public ulong GetXxHash3c()
        => TinyhandSerializer.GetXxHash3c(this.objectH2H);

    [Benchmark]
    public ulong GetXxHash3d()
        => TinyhandSerializer.GetXxHash3d(this.objectH2H);

    [Benchmark]
    public ulong GetXxHash3_Array()
        => TinyhandSerializer.GetXxHash3(this.array);

    [Benchmark]
    public ulong GetXxHash3b_Array()
        => TinyhandSerializer.GetXxHash3b(this.array);

    [Benchmark]
    public ulong GetXxHash3c_Array()
        => TinyhandSerializer.GetXxHash3c(this.array);
}
