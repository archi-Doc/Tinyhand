// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.IO;
using BenchmarkDotNet.Attributes;
using Tinyhand;
using Tinyhand.IO;

namespace Benchmark;

/// <summary>
/// Measures the parts of the UTF-8 (de)serialization: the binary/text converters and the text reader.
/// </summary>
[Config(typeof(BenchmarkConfig))]
public class Utf8ConverterBenchmark
{
    private H2HTest.ObjectH2H2 h2h2 = default!;
    private byte[] binary = default!;
    private byte[] utf8 = default!;
    private byte[] buffer = default!;

    [GlobalSetup]
    public void Setup()
    {
        this.h2h2 = new H2HTest.ObjectH2H2();
        this.binary = TinyhandSerializer.Serialize(this.h2h2);
        this.utf8 = TinyhandSerializer.SerializeToUtf8(this.h2h2);
        this.buffer = new byte[4096];
    }

    [Benchmark]
    public int FromBinaryToUtf8()
    {
        var writer = new TinyhandRawWriter(this.buffer);
        TinyhandTreeConverter.FromBinaryToUtf8(this.binary, ref writer, TinyhandSerializerOptions.ConvertToString, true);
        writer.FlushAndGetReadOnlySpan(out var span, out _);
        return span.Length;
    }

    [Benchmark]
    public int FromUtf8ToBinary()
    {
        var writer = new TinyhandWriter(this.buffer);
        TinyhandTreeConverter.FromUtf8ToBinary(this.utf8, ref writer, true);
        writer.FlushAndGetReadOnlySpan(out var span, out _);
        return span.Length;
    }

    [Benchmark]
    public int Utf8ReaderLoop()
    {
        var reader = new TinyhandUtf8Reader(this.utf8, false);
        var count = 0;
        while (reader.Read())
        {
            count++;
        }

        return count;
    }

    [Benchmark]
    public H2HTest.ObjectH2H2? DeserializeBinary()
    {
        return TinyhandSerializer.Deserialize<H2HTest.ObjectH2H2>(this.binary);
    }
}
