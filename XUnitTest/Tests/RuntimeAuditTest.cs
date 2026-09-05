// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tinyhand;
using Tinyhand.IO;
using Tinyhand.Resolvers;
using Xunit;

namespace XUnitTest;

public class RuntimeAuditTest
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ParserReadsShortChunksToEnd(bool seekable)
    {
        using var stream = new ChunkedStream(Encoding.UTF8.GetBytes("prefix name = \"日本語\" values = { 1, 2, 3 }"), seekable);
        stream.ReadExactly(new byte[7]);
        var expected = TinyhandParser.Parse("name = \"日本語\" values = { 1, 2, 3 }");
        Assert.Equal(TinyhandComposer.Compose(expected), TinyhandComposer.Compose(TinyhandParser.Parse(stream)));
        Assert.True(stream.CanRead);
    }

    [Fact]
    public void ParserHandlesMemoryStreamSlicesAndEmptyStreams()
    {
        var bytes = Encoding.UTF8.GetBytes("junk skip a = 1 tail");
        using var stream = new MemoryStream(bytes, 5, bytes.Length - 10, writable: false, publiclyVisible: true);
        stream.Position = 5;
        Assert.Equal(TinyhandComposer.Compose(TinyhandParser.Parse("a = 1")), TinyhandComposer.Compose(TinyhandParser.Parse(stream)));
        Assert.Equal(stream.Length, stream.Position);
        using var empty = new ChunkedStream([], false);
        Assert.Equal(TinyhandComposer.Compose(TinyhandParser.Parse("")), TinyhandComposer.Compose(TinyhandParser.Parse(empty)));
    }

    [Fact]
    public async Task ParserOpensFilesForReadAccess()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".tinyhand");
        try
        {
            File.WriteAllText(path, "value = 123");
            // An existing reader permits other readers but denies write access.
            using var held = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            var expected = TinyhandComposer.Compose(TinyhandParser.Parse("value = 123"));
            Assert.Equal(expected, TinyhandComposer.Compose(TinyhandParser.ParseFile(path)));
            Assert.Equal(expected, TinyhandComposer.Compose(await TinyhandParser.ParseFileAsync(path)));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void RefFormatterNilPreservesExistingValues()
    {
        CheckNil<byte?>(1);
        CheckNil<sbyte?>(1);
        CheckNil<ushort?>(1);
        CheckNil<short?>(1);
        CheckNil<uint?>(1);
        CheckNil<int?>(1);
        CheckNil<ulong?>(1);
        CheckNil<long?>(1);
        CheckNil<float?>(1);
        CheckNil<double?>(1);
        CheckNil<bool?>(true);
        CheckNil<char?>('a');
        CheckNil<DateTime?>(DateTime.UnixEpoch);
        CheckNil<Int128?>(1);
        CheckNil<UInt128?>(1);
        CheckNil<decimal?>(1);
        CheckNil<Guid?>(Guid.Empty);
        CheckNil<DayOfWeek?>(DayOfWeek.Monday);
        CheckNil<byte[]>([1]);
        CheckNil<List<byte>>([1]);
        CheckNil<Dictionary<int, string>>(new() { [1] = "old" });
        CheckNil<int[,]>(new int[1, 1]);
        CheckNil<int[,,]>(new int[1, 1, 1]);
        CheckNil<int[,,,]>(new int[1, 1, 1, 1]);
    }

    [Fact]
    public void ImmutableArrayPreservesDefaultAndEmptyDistinction()
    {
        var empty = ImmutableArray<int>.Empty;
        var absent = default(ImmutableArray<int>);
        Assert.True(TinyhandSerializer.Deserialize<ImmutableArray<int>>(TinyhandSerializer.Serialize(empty)).IsEmpty);
        Assert.True(TinyhandSerializer.Deserialize<ImmutableArray<int>>(TinyhandSerializer.Serialize(absent)).IsDefault);
        Assert.True(TinyhandSerializer.Clone(absent).IsDefault);
        Assert.True(TinyhandSerializer.Clone(empty).IsEmpty);
        var existing = ImmutableArray.Create(1, 2, 3);
        var reader = new TinyhandReader(TinyhandSerializer.Serialize(empty));
        TinyhandSerializerOptions.Standard.Resolver.GetFormatter<ImmutableArray<int>>().Deserialize(ref reader, ref existing, TinyhandSerializerOptions.Standard);
        Assert.True(existing.IsEmpty);
    }

    [Fact]
    public void PrimitiveArrayEmptyOperationsReuseSingletons()
    {
        CheckEmptyArray<byte>();
        CheckEmptyArray<sbyte>();
        CheckEmptyArray<ushort>();
        CheckEmptyArray<short>();
        CheckEmptyArray<uint>();
        CheckEmptyArray<int>();
        CheckEmptyArray<ulong>();
        CheckEmptyArray<long>();
        CheckEmptyArray<float>();
        CheckEmptyArray<double>();
        CheckEmptyArray<bool>();
        CheckEmptyArray<char>();
        CheckEmptyArray<DateTime>();
        CheckEmptyArray<Int128>();
        CheckEmptyArray<UInt128>();
    }

    [Fact]
    [Trait("Category", "Allocation")]
    public void ByteListUsesBinaryEncodingWithoutIntermediateArrays()
    {
        var value = new List<byte>(Enumerable.Range(0, 1024).Select(x => (byte)x));
        var encoded = TinyhandSerializer.Serialize(value);
        Assert.Equal(TinyhandSerializer.Serialize(value.ToArray()), encoded);
        Assert.Equal(value, TinyhandSerializer.Deserialize<List<byte>>(encoded));
        var destination = new ArrayBufferWriter<byte>(2048);
        Assert.Equal(0, Measure(() =>
        {
            destination.Clear();
            TinyhandSerializer.Serialize(destination, value);
        }));
        var expectedAllocation = Measure(() => GC.KeepAlive(new List<byte>(value)));
        Assert.Equal(expectedAllocation, Measure(() => GC.KeepAlive(TinyhandSerializer.Deserialize<List<byte>>(encoded))));
    }

    [Fact]
    [Trait("Category", "Allocation")]
    public void BorrowingASequenceAndWritingASmallStreamDoNotAllocate()
    {
        var buffer = new byte[128];
        Assert.Equal(0, Measure(() =>
        {
            var writer = new TinyhandWriter(buffer);
            try
            {
                writer.Write(12345);
                var sequence = writer.FlushAndGetReadOnlySequence();
                if (writer.Written != sequence.Length)
                {
                    throw new InvalidOperationException("Unexpected sequence length.");
                }
            }
            finally
            {
                writer.Dispose();
            }
        }));
        using var stream = new MemoryStream(128);
        Assert.Equal(0, Measure(() =>
        {
            stream.Position = 0;
            TinyhandSerializer.Serialize(stream, 12345);
        }));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void NestedSerializationDoesNotOverwriteActiveWriters(bool compressed, bool nestedText)
    {
        var options = (compressed ? TinyhandSerializerOptions.Lz4 : TinyhandSerializerOptions.Standard) with
        {
            Resolver = CompositeResolver.Create([new NestedFormatter(nestedText)], [TinyhandSerializerOptions.Standard.Resolver]),
        };
        var value = new NestedValue();
        Assert.NotNull(TinyhandSerializer.Deserialize<NestedValue>(TinyhandSerializer.Serialize(value, options), options));
        Assert.NotNull(TinyhandSerializer.DeserializeFromUtf8<NestedValue>(TinyhandSerializer.SerializeToUtf8(value, options), options));
    }

    [Fact]
    [Trait("Category", "Allocation")]
    public void ThreadStaticWriterLeasesAreReleasedAfterExceptions()
    {
        for (var i = 0; i < 2; i++)
        {
            using var stream = new ThrowingStream();
            Assert.Throws<TinyhandException>(() => TinyhandSerializer.Serialize(stream, 12345));
        }

        using var destination = new MemoryStream(128);
        Assert.Equal(0, Measure(() =>
        {
            destination.Position = 0;
            TinyhandSerializer.Serialize(destination, 12345);
        }));
    }

    private static void CheckNil<T>(T initial)
    {
        var reader = new TinyhandReader(new byte[] { 0xc0 });
        T? value = initial;
        TinyhandSerializerOptions.Standard.Resolver.GetFormatter<T>().Deserialize(ref reader, ref value, TinyhandSerializerOptions.Standard);
        Assert.Equal(initial, value);
        Assert.True(reader.End);
    }

    private static void CheckEmptyArray<T>()
    {
        Assert.Same(Array.Empty<T>(), TinyhandSerializer.Reconstruct<T[]>());
        Assert.Same(Array.Empty<T>(), TinyhandSerializer.Clone(Array.Empty<T>()));
        Assert.Null(TinyhandSerializer.Clone<T[]>(null));
    }

    private static long Measure(Action action)
    {
        for (var i = 0; i < 32; i++)
        {
            action();
        }

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 256; i++)
        {
            action();
        }

        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    private sealed class ChunkedStream(byte[] bytes, bool seekable) : Stream
    {
        private int offset;
        public override bool CanRead => true;
        public override bool CanSeek => seekable;
        public override bool CanWrite => false;
        public override long Length => seekable ? bytes.Length : throw new NotSupportedException();
        public override long Position { get => seekable ? this.offset : throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override int Read(byte[] buffer, int offset, int count) => this.Read(buffer.AsSpan(offset, count));
        public override int Read(Span<byte> buffer)
        {
            var count = Math.Min(Math.Min(3, buffer.Length), bytes.Length - this.offset);
            bytes.AsSpan(this.offset, count).CopyTo(buffer);
            this.offset += count;
            return count;
        }

        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    private sealed class ThrowingStream : MemoryStream
    {
        public override void Write(ReadOnlySpan<byte> buffer) => throw new IOException("Test write failure.");
    }

    private sealed class NestedValue;

    private sealed class NestedFormatter(bool nestedText) : ITinyhandFormatter<NestedValue>
    {
        private static readonly string Payload = new('x', 512);

        public void Serialize(ref TinyhandWriter writer, NestedValue? value, TinyhandSerializerOptions options)
        {
            writer.WriteArrayHeader(3);
            writer.Write(12345);
            writer.Write(nestedText ? TinyhandSerializer.SerializeToUtf8(Payload, options) : TinyhandSerializer.Serialize(Payload, options));
            writer.Write(67890);
        }

        public void Deserialize(ref TinyhandReader reader, ref NestedValue? value, TinyhandSerializerOptions options)
        {
            Assert.Equal(3, reader.ReadArrayHeader());
            Assert.Equal(12345, reader.ReadInt32());
            var nested = reader.ReadBytesToArray();
            Assert.Equal(Payload, nestedText ? TinyhandSerializer.DeserializeFromUtf8<string>(nested, options) : TinyhandSerializer.Deserialize<string>(nested, options));
            Assert.Equal(67890, reader.ReadInt32());
            value = new();
        }

        public NestedValue Reconstruct(TinyhandSerializerOptions options) => new();
        public NestedValue? Clone(NestedValue? value, TinyhandSerializerOptions options) => value is null ? null : new();
    }
}
