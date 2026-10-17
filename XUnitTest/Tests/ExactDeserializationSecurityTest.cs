// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.IO;
using System.Threading.Tasks;
using MessagePack.LZ4;
using Tinyhand;
using Tinyhand.IO;
using Tinyhand.Resolvers;
using Xunit;

namespace XUnitTest.Tests;

[Collection("SerializerDefaults")]
public class ExactDeserializationSecurityTest
{
    private static readonly TinyhandSerializerOptions Untrusted = TinyhandSerializerOptions.Standard with { Security = TinyhandSecurity.UntrustedData };
    private static readonly TinyhandSerializerOptions UntrustedLz4 = TinyhandSerializerOptions.Lz4 with { Security = TinyhandSecurity.UntrustedData };

    [Fact]
    public void CompleteBufferPolicyRejectsTrailingValuesAndCanBeDisabled()
    {
        byte[] data = [42, 43];
        Assert.Throws<TinyhandException>(() => TinyhandSerializer.DeserializeExact<int>(data, TinyhandSerializerOptions.Standard));
        Assert.Throws<TinyhandException>(() => TinyhandSerializer.Deserialize<int>(data, Untrusted));
        Assert.Equal(42, TinyhandSerializer.Deserialize<int>(data, TinyhandSerializerOptions.Standard));
        var options = Untrusted with { Security = TinyhandSecurity.UntrustedData.WithRequireCompleteInput(false) };
        Assert.Equal(42, TinyhandSerializer.Deserialize<int>(data, options));
        Assert.Throws<TinyhandException>(() => TinyhandSerializer.DeserializeExact<int>(data, options));
        Assert.True(TinyhandSecurity.UntrustedData.WithMaximumObjectGraphDepth(4).RequireCompleteInput);
    }

    [Fact]
    public void ReaderAndConsumedByteApisRemainSequential()
    {
        byte[] data = [42, 43];
        Assert.Equal(42, TinyhandSerializer.Deserialize<int>(data, out var consumed, Untrusted));
        Assert.Equal(1, consumed);
        Assert.Equal(43, TinyhandSerializer.Deserialize<int>(data.AsSpan(consumed), Untrusted));
        var reader = new TinyhandReader(data);
        Assert.Equal(42, TinyhandSerializer.Deserialize<int>(ref reader, Untrusted));
        Assert.Equal(1, reader.Remaining);
        Assert.Equal(43, TinyhandSerializer.Deserialize<int>(ref reader, Untrusted));
        Assert.True(reader.End);

        Assert.True(TinyhandSerializer.TryDeserialize<int>(data, out var value, out consumed, Untrusted));
        Assert.Equal(42, value);
        Assert.Equal(1, consumed);
        Assert.False(TinyhandSerializer.TryDeserialize<int>(data, out value, Untrusted));
        Assert.Equal(0, value);
    }

    [Fact]
    public async Task StreamApisRemainSequential()
    {
        using var stream = new MemoryStream(new byte[] { 42, 43 });
        Assert.Equal(42, TinyhandSerializer.Deserialize<int>(stream, Untrusted, TestContext.Current.CancellationToken));
        Assert.Equal(1, stream.Position);
        Assert.Equal(43, await TinyhandSerializer.DeserializeAsync<int>(stream, Untrusted, TestContext.Current.CancellationToken));
        Assert.Equal(2, stream.Position);
    }

    [Fact]
    public void CompressedEnvelopeRequiresExactlyOneInnerValue()
    {
        var frame = Compress([42, 43]);
        Assert.Throws<TinyhandException>(() => TinyhandSerializer.DeserializeExact<int>(frame, TinyhandSerializerOptions.Lz4));
        Assert.Throws<TinyhandException>(() => TinyhandSerializer.Deserialize<int>(frame, UntrustedLz4));
        Assert.Throws<TinyhandException>(() => ReadCompressedByReference(frame));
        Assert.False(TinyhandSerializer.TryDeserialize<int>(frame, out var value, out var consumed, UntrustedLz4));
        Assert.Equal(0, value);
        Assert.Equal(0, consumed);
        Assert.Equal(42, TinyhandSerializer.Deserialize<int>(frame, TinyhandSerializerOptions.Lz4));
    }

    [Fact]
    public void CompressedOuterTrailingValuesAreRejectedOnlyByCompleteBufferApis()
    {
        var frame = Compress([42]);
        var data = new byte[frame.Length + 1];
        frame.CopyTo(data, 0);
        data[^1] = 43;
        Assert.Throws<TinyhandException>(() => TinyhandSerializer.DeserializeExact<int>(data, TinyhandSerializerOptions.Lz4));
        Assert.Throws<TinyhandException>(() => TinyhandSerializer.Deserialize<int>(data, UntrustedLz4));
        Assert.Equal(42, TinyhandSerializer.Deserialize<int>(data, out var consumed, UntrustedLz4));
        Assert.Equal(frame.Length, consumed);
        Assert.Equal(43, TinyhandSerializer.Deserialize<int>(data.AsSpan(consumed), UntrustedLz4));
    }

    [Theory]
    [InlineData(new byte[] { 0x91, 0xa1 })]
    [InlineData(new byte[] { 0x91, 0xc1 })]
    [InlineData(new byte[] { 0x91, 0xa1, 0xff })]
    [InlineData(new byte[] { 0x91, 42, 43 })]
    public void StructuralFailuresPrecedeObjectConstruction(byte[] data)
    {
        ExactConstructedValue.Constructions = 0;
        Assert.Throws<TinyhandException>(() => TinyhandSerializer.Deserialize<ExactConstructedValue>(data, Untrusted));
        Assert.Equal(0, ExactConstructedValue.Constructions);
        Assert.False(TinyhandSerializer.TryDeserializeExact<ExactConstructedValue>(data, out var value, Untrusted));
        Assert.Null(value);
        Assert.Equal(0, ExactConstructedValue.Constructions);

        var compressed = Compress(data);
        Assert.Throws<TinyhandException>(() => TinyhandSerializer.DeserializeExact<ExactConstructedValue>(compressed, UntrustedLz4));
        Assert.Equal(0, ExactConstructedValue.Constructions);
    }

    [Fact]
    public void DirectObjectAndParameterlessApisHonorDefaultCompleteInputPolicy()
    {
        var originalOptions = TinyhandSerializer.DefaultOptions;
        try
        {
            TinyhandSerializer.DefaultOptions = Untrusted;
            byte[] trailing = [0x91, 42, 43];
            ExactConstructedValue.Constructions = 0;
            Assert.Throws<TinyhandException>(() => TinyhandSerializer.Deserialize<ExactConstructedValue>(trailing));
            Assert.Throws<TinyhandException>(() => TinyhandSerializer.DeserializeObject<ExactConstructedValue>(trailing));
            Assert.False(TinyhandSerializer.TryDeserializeObject<ExactConstructedValue>(trailing, out var rejected));
            Assert.Null(rejected);
            Assert.Equal(0, ExactConstructedValue.Constructions);

            ExactConstructedValue? existing = new() { Value = 7 };
            var original = existing;
            Assert.Throws<TinyhandException>(() => TinyhandSerializer.DeserializeObject(trailing, ref existing));
            Assert.Same(original, existing);
            Assert.Equal(7, existing!.Value);

            byte[] valid = [0x91, 42];
            Assert.Equal(42, TinyhandSerializer.DeserializeObject<ExactConstructedValue>(valid)!.Value);
            Assert.True(TinyhandSerializer.TryDeserializeObject<ExactConstructedValue>(valid, out var accepted));
            Assert.Equal(42, accepted.Value);
        }
        finally
        {
            TinyhandSerializer.DefaultOptions = originalOptions;
        }
    }

    [Fact]
    public void StructuralPreflightDoesNotChargePayloadElementsOrStorageTwice()
    {
        string[] expected = ["a", "b"];
        var data = TinyhandSerializer.Serialize(expected);
        var limits = TinyhandSecurity.UntrustedData.Limits with
        {
            MaximumTotalElements = 2,
            MaximumTotalPayloadBytes = 2,
            MaximumTotalCollectionBytes = 2 * IntPtr.Size,
        };
        var options = Untrusted with { Security = TinyhandSecurity.UntrustedData.WithLimits(limits) };
        Assert.Equal(expected, TinyhandSerializer.DeserializeExact<string[]>(data, options));
        Assert.Equal(expected, TinyhandSerializer.Deserialize<string[]>(data, options));
        Assert.True(TinyhandSerializer.TryDeserializeExact<string[]>(data, out var actual, options));
        Assert.Equal(expected, actual);
        TinyhandTreeConverter.FromBinaryToElement(data, out var element, options);
        Assert.NotNull(element);
    }

    [Fact]
    public void CompressedPreflightDoesNotChargeEnvelopeAndInnerDataTwice()
    {
        string[] expected = ["a", "b"];
        var plain = TinyhandSerializer.Serialize(expected);
        var frame = Compress(plain, out var compressedLength);
        var limits = TinyhandSecurity.UntrustedData.Limits with
        {
            MaximumTotalElements = 4, // Two envelope items and two string array items.
            MaximumTotalPayloadBytes = 1 + compressedLength + 2,
            MaximumDecompressedBytes = plain.Length,
            MaximumTotalCollectionBytes = 2 * IntPtr.Size,
        };
        var options = UntrustedLz4 with { Security = TinyhandSecurity.UntrustedData.WithLimits(limits) };
        Assert.Equal(expected, TinyhandSerializer.DeserializeExact<string[]>(frame, options));
        Assert.Equal(expected, TinyhandSerializer.Deserialize<string[]>(frame, options));
        TinyhandTreeConverter.FromBinaryToElement(frame, out var element, options);
        Assert.NotNull(element);
    }

    [Fact]
    public void TreeBufferApiRejectsPlainAndCompressedTrailingValues()
    {
        byte[] plain = [42, 43];
        Assert.Throws<TinyhandException>(() => TinyhandTreeConverter.FromBinaryToElement(plain, out _, Untrusted));
        var frame = Compress(plain);
        Assert.Throws<TinyhandException>(() => TinyhandTreeConverter.FromBinaryToElement(frame, out _, UntrustedLz4));
        frame = Compress([42]);
        var outerTrailing = new byte[frame.Length + 1];
        frame.CopyTo(outerTrailing, 0);
        outerTrailing[^1] = 43;
        Assert.Throws<TinyhandException>(() => TinyhandTreeConverter.FromBinaryToElement(outerTrailing, out _, UntrustedLz4));
    }

    [Fact]
    public void TryExactReportsEmptyMalformedAndNullResults()
    {
        Assert.False(TinyhandSerializer.TryDeserializeExact<int>(Array.Empty<byte>(), out var empty, Untrusted));
        Assert.Equal(0, empty);
        Assert.False(TinyhandSerializer.TryDeserializeExact<int>(new byte[] { 0xc1 }, out var malformed, Untrusted));
        Assert.Equal(0, malformed);
        Assert.False(TinyhandSerializer.TryDeserializeExact<string>(new byte[] { 0xc0 }, out var nil, Untrusted));
        Assert.Null(nil);
        Assert.True(TinyhandSerializer.TryDeserializeExact<int>(new byte[] { 42 }, out var valid, Untrusted));
        Assert.Equal(42, valid);
    }

    [Fact]
    public void ExactApiRejectsAFormatterThatLeavesPartOfItsValueUnread()
    {
        var options = Untrusted with { Resolver = CompositeResolver.Create([new UnderreadingFormatter()], [Untrusted.Resolver]) };
        byte[] data = [0x91, 42];
        Assert.Throws<TinyhandException>(() => TinyhandSerializer.DeserializeExact<UnderreadValue>(data, options));
        Assert.False(TinyhandSerializer.TryDeserializeExact<UnderreadValue>(data, out var value, options));
        Assert.Null(value);
    }

    private static byte[] Compress(byte[] payload) => Compress(payload, out _);

    private static byte[] Compress(byte[] payload, out int compressedLength)
    {
        Assert.InRange(payload.Length, 1, 127);
        var buffer = new byte[LZ4Codec.MaximumOutputLength(payload.Length)];
        compressedLength = LZ4Codec.Encode(payload, buffer);
        using var writer = TinyhandWriter.CreateFromBytePool();
        writer.WriteArrayHeader(2);
        writer.WriteExtensionFormatHeader(new ExtensionHeader(MessagePackExtensionCodes.Lz4BlockArray, 1));
        writer.Write(payload.Length);
        writer.Write(buffer.AsSpan(0, compressedLength));
        return writer.FlushAndGetArray();
    }

    private static void ReadCompressedByReference(byte[] frame)
    {
        var reader = new TinyhandReader(frame);
        TinyhandSerializer.Deserialize<int>(ref reader, UntrustedLz4);
    }

    private sealed class UnderreadValue;

    private sealed class UnderreadingFormatter : ITinyhandFormatter<UnderreadValue>
    {
        public void Serialize(ref TinyhandWriter writer, UnderreadValue? value, TinyhandSerializerOptions options) => throw new NotSupportedException();

        public void Deserialize(ref TinyhandReader reader, ref UnderreadValue? value, TinyhandSerializerOptions options)
        {
            reader.ReadArrayHeader();
            value = new UnderreadValue();
        }

        public UnderreadValue Reconstruct(TinyhandSerializerOptions options) => new();

        public UnderreadValue? Clone(UnderreadValue? value, TinyhandSerializerOptions options) => value;
    }
}

[TinyhandObject]
public partial class ExactConstructedValue
{
    public static int Constructions;

    public ExactConstructedValue() => Constructions++;

    [Key(0)]
    public int Value { get; set; }
}
