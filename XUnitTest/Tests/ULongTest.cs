// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Tinyhand;
using Xunit;

namespace Tinyhand.Tests;

public class ULongTest
{
    [Fact]
    public void Test1()
    {
        var st = TinyhandSerializer.SerializeToString(1);
        var i = TinyhandSerializer.DeserializeFromString<int>(st);
        i.Is(1);

        st = TinyhandSerializer.SerializeToString(-1);
        i = TinyhandSerializer.DeserializeFromString<int>(st);
        i.Is(-1);

        st = TinyhandSerializer.SerializeToString<long>(long.MinValue);
        var l = TinyhandSerializer.DeserializeFromString<long>(st);
        l.Is(long.MinValue);

        st = TinyhandSerializer.SerializeToString<long>(long.MaxValue);
        l = TinyhandSerializer.DeserializeFromString<long>(st);
        l.Is(long.MaxValue);

        st = TinyhandSerializer.SerializeToString<ulong>(14373468592798695424);
        var ul = TinyhandSerializer.DeserializeFromString<ulong>(st);
        ul.Is(14373468592798695424);
    }
}
