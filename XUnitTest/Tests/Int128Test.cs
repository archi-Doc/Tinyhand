// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Tinyhand;
using Xunit;

namespace XUnitTest.Tests;

[TinyhandObject]
public partial class Int128Class
{
    [Key(0)]
    public Int128 A;

    [Key(1)]
    public Int128[] B = [];

    [Key(2)]
    public List<Int128> C = [];
}

[TinyhandObject]
public partial class UInt128Class
{
    [Key(0)]
    public UInt128 A;

    [Key(1)]
    public UInt128[] B = [];

    [Key(2)]
    public List<UInt128> C = [];
}

public class Int128Test
{
    [Fact]
    public void TestInt128()
    {
        var tc = new Int128Class();
        tc.A = new(123, 456);
        tc.B = [0, 1, -10, 12, new(11, 22)];
        tc.C = [-2, 3, 44, 55, 666];
        tc.C.Add(123_000_000_000_000_000_000_000_000d.ToInt128());
        tc.C.Add(-123_000_000_000_000_000_000_000_000d.ToInt128());

        var bin = TinyhandSerializer.Serialize(tc);
        var tc2 = TinyhandSerializer.Deserialize<Int128Class>(bin);
        tc.IsStructuralEqual(tc2);
    }

    [Fact]
    public void TestUInt128()
    {
        var tc = new UInt128Class();
        tc.A = new(123, 456);
        tc.B = [0, 1, 10, 12, new(11, 22)];
        tc.C = [2, 3, 44, 55, 666];
        tc.C.Add(123_000_000_000_000_000_000_000_000d.ToUInt128());

        var bin = TinyhandSerializer.Serialize(tc);
        var tc2 = TinyhandSerializer.Deserialize<UInt128Class>(bin);
        tc.IsStructuralEqual(tc2);
    }
}
