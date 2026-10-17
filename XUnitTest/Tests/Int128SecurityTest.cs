// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using Tinyhand;
using Tinyhand.IO;
using Xunit;

namespace XUnitTest.Tests;

public class Int128SecurityTest
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void ExtensionPayloadRespectsIndividualLimit(bool unsignedEncoding, bool unsignedTarget)
    {
        var data = CreateExtension(unsignedEncoding);
        var exact = Options(16, 16);
        Assert.Equal((UInt128)42, Deserialize(data, unsignedTarget, exact));
        Assert.Equal((UInt128)42, Read(data, unsignedTarget, exact.Security));
        Assert.Throws<TinyhandException>(() => Deserialize(data, unsignedTarget, Options(15, 16)));
        Assert.Throws<TinyhandException>(() => Read(data, unsignedTarget, Options(15, 16).Security));
        Assert.Throws<TinyhandException>(() => Deserialize(data, unsignedTarget, Options(16, 15)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ExtensionsShareCumulativePayloadBudget(bool unsignedTarget)
    {
        var data = new byte[37];
        data[0] = MessagePackCode.MinFixArray + 2;
        CreateExtension(false).CopyTo(data, 1);
        CreateExtension(true).CopyTo(data, 19);
        if (unsignedTarget)
        {
            Assert.Equal(new UInt128[] { 42, 42 }, TinyhandSerializer.Deserialize<UInt128[]>(data, Options(16, 32)));
            Assert.Throws<TinyhandException>(() => TinyhandSerializer.Deserialize<UInt128[]>(data, Options(16, 31)));
        }
        else
        {
            Assert.Equal(new Int128[] { 42, 42 }, TinyhandSerializer.Deserialize<Int128[]>(data, Options(16, 32)));
            Assert.Throws<TinyhandException>(() => TinyhandSerializer.Deserialize<Int128[]>(data, Options(16, 31)));
        }
    }

    [Fact]
    public void DirectScalarAndNullableFormattersAttachPayloadLimits()
    {
        var data = CreateExtension(false);
        var options = Options(15, 16);
        Assert.Throws<TinyhandException>(() => Direct<Int128>(data, options));
        Assert.Throws<TinyhandException>(() => Direct<Int128?>(data, options));
        Assert.Throws<TinyhandException>(() => Direct<UInt128>(data, options));
        Assert.Throws<TinyhandException>(() => Direct<UInt128?>(data, options));
        Assert.Equal((Int128)42, Direct<Int128>(data, Options(16, 16)));
        Assert.Equal((UInt128)42, Direct<UInt128>(data, Options(16, 16)));
    }

    [Fact]
    public void OrdinaryIntegersDoNotConsumeExtensionPayloadBudget()
    {
        byte[] data = [0x92, 42, 43];
        Assert.Equal(new Int128[] { 42, 43 }, TinyhandSerializer.Deserialize<Int128[]>(data, Options(0, 0)));
        Assert.Equal(new UInt128[] { 42, 43 }, TinyhandSerializer.Deserialize<UInt128[]>(data, Options(0, 0)));
    }

    private static byte[] CreateExtension(bool unsigned)
    {
        var data = new byte[18];
        data[0] = MessagePackCode.FixExt16;
        data[1] = unsigned ? MessagePackExtensionCodes.UInt128 : MessagePackExtensionCodes.Int128;
        data[^1] = 42;
        return data;
    }

    private static TinyhandSerializerOptions Options(int maximumBinaryBytes, long maximumTotalPayloadBytes)
        => TinyhandSerializerOptions.Standard with
        {
            Security = TinyhandSecurity.TrustedData.WithLimits(TinyhandDeserializationLimits.Unrestricted with
            {
                MaximumBinaryBytes = maximumBinaryBytes,
                MaximumTotalPayloadBytes = maximumTotalPayloadBytes,
            }),
        };

    private static UInt128 Deserialize(byte[] data, bool unsigned, TinyhandSerializerOptions options)
        => unsigned ? TinyhandSerializer.Deserialize<UInt128>(data, options) : (UInt128)TinyhandSerializer.Deserialize<Int128>(data, options);

    private static UInt128 Read(byte[] data, bool unsigned, TinyhandSecurity security)
    {
        var reader = new TinyhandReader(data, security);
        return unsigned ? reader.ReadUInt128() : (UInt128)reader.ReadInt128();
    }

    private static T? Direct<T>(byte[] data, TinyhandSerializerOptions options)
    {
        var reader = new TinyhandReader(data);
        T? value = default;
        options.Resolver.GetFormatter<T>().Deserialize(ref reader, ref value, options);
        return value;
    }
}
