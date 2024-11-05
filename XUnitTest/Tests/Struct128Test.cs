// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Text;
using Arc.Crypto;
using Tinyhand;
using Xunit;

namespace XUnitTest.Tests;

[TinyhandObject]
public partial class StructTestClass
{
    public StructTestClass()
    {
    }

    public StructTestClass(long x)
    {
        this.A = new(x, x + 1);
        this.C = new(x, x + 1, x * 2, x * x);
    }

    [Key(0)]
    public Struct128 A { get; set; }

    [Key(1)]
    public Struct128 B { get; set; } = Struct128.Zero;

    [Key(2)]
    public Struct256 C { get; set; }

    [Key(3)]
    public Struct256 D { get; set; } = Struct256.One;
}

public class Struct128Test
{
    [Fact]
    public void Test1()
    {
        var tc = new StructTestClass(123456);
        var tc2 = TinyhandSerializer.Deserialize<StructTestClass>(TinyhandSerializer.Serialize(tc));
        tc.A.Equals(tc2.A).IsTrue();
        tc.B.Equals(tc2.B).IsTrue();
        tc.C.Equals(tc2.C).IsTrue();
        tc.D.Equals(tc2.D).IsTrue();
    }
}
