// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Text;
using Arc.Collections;
using Tinyhand;
using Xunit;

namespace XUnitTest.Tests;

public class Utf16HashtableTest
{
    [Fact]
    public void Test1()
    {
        var table = new Utf16Hashtable<int>();

        table.Add("One", 0);
        table.Add("One", 1);
        table.TryAdd("Two", 2);
        table.GetOrAdd("Three", x => 3);

        int x;
        table.Count.Is(3);
        table.TryGetValue("One", out x);
        x.Is(1);
        table.TryGetValue("Two", out x);
        x.Is(2);
        table.TryGetValue("Three", out x);
        x.Is(3);

        var bin = TinyhandSerializer.Serialize(table);
        table = TinyhandSerializer.Deserialize<Utf16Hashtable<int>>(bin);

        table.Count.Is(3);
        table.TryGetValue("One", out x);
        x.Is(1);
        table.TryGetValue("Two", out x);
        x.Is(2);
        table.TryGetValue("Three", out x);
        x.Is(3);
    }
}
