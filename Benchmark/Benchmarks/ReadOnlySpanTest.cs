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

[Config(typeof(BenchmarkConfig))]
public class ReadOnlySpanTest
{
    public static ReadOnlySpan<byte> TrueSpan => "true"u8;

    public static ReadOnlySpan<byte> TrueSpan2 => new byte[] { (byte)'t', (byte)'r', (byte)'u', (byte)'e', };

    public static byte[] TrueByte => new byte[] { (byte)'t', (byte)'r', (byte)'u', (byte)'e', };

    private byte[] trueByte;

    public ReadOnlySpanTest()
    {
        this.trueByte = TrueSpan.ToArray();
    }

    [GlobalSetup]
    public void Setup()
    {
    }

    [Benchmark]
    public ReadOnlySpan<byte> ReadOnlySpan()
    {
        return TrueSpan;
    }

    [Benchmark]
    public ReadOnlySpan<byte> ReadOnlySpan2()
    {
        return TrueSpan2;
    }

    [Benchmark]
    public byte[] ByteArray()
    {
        return TrueByte;
    }

    [Benchmark]
    public ReadOnlySpan<byte> ByteArray2()
    {
        return TrueByte.AsSpan();
    }
}
