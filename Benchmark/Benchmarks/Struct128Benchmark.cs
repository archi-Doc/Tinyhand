// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Linq;
using Arc.Crypto;
using BenchmarkDotNet.Attributes;
using Tinyhand;

namespace Benchmark;

[TinyhandObject]
public partial class StructTestClass
{
    public StructTestClass()
    {
    }

    public StructTestClass(long x)
    {
        this.A = new(x, x + 1);
        this.C = new(x, x + 1, x * 2, x * x);
    }

    [Key(0)]
    public Struct128 A { get; set; }

    [Key(1)]
    public Struct128 B { get; set; } = Struct128.Zero;

    [Key(2)]
    public Struct256 C { get; set; }

    [Key(3)]
    public Struct256 D { get; set; } = Struct256.One;
}

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

    /*[Benchmark]
    public int GetHashCodeTest()
    {
        var st = new Struct128(123456789, -987654321);
        return st.GetHashCode();
    }*/

    [Benchmark]
    public int Serialize()
    {
        var tc = new StructTestClass(123456);
        var tc2 = TinyhandSerializer.Deserialize<StructTestClass>(TinyhandSerializer.Serialize(tc))!;
        return tc2.A.Int0;
    }
}
