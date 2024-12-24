// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Tinyhand;
using Xunit;

namespace XUnitTest.Tests;

[TinyhandObject]
public partial class InstanceTestClass
{
    public InstanceTestClass()
    {
    }

    [Key(0)]
    public Dictionary<string, string> Dictionary { get; set; } = new(StringComparer.InvariantCultureIgnoreCase);
}

public class InstanceTest
{
    [Fact]
    public void Test1()
    {
        var tc = new InstanceTestClass();
        tc.Dictionary.Add("A", "1");
        tc.Dictionary.ContainsKey("A").IsTrue();
        tc.Dictionary.ContainsKey("a").IsTrue();

        tc = TinyhandSerializer.Deserialize<InstanceTestClass>(TinyhandSerializer.Serialize(tc));
        tc.Dictionary.ContainsKey("A").IsTrue();
        //tc.Dictionary.ContainsKey("a").IsTrue();
    }
}
