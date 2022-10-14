// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Tinyhand;
using Xunit;

namespace Tinyhand.Tests;

[TinyhandObject]
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

        var b = TinyhandSerializer.Serialize(tc);
    }
}
