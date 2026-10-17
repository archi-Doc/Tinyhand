// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using System.Linq;
using Tinyhand.Formatters;
using Tinyhand.IO;
using Xunit;

namespace Tinyhand.Tests;

public class UnsafeFormattersTest
{
    [Fact]
    public void GuidTest()
    {
        var guid = Guid.NewGuid();
        var sequenceWriter = new TinyhandWriter();
        NativeGuidFormatter.Instance.Serialize(ref sequenceWriter, guid, TinyhandSerializerOptions.Standard);
        var sequence = sequenceWriter.FlushAndGetReadOnlySequence();
        sequence.Length.Is(18);

        var sequenceReader = new TinyhandReader(sequence.ToArray());
        Guid nguid = NativeGuidFormatter.Instance.Deserialize(ref sequenceReader, TinyhandSerializerOptions.Standard);
        Assert.True(sequenceReader.End);

        guid.Is(nguid);
    }

    [Fact]
    public void DecimalTest()
    {
        var d = new Decimal(1341, 53156, 61, true, 3);
        var sequenceWriter = new TinyhandWriter();
        NativeDecimalFormatter.Instance.Serialize(ref sequenceWriter, d, TinyhandSerializerOptions.Standard);
        var sequence = sequenceWriter.FlushAndGetReadOnlySequence();
        sequence.Length.Is(18);

        var sequenceReader = new TinyhandReader(sequence.ToArray());
        var nd = NativeDecimalFormatter.Instance.Deserialize(ref sequenceReader, TinyhandSerializerOptions.Standard);
        Assert.True(sequenceReader.End);

        d.Is(nd);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NativeFormattersApplyLimitsBeforeChangingValues(bool untrusted)
    {
        var security = untrusted ? TinyhandSecurity.UntrustedData : TinyhandSecurity.TrustedData;
        CheckLimits(NativeGuidFormatter.Instance, Guid.NewGuid(), Guid.Empty, security);
        CheckLimits(NativeDecimalFormatter.Instance, 123m, 456m, security);
    }

    private static void CheckLimits<T>(ITinyhandFormatter<T> formatter, T source, T existing, TinyhandSecurity security)
    {
        var writer = new TinyhandWriter();
        byte[] bytes;
        try
        {
            formatter.Serialize(ref writer, source, TinyhandSerializerOptions.Standard);
            bytes = writer.FlushAndGetArray();
        }
        finally
        {
            writer.Dispose();
        }

        var limits = TinyhandDeserializationLimits.UntrustedData with
        {
            MaximumInputBytes = bytes.Length,
            MaximumBinaryBytes = 16,
            MaximumTotalPayloadBytes = 16,
        };
        foreach (var rejected in new[]
        {
            limits with { MaximumInputBytes = bytes.Length - 1 },
            limits with { MaximumBinaryBytes = 15 },
            limits with { MaximumTotalPayloadBytes = 15 },
        })
        {
            var value = existing;
            var options = TinyhandSerializerOptions.Standard with { Security = security.WithLimits(rejected) };
            Assert.Throws<TinyhandException>(() =>
            {
                var reader = new TinyhandReader(bytes);
                formatter.Deserialize(ref reader, ref value, options);
            });
            Assert.Equal(existing, value);
        }

        var accepted = TinyhandSerializerOptions.Standard with { Security = security.WithLimits(limits) };
        var boundedReader = new TinyhandReader(bytes);
        Assert.Equal(source, formatter.Deserialize(ref boundedReader, accepted));
        Assert.True(boundedReader.End);

        // A fresh sub-reader shares the consumed payload budget instead of replenishing it.
        Assert.Throws<TinyhandException>(() =>
        {
            var reader = new TinyhandReader(bytes);
            formatter.Deserialize(ref reader, accepted);
            var next = reader.CreateSubReader(bytes);
            formatter.Deserialize(ref next, accepted);
        });
    }
}
