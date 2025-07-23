// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Text;
using Arc.Crypto;
using Tinyhand;
using Tinyhand.Tests;
using Xunit;

namespace XUnitTest.Tests;

public class TypeIdentifierTest
{// GetTypeIdentifierCode

    [Fact]
    public void Test()
    {
        var typeIdentifier = TinyhandTypeIdentifier.GetTypeIdentifier<TestRecord>();
        typeIdentifier.IsNot(0u);

        var tc = new TestRecord(1, 2, "a", "x");
        var r = TinyhandTypeIdentifier.TrySerializeRentMemory(tc);
        r.RentMemory.IsEmpty.IsFalse();
        r = TinyhandTypeIdentifier.TrySerializeRentMemory(new TypeIdentifierTest());
        r.RentMemory.IsEmpty.IsTrue();

        r = TinyhandTypeIdentifier.TrySerializeRentMemory(typeIdentifier, (object)tc);
        r.RentMemory.IsEmpty.IsFalse();
    }

    /*[Fact]
    public void Test1()
    {
        var t = (ITinyhandSerializable)new OuterClass();
        var identifier = FarmHash.Hash64("Tinyhand.Tests.OuterClass");
        t.GetTypeIdentifier().Is(identifier);
        TinyhandSerializer.GetTypeIdentifierObject<OuterClass>().Is(identifier);

        t = (ITinyhandSerializable)new GenericsTestClass<string>();
        identifier = FarmHash.Hash64("Tinyhand.Tests.GenericsTestClass<string>");
        t.GetTypeIdentifier().Is(identifier);
        TinyhandSerializer.GetTypeIdentifierObject<GenericsTestClass<string>>().Is(identifier);

        t = (ITinyhandSerializable)new GenericsTestClass<int>();
        identifier = FarmHash.Hash64("Tinyhand.Tests.GenericsTestClass<int>");
        t.GetTypeIdentifier().Is(identifier);
        TinyhandSerializer.GetTypeIdentifierObject<GenericsTestClass<int>>().Is(identifier);

        t = (ITinyhandSerializable)(new GenericsTestClass<string>.GenericsNestedClass<double>());
        identifier = FarmHash.Hash64("Tinyhand.Tests.GenericsTestClass<string>.GenericsNestedClass<double>");
        t.GetTypeIdentifier().Is(identifier);
        TinyhandSerializer.GetTypeIdentifierObject<GenericsTestClass<string>.GenericsNestedClass<double>>().Is(identifier);
    }*/
}
