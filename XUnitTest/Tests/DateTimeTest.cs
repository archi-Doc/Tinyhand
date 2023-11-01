// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Tinyhand;
using Xunit;

namespace XUnitTest.Tests;

[TinyhandObject]
public partial class DateTimeTestClass
{
    [Key(0)]
    public Int128 A;

    [Key(1)]
    public Int128[] B = Array.Empty<Int128>();

    [Key(2)]
    public List<Int128> C = new();
}

public class DateTimeTest
{
    [Fact]
    public void Test1()
    {
        var dt = DateTime.UtcNow;
        dt.Equals(TinyhandSerializer.Deserialize<DateTime>(TinyhandSerializer.Serialize(dt))).IsTrue();
        dt.Equals(TinyhandSerializer.DeserializeFromString<DateTime>(TinyhandSerializer.SerializeToString(dt))).IsTrue();

        dt = DateTime.MinValue;
        dt.Equals(TinyhandSerializer.Deserialize<DateTime>(TinyhandSerializer.Serialize(dt))).IsTrue();
        dt.Equals(TinyhandSerializer.DeserializeFromString<DateTime>(TinyhandSerializer.SerializeToString(dt))).IsTrue();

        dt = DateTime.MaxValue;
        dt.Equals(TinyhandSerializer.Deserialize<DateTime>(TinyhandSerializer.Serialize(dt))).IsTrue();
        dt.Equals(TinyhandSerializer.DeserializeFromString<DateTime>(TinyhandSerializer.SerializeToString(dt))).IsTrue();
    }
}
