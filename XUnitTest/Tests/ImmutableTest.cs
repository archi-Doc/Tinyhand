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

[TinyhandObject(ImplicitMemberNameAsKey = true, AddImmutable = true, IncludePrivateMembers = true)]
public partial class ImmutableTestClass
{
    public ImmutableTestClass()
    {
    }

    /// <summary>
    /// Gets or sets A.
    /// </summary>
    public string A { get; set; } = "Test";

    /// <summary>
    /// Gets B.<br/>
    /// 2nd line.
    /// </summary>
    public int B { get; private set; } = 123;

#pragma warning disable CS1570 // XML comment has badly formed XML
    /// <summary>
    /// Gets or sets C.<see>
    /// </summary>
    protected string C { get; set; } = "Protected";
#pragma warning restore CS1570 // XML comment has badly formed XML

    private string D { get; set; } = "Private";

    private int E = 100;

    protected int F = 200;

    private readonly int G = 1;
}

public class ImmutableTest
{
    [Fact]
    public void Test1()
    {
        var tc = new ImmutableTestClass();
        var im = tc.ToImmutable();
        var x = im.A;
        var y = im.B;

        var bin = TinyhandSerializer.Serialize(tc);
        var bin2 = TinyhandSerializer.Serialize(im);
        bin.AsSpan().SequenceEqual(bin2.AsSpan());
    }
}
