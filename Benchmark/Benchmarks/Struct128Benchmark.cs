// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Tinyhand;

namespace Benchmark;

[Config(typeof(BenchmarkConfig))]
public class Struct128Benchmark
{
    private byte[] buffer = new byte[32];

    public Struct128Benchmark()
    {
    }

    [GlobalSetup]
    public void Setup()
    {
    }

    /*[Benchmark]
    public int ConstructorTest()
    {
        var st = new Struct128(1, 2, 3, 4);
        var st2 = new Struct128(ref st);
        var st3 = new Struct128(ref st2);
        var st4 = new Struct128(ref st3);
        return st4.Int4;
    }*/

    /*[Benchmark]
    public byte[] TryWriteBytes()
    {
        var st = new Struct128(1, 2, 3, 4);
        st.TryWriteBytes(this.buffer);
        return buffer;
    }*/

    [Benchmark]
    public int GetHashCodeTest()
    {
        var st = new Struct128(123456789, -987654321);
        return st.GetHashCode();
    }
}
