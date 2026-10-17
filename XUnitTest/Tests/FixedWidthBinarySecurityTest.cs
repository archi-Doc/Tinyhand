// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Net;
using Arc;
using Tinyhand;
using Tinyhand.Formatters;
using Tinyhand.IO;
using Xunit;

namespace XUnitTest.Tests;

public class FixedWidthBinarySecurityTest
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FixedWidthCryptoStructsRejectShortAndLongPayloadsBeforeAssignment(bool untrusted)
    {
        var options = Options(untrusted);
        CheckInvalidLengths(Struct128Formatter.Instance, Struct128.One, Struct128.Length, options);
        CheckInvalidLengths(Struct256Formatter.Instance, Struct256.One, Struct256.Length, options);
        Assert.Equal(Struct128.One, TinyhandSerializer.Deserialize<Struct128>(TinyhandSerializer.Serialize(Struct128.One), options));
        Assert.Equal(Struct256.One, TinyhandSerializer.Deserialize<Struct256>(TinyhandSerializer.Serialize(Struct256.One), options));
        Assert.Equal(default, TinyhandSerializer.Deserialize<Struct128>(new byte[] { MessagePackCode.Nil }, options));
        Assert.Equal(default, TinyhandSerializer.Deserialize<Struct256>(new byte[] { MessagePackCode.Nil }, options));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EndPointsRejectInvalidNonNilLengths(bool untrusted)
    {
        var options = Options(untrusted);
        foreach (var length in new[] { 0, 1, 3, 4, 7, 9, 19, 21 })
        {
            var original = new IPEndPoint(IPAddress.Loopback, 1234);
            IPEndPoint? result = original;
            var bytes = TinyhandSerializer.Serialize(new byte[length]);
            Assert.Throws<TinyhandException>(() =>
            {
                var reader = new TinyhandReader(bytes);
                IPEndPointFormatter.Instance.Deserialize(ref reader, ref result, options);
            });
            Assert.Same(original, result);
        }

        foreach (var address in new[] { IPAddress.Loopback, IPAddress.IPv6Loopback })
        {
            var endpoint = new IPEndPoint(address, 1234);
            Assert.Equal(endpoint, TinyhandSerializer.Deserialize<IPEndPoint>(TinyhandSerializer.Serialize(endpoint), options));
        }
    }

    private static void CheckInvalidLengths<T>(ITinyhandFormatter<T> formatter, T original, int length, TinyhandSerializerOptions options)
    {
        foreach (var size in new[] { 0, length - 1, length + 1 })
        {
            var bytes = TinyhandSerializer.Serialize(new byte[size]);
            T? result = original;
            Assert.Throws<TinyhandException>(() =>
            {
                var reader = new TinyhandReader(bytes);
                formatter.Deserialize(ref reader, ref result, options);
            });
            Assert.Equal(original, result);
        }
    }

    private static TinyhandSerializerOptions Options(bool untrusted)
        => TinyhandSerializerOptions.Standard with { Security = untrusted ? TinyhandSecurity.UntrustedData : TinyhandSecurity.TrustedData };
}
