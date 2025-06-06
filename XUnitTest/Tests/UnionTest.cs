﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Xunit;

namespace Tinyhand.Tests;

[TinyhandUnion(0, typeof(UnionTestClassA))]
[TinyhandUnion(1, typeof(UnionTestClassB))]
[TinyhandUnion(2, typeof(UnionTestClassC<string>))]
public partial interface IUnionTestInterface
{
    public int Id { get; set; }
}

[TinyhandObject]
public partial class UnionTestClassA : IUnionTestInterface
{
    [Key(0)]
    public int X { get; set; }

    [IgnoreMember]
    public int Token;

    [Key(1)]
    public int Id { get; set; }
}

[TinyhandObject]
public partial class UnionTestClassB : IUnionTestInterface
{
    [Key(0)]
    public string Name { get; set; }

    [IgnoreMember]
    public int Token;

    [Key(1)]
    public int Id { get; set; }
}

[TinyhandObject]
public partial class UnionTestClassC<T> : IUnionTestInterface
{
    public UnionTestClassC(T data, int id)
    {
        Data = data;
        Id = id;
    }

    [Key(0)]
    public T Data { get; set; }

    [Key(1)]
    public int Id { get; set; }
}

[TinyhandUnion(0, typeof(UnionTestSubA))]
[TinyhandUnion(1, typeof(UnionTestSubB))]
public abstract partial class UnionTestBase
{
    [Key(0)]
    public int ID { get; set; }
}

[TinyhandObject]
public partial class UnionTestSubA : UnionTestBase
{
    [Key(1)]
    public string Name { get; set; }
}

[TinyhandObject]
public partial class UnionTestSubB : UnionTestBase
{
    [Key(1)]
    public double Height { get; set; }
}

[TinyhandObject]
public partial class UnionTestClassX
{
    [Key(0)]
    public IUnionTestInterface IUnion = default!;

    [Key(1)]
    public IUnionTestInterface? IUnionNullable = default!;

    [Key(2)]
    [Reuse(false)]
    public IUnionTestInterface IUnionNoReuse = default!;

    [Key(3)]
    public IUnionTestInterface IUnionCantCast = default!;
}

public class UnionTest
{
    [Fact]
    public void TestInterface()
    {
        var classA = new UnionTestClassA() { X = 10, };
        var classB = new UnionTestClassB() { Name = "test", };
        var classC = new UnionTestClassC<string>("test", 2);

        TestHelper.TestWithoutMessagePack(classA); // Not compatible with MessagePack
        TestHelper.TestWithoutMessagePack(classB);
        TestHelper.TestWithoutMessagePack(classC);

        TestHelper.TestWithoutMessagePack((IUnionTestInterface)classA, false);
        TestHelper.TestWithoutMessagePack((IUnionTestInterface)classB, false);
        TestHelper.TestWithoutMessagePack((IUnionTestInterface)classC, false);
    }

    [Fact]
    public void TestAbstract()
    {
        var classA = new UnionTestSubA() { Name = "10", };
        var classB = new UnionTestSubB() { Height = 1.23d, };

        TestHelper.TestWithoutMessagePack(classA); // Not compatible with MessagePack
        TestHelper.TestWithoutMessagePack(classB);

        TestHelper.TestWithoutMessagePack((UnionTestBase)classA, false);
        TestHelper.TestWithoutMessagePack((UnionTestBase)classB, false);
    }

    [Fact]
    public void TestInterface2()
    {
        var x = new UnionTestClassX();
        x.IUnionNullable = new UnionTestClassB() { Name = "nullable", Token = 11, };
        x.IUnionNoReuse = new UnionTestClassA() { X = 2, Token = 12, };
        x.IUnionCantCast = new UnionTestClassA() { X = 3, Token = 13, };

        var y = new UnionTestClassX();
        y.IUnionNullable = new UnionTestClassB() { Name = "yn", Token = 110, };
        y.IUnionNoReuse = new UnionTestClassA() { X = 12, Token = 120, };
        y.IUnionCantCast = new UnionTestClassB() { Name = "tc", Token = 130, };

        var b = TinyhandSerializer.Serialize(x);
        var x2 = TinyhandSerializer.Deserialize<UnionTestClassX>(b);
        x2.IUnion.IsNull();
        x2.IUnionNullable.IsNotNull();
        x2.IUnionNoReuse.IsNotNull();
        x2.IUnionCantCast.IsNotNull();
        var classB = (UnionTestClassB)x2.IUnionNullable;
        classB.Name.Is("nullable");
        classB.Token.Is(0);
        var classA = (UnionTestClassA)x2.IUnionNoReuse;
        classA.X.Is(2);
        classA.Token.Is(0);
        classA = (UnionTestClassA)x2.IUnionCantCast;
        classA.X.Is(3);
        classA.Token.Is(0);

        x2 = y;
        TinyhandSerializer.DeserializeObject<UnionTestClassX>(b, ref x2);
        x2.IUnion.IsNull();
        x2.IUnionNullable.IsNotNull();
        x2.IUnionNoReuse.IsNotNull();
        x2.IUnionCantCast.IsNotNull();
        classB = (UnionTestClassB)x2.IUnionNullable;
        classB.Name.Is("nullable");
        classB.Token.Is(110);
        classA = (UnionTestClassA)x2.IUnionNoReuse;
        classA.X.Is(2);
        classA.Token.Is(0);
        classA = x2.IUnionCantCast as UnionTestClassA;
        classA.IsNotNull();
        classA.X.Is(3);
        classA.Token.Is(0);
    }
}
