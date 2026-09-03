// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using Arc.IO;
using Tinyhand;
using Tinyhand.IO;
using Xunit;

namespace XUnitTest.Tests;

public class ByteSequenceTest
{
    [Fact]
    public void SingleAndMultipleVaults()
    {
        // 100 bytes fits in one vault; 100_000 spans several.
        foreach (var length in new[] { 0, 1, 100, ByteSequence.DefaultVaultSize, 100_000 })
        {
            var source = new byte[length];
            new Random(length).NextBytes(source);

            using var sequence = new ByteSequence();
            var offset = 0;
            while (offset < length)
            {
                var chunk = Math.Min(1000, length - offset);
                var span = sequence.GetSpan(chunk);
                source.AsSpan(offset, chunk).CopyTo(span);
                sequence.Advance(chunk);
                offset += chunk;
            }

            sequence.ToReadOnlySpan().SequenceEqual(source).IsTrue();
            sequence.ToReadOnlySequence().ToArray().SequenceEqual(source).IsTrue();
            sequence.ToReadOnlyMemory().Span.SequenceEqual(source).IsTrue();

            var rentMemory = sequence.ToRentMemory();
            try
            {
                rentMemory.Span.SequenceEqual(source).IsTrue();
            }
            finally
            {
                rentMemory.Return();
            }
        }
    }

    [Fact]
    public void AdvanceBeforeGetMemoryThrows()
    {
        using var sequence = new ByteSequence();
        Assert.Throws<InvalidOperationException>(() => sequence.Advance(1));
    }
}

public class TinyhandWriterBufferTest
{
    /// <summary>
    /// Writes more than the initial buffer so the writer has to spill into a <see cref="ByteSequence"/>.
    /// </summary>
    [Fact]
    public void GrowsBeyondInitialBuffer()
    {
        const int Count = 20_000;

        var writer = new TinyhandWriter(new byte[64]);
        try
        {
            writer.WriteArrayHeader(Count);
            for (var i = 0; i < Count; i++)
            {
                writer.Write(i);
            }

            var array = writer.FlushAndGetArray();
            var reader = new TinyhandReader(array);
            reader.ReadArrayHeader().Is(Count);
            for (var i = 0; i < Count; i++)
            {
                reader.ReadInt32().Is(i);
            }

            reader.End.IsTrue();
        }
        finally
        {
            writer.Dispose();
        }
    }

    [Fact]
    public void WriteToExternalBufferWriter()
    {
        var bufferWriter = new ArrayBufferWriter<byte>();
        TinyhandSerializer.Serialize(bufferWriter, new[] { 1, 2, 3 });

        var value = TinyhandSerializer.Deserialize<int[]>(bufferWriter.WrittenSpan);
        value!.SequenceEqual([1, 2, 3]).IsTrue();
    }

    [Fact]
    public void WrittenCountIsTracked()
    {
        var writer = TinyhandWriter.CreateFromBytePool(64);
        try
        {
            writer.Written.Is(0L);
            writer.Write(true);
            writer.Written.Is(1L);
            writer.Write(new byte[1000]);
            writer.Written.Is(1L + 3 + 1000); // bool + bin16 header + payload
        }
        finally
        {
            writer.Dispose();
        }
    }
}

public class CorruptedDataTest
{
    [Fact]
    public void GarbageDoesNotCrash()
    {
        var random = new Random(42);
        for (var i = 0; i < 500; i++)
        {
            var bin = new byte[random.Next(1, 32)];
            random.NextBytes(bin);

            // Any outcome other than a hang or a process crash is acceptable.
            try
            {
                TinyhandSerializer.Deserialize<int[]>(bin);
            }
            catch (Exception ex)
            {
                Assert.False(ex is AccessViolationException or IndexOutOfRangeException);
            }
        }
    }

    [Fact]
    public void TruncatedDataThrows()
    {
        var bin = TinyhandSerializer.Serialize(new[] { "abcdefg", "hijklmn" });
        for (var length = 0; length < bin.Length; length++)
        {
            Assert.ThrowsAny<Exception>(() => TinyhandSerializer.Deserialize<string[]>(bin.AsSpan(0, length).ToArray()));
        }

        TinyhandSerializer.Deserialize<string[]>(bin)!.Length.Is(2);
    }

    [Fact]
    public void TrySkipOnTruncatedDataReturnsFalse()
    {
        var bin = TinyhandSerializer.Serialize(new object[] { 1, "test", new byte[300], });
        for (var length = 1; length < bin.Length; length++)
        {
            var reader = new TinyhandReader(bin.AsSpan(0, length));
            reader.TrySkip().IsFalse();
        }
    }
}
