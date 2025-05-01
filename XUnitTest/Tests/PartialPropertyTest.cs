// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Text;
using Tinyhand;
using Xunit;

namespace XUnitTest.Tests;

[TinyhandObject]
public partial class PartialPropertyTestClass
{
    [Key(0)]
    [MaxLength(3)]
    public partial string Name { get; set; } = string.Empty;
}

[TinyhandObject]
public partial class PartialPropertyTestClass2 : PartialPropertyTestClass
{
    [Key(1)]
    [MaxLength(3)]
    public partial string Name2 { get; set; } = string.Empty;
}

public class PartialPropertyTest
{
    [Fact]
    public void Test1()
    {
        var c = new PartialPropertyTestClass();
        c.Name = "Test";
        c.Name.Is("Tes");
        var c2 = TinyhandSerializer.Deserialize<PartialPropertyTestClass>(TinyhandSerializer.Serialize(c));
        c2.Name.Is("Tes");
    }
}
