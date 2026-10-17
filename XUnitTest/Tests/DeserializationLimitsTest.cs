// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Tinyhand;
using Tinyhand.IO;
using Xunit;

namespace XUnitTest.Tests;

public class DeserializationLimitsTest
{
    [Fact]
    public void DefaultsAreFiniteOnlyForUntrustedData()
    {
        Assert.Equal(int.MaxValue, TinyhandSecurity.TrustedData.Limits.MaximumInputBytes);
        Assert.Equal(65_536, TinyhandSecurity.UntrustedData.Limits.MaximumCollectionLength);
        Assert.Throws<ArgumentOutOfRangeException>(() => new TinyhandDeserializationLimits { MaximumTotalElements = -1 });
        var policy = TinyhandSecurity.UntrustedData.WithMaximumObjectGraphDepth(4);
        Assert.Same(TinyhandSecurity.UntrustedData.Limits, policy.Limits);
    }

    [Fact]
    public void TrustedReaderDoesNotAllocateSecurityState()
    {
        byte[] data = [0x92, 0xa1, 65, 1];
        ReadTrusted(data); // Warm static initialization and the read path.
        long before = GC.GetAllocatedBytesForCurrentThread();
        int sum = 0;
        for (int i = 0; i < 1_000; i++)
        {
            sum += ReadTrusted(data);
        }

        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.Equal(4_000, sum);
        Assert.Equal(0, allocated);
    }

    [Fact]
    public void StringLimitRejectsBeforeDecodingAllocation()
    {
        var data = TinyhandSerializer.Serialize(new string('x', 512 * 1024));
        var options = Options(TinyhandDeserializationLimits.UntrustedData with { MaximumStringBytes = 16 });
        Assert.Throws<TinyhandException>(() => TinyhandSerializer.Deserialize<string>(data, options)); // Warm the failure path.
        long before = GC.GetAllocatedBytesForCurrentThread();
        Assert.Throws<TinyhandException>(() => TinyhandSerializer.Deserialize<string>(data, options));
        Assert.True(GC.GetAllocatedBytesForCurrentThread() - before < 64 * 1024, "The rejected string must not be decoded into a large UTF-16 allocation.");
        Assert.Equal(512 * 1024, TinyhandSerializer.Deserialize<string>(data)!.Length);
    }

    [Fact]
    public void CollectionLimitRejectsDirectFormatterBeforeArrayAllocation()
    {
        var data = TinyhandSerializer.Serialize(new long[16_384]);
        var options = Options(TinyhandDeserializationLimits.UntrustedData with { MaximumCollectionLength = 8 });
        Assert.Throws<TinyhandException>(() => DirectArray(data, options));
        long before = GC.GetAllocatedBytesForCurrentThread();
        Assert.Throws<TinyhandException>(() => DirectArray(data, options));
        Assert.True(GC.GetAllocatedBytesForCurrentThread() - before < 64 * 1024, "The rejected 128 KiB array must not be allocated.");
    }

    [Fact]
    public void DirectStringAndBinaryFormattersAttachLimits()
    {
        var options = Options(TinyhandDeserializationLimits.UntrustedData with { MaximumStringBytes = 3, MaximumBinaryBytes = 3 });
        Assert.Throws<TinyhandException>(() => DirectValue<string>([0xa4, 65, 66, 67, 68], options));
        Assert.Throws<TinyhandException>(() => DirectValue<byte[]>([0xc4, 4, 1, 2, 3, 4], options));
        Assert.Equal("ABC", DirectValue<string>([0xa3, 65, 66, 67], options));
        Assert.Equal(new byte[] { 1, 2, 3 }, DirectValue<byte[]>([0xc4, 3, 1, 2, 3], options));
    }

    [Fact]
    public void NestedCollectionsShareCumulativeElementLimit()
    {
        int[][] value = [[1, 2, 3, 4], [1, 2, 3, 4], [1, 2, 3, 4]];
        var data = TinyhandSerializer.Serialize(value);
        var limits = TinyhandDeserializationLimits.UntrustedData with { MaximumCollectionLength = 4, MaximumTotalElements = 15 };
        Assert.Equal(3, TinyhandSerializer.Deserialize<int[][]>(data, Options(limits))!.Length);
        Assert.Throws<TinyhandException>(() => TinyhandSerializer.Deserialize<int[][]>(data, Options(limits with { MaximumTotalElements = 14 })));
    }

    [Fact]
    public void RepeatedPayloadsShareCumulativeByteLimit()
    {
        var data = TinyhandSerializer.Serialize(new[] { "12345678", "12345678", "12345678" });
        var limits = TinyhandDeserializationLimits.UntrustedData with { MaximumStringBytes = 8, MaximumTotalPayloadBytes = 24 };
        Assert.Equal(3, TinyhandSerializer.Deserialize<string[]>(data, Options(limits))!.Length);
        Assert.Throws<TinyhandException>(() => TinyhandSerializer.Deserialize<string[]>(data, Options(limits with { MaximumTotalPayloadBytes = 23 })));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReaderCopiesAndSubReadersCannotResetBudget(bool subReader)
    {
        Assert.Throws<TinyhandException>(() => ReadThroughCopy(subReader));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public void ActiveReaderRejectsDifferentFiniteLimits(int newLimit)
    {
        var limits = TinyhandDeserializationLimits.UntrustedData with { MaximumTotalElements = 2 };
        var first = TinyhandSecurity.UntrustedData.WithLimits(limits);
        var replacement = first.WithLimits(limits with { MaximumTotalElements = newLimit });
        Assert.Throws<TinyhandException>(() => ReapplyLimits(first, replacement));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void CompatiblePoliciesPreserveConsumedBudget(int policyKind)
    {
        var limits = TinyhandDeserializationLimits.UntrustedData with { MaximumTotalElements = 2 };
        var first = TinyhandSecurity.UntrustedData.WithLimits(limits);
        var replacement = policyKind switch
        {
            0 => first.WithMaximumObjectGraphDepth(4), // The same Limits reference.
            1 => TinyhandSecurity.TrustedData,
            2 => first.WithLimits(TinyhandDeserializationLimits.Unrestricted),
            _ => TinyhandSecurity.TrustedData.WithLimits(limits with { }), // Equal limits in another record.
        };
        var reader = new TinyhandReader(new byte[] { 0x92, 0, 0, 0x91, 0 }, first);
        Assert.Equal(2, reader.ReadArrayHeader());
        reader.ReadInt32();
        reader.ReadInt32();
        reader.ApplySecurity(replacement); // Must succeed without replacing or replenishing the budget.
        bool rejected = false;
        try
        {
            reader.ReadArrayHeader();
        }
        catch (TinyhandException)
        {
            rejected = true;
        }

        Assert.True(rejected);
    }

    [Fact]
    public void UnknownFieldsCannotBypassCollectionLimits()
    {
        var options = Options(TinyhandDeserializationLimits.UntrustedData with { MaximumCollectionLength = 2 });
        Assert.Throws<TinyhandException>(() => TinyhandSerializer.Deserialize<LimitsEmpty>([0x91, 0x93, 0, 0, 0], options));
        Assert.Throws<TinyhandException>(() => TinyhandSerializer.Deserialize<LimitsEmpty>([0x91, 0xdc, 0, 3, 0, 0, 0], options));
        Assert.Throws<TinyhandException>(() => TinyhandSerializer.Deserialize<LimitsEmpty>([0x91, 0xdd, 0, 0, 0, 3, 0, 0, 0], options));
    }

    [Fact]
    public void StaticGeneratedEntryAndObjectEntryApplyInputLimitBeforeConstruction()
    {
        var data = new byte[32];
        data[0] = 0x90;
        var options = Options(TinyhandDeserializationLimits.UntrustedData with { MaximumInputBytes = 8 });
        LimitsConstructed.Constructions = 0;
        Assert.Throws<TinyhandException>(() => DirectGenerated<LimitsConstructed>(data, options));
        Assert.Throws<TinyhandException>(() => ObjectEntry<LimitsConstructed>(data, options));
        Assert.Equal(0, LimitsConstructed.Constructions);
    }

    [Fact]
    public void GeneratedArrayChecksDepthBeforeAllocation()
    {
        var data = new byte[16_388];
        data[0] = 0x91;
        data[1] = 0xdc;
        data[2] = 0x40;
        data[3] = 0;
        data.AsSpan(4).Fill(0xc0);
        var options = TinyhandSerializerOptions.Standard with { Security = TinyhandSecurity.UntrustedData.WithMaximumObjectGraphDepth(1) };
        Assert.Throws<TinyhandException>(() => TinyhandSerializer.Deserialize<LimitsNodes>(data, options));
        long before = GC.GetAllocatedBytesForCurrentThread();
        Assert.Throws<TinyhandException>(() => TinyhandSerializer.Deserialize<LimitsNodes>(data, options));
        Assert.True(GC.GetAllocatedBytesForCurrentThread() - before < 64 * 1024, "The rejected reference array must not be allocated before the depth check.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void ContainerDepthIsCheckedBeforeAllocation(int containerKind)
    {
        var options = TinyhandSerializerOptions.Standard with { Security = TinyhandSecurity.UntrustedData.WithMaximumObjectGraphDepth(0) };
        byte[] data;
        Action deserialize;
        if (containerKind == 0)
        {
            data = TinyhandSerializer.Serialize(new object[16_384]);
            deserialize = () => TinyhandSerializer.Deserialize<object>(data, options);
        }
        else if (containerKind == 1)
        {
            var values = new Dictionary<long, long>();
            for (int i = 0; i < 8_192; i++)
            {
                values.Add(i, i);
            }

            data = TinyhandSerializer.Serialize(values);
            deserialize = () => TinyhandSerializer.Deserialize<Dictionary<long, long>>(data, options);
        }
        else
        {
            data = TinyhandSerializer.Serialize(new long[128, 128]);
            deserialize = () => TinyhandSerializer.Deserialize<long[,]>(data, options);
        }

        Assert.Throws<TinyhandException>(deserialize);
        long before = GC.GetAllocatedBytesForCurrentThread();
        Assert.Throws<TinyhandException>(deserialize);
        Assert.True(GC.GetAllocatedBytesForCurrentThread() - before < 64 * 1024, "Depth rejection must precede the large container allocation.");
    }

    [Fact]
    public void CompressedOutputLimitIsCheckedBeforeRequestingOutputBuffer()
    {
        var data = TinyhandSerializer.Serialize(new byte[4096], TinyhandSerializerOptions.Lz4);
        var writer = new RecordingWriter();
        var options = Options(TinyhandDeserializationLimits.UntrustedData with { MaximumDecompressedBytes = 32 });
        Assert.Throws<TinyhandException>(() => Decompress(data, options, writer));
        Assert.Equal(0, writer.BufferRequests);
    }

    [Fact]
    public void DecompressedSubReaderUsesOutputLimitRatherThanInputLimit()
    {
        var value = new byte[4096];
        var compressed = TinyhandSerializer.Serialize(value, TinyhandSerializerOptions.Lz4);
        var plain = TinyhandSerializer.Serialize(value);
        var limits = TinyhandDeserializationLimits.UntrustedData with { MaximumInputBytes = compressed.Length, MaximumDecompressedBytes = plain.Length };
        var options = Options(limits) with { Flags = TinyhandSerializerOptions.SerializationFlags.Lz4Compress };
        Assert.Equal(value, TinyhandSerializer.Deserialize<byte[]>(compressed, options));
        Assert.Throws<TinyhandException>(() => TinyhandSerializer.Deserialize<byte[]>(compressed, options with { Security = options.Security.WithLimits(limits with { MaximumDecompressedBytes = plain.Length - 1 }) }));
    }

    [Fact]
    public void CompressionLookaheadDoesNotConsumeAnUncompressedMessagesBudget()
    {
        byte[] data = [0x91, 0x92, 1, 2];
        var options = Options(TinyhandDeserializationLimits.UntrustedData with { MaximumTotalElements = 3 }) with { Flags = TinyhandSerializerOptions.SerializationFlags.Lz4Compress };
        Assert.Equal(new[] { 1, 2 }, TinyhandSerializer.Deserialize<int[][]>(data, options)![0]);
    }

    [Fact]
    public void NonseekableStreamStopsAtInputLimit()
    {
        using var stream = new EndlessStream();
        var options = Options(TinyhandDeserializationLimits.UntrustedData with { MaximumInputBytes = 32 });
        Assert.Throws<TinyhandException>(() => TinyhandSerializer.Deserialize<int>(stream, options, TestContext.Current.CancellationToken));
        Assert.Equal(33, stream.BytesRead);
    }

    [Fact]
    public async Task NonseekableAsyncStreamStopsAtInputLimit()
    {
        using var stream = new EndlessStream();
        var options = Options(TinyhandDeserializationLimits.UntrustedData with { MaximumInputBytes = 32 });
        await Assert.ThrowsAsync<TinyhandException>(async () => await TinyhandSerializer.DeserializeAsync<int>(stream, options, TestContext.Current.CancellationToken));
        Assert.Equal(33, stream.BytesRead);
    }

    [Fact]
    public async Task StreamCancellationIsObservedBeforeReading()
    {
        using var stream = new EndlessStream();
        using var source = new CancellationTokenSource();
        source.Cancel();
        Assert.Throws<OperationCanceledException>(() => TinyhandSerializer.Deserialize<int>(stream, null, source.Token));
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await TinyhandSerializer.DeserializeAsync<int>(stream, null, source.Token));
        Assert.Equal(0, stream.BytesRead);
    }

    private static TinyhandSerializerOptions Options(TinyhandDeserializationLimits limits)
        => TinyhandSerializerOptions.Standard with { Security = TinyhandSecurity.UntrustedData.WithLimits(limits) };

    private static int ReadTrusted(byte[] data)
    {
        var reader = new TinyhandReader(data, TinyhandSecurity.TrustedData);
        return reader.ReadArrayHeader() + reader.ReadStringSpan().Length + reader.ReadInt32();
    }

    private static void DirectArray(byte[] data, TinyhandSerializerOptions options) => DirectValue<long[]>(data, options);

    private static T? DirectValue<T>(byte[] data, TinyhandSerializerOptions options)
    {
        var reader = new TinyhandReader(data);
        T? value = default;
        options.Resolver.GetFormatter<T>().Deserialize(ref reader, ref value, options);
        return value;
    }

    private static void DirectGenerated<T>(byte[] data, TinyhandSerializerOptions options)
        where T : ITinyhandSerializable<T>
    {
        var reader = new TinyhandReader(data);
        T? value = default;
        T.Deserialize(ref reader, ref value, options);
    }

    private static void ObjectEntry<T>(byte[] data, TinyhandSerializerOptions options)
        where T : ITinyhandSerializable<T>
    {
        var reader = new TinyhandReader(data);
        TinyhandSerializer.DeserializeObject<T>(ref reader, options);
    }

    private static void ReadThroughCopy(bool subReader)
    {
        byte[] data = [0x92, 0, 0];
        var security = TinyhandSecurity.UntrustedData.WithLimits(TinyhandDeserializationLimits.UntrustedData with { MaximumTotalElements = 2 });
        var reader = new TinyhandReader(data, security);
        var copy = subReader ? reader.CreateSubReader(data) : reader.Fork();
        reader.ReadArrayHeader();
        copy.ApplySecurity(TinyhandSecurity.TrustedData);
        copy.ReadArrayHeader();
    }

    private static void ReapplyLimits(TinyhandSecurity first, TinyhandSecurity replacement)
    {
        var reader = new TinyhandReader(new byte[] { 0x90 }, first);
        TinyhandSerializer.Deserialize<int[]>(ref reader, TinyhandSerializerOptions.Standard with { Security = replacement });
    }

    private static void Decompress(byte[] data, TinyhandSerializerOptions options, IBufferWriter<byte> writer)
    {
        var reader = new TinyhandReader(data, options.Security);
        TinyhandSerializer.TryDecompress(ref reader, writer);
    }

    private sealed class RecordingWriter : IBufferWriter<byte>
    {
        public int BufferRequests { get; private set; }

        public void Advance(int count) => throw new InvalidOperationException();

        public Memory<byte> GetMemory(int sizeHint = 0)
        {
            this.BufferRequests++;
            throw new InvalidOperationException("Output was requested before validating its limit.");
        }

        public Span<byte> GetSpan(int sizeHint = 0)
        {
            this.BufferRequests++;
            throw new InvalidOperationException("Output was requested before validating its limit.");
        }
    }

    private sealed class EndlessStream : Stream
    {
        public long BytesRead { get; private set; }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override int Read(Span<byte> buffer)
        {
            buffer.Clear();
            this.BytesRead += buffer.Length;
            return buffer.Length;
        }

        public override int Read(byte[] buffer, int offset, int count) => this.Read(buffer.AsSpan(offset, count));

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(this.Read(buffer.Span));
        }

        public override void Flush() => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}

[TinyhandObject]
public partial class LimitsEmpty
{
}

[TinyhandObject]
public partial class LimitsConstructed
{
    public static int Constructions;

    public LimitsConstructed() => Constructions++;
}

[TinyhandObject]
public partial class LimitsNodes
{
    [Key(0)]
    public LimitsEmpty?[] Items = [];
}
