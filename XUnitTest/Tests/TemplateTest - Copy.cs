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

        var bin = TinyhandSerializer.Serialize(table);
    }
}
