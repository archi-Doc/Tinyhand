// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
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
    public unsafe void ShortMatchNearOutputEndDoesNotOverwriteGuardBytes(int bits)
    {
        var decode = typeof(LZ4Codec).GetMethod($"LZ4_uncompress_{bits}", BindingFlags.Static | BindingFlags.NonPublic)!.CreateDelegate<Decoder>();
        byte[] input = [0x10, (byte)'x', 1, 0, 0x50, 1, 2, 3, 4, 5];
        const int outputLength = 10;
        // Keep the guard inside the actual allocation so a regression cannot corrupt the test process.
        var output = new byte[outputLength + 16];
        Array.Fill(output, (byte)0xcc);
        fixed (byte* source = input)
        {
            fixed (byte* target = output)
            {
                Assert.Equal(input.Length, decode(source, input.Length, target, outputLength));
            }
        }

        Assert.Equal(new byte[] { (byte)'x', (byte)'x', (byte)'x', (byte)'x', (byte)'x', 1, 2, 3, 4, 5 }, output.AsSpan(0, outputLength).ToArray());
        foreach (var value in output.AsSpan(outputLength))
        {
            Assert.Equal((byte)0xcc, value);
        }
    }

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

    [Theory]
    [InlineData(32)]
    [InlineData(64)]
    public unsafe void BoundedMalformedCorpusRespectsCanariesAndDeclaredInput(int bits)
    {
        var decode = typeof(LZ4Codec).GetMethod($"LZ4_uncompress_{bits}", BindingFlags.Static | BindingFlags.NonPublic)!.CreateDelegate<Decoder>();
        // Both logical spans are surrounded by actual allocated memory. This bounds the maximum
        // ushort back-reference and the length extensions possible in this small corpus.
        const int padding = 128 * 1024;
        var firstSource = new byte[(padding * 2) + 1024];
        var secondSource = new byte[firstSource.Length];
        var firstOutput = new byte[(padding * 2) + 256];
        var secondOutput = new byte[firstOutput.Length];
        firstSource.AsSpan().Fill(0x11);
        secondSource.AsSpan().Fill(0xee);

        foreach (var (input, outputLength) in MalformedCorpus())
        {
            firstSource.AsSpan(padding, 1024).Fill(0x11);
            secondSource.AsSpan(padding, 1024).Fill(0xee);
            input.CopyTo(firstSource, padding);
            input.CopyTo(secondSource, padding);
            firstOutput.AsSpan().Fill(0xcc);
            secondOutput.AsSpan().Fill(0xcc);
            int firstResult;
            int secondResult;
            fixed (byte* firstInput = firstSource, secondInput = secondSource, firstTarget = firstOutput, secondTarget = secondOutput)
            {
                firstResult = decode(firstInput + padding, input.Length, firstTarget + padding, outputLength);
                secondResult = decode(secondInput + padding, input.Length, secondTarget + padding, outputLength);
            }

            Assert.True(firstOutput.AsSpan(0, padding).IndexOfAnyExcept((byte)0xcc) < 0);
            Assert.True(firstOutput.AsSpan(padding + outputLength).IndexOfAnyExcept((byte)0xcc) < 0);
            Assert.True(secondOutput.AsSpan(0, padding).IndexOfAnyExcept((byte)0xcc) < 0);
            Assert.True(secondOutput.AsSpan(padding + outputLength).IndexOfAnyExcept((byte)0xcc) < 0);
            // Changing only bytes outside the declared source span must not affect decoding.
            Assert.Equal(firstResult, secondResult);
            Assert.Equal(firstOutput.AsSpan(padding, outputLength).ToArray(), secondOutput.AsSpan(padding, outputLength).ToArray());
            if (firstResult == input.Length)
            {
                var expected = new byte[outputLength];
                Assert.True(TryDecodeReference(input, expected));
                Assert.Equal(expected, firstOutput.AsSpan(padding, outputLength).ToArray());
            }
        }
    }

    private static IEnumerable<(byte[] Input, int OutputLength)> MalformedCorpus()
    {
        foreach (var size in new[] { 1, 16, 64, 256 })
        {
            foreach (var random in new[] { false, true })
            {
                var plain = new byte[size];
                if (random)
                {
                    new Random(size).NextBytes(plain);
                }

                var buffer = new byte[LZ4Codec.MaximumOutputLength(size)];
                var length = LZ4Codec.Encode(plain, buffer);
                var encoded = buffer.AsSpan(0, length).ToArray();
                yield return (encoded, size);
                for (var position = 0; position < length; position += Math.Max(1, length / 8))
                {
                    foreach (var replacement in new byte[] { 0, 0xff, 0xf0 })
                    {
                        var mutated = (byte[])encoded.Clone();
                        mutated[position] = replacement;
                        yield return (mutated, size);
                    }
                }
            }
        }

        var generator = new Random(0x54696e79);
        for (var i = 0; i < 256; i++)
        {
            var input = new byte[generator.Next(1, 65)];
            generator.NextBytes(input);
            yield return (input, generator.Next(1, 257));
        }
    }

    private static bool TryDecodeReference(ReadOnlySpan<byte> input, Span<byte> output)
    {
        var source = 0;
        var written = 0;
        while (source < input.Length)
        {
            var token = input[source++];
            if (!TryReadLength(input, ref source, token >> 4, out var literals) || literals > input.Length - source || literals > output.Length - written)
            {
                return false;
            }

            input.Slice(source, literals).CopyTo(output.Slice(written));
            source += literals;
            written += literals;
            if (source == input.Length)
            {
                return written == output.Length;
            }

            if (input.Length - source < 2)
            {
                return false;
            }

            var offset = input[source] | (input[source + 1] << 8);
            source += 2;
            if (offset == 0 || offset > written || !TryReadLength(input, ref source, token & 0xf, out var match) || match + 4 > output.Length - written)
            {
                return false;
            }

            for (var i = 0; i < match + 4; i++)
            {
                output[written] = output[written - offset];
                written++;
            }
        }

        return false;
    }

    private static bool TryReadLength(ReadOnlySpan<byte> input, ref int source, int initial, out int length)
    {
        length = initial;
        if (initial != 15)
        {
            return true;
        }

        while (source < input.Length)
        {
            var next = input[source++];
            length += next;
            if (next != 255)
            {
                return true;
            }
        }

        return false;
    }
}
