// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Text;
using Tinyhand;
using Xunit;

namespace XUnitTest.Tests;

[TinyhandObject]
public partial class TemplateTestClass
{
    public TemplateTestClass()
    {
    }

    [Key(0)]
    public string Name { get; set; } = "Test";
}

public class TemplateTest
{
    [Fact]
    public void Test1()
    {
    }
}
