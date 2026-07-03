// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Text;
using Tinyhand;
using Xunit;

namespace XUnitTest.Tests;

[TinyhandObject]
public partial class ConstructorWithRefClass
{
    public ConstructorWithRefClass(ref string x)
    {
    }

    [Key(0)]
    public string Name { get; set; } = "Test";
}

[TinyhandObject]
public partial class ConstructorWithRefClass2 : ConstructorWithRefClass
{
    public ConstructorWithRefClass2(ref string x)
        : base(ref x)
    {
    }
}

public class ConstructorWithRefTest
{
    [Fact]
    public void Test1()
    {
    }
}
