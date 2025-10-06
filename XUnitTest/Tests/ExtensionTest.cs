// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using Tinyhand;
using Xunit;

namespace XUnitTest.Tests;

[TinyhandObject(ImplicitMemberNameAsKey = true)]
public partial record ExtensionTestClass(int Id, string Name);

public class ExtensionTest
{
    [Fact]
    public void Test1()
    {
        var c = new ExtensionTestClass(1, "A");
        var byteArray = c.Serialize();
        var rentMemory = c.SerializeToRentMemory();
        rentMemory.Span.SequenceEqual(byteArray.AsSpan()).IsTrue();

        var d = new ExtensionTestClass(0, string.Empty);
        d.TryDeserialize(byteArray.AsSpan());
        d.Equals(c).IsTrue();

        byteArray = c.Serialize(TinyhandSerializerOptions.Lz4);
        d.TryDeserialize(byteArray.AsSpan());
        d.Equals(c).IsTrue();

        var hash = c.GetXxHash3();
    }
}
