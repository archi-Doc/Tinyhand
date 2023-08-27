// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Linq;
using Tinyhand;
using Xunit;

namespace Tinyhand.Tests;

/*[TinyhandObject]
public partial class ConditionIntClass
{
    [Key(0)]
    public int X { get; set; }

    [Key(1, Condition = false)]
    public int Y { get; set; }

    [Key(2)]
    public string A { get; set; } = string.Empty;

    [Key(3, Condition = false)]
    public string B { get; set; } = string.Empty;

    [Key(4)]
    public int Z { get; set; }
}

[TinyhandObject]
public partial class ConditionIntClass2
{
    [Key(0)]
    public int X { get; set; }

    [Key(2)]
    public string A { get; set; } = string.Empty;

    [Key(4)]
    public int Z { get; set; }
}

[TinyhandObject]
public partial class ConditionStringClass
{
    [Key("X")]
    public int X { get; set; }

    [Key("Y", Condition = false)]
    public int Y { get; set; }

    [Key("A")]
    public string A { get; set; } = string.Empty;

    [Key("B", Condition = false)]
    public string B { get; set; } = string.Empty;

    [Key("Z")]
    public int Z { get; set; }
}

[TinyhandObject]
public partial class ConditionStringClass2
{
    [Key("X")]
    public int X { get; set; }

    [Key("A")]
    public string A { get; set; } = string.Empty;

    [Key("Z")]
    public int Z { get; set; }
}

public class ConditionTest
{
    [Fact]
    public void Test1()
    {
        var tc = new ConditionIntClass();
        tc.X = 1;
        tc.Y = 2;
        tc.A = "A";
        tc.B = "B";
        tc.Z = 3;

        var tc2 = new ConditionIntClass2();
        tc2.X = 1;
        tc2.A = "A";
        tc2.Z = 3;

        var b = TinyhandSerializer.Serialize(tc, TinyhandSerializerOptions.Standard); // tempcode
        var b2 = TinyhandSerializer.Serialize(tc2, TinyhandSerializerOptions.Standard); // tempcode
        b.SequenceEqual(b2).IsTrue();

        var d = TinyhandSerializer.Deserialize<ConditionIntClass>(b);
        var d2 = TinyhandSerializer.Deserialize<ConditionIntClass>(b2);
        var e = TinyhandSerializer.Deserialize<ConditionIntClass2>(b);
        var e2 = TinyhandSerializer.Deserialize<ConditionIntClass2>(b2);

        b = TinyhandSerializer.Serialize(tc, TinyhandSerializerOptions.Standard);
        b2 = TinyhandSerializer.Serialize(tc2, TinyhandSerializerOptions.Standard);
        b.SequenceEqual(b2).IsFalse();
    }

    [Fact]
    public void Test2()
    {
        var tc = new ConditionStringClass();
        tc.X = 1;
        tc.Y = 2;
        tc.A = "A";
        tc.B = "B";
        tc.Z = 3;

        var tc2 = new ConditionStringClass();
        tc2.X = 1;
        tc2.A = "A";
        tc2.Z = 3;

        var b = TinyhandSerializer.Serialize(tc, TinyhandSerializerOptions.Standard); // tempcode
        var b2 = TinyhandSerializer.Serialize(tc2, TinyhandSerializerOptions.Standard); // tempcode
        b.SequenceEqual(b2).IsTrue();

        var d = TinyhandSerializer.Deserialize<ConditionStringClass>(b);
        var d2 = TinyhandSerializer.Deserialize<ConditionStringClass>(b2);
        var e = TinyhandSerializer.Deserialize<ConditionStringClass2>(b);
        var e2 = TinyhandSerializer.Deserialize<ConditionStringClass2>(b2);

        b = TinyhandSerializer.Serialize(tc, TinyhandSerializerOptions.Standard);
        b2 = TinyhandSerializer.Serialize(tc2, TinyhandSerializerOptions.Standard);
        b.SequenceEqual(b2).IsFalse();
    }
}*/
