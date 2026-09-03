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
using System.Globalization;
using Arc;
using Tinyhand.IO;

namespace Benchmark;

#pragma warning disable PBN0020

[Config(typeof(BenchmarkConfig))]
public class Utf8Benchmark
{
    H2HTest.ObjectH2H h2h = default!;
    byte[] data = default!;

    H2HTest.ObjectH2H2 h2h2 = default!;
    byte[] utf8b = default!;

    byte[] stringConvertibleData = default!;

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

        using var writer = TinyhandWriter.CreateFromBytePool();
        writer.Write("123456789");
        this.stringConvertibleData = writer.FlushAndGetArray();
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

    /*[Benchmark]
    public int StringConvertibleViaString()
    {
        var reader = new TinyhandReader(this.stringConvertibleData);
        var text = reader.ReadString();
        var value = default(StringConvertibleValue);
        if (text is not null)
        {
            StringConvertibleValue.TryParse(text, out value, out _);
        }

        return value.Value;
    }

    [Benchmark]
    public int StringConvertibleViaSpan()
    {
        var reader = new TinyhandReader(this.stringConvertibleData);
        var value = default(StringConvertibleValue);
        reader.TryReadStringConvertible(ref value);
        return value.Value;
    }*/

    public struct StringConvertibleValue : IStringConvertible<StringConvertibleValue>
    {
        public int Value;

        public static int MaxStringLength => 11;

        public int GetStringLength() => -1;

        public static bool TryParse(ReadOnlySpan<char> source, out StringConvertibleValue instance, out int read, IConversionOptions? conversionOptions = default)
        {
            var success = int.TryParse(source, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value);
            instance = new StringConvertibleValue { Value = value };
            read = success ? source.Length : 0;
            return success;
        }

        public bool TryFormat(Span<char> destination, out int written, IConversionOptions? conversionOptions = default)
            => this.Value.TryFormat(destination, out written, provider: CultureInfo.InvariantCulture);
    }
}
