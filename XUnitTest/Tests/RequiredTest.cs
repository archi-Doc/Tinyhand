// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using Tinyhand;
using Xunit;

namespace XUnitTest.Tests;

[TinyhandObject]
public partial class RequiredBaseClass
{
    public RequiredBaseClass(/*int x*/)
    {
    }
}

[TinyhandObject]
public partial class RequiredTestClass // : BaseClass
{
    [SetsRequiredMembers]
    public RequiredTestClass(int x, string text)
    {
        this.X = x;
        this.Text = text;
    }

    public RequiredTestClass()
    {
    }

    [Key(0)]
    public required int X { get; set; } = 49;

    [Key(1)]
    public required string Text { get; set; } = "Test";
}

public class RequiredTest
{
    [Fact]
    public void Test1()
    {
        var a = new RequiredTestClass(1, "a");
        a.X.Is(1);
        a.Text.Is("a");

        a = TinyhandSerializer.Deserialize<RequiredTestClass>(TinyhandSerializer.Serialize(a));
        a.X.Is(1);
        a.Text.Is("a");

        var b = RequiredTestClass.UnsafeConstructor();
        b.X.Is(49);
        b.Text.Is("Test");

        b = new()
        {
            X = 2,
            Text = "b",
        };

        b.X.Is(2);
        b.Text.Is("b");
    }
}
