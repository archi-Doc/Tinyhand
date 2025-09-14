// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Xunit;

namespace Tinyhand.Tests;

[TinyhandObject(ImplicitKeyAsName = true)]
public partial record TextSerializeClass1
{
    public MyClass2[] MyClass2Array { get; set; } = [];

    [Key("2int-string")]
    public Dictionary<int, string> DictionaryIntString { get; set; } = [];

    [Key(" st d ")]
    public IDictionary<string, double> IDictionaryStringDouble { get; set; } = new Dictionary<string, double>();

    public byte Byte { get; set; }

    [Key("2")]
    public int Int { get; set; } = 77;

    public MyClass? MyClass0 { get; set; }

    [Key("St{")]
    public string String { get; set; } = "test\"\"\"e";

    [Key("3 2")]
    public float Float { get; set; }

    public double Double { get; set; } = 1d;

    public DateTime Date { get; set; } = DateTime.UtcNow;
}

[TinyhandObject(ImplicitKeyAsName = true)]
public partial class TextSerializeClass2
{
    public int Int { get; set; } = 11;

    public string String { get; set; } = "Test";

    public int[] Array { get; set; }
}

[TinyhandObject(ImplicitKeyAsName = true)]
public partial class TextSerializeClass3
{
    public double Double1 { get; set; } = 1.0d;

    public double DoubleNaN { get; set; } = double.NaN;

    public double DoublePositiveInfinity { get; set; } = double.PositiveInfinity;

    public double DoubleNegativeInfinity { get; set; } = double.NegativeInfinity;
}

[TinyhandObject]
public partial class TextSerializeClass4
{
    [TinyhandObject]
    public partial class NestedClass1
    {
        [TinyhandObject]
        public partial class NestedClass2
        {
            [TinyhandObject(ImplicitKeyAsName = true)]
            public partial class NestedClass3
            {
                [KeyAsName]
                public int[] IntArray { get; set; } = [1, 2, 3,];

                public partial string[] StringArray { get; set; } = [];

                [KeyAsName]
                public Dictionary<int, int> IntMap { get; set; } = [];

                [KeyAsName]
                public string[] StringArray2 { get; set; } = [];
            }

            [Key(0)]
            public NestedClass3 Class3 { get; set; } = new();

            [Key(1)]
            public int[] Array { get; set; } = [];
        }

        [Key(0)]
        public NestedClass2[] Class2 { get; set; } = [new(), new(),];
    }

    [Key(0)]
    public string? Name { get; set; } = default;

    [Key(1)]
    public NestedClass1 Class1 { get; set; } = new();

    [Key(2)]
    public int Id { get; set; }
}

public class TextSerializeTest
{
    [Fact]
    public void Test0()
    {// Requires visual assessment: st
        var array = new TextSerializeClass4[2];
        array[0] = new();
        array[1] = new();

        string st;
        // st = TinyhandSerializer.SerializeToString(array, TinyhandSerializerOptions.Standard with { Compose = TinyhandComposeOption.Simple });
        st = TinyhandSerializer.SerializeToString(array);
    }

    [Fact]
    public void Test1()
    {// Requires visual assessment: st
        string st;
        var simple = TinyhandSerializerOptions.Standard with { Compose = TinyhandComposeOption.Simple, };

        var c1 = TinyhandSerializer.Reconstruct<TextSerializeClass1>();
        c1.DictionaryIntString = new(new KeyValuePair<int, string>[] { new KeyValuePair<int, string>(33, "rr") });
        c1.IDictionaryStringDouble = new Dictionary<string, double>(new KeyValuePair<string, double>[] { new KeyValuePair<string, double>("test", 33d) });
        var mc = new MyClass2(1, 2, ["A"]);
        var mc2 = new MyClass2(10, 20, ["AA"]);
        c1.MyClass2Array = [mc, mc2,];

        st = TinyhandSerializer.SerializeToString(c1, simple);
        st = TinyhandSerializer.SerializeToString(c1);
        var c2 = TinyhandSerializer.DeserializeFromString<TextSerializeClass1>(st);
        TinyhandSerializer.Serialize(c1).SequenceEqual(TinyhandSerializer.Serialize(c2)).IsTrue();

        st = TinyhandSerializer.SerializeToString(c1, simple);
        c2 = TinyhandSerializer.DeserializeFromString<TextSerializeClass1>(st);
        TinyhandSerializer.Serialize(c1).SequenceEqual(TinyhandSerializer.Serialize(c2)).IsTrue();
    }

    [Fact]
    public void Test2()
    {// Requires visual assessment: st
        string st;
        var simple = TinyhandSerializerOptions.Standard with { Compose = TinyhandComposeOption.Simple };

        var c1 = TinyhandSerializer.Reconstruct<SimpleIntKeyData>();
        st = TinyhandSerializer.SerializeToString(c1);
        c1.IsStructuralEqual(TinyhandSerializer.DeserializeFromString<SimpleIntKeyData>(st));
        st = TinyhandSerializer.SerializeToString(c1, simple);
        c1.IsStructuralEqual(TinyhandSerializer.DeserializeFromString<SimpleIntKeyData>(st));

        var c2 = TinyhandSerializer.Reconstruct<EmptyClass>();
        st = TinyhandSerializer.SerializeToString(c2);
        c2.IsStructuralEqual(TinyhandSerializer.DeserializeFromString<EmptyClass>(st));
        st = TinyhandSerializer.SerializeToString(c2, simple);
        c2.IsStructuralEqual(TinyhandSerializer.DeserializeFromString<EmptyClass>(st));

        var c3 = TinyhandSerializer.Reconstruct<EmptyClass2>();
        st = TinyhandSerializer.SerializeToString(c3);
        c3.IsStructuralEqual(TinyhandSerializer.DeserializeFromString<EmptyClass2>(st));
        st = TinyhandSerializer.SerializeToString(c3, simple);
        c3.IsStructuralEqual(TinyhandSerializer.DeserializeFromString<EmptyClass2>(st));

        var c4 = new FormatterResolverClass();
        st = TinyhandSerializer.SerializeToString(c4);
        var c5 = TinyhandSerializer.DeserializeFromString<FormatterResolverClass>(st);
        c5.ObjectArray = c4.ObjectArray; // avoid int != byte issue
        c5.ObjectList = c4.ObjectList; // avoid int != byte issue

        st = TinyhandSerializer.SerializeToString(c4, simple);
        // var json = MessagePack.MessagePackSerializer.ConvertToJson(MessagePack.MessagePackSerializer.Serialize<FormatterResolverClass>(c4));
        // var cc = MessagePack.MessagePackSerializer.Deserialize<FormatterResolverClass>(MessagePack.MessagePackSerializer.ConvertFromJson(json));
        c5 = TinyhandSerializer.DeserializeFromString<FormatterResolverClass>(st);
        c5.ObjectArray = c4.ObjectArray; // avoid int != byte issue
        c5.ObjectList = c4.ObjectList; // avoid int != byte issue
    }

    [Fact]
    public void Test3()
    {// Requires visual assessment: st
        var standard = TinyhandSerializerOptions.Standard;
        var strict = standard with { Compose = TinyhandComposeOption.Strict, };

        var c1 = TinyhandSerializer.Reconstruct<TextSerializeClass2>();
        var st = TinyhandSerializer.SerializeToString(c1, standard);
        var d1 = TinyhandSerializer.DeserializeFromString<TextSerializeClass2>(st);
        st = TinyhandSerializer.SerializeToString(c1, strict);
        st = TinyhandSerializer.SerializeToString(c1, standard);

        var c2 = TinyhandSerializer.Reconstruct<TextSerializeClass2>();
        c2.Int = 22;
        c2.String = "Test2";

        var array = new TextSerializeClass2[] { c1, c2, };
        // st = TinyhandSerializer.SerializeToString(array, TinyhandSerializerOptions.Standard with { Compose = TinyhandComposeOption.Simple, });
        st = TinyhandSerializer.SerializeToString(array);

        TinyhandSerializer.SerializeToString(42).Is("42");
        TinyhandSerializer.SerializeToString(3.14d).Is("3.14");
    }

    [Fact]
    public void Test4()
    {// Requires visual assessment: st
        var c1 = TinyhandSerializer.Reconstruct<TextSerializeClass3>();
        var c2 = TinyhandSerializer.Deserialize<TextSerializeClass3>(TinyhandSerializer.Serialize(c1));

        c1.IsStructuralEqual(c2);

        var st = TinyhandSerializer.SerializeToString(c1);
        var c3 = TinyhandSerializer.DeserializeFromString<TextSerializeClass3>(st);
        c1.IsStructuralEqual(c3);
    }
}
