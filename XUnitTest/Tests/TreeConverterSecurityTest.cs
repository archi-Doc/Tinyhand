// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using Arc.IO;
using Tinyhand.IO;
using Tinyhand.Tree;
using Xunit;

namespace Tinyhand.Tests;

public class TreeConverterSecurityTest
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void BinaryConvertersEnforceInputLimit(bool compressed)
    {
        var binary = TinyhandSerializer.Serialize(new byte[64], compressed ? TinyhandSerializerOptions.Lz4 : TinyhandSerializerOptions.Standard);
        var options = WithLimits(TinyhandDeserializationLimits.UntrustedData with { MaximumInputBytes = binary.Length - 1 });
        Assert.Throws<TinyhandException>(() => TinyhandTreeConverter.FromBinaryToElement(binary, out _, options));
        Assert.Throws<TinyhandException>(() => ConvertToText(binary, options));

        options = WithLimits(TinyhandDeserializationLimits.UntrustedData with { MaximumInputBytes = binary.Length });
        TinyhandTreeConverter.FromBinaryToElement(binary, out var element, options);
        Assert.IsType<BinaryValue>(element);
        Assert.Equal(ConvertToText(binary, TinyhandSerializerOptions.Standard), ConvertToText(binary, options));
    }

    [Fact]
    public void BinaryConvertersEnforceLz4OutputLimitBeforeDecoding()
    {
        var value = new byte[1024];
        var plain = TinyhandSerializer.Serialize(value);
        var compressed = TinyhandSerializer.Serialize(value, TinyhandSerializerOptions.Lz4);
        var options = WithLimits(TinyhandDeserializationLimits.UntrustedData with { MaximumDecompressedBytes = plain.Length - 1 });
        Assert.Throws<TinyhandException>(() => TinyhandTreeConverter.FromBinaryToElement(compressed, out _, options));
        Assert.Throws<TinyhandException>(() => ConvertToText(compressed, options));

        options = WithLimits(TinyhandDeserializationLimits.UntrustedData with { MaximumDecompressedBytes = plain.Length });
        TinyhandTreeConverter.FromBinaryToElement(compressed, out var element, options);
        TinyhandTreeConverter.FromElementToBinary(element, out var roundTrip, TinyhandSerializerOptions.Standard);
        Assert.Equal(plain, roundTrip);
        Assert.Equal(ConvertToText(plain, TinyhandSerializerOptions.Standard), ConvertToText(compressed, options));
    }

    [Fact]
    public void ReaderToElementAttachesInputAndPayloadLimits()
    {
        var binary = TinyhandSerializer.Serialize("abcd");
        var inputOptions = WithLimits(TinyhandDeserializationLimits.UntrustedData with { MaximumInputBytes = binary.Length - 1 });
        Assert.Throws<TinyhandException>(() => ConvertReaderToElement(binary, inputOptions));
        var stringOptions = WithLimits(TinyhandDeserializationLimits.UntrustedData with { MaximumStringBytes = 3 });
        Assert.Throws<TinyhandException>(() => ConvertReaderToElement(binary, stringOptions));
        var exactOptions = WithLimits(TinyhandDeserializationLimits.UntrustedData with { MaximumStringBytes = 4 });
        Assert.IsType<StringValue>(ConvertReaderToElement(binary, exactOptions));
    }

    [Fact]
    public void ElementConvertersEnforceCumulativeElementBudget()
    {
        // Two arrays each declare two elements, so the cumulative total is four.
        byte[] binary = [0x92, 0x92, 1, 2, 3];
        var options = WithLimits(TinyhandDeserializationLimits.UntrustedData with { MaximumTotalElements = 3 });
        Assert.Throws<TinyhandException>(() => TinyhandTreeConverter.FromBinaryToElement(binary, out _, options));
        Assert.Throws<TinyhandException>(() => ConvertReaderToElement(binary, options));
        options = WithLimits(TinyhandDeserializationLimits.UntrustedData with { MaximumTotalElements = 4 });
        Assert.IsType<Group>(ConvertReaderToElement(binary, options));
    }

    private static TinyhandSerializerOptions WithLimits(TinyhandDeserializationLimits limits)
        => TinyhandSerializerOptions.Standard with { Security = TinyhandSecurity.UntrustedData.WithLimits(limits) };

    private static Element ConvertReaderToElement(byte[] binary, TinyhandSerializerOptions options)
    {
        var reader = new TinyhandReader(binary);
        TinyhandTreeConverter.FromReaderToElement(ref reader, out var element, options);
        return element;
    }

    private static byte[] ConvertToText(byte[] binary, TinyhandSerializerOptions options)
    {
        var writer = TinyhandRawWriter.CreateFromBytePool();
        try
        {
            TinyhandTreeConverter.FromBinaryToUtf8(binary, ref writer, options);
            return writer.FlushAndGetArray();
        }
        finally
        {
            writer.Dispose();
        }
    }
}
