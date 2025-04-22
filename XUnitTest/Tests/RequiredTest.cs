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
public partial class RequiredtestClass // : BaseClass
{
    [Key(0)]
    public int X { get; set; } = 49;

    [Key(1)]
    public string Text { get; set; } = "Test";
}

public class RequiredTest
{
    [Fact]
    public void Test1()
    {
    }
}
