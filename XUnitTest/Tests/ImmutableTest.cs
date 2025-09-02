// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using Tinyhand;
using Xunit;

namespace XUnitTest.Tests;

/*[TinyhandObject(AddImmutable = true)]
public partial struct ImmutableTestStruct
{
    [Key(0)]
    public string Name;
}*/

[TinyhandObject(ImplicitKeyAsName = true, AddImmutable = true)]
public partial class ImmutableTestClass
{
    public ImmutableTestClass()
    {
    }

    public string A { get; set; } = "Test";

    public int B { get; private set; } = 123;

    protected string C { get; set; } = "Protected";

    private string D { get; set; } = "Private";

    private int E = 100;

    protected int F = 200;
}

public class ImmutableTest
{
    [Fact]
    public void Test1()
    {
        var tc = new ImmutableTestClass();
        var im = tc.ToImmutable();

        var bin = TinyhandSerializer.Serialize(tc);
        var bin2 = TinyhandSerializer.Serialize(im);
        bin.AsSpan().SequenceEqual(bin2.AsSpan());
    }
}
