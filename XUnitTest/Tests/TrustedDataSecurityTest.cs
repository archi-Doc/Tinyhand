// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO;
using System.Threading.Tasks;
using Tinyhand;
using Tinyhand.IO;
using Xunit;

namespace XUnitTest.Tests;

public class TrustedDataSecurityTest
{
    [Fact]
    public void BuiltInDefaultsHaveFiniteLimitsAndDistinctValidationPolicies()
    {
        var trusted = TinyhandSecurity.TrustedData;
        Assert.Equal(256, trusted.MaximumObjectGraphDepth);
        Assert.True(trusted.ValidateNativeValues);
        Assert.False(trusted.HashCollisionResistant);
        Assert.False(trusted.ValidateStringValues);
        Assert.False(trusted.RejectDuplicateKeys);
        Assert.False(trusted.RejectUnknownUnionTypes);
        Assert.False(trusted.RequireCompleteInput);
        Assert.False(trusted.CheckAvailableStack);
        Assert.False(trusted.SortBeforeInsertion);
        Assert.Equal(long.MaxValue, trusted.Limits.MaximumTotalCollectionBytes);
        Assert.Throws<ArgumentOutOfRangeException>(() => trusted.WithMaximumObjectGraphDepth(-1));
        Assert.Equal(0, trusted.WithMaximumObjectGraphDepth(0).MaximumObjectGraphDepth);
        AssertLimits(trusted.Limits, 64 * 1024 * 1024, 128 * 1024 * 1024, 2_097_152, 4_194_304, 16 * 1024 * 1024, 64 * 1024 * 1024, 128 * 1024 * 1024);

        var untrusted = TinyhandSecurity.UntrustedData;
        Assert.Equal(500, untrusted.MaximumObjectGraphDepth);
        Assert.True(untrusted.ValidateNativeValues);
        Assert.True(untrusted.HashCollisionResistant);
        Assert.True(untrusted.ValidateStringValues);
        Assert.True(untrusted.RejectDuplicateKeys);
        Assert.True(untrusted.RejectUnknownUnionTypes);
        Assert.True(untrusted.RequireCompleteInput);
        Assert.True(untrusted.CheckAvailableStack);
        Assert.True(untrusted.SortBeforeInsertion);
        Assert.Equal(64 * 1024 * 1024, untrusted.Limits.MaximumTotalCollectionBytes);
        AssertLimits(untrusted.Limits, 16 * 1024 * 1024, 64 * 1024 * 1024, 65_536, 1_048_576, 16 * 1024 * 1024, 16 * 1024 * 1024, 64 * 1024 * 1024);
    }

    [Fact]
    public void DefaultCollectionLimitRejectsHeaderBeforeAllocation()
    {
        var length = TinyhandSecurity.TrustedData.Limits.MaximumCollectionLength;
        var header = CollectionHeader(length);
        Assert.Equal(length, ReadArrayHeaderOnly(header, TinyhandSecurity.TrustedData));
        header = CollectionHeader(length + 1);
        Assert.Throws<TinyhandException>(() => ReadArrayHeaderOnly(header, TinyhandSecurity.TrustedData));
    }

    [Fact]
    public void DefaultCumulativeElementBudgetIsSharedByReaderCopies()
    {
        var header = CollectionHeader(TinyhandSecurity.TrustedData.Limits.MaximumCollectionLength);
        Assert.Throws<TinyhandException>(() => ConsumeRepeatedHeaders(header));
    }

    [Theory]
    [InlineData(MessagePackCode.Str32)]
    [InlineData(MessagePackCode.Bin32)]
    public void DefaultPayloadLimitRejectsOversizedHeader(byte code)
    {
        var maximum = code == MessagePackCode.Str32
            ? TinyhandSecurity.TrustedData.Limits.MaximumStringBytes
            : TinyhandSecurity.TrustedData.Limits.MaximumBinaryBytes;
        var header = new byte[5];
        header[0] = code;
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(1), maximum + 1);
        Assert.Throws<TinyhandException>(() => ReadOversizedPayload(header, code));
    }

    [Fact]
    public void DefaultDepthLimitRejectsNestedArraysAndCanBeOverridden()
    {
        const int depth = 256;
        Assert.Equal(depth, TinyhandSecurity.TrustedData.MaximumObjectGraphDepth);
        var data = NestedArrays(depth);
        Assert.NotNull(TinyhandSerializer.Deserialize<object>(data, TinyhandSerializerOptions.Standard));
        data = NestedArrays(depth + 1);
        Assert.Throws<TinyhandException>(() => TinyhandSerializer.Deserialize<object>(data, TinyhandSerializerOptions.Standard));
        var options = TinyhandSerializerOptions.Standard with { Security = TinyhandSecurity.TrustedData.WithMaximumObjectGraphDepth(depth + 1) };
        Assert.NotNull(TinyhandSerializer.Deserialize<object>(data, options));
    }

    [Fact]
    public async Task DefaultInputLimitRejectsOversizedStreamsBeforeReading()
    {
        using var stream = new LengthOnlyStream((long)TinyhandSecurity.TrustedData.Limits.MaximumInputBytes + 1);
        Assert.Throws<TinyhandException>(() => TinyhandSerializer.Deserialize<int>(stream, TinyhandSerializerOptions.Standard, TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<TinyhandException>(async () => await TinyhandSerializer.DeserializeAsync<int>(stream, TinyhandSerializerOptions.Standard, TestContext.Current.CancellationToken));
        Assert.Equal(0, stream.ReadCalls);
    }

    [Fact]
    public void DefaultCompressedOutputLimitIsCheckedBeforeAllocating()
    {
        var maximum = TinyhandSecurity.TrustedData.Limits.MaximumDecompressedBytes;
        var writer = new RecordingWriter();
        Assert.Throws<TinyhandException>(() => Decompress(CreateCompressedHeader(maximum + 1), writer));
        Assert.Equal(0, writer.Requested);

        // Stop at the output request: neither case allocates the claimed 128 MiB or invokes the decoder.
        Assert.Throws<OutputBufferRequestedException>(() => Decompress(CreateCompressedHeader(maximum), writer));
        Assert.Equal(maximum, writer.Requested);
    }

    [Fact]
    public void TrustedLimitsCanBeAdjustedOrExplicitlyDisabled()
    {
        var limits = TinyhandSecurity.TrustedData.Limits;
        var header = CollectionHeader(limits.MaximumCollectionLength + 1);
        var adjusted = TinyhandSecurity.TrustedData.WithLimits(limits with { MaximumCollectionLength = limits.MaximumCollectionLength + 1 });
        Assert.Equal(limits.MaximumCollectionLength + 1, ReadArrayHeaderOnly(header, adjusted));
        var unrestricted = TinyhandSecurity.TrustedData.WithLimits(TinyhandDeserializationLimits.Unrestricted);
        Assert.Equal(limits.MaximumCollectionLength + 1, ReadArrayHeaderOnly(header, unrestricted));
        Assert.Same(limits, TinyhandSecurity.TrustedData.Limits);
    }

    [Fact]
    public void DefaultTrustedPolicyCannotReplaceDifferentActiveLimits()
    {
        Assert.Throws<TinyhandException>(() => ReapplyTrustedPolicy(TinyhandSecurity.UntrustedData));
        Assert.Throws<TinyhandException>(() => ReapplyTrustedPolicy(TinyhandSecurity.TrustedData.WithLimits(TinyhandSecurity.TrustedData.Limits with { MaximumTotalElements = 2 })));
    }

    private static void AssertLimits(TinyhandDeserializationLimits limits, int input, int decompressed, int collection, long elements, int text, int binary, long payload)
    {
        Assert.Equal(input, limits.MaximumInputBytes);
        Assert.Equal(decompressed, limits.MaximumDecompressedBytes);
        Assert.Equal(collection, limits.MaximumCollectionLength);
        Assert.Equal(elements, limits.MaximumTotalElements);
        Assert.Equal(text, limits.MaximumStringBytes);
        Assert.Equal(binary, limits.MaximumBinaryBytes);
        Assert.Equal(payload, limits.MaximumTotalPayloadBytes);
    }

    private static byte[] CollectionHeader(int length)
    {
        var header = new byte[5];
        header[0] = MessagePackCode.Array32;
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(1), length);
        return header;
    }

    private static int ReadArrayHeaderOnly(byte[] header, TinyhandSecurity security)
    {
        var reader = new TinyhandReader(header, security);
        Assert.True(reader.TryReadArrayHeader(out var length));
        return length;
    }

    private static void ConsumeRepeatedHeaders(byte[] header)
    {
        var reader = new TinyhandReader(header, TinyhandSecurity.TrustedData);
        var firstCopy = reader.Fork();
        var secondCopy = reader.CreateSubReader(header);
        Assert.True(reader.TryReadArrayHeader(out _));
        Assert.True(firstCopy.TryReadArrayHeader(out _));
        secondCopy.ApplySecurity(TinyhandSecurity.TrustedData);
        secondCopy.TryReadArrayHeader(out _);
    }

    private static void ReadOversizedPayload(byte[] header, byte code)
    {
        var reader = new TinyhandReader(header, TinyhandSecurity.TrustedData);
        if (code == MessagePackCode.Str32)
        {
            reader.ReadStringSpan();
        }
        else
        {
            reader.ReadBinHeader();
        }
    }

    private static byte[] NestedArrays(int depth)
    {
        var data = new byte[depth + 1];
        data.AsSpan(0, depth).Fill(MessagePackCode.MinFixArray + 1);
        data[^1] = 42;
        return data;
    }

    private static byte[] CreateCompressedHeader(int outputLength)
    {
        var compressedLength = (outputLength + 254) / 255;
        var data = new byte[14 + compressedLength];
        data[0] = MessagePackCode.MinFixArray + 2;
        data[1] = MessagePackCode.Ext8;
        data[2] = 5;
        data[3] = MessagePackExtensionCodes.Lz4BlockArray;
        data[4] = MessagePackCode.Int32;
        BinaryPrimitives.WriteInt32BigEndian(data.AsSpan(5), outputLength);
        data[9] = MessagePackCode.Bin32;
        BinaryPrimitives.WriteInt32BigEndian(data.AsSpan(10), compressedLength);
        return data;
    }

    private static void Decompress(byte[] data, IBufferWriter<byte> writer)
    {
        var reader = new TinyhandReader(data, TinyhandSecurity.TrustedData);
        TinyhandSerializer.TryDecompress(ref reader, writer);
    }

    private static void ReapplyTrustedPolicy(TinyhandSecurity initial)
    {
        var reader = new TinyhandReader(new byte[] { MessagePackCode.Nil }, initial);
        reader.ApplySecurity(TinyhandSecurity.TrustedData);
    }

    private sealed class OutputBufferRequestedException : Exception
    {
    }

    private sealed class RecordingWriter : IBufferWriter<byte>
    {
        public int Requested { get; private set; }

        public void Advance(int count) => throw new InvalidOperationException();

        public Memory<byte> GetMemory(int sizeHint = 0)
        {
            this.Requested = sizeHint;
            throw new OutputBufferRequestedException();
        }

        public Span<byte> GetSpan(int sizeHint = 0)
        {
            this.Requested = sizeHint;
            throw new OutputBufferRequestedException();
        }
    }

    private sealed class LengthOnlyStream(long length) : Stream
    {
        public int ReadCalls { get; private set; }

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => false;

        public override long Length => length;

        public override long Position { get => 0; set => throw new NotSupportedException(); }

        public override int Read(byte[] buffer, int offset, int count)
        {
            this.ReadCalls++;
            throw new InvalidOperationException("The input limit must be checked before reading.");
        }

        public override void Flush() => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
