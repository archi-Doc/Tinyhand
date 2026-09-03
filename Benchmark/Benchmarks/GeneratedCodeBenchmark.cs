// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Tinyhand;
using Tinyhand.IO;

namespace Benchmark;

[MemoryDiagnoser]
public class GeneratedCodeBenchmark
{
    private readonly byte[] buffer = new byte[32 * 1024];
    private readonly GeneratorMap map = new();
    private GeneratorIntList integers = default!;
    private GeneratorStringList strings = default!;
    private byte[] mapBytes = default!;
    private byte[] integerBytes = default!;
    private byte[] enumBytes = default!;
    private byte[] objectBytes = default!;
    private byte[] emptyBytes = default!;
    private readonly GeneratorEmptyArrays empty = new();

    [Params(4, 256)]
    public int Count { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        this.mapBytes = TinyhandSerializer.Serialize(this.map);
        this.integers = new() { Values = Enumerable.Range(0, this.Count).ToList() };
        this.strings = new() { Values = Enumerable.Repeat("Tinyhand", this.Count).ToList() };
        this.integerBytes = TinyhandSerializer.Serialize(this.integers);
        this.enumBytes = TinyhandSerializer.Serialize(new GeneratorEnumList { Values = Enumerable.Range(0, this.Count).Select(x => (GeneratorEnum)x).ToList() });
        this.objectBytes = TinyhandSerializer.Serialize(new GeneratorObjectList { Values = Enumerable.Range(0, this.Count).Select(x => new GeneratorPoint { X = x, Y = -x }).ToList() });
        // Encode empty arrays explicitly; SkipDefaultValues would otherwise write nil.
        this.emptyBytes = [0x98, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90];
    }

    [Benchmark]
    public int SerializeStringKeys()
    {
        var writer = new TinyhandWriter(this.buffer);
        try
        {
            TinyhandSerializer.SerializeObject(ref writer, this.map);
            writer.FlushAndGetReadOnlySpan(out var span, out _);
            return span.Length;
        }
        finally
        {
            writer.Dispose();
        }
    }

    [Benchmark]
    public GeneratorMap? DeserializeStringKeys() => TinyhandSerializer.DeserializeObject<GeneratorMap>(this.mapBytes);

    [Benchmark]
    public int SerializePrimitiveList()
    {
        var writer = new TinyhandWriter(this.buffer);
        try
        {
            TinyhandSerializer.SerializeObject(ref writer, this.integers);
            writer.FlushAndGetReadOnlySpan(out var span, out _);
            return span.Length;
        }
        finally
        {
            writer.Dispose();
        }
    }

    [Benchmark]
    public int SerializeStringList()
    {
        var writer = new TinyhandWriter(this.buffer);
        try
        {
            TinyhandSerializer.SerializeObject(ref writer, this.strings);
            writer.FlushAndGetReadOnlySpan(out var span, out _);
            return span.Length;
        }
        finally
        {
            writer.Dispose();
        }
    }

    [Benchmark]
    public GeneratorIntList? DeserializePrimitiveList() => TinyhandSerializer.DeserializeObject<GeneratorIntList>(this.integerBytes);

    [Benchmark]
    public GeneratorEnumList? DeserializeEnumList() => TinyhandSerializer.DeserializeObject<GeneratorEnumList>(this.enumBytes);

    [Benchmark]
    public GeneratorObjectList? DeserializeObjectList() => TinyhandSerializer.DeserializeObject<GeneratorObjectList>(this.objectBytes);

    [Benchmark]
    public GeneratorEmptyArrays? DeserializeEmptyArrays() => TinyhandSerializer.DeserializeObject<GeneratorEmptyArrays>(this.emptyBytes);

    [Benchmark]
    public GeneratorEmptyArrays? CloneEmptyArrays() => TinyhandSerializer.CloneObject(this.empty);
}

[TinyhandObject(ImplicitMemberNameAsKey = true)]
public partial class GeneratorMap
{
    [DefaultValue(1)] public int Alpha { get; set; } = 1;
    [DefaultValue(2)] public int Bravo { get; set; } = 2;
    [DefaultValue(3)] public int Charlie { get; set; } = 3;
    [DefaultValue(4)] public int Delta { get; set; } = 4;
    [DefaultValue(5)] public int Echo { get; set; } = 5;
    [DefaultValue(6)] public int Foxtrot { get; set; } = 6;
    [DefaultValue(7)] public int Golf { get; set; } = 7;
    [DefaultValue(8)] public int Hotel { get; set; } = 8;
}

[TinyhandObject]
public partial class GeneratorIntList
{
    [Key(0)] public List<int> Values { get; set; } = new();
}

[TinyhandObject]
public partial class GeneratorStringList
{
    [Key(0)] public List<string> Values { get; set; } = new();
}

public enum GeneratorEnum
{
    Zero,
}

[TinyhandObject]
public partial class GeneratorEnumList
{
    [Key(0)] public List<GeneratorEnum> Values { get; set; } = new();
}

[TinyhandObject]
public partial struct GeneratorPoint
{
    [Key(0)] public int X;
    [Key(1)] public int Y;
}

[TinyhandObject]
public partial class GeneratorObjectList
{
    [Key(0)] public List<GeneratorPoint> Values { get; set; } = new();
}

[TinyhandObject]
public partial class GeneratorEmptyArrays
{
    [Key(0)] public int[] Integers { get; set; } = [];
    [Key(1)] public bool[] Booleans { get; set; } = [];
    [Key(2)] public DateTime[] Dates { get; set; } = [];
    [Key(3)] public double[] Doubles { get; set; } = [];
    [Key(4)] public GeneratorPoint[] Points { get; set; } = [];
    [Key(5)] public GeneratorEnum[] Enums { get; set; } = [];
    [Key(6)] public int[][] Nested { get; set; } = [];
    [Key(7)] public string[] Strings { get; set; } = [];
}
