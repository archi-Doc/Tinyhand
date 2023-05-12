// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace Tinyhand.Tests;

[TinyhandObject]
public partial class OuterClass
{
    [Key(0)]
    public List<int> IntList;

    [Key(1)]
    public List<InnerClass> MyList;
}

[TinyhandObject]
public partial class InnerClass
{
    [Key(2)]
    public int i;
}

public partial class NestedStructClass<T, U>
where T : struct
where U : class
{
    [TinyhandObject]
    public sealed partial class Item
    {
        public Item(int key, T value)
        {
            this.Key = key;
            this.Value = value;
        }

        public Item()
        {
        }

        [Key(0)]
        internal T Value;

        [Key(1)]
        internal int Key;
    }

    public NestedStructClass()
    {
    }

    public List<Item> Items { get; } = new();
}

public class NestedTest
{
    [Fact]
    public void Test1()
    {
        var test = new OuterClass();
        test.IntList = new List<int>() { 1 };
        test.MyList = new List<InnerClass>() { new InnerClass { i = 25 } };
        var bytes = Tinyhand.TinyhandSerializer.Serialize<OuterClass>(test);
        var test2 = Tinyhand.TinyhandSerializer.Deserialize<OuterClass>(bytes);
        test2.IsStructuralEqual(test);
    }

    [Fact]
    public void Test2()
    {
        var test = new NestedStructClass<double, object>();
        test.Items.Add(new(1, 1.1));
        test.Items.Add(new(2, 2.2));

        var i = new NestedStructClass<double, object>.Item(3, 3.33);
        var bytes = Tinyhand.TinyhandSerializer.Serialize(i);
        var i2 = Tinyhand.TinyhandSerializer.Deserialize<NestedStructClass<double, object>.Item>(bytes);
        i.IsStructuralEqual(i2);

        bytes = Tinyhand.TinyhandSerializer.Serialize<List<NestedStructClass<double, object>.Item>>(test.Items);
        var items = Tinyhand.TinyhandSerializer.Deserialize<List<NestedStructClass<double, object>.Item>>(bytes);
        items.IsStructuralEqual(test.Items);
    }
}
