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

[TinyhandObject(ReservedKeys = 10)]
public partial class ReserveKeyClass
{
    [Key(0)]
    public int X { get; set; }

    [Key(5)]
    public int Y { get; set; }
}

[TinyhandObject]
public partial class ReserveKeyDerived : ReserveKeyClass
{
    // [Key(10)]
    // public int A { get; set; }

    [Key(11)]
    public int B { get; set; }
}

public class ReserveKeyTest
{
    [Fact]
    public void Test1()
    {
    }
}
