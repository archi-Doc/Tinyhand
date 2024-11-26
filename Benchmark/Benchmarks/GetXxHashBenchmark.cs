// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Benchmark.H2HTest;
using Tinyhand;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Tinyhand.IO;

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
    public const int InitialBufferSize = 32 * 1024;

    [ThreadStatic]
    private static byte[]? initialBuffer;

    private ObjectH2H objectH2H = new();
    private ObjectH2HArray array = new();

    public GetXxHashBenchmark()
    {
        var bin = TinyhandSerializer.Serialize(this.objectH2H);
        bin = TinyhandSerializer.Serialize(this.array);

        Debug.Assert(this.GetXxHash3() == this.GetXxHash3a());
        Debug.Assert(this.GetXxHash3() == this.GetXxHash3b());
        Debug.Assert(this.GetXxHash3_Array() == this.GetXxHash3a_Array());
        Debug.Assert(this.GetXxHash3_Array() == this.GetXxHash3b_Array());
    }

    [GlobalSetup]
    public void Setup()
    {
    }

    [Benchmark]
    public ulong GetXxHash3()
        => TinyhandSerializer.GetXxHash3(this.objectH2H);

    [Benchmark]
    public ulong GetXxHash3i()
        => this.objectH2H.GetXxHash3();

    [Benchmark]
    public ulong GetXxHash3a()
        => GetXxHash3a(this.objectH2H);

    [Benchmark]
    public ulong GetXxHash3b()
        => GetXxHash3b(this.objectH2H);

    [Benchmark]
    public ulong GetXxHash3_Array()
        => TinyhandSerializer.GetXxHash3(this.array);

    [Benchmark]
    public ulong GetXxHash3a_Array()
        => GetXxHash3a(this.array);

    [Benchmark]
    public ulong GetXxHash3b_Array()
        => GetXxHash3b(this.array);

    public static ulong GetXxHash3a<T>(in T? value)
        where T : ITinyhandSerializable<T>
    {
        if (initialBuffer == null)
        {
            initialBuffer = new byte[InitialBufferSize];
        }

        var writer = new TinyhandWriter(initialBuffer);
        try
        {
            T.Serialize(ref writer, ref Unsafe.AsRef(in value), TinyhandSerializer.DefaultOptions);
            writer.FlushAndGetReadOnlySpan(out var span, out _);
            return Arc.Crypto.XxHash3.Hash64(span);
        }
        catch
        {
            return 0;
        }
        finally
        {
            writer.Dispose();
        }
    }

    public static ulong GetXxHash3b<T>(in T? value)
        where T : ITinyhandSerializable<T>
    {
        if (initialBuffer == null)
        {
            initialBuffer = new byte[InitialBufferSize];
        }

        var writer = new TinyhandWriter(initialBuffer);
        try
        {
            T.Serialize(ref writer, ref Unsafe.AsRef(in value), TinyhandSerializer.DefaultOptions);
            var memoryOwner = writer.FlushAndGetRentMemory();
            var hash = Arc.Crypto.XxHash3.Hash64(memoryOwner.Span);
            memoryOwner.Return();
            return hash;
        }
        catch
        {
            return 0;
        }
        finally
        {
            writer.Dispose();
        }
    }
}
