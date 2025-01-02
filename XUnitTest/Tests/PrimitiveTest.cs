// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Xunit;

namespace Tinyhand.Tests;

public class PrimitiveTest
{
    [Fact]
    public void TryTest()
    {
        var c = new SimpleStructIntKeyData(1, 2, [3, 4,]);
        TinyhandSerializer.TryDeserializeObject<SimpleStructIntKeyData>(TinyhandSerializer.Serialize(c), out var c2).IsTrue();
        c2.Equals(c).IsTrue();
        TinyhandSerializer.TryDeserializeObject<SimpleStructIntKeyData>([1, 2, 3,], out c2).IsFalse();

        var d = new SimpleStringKeyData();
        d.Prop1 = 10;
        d.Prop3 = 33;
        TinyhandSerializer.TryDeserializeObject<SimpleStringKeyData>(TinyhandSerializer.Serialize(d), out var d2).IsTrue();
        d2.Equals(d).IsTrue();
        TinyhandSerializer.TryDeserializeObject<SimpleStringKeyData>([1, 2, 3,], out d2).IsFalse();
        d2.IsNull();
    }

    [Fact]
    public void PrimitiveIntKeyTest()
    {
        var t = new PrimitiveIntKeyClass();
        var t2 = TestHelper.TestWithoutMessagePack(t);
    }

    [Fact]
    public void PrimitiveStringKeyTest()
    {
        var t = new PrimitiveStringKeyClass();
        var t2 = TestHelper.TestWithoutMessagePack(t);
    }

    [Fact]
    public void PrimitiveArrayTest()
    {
        var t = new PrimitiveArrayClass();
        var t2 = TestHelper.TestWithoutMessagePack(t);
    }

    [Fact]
    public void PrimitiveNullableArrayTest()
    {
        var t = new PrimitiveNullableArrayClass();
        var t2 = TestHelper.TestWithoutMessagePack(t);
    }

    [Fact]
    public void PrimitiveNullableArray2Test()
    {
        var t = new PrimitiveNullableArrayClass2();
        var t2 = TestHelper.TestWithoutMessagePack(t);
    }

    [Fact]
    public void PrimitiveEmptyTest()
    {
        var t = new EmptyClass();
        TestHelper.TestWithoutMessagePack(t);

        var t2 = new EmptyClass2();
        TestHelper.TestWithoutMessagePack(t2);

        var t3 = new EmptyStruct();
        TestHelper.TestWithoutMessagePack(t3);

        var t4 = new EmptyStruct2();
        TestHelper.TestWithoutMessagePack(t4);
    }
}
