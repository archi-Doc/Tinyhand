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
public partial class KeyMarkerIntClass
{
    [Key(0)]
    public int X { get; set; }

    [Key(1, Marker = true)]
    public int Y { get; set; }

    [Key(2)]
    public int Z { get; set; }
}

[TinyhandObject]
public partial class KeyMarkerStringClass
{
    [Key("X")]
    public int X { get; set; }

    // [Key("Y", Marker = true)]
    [Key("Y")]
    public int Y { get; set; }
}

public class KeyMarkerTest
{
    [Fact]
    public void Test1()
    {
        var tc = new KeyMarkerIntClass();
        tc.X = 1;
        tc.Y = 2;
        tc.Z = 3;

        var b = TinyhandSerializer.Serialize(tc);
        var c = TinyhandSerializer.SerializeAndGetMarker(tc);
    }
}
