// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
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
}
public class Int128Test
{
    [Fact]
    public void Test()
    {
        var tc = new Int128Class();
        tc.A = new(123, 456);
        tc.B = [0, 1, 10, 12,];

        var bin = TinyhandSerializer.Serialize(tc);
        var tc2 = TinyhandSerializer.Deserialize<Int128Class>(bin);
        tc.IsStructuralEqual(tc2);
    }
}
