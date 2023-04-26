// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Tinyhand;
using Xunit;

namespace Tinyhand.Tests;

public class ThreadsafeTypeKeyHashTableTest
{
    [Fact]
    public void Test1()
    {
        var table = new ThreadsafeTypeKeyHashTable<int>();
        table.TryAdd(typeof(int), 1);
        table.TryAdd(typeof(string), 2);

        table.TryGetValue(typeof(int), out var value).IsTrue();
        value.Is(1);
        table.TryGetValue(typeof(string), out value).IsTrue();
        value.Is(2);

        var keys = table.Keys;
        keys.Length.Is(2);
        keys.Contains(typeof(int)).IsTrue();
        keys.Contains(typeof(string)).IsTrue();

        var values = table.Values;
        values.Length.Is(2);
        values.Contains(1).IsTrue();
        values.Contains(2).IsTrue();

        var kv = table.ToArray();
        kv.Length.Is(2);
        kv.Contains(new(typeof(int), 1)).IsTrue();
        kv.Contains(new(typeof(string), 2)).IsTrue();
    }
}
