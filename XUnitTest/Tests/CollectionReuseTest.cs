// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using Arc.Collections;
using Tinyhand;
using Xunit;

namespace XUnitTest.Tests;

/// <summary>
/// Every member has an initializer with entries that are not in the serialized data.
/// The members are reused on deserialization, and the initializer's entries must not survive.
/// </summary>
[TinyhandObject]
public partial class CollectionReuseClass
{
    [Key(0)]
    public Dictionary<int, int> Dictionary { get; set; } = new() { [1] = 1, [3] = 3 };

    [Key(1)]
    public SortedDictionary<int, int> SortedDictionary { get; set; } = new() { [1] = 1, [3] = 3 };

    [Key(2)]
    public SortedList<int, int> SortedList { get; set; } = new() { [1] = 1, [3] = 3 };

    [Key(3)]
    public ConcurrentDictionary<int, int> ConcurrentDictionary { get; set; } = new(new Dictionary<int, int> { [1] = 1, [3] = 3 });

    [Key(4)]
    public IDictionary<int, int> InterfaceDictionary { get; set; } = new Dictionary<int, int> { [1] = 1, [3] = 3 };

    [Key(5)]
    public ReadOnlyDictionary<int, int> ReadOnlyDictionary { get; set; } = new(new Dictionary<int, int> { [1] = 1, [3] = 3 });

    [Key(6)]
    public ImmutableDictionary<int, int> ImmutableDictionary { get; set; } = ImmutableDictionary<int, int>.Empty.Add(1, 1).Add(3, 3);

    [Key(7)]
    public OrderedList<int> OrderedList { get; set; } = new() { 100 };

    [Key(8)]
    public UnorderedList<int> UnorderedList { get; set; } = new() { 100 };

    [Key(9)]
    public KeyValueList<int, int> KeyValueList { get; set; } = new() { new(9, 9) };

    [Key(10)]
    public OrderedKeyValueList<int, int> OrderedKeyValueList { get; set; } = CreateOrderedKeyValueList(9, 9);

    [Key(11)]
    public Utf16Hashtable<int> Utf16Hashtable { get; set; } = CreateUtf16Hashtable("x", 1);

    [Key(12)]
    public List<string> List { get; set; } = new() { "init" };

    public static OrderedKeyValueList<int, int> CreateOrderedKeyValueList(int key, int value)
    {
        var list = new OrderedKeyValueList<int, int>();
        list.Add(key, value);
        return list;
    }

    public static Utf16Hashtable<int> CreateUtf16Hashtable(string key, int value)
    {
        var table = new Utf16Hashtable<int>();
        table.TryAdd(key, value);
        return table;
    }
}

public class CollectionReuseTest
{
    [Fact]
    public void ReusedMembersAreReplaced()
    {
        var source = new CollectionReuseClass
        {
            Dictionary = new() { [1] = 5, [2] = 6 },
            SortedDictionary = new() { [1] = 5, [2] = 6 },
            SortedList = new() { [1] = 5, [2] = 6 },
            ConcurrentDictionary = new(new Dictionary<int, int> { [1] = 5, [2] = 6 }),
            InterfaceDictionary = new Dictionary<int, int> { [1] = 5, [2] = 6 },
            ReadOnlyDictionary = new(new Dictionary<int, int> { [1] = 5, [2] = 6 }),
            ImmutableDictionary = ImmutableDictionary<int, int>.Empty.Add(1, 5).Add(2, 6),
            OrderedList = new() { 7 },
            UnorderedList = new() { 7 },
            KeyValueList = new() { new(1, 5) },
            OrderedKeyValueList = CollectionReuseClass.CreateOrderedKeyValueList(1, 5),
            Utf16Hashtable = CollectionReuseClass.CreateUtf16Hashtable("y", 2),
            List = new() { "a" },
        };

        var bytes = TinyhandSerializer.Serialize(source);
        var expected = new[] { (1, 5), (2, 6) };

        // A fresh result and deserialization into an existing instance both replace the reused members.
        var fresh = TinyhandSerializer.Deserialize<CollectionReuseClass>(bytes)!;
        var existing = new CollectionReuseClass();
        TinyhandSerializer.DeserializeObject(bytes, ref existing);
        foreach (var x in new[] { fresh, existing! })
        {
            Pairs(x.Dictionary).Is(expected);
            Pairs(x.SortedDictionary).Is(expected);
            Pairs(x.SortedList).Is(expected);
            Pairs(x.ConcurrentDictionary).Is(expected);
            Pairs(x.InterfaceDictionary).Is(expected);
            Pairs(x.ReadOnlyDictionary).Is(expected);
            Pairs(x.ImmutableDictionary).Is(expected);
            x.OrderedList.ToArray().Is(7);
            x.UnorderedList.ToArray().Is(7);
            x.KeyValueList.Select(y => (y.Key, y.Value)).ToArray().Is((1, 5));
            x.OrderedKeyValueList.Select(y => (y.Key, y.Value)).ToArray().Is((1, 5));
            x.Utf16Hashtable.ToKeyValuePairs().Select(y => (y.Key, y.Value)).ToArray().Is(("y", 2));
            x.List.ToArray().Is("a");
        }
    }

    [Fact]
    public void ReusedDictionaryKeepsItsInstance()
    {
        var existing = new CollectionReuseClass();
        var dictionary = existing.Dictionary;
        TinyhandSerializer.DeserializeObject(TinyhandSerializer.Serialize(new CollectionReuseClass { Dictionary = new() { [2] = 2 } }), ref existing);
        existing!.Dictionary.IsSameReferenceAs(dictionary);
        Pairs(existing.Dictionary).Is((2, 2));
    }

    private static (int Key, int Value)[] Pairs(IEnumerable<KeyValuePair<int, int>> pairs)
        => pairs.OrderBy(x => x.Key).Select(x => (x.Key, x.Value)).ToArray();
}
