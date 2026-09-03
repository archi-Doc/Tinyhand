// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Linq;
using Tinyhand.IO;
using Xunit;

namespace Tinyhand.Tests;

public class NonGenericCollectionTest
{
    [Fact]
    public void ListInterface()
    {
        var xs = new ArrayList { 1, 100, "hoge", 999.888 };
        var bin = TinyhandSerializer.Serialize<IList>(xs);
        var v = TinyhandSerializer.Deserialize<IList>(bin);

        Assert.NotNull(v);
        Assert.Equal(1, Convert.ToInt32(v[0]));
        Assert.Equal(100, Convert.ToInt32(v[1]));
        Assert.Equal("hoge", v[2]);
        Assert.Equal(999.888, v[3]);
    }

    [Fact]
    public void DictionaryInterface()
    {
        var xs = new Hashtable { { "a", 1 }, { 100, "hoge" }, { "foo", 999.888 } };
        var bin = TinyhandSerializer.Serialize<IDictionary>(xs);
        var v = TinyhandSerializer.Deserialize<IDictionary>(bin);

        Assert.NotNull(v);
        Assert.Equal(1, Convert.ToInt32(v["a"]));
        Assert.Equal("hoge", v[100]);
        Assert.Equal(999.888, v["foo"]);
    }

    [Fact]
    public void InterfaceCollectionClonesPreserveAllElements()
    {
        var source = new object?[] { 1, "value", null };
        foreach (var clone in new IEnumerable?[]
        {
            TinyhandSerializer.Clone<ICollection>(source),
            TinyhandSerializer.Clone<IEnumerable>(source),
            TinyhandSerializer.Clone<IList>(source),
        })
        {
            Assert.NotNull(clone);
            Assert.NotSame(source, clone);
            Assert.Equal(source, clone.Cast<object?>());
        }
    }

    [Fact]
    public void EmptyInterfaceArraysReplaceExistingContents()
    {
        var source = new object[] { 1, 2, 3 };
        AssertEmptyArrayReplacesExisting<ICollection>(source);
        AssertEmptyArrayReplacesExisting<IEnumerable>(source);
        AssertEmptyArrayReplacesExisting<IList>(source);
    }

    private static void AssertEmptyArrayReplacesExisting<T>(T value)
        where T : class, IEnumerable
    {
        var options = TinyhandSerializerOptions.Standard;
        var reader = new TinyhandReader(TinyhandSerializer.Serialize(Array.Empty<object>(), options));
        options.Resolver.GetFormatter<T>().Deserialize(ref reader, ref value, options);
        Assert.NotNull(value);
        Assert.Empty(value.Cast<object>());
    }
}
