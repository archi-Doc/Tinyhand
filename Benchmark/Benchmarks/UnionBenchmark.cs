// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using Tinyhand;
using Tinyhand.IO;

namespace Benchmark;

[TinyhandUnion(0, typeof(UnionTestClassA))]
[TinyhandUnion(1, typeof(UnionTestClassB))]
public interface UnionTestInterface
{
}

[TinyhandObject]
public partial class UnionTestClassA : UnionTestInterface
{
    [Key(0)]
    ulong Id;

    [Key(1)]
    string Name;

    [Key(2)]
    int Int;

    [Key(3)]
    bool Bool;

    public UnionTestClassA()
    {
        this.Name = "TestA";
    }
}

[TinyhandObject]
public partial class UnionTestClassB : UnionTestInterface
{
    [Key(0)]
    ulong Id;

    [Key(1)]
    string Name;

    [Key(2)]
    int Int;

    [Key(3)]
    bool Bool;

    public UnionTestClassB()
    {
        this.Name = "TestB";
    }
}

[Config(typeof(BenchmarkConfig))]
public class UnionBenchmark
{
    public UnionTestClassA ClassA { get; } = new();

    public UnionTestClassB ClassB { get; } = new();

    public UnionTestInterface ClassX { get; } = new UnionTestClassA();

    public byte[] ByteA { get; }

    public byte[] ByteB { get; }

    public byte[] ByteX { get; }

    public UnionBenchmark()
    {
        this.ClassA = new();
        this.ClassB = new();
        this.ClassX = new UnionTestClassA();

        this.ByteA = TinyhandSerializer.Serialize(this.ClassA);
        this.ByteB = TinyhandSerializer.Serialize(this.ClassB);
        this.ByteX = TinyhandSerializer.Serialize(this.ClassX);
    }

    [GlobalSetup]
    public void Setup()
    {
    }

    [Benchmark]
    public byte[] SerializeUnionClassA()
    {
        return TinyhandSerializer.Serialize(this.ClassA);
    }

    [Benchmark]
    public byte[] SerializeUnionClassB()
    {
        return TinyhandSerializer.Serialize(this.ClassB);
    }

    [Benchmark]
    public byte[] SerializeUnionInterface()
    {
        return TinyhandSerializer.Serialize(this.ClassX);
    }

    [Benchmark]
    public UnionTestClassA? DeserializeUnionClassA()
    {
        return TinyhandSerializer.Deserialize<UnionTestClassA>(this.ByteA);
    }

    [Benchmark]
    public UnionTestClassB? DeserializeUnionClassB()
    {
        return TinyhandSerializer.Deserialize<UnionTestClassB>(this.ByteB);
    }

    [Benchmark]
    public UnionTestInterface? DeserializeUnionInterface()
    {
        return TinyhandSerializer.Deserialize<UnionTestInterface>(this.ByteX);
    }
}
