// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Arc.Collections;
using Newtonsoft.Json.Linq;
using Tinyhand;
using Xunit;

namespace Tinyhand.Tests;

[TinyhandObject]
public partial class ArcCollectionsTestClass
{
    [Key(0)]
    public OrderedMap<int, string> Map1 { get; set; } = default!;

    [Key(1)]
    public OrderedMultiMap<int, string> Map2 { get; set; } = default!;

    [Key(2)]
    public OrderedSet<int> Set1 { get; set; } = default!;

    [Key(3)]
    public OrderedMultiSet<int> Set2 { get; set; } = default!;

    [Key(4)]
    public UnorderedMap<int, string> Map3 { get; set; } = default!;

    [Key(5)]
    public UnorderedMultiMap<int, string> Map4 { get; set; } = default!;

    [Key(6)]
    public UnorderedSet<int> Set3 { get; set; } = default!;

    [Key(7)]
    public UnorderedMultiSet<int> Set4 { get; set; } = default!;

    [Key(8)]
    public OrderedList<int> List1 { get; set; } = default!;

    [Key(9)]
    public UnorderedList<int> List2 { get; set; } = default!;

    [Key(10)]
    public UnorderedLinkedList<int> List3 { get; set; } = default!;

    [Key(11)]
    public OrderedKeyValueList<int, string> List4 { get; set; } = default!;
}

public class ArcCollectionsTest
{
    [Fact]
    public void Test1()
    {
        string st = string.Empty;

        var list = new KeyValueList<int, string>();
        list.Add(new(1, "a"));
        list.Add(new(4, "dddd"));
        list.Add(new(3, "ccc"));
        list.Add(new(2, "bb"));

        var orderedMap = new OrderedMap<int, string>();
        foreach (var x in list)
        {
            orderedMap.Add(x.Key, x.Value);
        }

        st = TinyhandSerializer.SerializeToString(orderedMap);
        TinyhandSerializer.DeserializeFromString<OrderedMap<int, string>>(st)!.SequenceEqual(orderedMap);

        var orderedSet = new OrderedSet<int>();
        foreach (var x in list)
        {
            orderedSet.Add(x.Key);
        }

        st = TinyhandSerializer.SerializeToString(orderedSet);
        TinyhandSerializer.DeserializeFromString<OrderedSet<int>>(st)!.SequenceEqual(orderedSet);

        list.Add(new(3, "ccc2"));

        var orderedMultiMap = new OrderedMultiMap<int, string>();
        foreach (var x in list)
        {
            orderedMultiMap.Add(x.Key, x.Value);
        }

        st = TinyhandSerializer.SerializeToString(orderedMultiMap);
        TinyhandSerializer.DeserializeFromString<OrderedMultiMap<int, string>>(st)!.SequenceEqual(orderedMultiMap);

        var orderedMultiSet = new OrderedMultiSet<int>();
        foreach (var x in list)
        {
            orderedMultiSet.Add(x.Key);
        }

        st = TinyhandSerializer.SerializeToString(orderedMultiSet);
        TinyhandSerializer.DeserializeFromString<OrderedMultiSet<int>>(st)!.SequenceEqual(orderedMultiSet);

        var unorderedMap = new UnorderedMap<int, string>();
        foreach (var x in list)
        {
            unorderedMap.Add(x.Key, x.Value);
        }

        st = TinyhandSerializer.SerializeToString(unorderedMap);
        TinyhandSerializer.DeserializeFromString<UnorderedMap<int, string>>(st)!.SequenceEqual(unorderedMap);

        var unorderedSet = new UnorderedSet<int>();
        foreach (var x in list)
        {
            unorderedSet.Add(x.Key);
        }

        st = TinyhandSerializer.SerializeToString(unorderedSet);
        TinyhandSerializer.DeserializeFromString<UnorderedSet<int>>(st)!.SequenceEqual(unorderedSet);

        var unorderedMultiMap = new UnorderedMultiMap<int, string>();
        foreach (var x in list)
        {
            unorderedMultiMap.Add(x.Key, x.Value);
        }

        st = TinyhandSerializer.SerializeToString(unorderedMultiMap);
        TinyhandSerializer.DeserializeFromString<UnorderedMultiMap<int, string>>(st)!.SequenceEqual(unorderedMultiMap);

        var unorderedMultiSet = new UnorderedMultiSet<int>();
        foreach (var x in list)
        {
            unorderedMultiSet.Add(x.Key);
        }

        st = TinyhandSerializer.SerializeToString(unorderedMultiSet);
        TinyhandSerializer.DeserializeFromString<UnorderedMultiSet<int>>(st)!.SequenceEqual(unorderedMultiSet);

        var orderedList = new OrderedList<int>(list.Select(x => x.Key));
        st = TinyhandSerializer.SerializeToString(orderedList);
        TinyhandSerializer.DeserializeFromString<OrderedList<int>>(st)!.SequenceEqual(orderedList);

        var unorderedList = new UnorderedList<int>(list.Select(x => x.Key));
        st = TinyhandSerializer.SerializeToString(unorderedList);
        TinyhandSerializer.DeserializeFromString<UnorderedList<int>>(st)!.SequenceEqual(unorderedList);

        var unorderedLinkedList = new UnorderedLinkedList<int>(list.Select(x => x.Key));
        st = TinyhandSerializer.SerializeToString(unorderedLinkedList);
        TinyhandSerializer.DeserializeFromString<UnorderedLinkedList<int>>(st)!.SequenceEqual(unorderedLinkedList);

        var orderedKeyValueList = new OrderedKeyValueList<int, string>(orderedMultiMap);
        st = TinyhandSerializer.SerializeToString(orderedKeyValueList);
        TinyhandSerializer.DeserializeFromString<OrderedKeyValueList<int, string>>(st)!.SequenceEqual(orderedKeyValueList);
    }

    [Fact]
    public void Test2()
    {
        var list = new KeyValueList<int, string>();
        list.Add(new(1, "a"));
        list.Add(new(4, "dddd"));
        list.Add(new(3, "ccc"));
        list.Add(new(2, "bb"));

        var tc = TinyhandSerializer.Reconstruct<ArcCollectionsTestClass>();

        foreach (var x in list)
        {
            tc.Map1.Add(x.Key, x.Value);
            tc.Map2.Add(x.Key, x.Value);
            tc.Set1.Add(x.Key);
            tc.Set2.Add(x.Key);

            tc.Map3.Add(x.Key, x.Value);
            tc.Map4.Add(x.Key, x.Value);
            tc.Set3.Add(x.Key);
            tc.Set4.Add(x.Key);

            tc.List1.Add(x.Key);
            tc.List2.Add(x.Key);
            tc.List3.AddLast(x.Key);
            tc.List4.Add(x.Key, x.Value);
        }

        var st = TinyhandSerializer.SerializeToString(tc);
        TinyhandSerializer.DeserializeFromString<ArcCollectionsTestClass>(st)!.IsStructuralEqual(tc);

        var tc2 = TinyhandSerializer.Clone(tc);
        tc2.IsStructuralEqual(tc);
    }
}
