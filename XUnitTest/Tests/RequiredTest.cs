// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Tinyhand;
using Xunit;

namespace XUnitTest.Tests;

[TinyhandObject]
public partial class RequiredBaseClass
{
    public RequiredBaseClass(int x)
    {
    }
}

[TinyhandObject]
public partial class RequiredTestClass : RequiredBaseClass
{
    [SetsRequiredMembers]
    public RequiredTestClass(int x, string text)
        : base(2)
    {
        this.X = x;
        this.Text = text;
    }

    public RequiredTestClass()
        : base(2)
    {
    }

    [Key(0)]
    public required int X { get; set; } = 49;

    [Key(1)]
    public required string Text { get; set; } = "Test";
}

[TinyhandObject]
public partial record class RequiredTestClass2([property: Key(2)] int Y)
{// Primary ctor (int), required true
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
