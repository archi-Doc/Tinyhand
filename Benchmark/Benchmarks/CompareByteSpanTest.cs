// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Linq;
using BenchmarkDotNet.Attributes;

namespace Benchmark;

[Config(typeof(BenchmarkConfig))]
public class CompareByteSpanTest
{
    public static ReadOnlySpan<byte> TrueSpan => "true"u8;

    public static ReadOnlySpan<byte> DoublePositiveInfinitySpan => "double.PositiveInfinity"u8;

    private byte[] trueByte;
    private byte[] doublePositiveInfinityByte;

    public CompareByteSpanTest()
    {
        this.trueByte = TrueSpan.ToArray();
        this.doublePositiveInfinityByte = DoublePositiveInfinitySpan.ToArray();
    }

    [GlobalSetup]
    public void Setup()
    {
    }

    [Benchmark]
    public bool CompareTrue()
    {
        var span = this.trueByte.AsSpan();
        return span[0] == 't' && span[1] == 'r' && span[2] == 'u' && span[3] == 'e';
    }

    [Benchmark]
    public bool SequenceEqualTrue()
    {
        return this.trueByte.AsSpan().SequenceEqual(TrueSpan);
    }

    [Benchmark]
    public bool ComparePositiveInfinity()
    {
        var span = this.doublePositiveInfinityByte.AsSpan();
        return span[0] == 'd' && span[1] == 'o' && span[2] == 'u' && span[3] == 'b' &&
            span[4] == 'l' && span[5] == 'e' && span[6] == '.' && span[7] == 'P' &&
            span[8] == 'o' && span[9] == 's' && span[10] == 'i' && span[11] == 't' &&
            span[12] == 'i' && span[13] == 'v' && span[14] == 'e' && span[15] == 'I' &&
            span[16] == 'n' && span[17] == 'f' && span[18] == 'i' && span[19] == 'n' &&
            span[20] == 'i' && span[21] == 't' && span[22] == 'y';
    }

    [Benchmark]
    public bool SequenceEqualPositiveInfinity()
    {
        return this.doublePositiveInfinityByte.AsSpan().SequenceEqual(DoublePositiveInfinitySpan);
    }

}
