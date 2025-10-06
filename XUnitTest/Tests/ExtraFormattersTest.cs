// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Net;
using Arc.Collections;
using Xunit;

namespace Tinyhand.Tests;

[TinyhandObject(ImplicitMemberNameAsKey = true)]
public partial class ExtraFormatterClass
{
    public ExtraFormatterClass()
    {
        this.IPv4 = IPAddress.Parse("192.168.0.1");
        this.IPv6 = IPAddress.Parse("2001:0db8:1234:5678:90ab:cdef:0000:0000");
        this.IPArray = new IPAddress?[] { this.IPv4, this.IPv6, IPNull, };
        this.EndPoint = new(IPAddress.Parse("192.168.0.1"), 1234);
        // this.RentMemory = new BytePool.RentMemory([1, 2, 3, 4,]);
    }

    public IPAddress IPv4 { get; set; }

    public IPAddress IPv6 { get; set; }

    public IPAddress? IPNull { get; set; } = default!;

    public IPAddress?[] IPArray { get; set; }

    public IPEndPoint EndPoint { get; set; }

    // public BytePool.RentMemory RentMemory { get; set; } = default!;

    // public BytePool.RentReadOnlyMemory RentReadOnlyMemory { get; set; } = default!;
}

public class ExtraFormattersTest
{
    [Fact]
    public void IPAddressTest()
    {
        var address = IPAddress.IPv6Loopback;
        var b = TinyhandSerializer.Serialize(address);
        var address2 = TinyhandSerializer.Deserialize<IPAddress>(b);
        address.Is(address2);
    }

    [Fact]
    public void IPEndPointTest()
    {
        var endPoint = new IPEndPoint(IPAddress.Loopback, 1234);
        var b = TinyhandSerializer.Serialize(endPoint);
        var endPoint2 = TinyhandSerializer.Deserialize<IPEndPoint>(b);
        endPoint.Is(endPoint2);

        endPoint = new IPEndPoint(IPAddress.IPv6Loopback, 1234);
        b = TinyhandSerializer.Serialize(endPoint);
        endPoint2 = TinyhandSerializer.Deserialize<IPEndPoint>(b);
        endPoint.Is(endPoint2);
    }

    [Fact]
    public void ClassTest()
    {
        var c = new ExtraFormatterClass();
        var b = TinyhandSerializer.Serialize(c);
        var c2 = TinyhandSerializer.Deserialize<ExtraFormatterClass>(b);

        // c.IsStructuralEqual(c2);
        c.IPv4.Is(c2.IPv4);
        c.IPv6.Is(c2.IPv6);
        c.IPNull.Is(c2.IPNull);
        for (var n = 0; n < c.IPArray.Length; n++)
        {
            c.IPArray[n].Is(c2.IPArray[n]);
        }

        c.EndPoint.Is(c2.EndPoint);
        // c.RentMemory.Span.SequenceEqual(c2.RentMemory.Span).IsTrue();
        // c.RentReadOnlyMemory.Span.SequenceEqual(c2.RentReadOnlyMemory.Span).IsTrue();
    }
}
