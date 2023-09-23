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
    public Int128[] B = Array.Empty<Int128>();

    [Key(2)]
    public List<Int128> C = new();
}

[TinyhandObject]
public partial class UInt128Class
{
    [Key(0)]
    public UInt128 A;

    [Key(1)]
    public UInt128[] B = Array.Empty<UInt128>();

    [Key(2)]
    public List<UInt128> C = new();
}

public class Int128Test
{
    [Fact]
    public void TestInt128()
    {
        var tc = new Int128Class();
        tc.A = new(123, 456);
        tc.B = new Int128[] { 0, 1, -10, 12, new(11, 22), };
        tc.C.Add(-2);
        tc.C.Add(3);
        tc.C.Add(444);
        tc.C.Add(long.MaxValue);
        tc.C.Add(long.MinValue);
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
        tc.B = new UInt128[] { 0, 1, 10, 12, new(11, 22), };
        tc.C.Add(2);
        tc.C.Add(3);
        tc.C.Add(444);
        tc.C.Add(long.MaxValue);
        tc.C.Add(ulong.MaxValue);
        tc.C.Add(123_000_000_000_000_000_000_000_000d.ToUInt128());

        var bin = TinyhandSerializer.Serialize(tc);
        var tc2 = TinyhandSerializer.Deserialize<UInt128Class>(bin);
        tc.IsStructuralEqual(tc2);
    }
}
