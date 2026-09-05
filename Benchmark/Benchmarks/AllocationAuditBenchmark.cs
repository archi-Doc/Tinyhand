// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using BenchmarkDotNet.Attributes;
using Tinyhand;
using Tinyhand.IO;

namespace Benchmark;

[MemoryDiagnoser]
public class AllocationAuditBenchmark
{
    private readonly List<byte> values = new(new byte[1024]);
    private readonly ArrayBufferWriter<byte> destination = new(2048);
    private readonly MemoryStream stream = new(2048);
    private readonly byte[] buffer = new byte[2048];
    private byte[] encoded = null!;

    [GlobalSetup]
    public void Setup() => this.encoded = TinyhandSerializer.Serialize(this.values);

    [Benchmark]
    public int SerializeByteList()
    {
        this.destination.Clear();
        TinyhandSerializer.Serialize(this.destination, this.values);
        return this.destination.WrittenCount;
    }

    [Benchmark]
    public List<byte>? DeserializeByteList() => TinyhandSerializer.Deserialize<List<byte>>(this.encoded);

    [Benchmark]
    public long SerializeStream()
    {
        this.stream.Position = 0;
        TinyhandSerializer.Serialize(this.stream, 123456);
        return this.stream.Position;
    }

    [Benchmark]
    public byte[] SerializeLz4() => TinyhandSerializer.Serialize(this.values, TinyhandSerializerOptions.Lz4);

    [Benchmark]
    public long BorrowSequence()
    {
        var writer = new TinyhandWriter(this.buffer);
        try
        {
            writer.Write(123456);
            return writer.FlushAndGetReadOnlySequence().Length;
        }
        finally
        {
            writer.Dispose();
        }
    }

    [Benchmark]
    public int[] ReconstructEmptyArray() => TinyhandSerializer.Reconstruct<int[]>();

    [Benchmark]
    public int[]? CloneEmptyArray() => TinyhandSerializer.Clone(Array.Empty<int>());
}
