// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Linq;
using Xunit;

namespace Tinyhand.Tests;

[TinyhandObject]
public partial class SelectionTestClass
{
    public SelectionTestClass()
    {
    }

    public SelectionTestClass(int x, int y, int z)
    {
        this.X = x;
        this.Y = y;
        this.Z = z;
    }

    [Key(0)]
    public int X { get; set; }

    [Key(1, Selection = false)]
    public int Y { get; set; }

    [Key(2, Level = 0, Selection = false)]
    public int Z { get; set; }
}

public class SelectionTest
{
    [Fact]
    public void Test1()
    {
        SelectionTestClass tc;
        SelectionTestClass tc2;
        var selection = TinyhandSerializerOptions.Selection;

        tc = new SelectionTestClass(1, 2, 3);
        tc2 = TinyhandSerializer.Deserialize<SelectionTestClass>(TinyhandSerializer.Serialize(tc));
        tc2.IsStructuralEqual(tc);

        var bin = TinyhandSerializer.Serialize(tc, selection);
        tc2 = TinyhandSerializer.Deserialize<SelectionTestClass>(bin, selection);
        tc.X.Is(1);
        tc.Y.Is(0);
        tc.Z.Is(0);

        tc2 = TinyhandSerializer.Reconstruct<SelectionTestClass>();
        TinyhandSerializer.DeserializeObject(bin, ref tc2);
        tc.X.Is(1);
        tc.Y.Is(0);
        tc.Z.Is(0);
    }
}
