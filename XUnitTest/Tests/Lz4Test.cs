// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Linq;
using Tinyhand;
using Tinyhand.IO;
using Xunit;

namespace XUnitTest.Tests;

[TinyhandObject]
public partial class Lz4BinaryClass
{
    [Key(0)]
    public byte[] Binary { get; set; } = [];

    [Key(1)]
    public string Text { get; set; } = string.Empty;
}

/// <summary>
/// Exercises the LZ4 compression option.<br/>
/// A block larger than 64 KB uses a different compressor than a small one, and a single
/// <see cref="byte"/> array is written to one contiguous buffer, so the large payloads below
/// are what reach the 64-bit "large block" compressor.
/// </summary>
public class Lz4Test
{
    [Fact]
    public void SmallAndLargeBlocks()
    {
        // Sizes around the 64 KB block boundary that selects the compressor.
        int[] sizes = [0, 1, 100, 1024, 65_535, 65_536, 65_537, 200_000, 1_000_000];

        foreach (var size in sizes)
        {
            foreach (var random in new[] { false, true })
            {
                var binary = new byte[size];
                if (random)
                {// Incompressible data: the compressed block is larger than the input.
                    new Random(size).NextBytes(binary);
                }
                else
                {// Highly compressible data.
                    for (var i = 0; i < binary.Length; i++)
                    {
                        binary[i] = (byte)(i % 7);
                    }
                }

                var c = new Lz4BinaryClass
                {
                    Binary = binary,
                    Text = new string('t', Math.Min(size, 70_000)),
                };

                var options = TinyhandSerializerOptions.Lz4;
                var bin = TinyhandSerializer.Serialize(c, options);
                var c2 = TinyhandSerializer.Deserialize<Lz4BinaryClass>(bin, options);

                c2!.Binary.SequenceEqual(c.Binary).IsTrue();
                c2.Text.Is(c.Text);
            }
        }
    }

    [Fact]
    public void CompressedIsSmallerForCompressibleData()
    {
        var c = new Lz4BinaryClass { Binary = new byte[500_000], };

        var plain = TinyhandSerializer.Serialize(c);
        var compressed = TinyhandSerializer.Serialize(c, TinyhandSerializerOptions.Lz4);

        (compressed.Length < plain.Length).IsTrue();

        // Both encodings deserialize to the same value with the Lz4 option.
        TinyhandSerializer.Deserialize<Lz4BinaryClass>(plain, TinyhandSerializerOptions.Lz4)!.Binary.Length.Is(c.Binary.Length);
        TinyhandSerializer.Deserialize<Lz4BinaryClass>(compressed, TinyhandSerializerOptions.Lz4)!.Binary.Length.Is(c.Binary.Length);
    }

    [Fact]
    public void CompressedDataRequiresTheOption()
    {
        // Decompression only happens when the Lz4 option is set, so compressed data cannot be
        // read with the standard option (the reverse direction is allowed, see RoundTrip above).
        var c = new Lz4BinaryClass { Binary = new byte[100_000], Text = "text", };
        var compressed = TinyhandSerializer.Serialize(c, TinyhandSerializerOptions.Lz4);

        Assert.Throws<TinyhandException>(() => TinyhandSerializer.Deserialize<Lz4BinaryClass>(compressed));

        var c2 = TinyhandSerializer.Deserialize<Lz4BinaryClass>(compressed, TinyhandSerializerOptions.Lz4);
        c2!.Text.Is("text");
        c2.Binary.Length.Is(100_000);
    }

    [Fact]
    public void CorruptedBlockThrows()
    {
        var c = new Lz4BinaryClass { Binary = new byte[100_000], Text = "text", };
        var compressed = TinyhandSerializer.Serialize(c, TinyhandSerializerOptions.Lz4);

        // Damage the compressed payload (not the header) and make sure it is rejected
        // rather than silently producing a wrong value.
        var corrupted = compressed.ToArray();
        for (var i = corrupted.Length - 40; i < corrupted.Length; i++)
        {
            corrupted[i] ^= 0xFF;
        }

        Assert.ThrowsAny<Exception>(() => TinyhandSerializer.Deserialize<Lz4BinaryClass>(corrupted, TinyhandSerializerOptions.Lz4));
    }

    [Fact]
    public void InvalidUncompressedLengthIsRejected()
    {
        // The uncompressed length of each block is stored in the extension payload and is therefore
        // attacker controlled. A bogus value must be reported as invalid data instead of being used
        // to size a buffer (int.MaxValue would otherwise ask for a 2 GB allocation).
        var block = CompressedBlockOf(new byte[1_000]);

        foreach (var declaredLength in new[] { int.MinValue, -1, 0, int.MaxValue, (block.Length * 255) + 1 })
        {
            var message = BuildLz4Message(declaredLength, block);
            Assert.Throws<TinyhandException>(() => Decompress(message));
        }

        // The same message with the correct length decompresses.
        Decompress(BuildLz4Message(1_000, block)).Is(1_000);
    }

    /// <summary>
    /// Decompresses an LZ4 block array message and returns the number of uncompressed bytes.
    /// </summary>
    private static int Decompress(byte[] message)
    {
        var reader = new TinyhandReader(message);
        var sequence = new Arc.IO.ByteSequence();
        try
        {
            TinyhandSerializer.TryDecompress(ref reader, sequence).IsTrue();
            return sequence.ToReadOnlySpan().Length;
        }
        finally
        {
            sequence.Dispose();
        }
    }

    private static byte[] CompressedBlockOf(byte[] data)
    {
        var block = new byte[MessagePack.LZ4.LZ4Codec.MaximumOutputLength(data.Length)];
        var length = MessagePack.LZ4.LZ4Codec.Encode(data, block);
        return block.AsSpan(0, length).ToArray();
    }

    /// <summary>
    /// Builds an LZ4 block array message: [Ext(Lz4BlockArray, block lengths), bin].
    /// </summary>
    private static byte[] BuildLz4Message(int declaredLength, byte[] block)
    {
        var lengthWriter = new TinyhandWriter(new byte[16]);
        lengthWriter.Write(declaredLength);
        var lengths = lengthWriter.FlushAndGetArray();

        var writer = new TinyhandWriter(new byte[64]);
        writer.WriteArrayHeader(2);
        writer.WriteExtensionFormatHeader(new ExtensionHeader(MessagePackExtensionCodes.Lz4BlockArray, lengths.Length));
        writer.WriteSpan(lengths);
        writer.Write(block.AsSpan());
        return writer.FlushAndGetArray();
    }
}
