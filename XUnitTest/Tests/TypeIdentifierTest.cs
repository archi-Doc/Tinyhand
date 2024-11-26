// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Text;
using Arc.Crypto;
using Tinyhand;
using Tinyhand.Tests;
using Xunit;

namespace XUnitTest.Tests;

public class TypeIdentifierTest
{// GetTypeIdentifierCode
    /*[Fact]
    public void Test1()
    {
        var t = (ITinyhandSerialize)new OuterClass();
        var identifier = FarmHash.Hash64("Tinyhand.Tests.OuterClass");
        t.GetTypeIdentifier().Is(identifier);
        TinyhandSerializer.GetTypeIdentifierObject<OuterClass>().Is(identifier);

        t = (ITinyhandSerialize)new GenericsTestClass<string>();
        identifier = FarmHash.Hash64("Tinyhand.Tests.GenericsTestClass<string>");
        t.GetTypeIdentifier().Is(identifier);
        TinyhandSerializer.GetTypeIdentifierObject<GenericsTestClass<string>>().Is(identifier);

        t = (ITinyhandSerialize)new GenericsTestClass<int>();
        identifier = FarmHash.Hash64("Tinyhand.Tests.GenericsTestClass<int>");
        t.GetTypeIdentifier().Is(identifier);
        TinyhandSerializer.GetTypeIdentifierObject<GenericsTestClass<int>>().Is(identifier);

        t = (ITinyhandSerialize)(new GenericsTestClass<string>.GenericsNestedClass<double>());
        identifier = FarmHash.Hash64("Tinyhand.Tests.GenericsTestClass<string>.GenericsNestedClass<double>");
        t.GetTypeIdentifier().Is(identifier);
        TinyhandSerializer.GetTypeIdentifierObject<GenericsTestClass<string>.GenericsNestedClass<double>>().Is(identifier);
    }*/
}
