// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using Tinyhand;
using Xunit;

namespace XUnitTest.Tests;

[TinyhandObject(ImplicitKeyAsName = true)]
public partial record InterfaceTestClass(int Id, string Name);

public class InterfaceTest
{
    [Fact]
    public void Test1()
    {
        var c = new InterfaceTestClass(1, "A");
        var byteArray = c.SerializeInterface();
        var rentMemory = c.SerializeInterfaceToRentMemory();
        rentMemory.Span.SequenceEqual(byteArray.AsSpan()).IsTrue();

        var d = new InterfaceTestClass(0, string.Empty);
        d.TryDeserializeInterface(byteArray.AsSpan());
        d.Equals(c).IsTrue();
    }
}
