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

        dt = DateTime.Now;
        dt.Equals(TinyhandSerializer.Deserialize<DateTime>(TinyhandSerializer.Serialize(dt))).IsTrue();
        dt.ToUniversalTime().Equals(TinyhandSerializer.DeserializeFromString<DateTime>(TinyhandSerializer.SerializeToString(dt))).IsTrue();

        dt = DateTime.MinValue;
        dt.Equals(TinyhandSerializer.Deserialize<DateTime>(TinyhandSerializer.Serialize(dt))).IsTrue();
        dt.Equals(TinyhandSerializer.DeserializeFromString<DateTime>(TinyhandSerializer.SerializeToString(dt))).IsTrue();

        dt = DateTime.MaxValue;
        dt.Equals(TinyhandSerializer.Deserialize<DateTime>(TinyhandSerializer.Serialize(dt))).IsTrue();
        dt.ToUniversalTime().Equals(TinyhandSerializer.DeserializeFromString<DateTime>(TinyhandSerializer.SerializeToString(dt))).IsTrue();
    }

    [Fact]
    public void StringWithoutOffsetIsUtc()
    {
        // The result must not depend on the local time zone of the machine.
        var dt = TinyhandSerializer.DeserializeFromString<DateTime>("\"2024-01-02T03:04:05\"");
        dt.Kind.Is(DateTimeKind.Utc);
        dt.Is(new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc));

        dt = TinyhandSerializer.DeserializeFromString<DateTime>("\"2024-01-02T03:04:05+09:00\"");
        dt.Kind.Is(DateTimeKind.Utc);
        dt.Is(new DateTime(2024, 1, 1, 18, 4, 5, DateTimeKind.Utc));
    }

    [Fact]
    public void ElementTextIsUtc()
    {
        // The element path writes UTC like the UTF-8 path, so the text reads back as the same instant.
        var local = new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Local);
        TinyhandTreeConverter.FromBinaryToElement(TinyhandSerializer.Serialize(local), out var element, TinyhandSerializerOptions.Standard);
        var text = ((Tinyhand.Tree.StringValue)element).Utf16;
        text.EndsWith("Z").IsTrue();
        TinyhandSerializer.DeserializeFromElement<DateTime>(element).Is(local.ToUniversalTime());
    }
}
