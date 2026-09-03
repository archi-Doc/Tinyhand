// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Reflection;
using MessagePack.LZ4;
using Tinyhand;
using Xunit;

namespace XUnitTest.Tests;

public class Lz4BoundaryTest
{
    private unsafe delegate int Decoder(byte* input, int inputLength, byte* output, int outputLength);

    [Theory]
    [InlineData(32)]
    [InlineData(64)]
    public unsafe void BothDecoderImplementationsRespectInputBoundaries(int bits)
    {
        // Exercise both implementations even on a 64-bit test host.
        var decode = typeof(LZ4Codec).GetMethod($"LZ4_uncompress_{bits}", BindingFlags.Static | BindingFlags.NonPublic)!.CreateDelegate<Decoder>();
        foreach (var size in new[] { 1, 15, 256, 4096, 65536 })
        {
            foreach (var random in new[] { false, true })
            {
                var input = new byte[size];
                if (random)
                {
                    new Random(size).NextBytes(input);
                }

                var compressed = new byte[LZ4Codec.MaximumOutputLength(size)];
                var length = LZ4Codec.Encode(input, compressed);
                var output = new byte[size];
                fixed (byte* source = compressed)
                {
                    fixed (byte* target = output)
                    {
                        Assert.Equal(length, decode(source, length, target, size));
                        Assert.Equal(input, output);
                        foreach (var prefix in new[] { 0, 1, length / 2, length - 1 })
                        {
                            Assert.True(decode(source, prefix, target, size) <= 0);
                        }
                    }
                }
            }
        }

        byte[] malformed = [0x10, (byte)'x', 0, 0, 0x50, 1, 2, 3, 4, 5];
        fixed (byte* source = malformed)
        {
            fixed (byte* target = new byte[10])
            {
                Assert.True(decode(source, malformed.Length, target, 10) < 0);
            }
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(3)]
    public void ExtensionLengthMustMatchTheBlockLengthMetadata(byte extensionLength)
    {
        // [Ext8(Lz4BlockArray, 1), bin(0x10, 42)] contains one length byte.
        byte[] message = [0x92, 0xc7, extensionLength, 98, 1, 0xc4, 2, 0x10, 42];
        Assert.False(TinyhandSerializer.TryDeserialize<int>(message, out _, TinyhandSerializerOptions.Lz4));
        message[2] = 1;
        Assert.Equal(42, TinyhandSerializer.Deserialize<int>(message, TinyhandSerializerOptions.Lz4));
    }

    [Fact]
    public void ZeroMatchOffsetIsRejected()
    {
        byte[] input = [0x10, (byte)'x', 0, 0, 0x50, 1, 2, 3, 4, 5];
        Assert.Throws<LZ4Exception>(() => LZ4Codec.Decode(input, new byte[10]));
    }

    [Fact]
    public void TruncatedLiteralDoesNotCopyBytesOutsideTheInputSpan()
    {
        byte[] input = [0x50, 1, 2, 3, 4, 5];
        var output = new byte[5];
        Assert.Throws<LZ4Exception>(() => LZ4Codec.Decode(input.AsSpan(0, 1), output));
        Assert.Equal(new byte[5], output);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(15)]
    [InlineData(256)]
    [InlineData(4096)]
    public void EveryTruncatedPrefixIsRejected(int length)
    {
        var input = new byte[length];
        new Random(length).NextBytes(input);
        var compressed = new byte[LZ4Codec.MaximumOutputLength(length)];
        var size = LZ4Codec.Encode(input, compressed);
        var output = new byte[length];
        Assert.Equal(length, LZ4Codec.Decode(compressed.AsSpan(0, size), output));
        Assert.Equal(input, output);
        for (var prefix = 0; prefix < size; prefix++)
        {
            Assert.Throws<LZ4Exception>(() => LZ4Codec.Decode(compressed.AsSpan(0, prefix), output));
        }
    }

    [Theory]
    [InlineData(new byte[] { 0xf0, 255 })]
    [InlineData(new byte[] { 0x10, 1 })]
    [InlineData(new byte[] { 0x1f, 1, 1, 0, 255 })]
    public void IncompleteLengthOrOffsetIsRejected(byte[] input)
        => Assert.Throws<LZ4Exception>(() => LZ4Codec.Decode(input, new byte[1024]));
}
