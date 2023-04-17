// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Xunit;

namespace Tinyhand.Tests;

[TinyhandUnion(0, typeof(UnionTestInt0))]
[TinyhandUnion(1, typeof(UnionTestInt1))]
[TinyhandUnion(2, typeof(UnionTestInt2))]
[TinyhandUnion(3, typeof(UnionTestInt3))]
[TinyhandUnion(4, typeof(UnionTestInt4))]
[TinyhandUnion(5, typeof(UnionTestInt5))]
public partial interface IUnionTestInt
{
    public int Id { get; set; }
}

[TinyhandObject]
public partial class UnionTestInt0 : IUnionTestInt
{
    [Key(0)]
    public int Id { get; set; }
}

[TinyhandObject]
public partial class UnionTestInt1 : IUnionTestInt
{
    [Key(0)]
    public int Id { get; set; }
}

[TinyhandObject]
public partial class UnionTestInt2 : IUnionTestInt
{
    [Key(0)]
    public int Id { get; set; }
}

[TinyhandObject]
public partial class UnionTestInt3 : IUnionTestInt
{
    [Key(0)]
    public int Id { get; set; }
}

[TinyhandObject]
public partial class UnionTestInt4 : IUnionTestInt
{
    [Key(0)]
    public int Id { get; set; }
}

[TinyhandObject]
public partial class UnionTestInt5 : IUnionTestInt
{
    [Key(0)]
    public int Id { get; set; }
}

public class UnionTest2
{
    [Fact]
    public void Test()
    {
        var a0 = new UnionTestInt0() { Id = 0 };
        var b0 = TinyhandSerializer.Deserialize<IUnionTestInt>(TinyhandSerializer.Serialize((IUnionTestInt)a0));
        b0.IsStructuralEqual(a0);

        var a1 = new UnionTestInt0() { Id = 1 };
        var b1 = TinyhandSerializer.Deserialize<IUnionTestInt>(TinyhandSerializer.Serialize((IUnionTestInt)a1));
        b1.IsStructuralEqual(a1);

        var a2 = new UnionTestInt0() { Id = 2 };
        var b2 = TinyhandSerializer.Deserialize<IUnionTestInt>(TinyhandSerializer.Serialize((IUnionTestInt)a2));
        b2.IsStructuralEqual(a2);

        var a3 = new UnionTestInt0() { Id = 3 };
        var b3 = TinyhandSerializer.Deserialize<IUnionTestInt>(TinyhandSerializer.Serialize((IUnionTestInt)a3));
        b3.IsStructuralEqual(a3);

        var a4 = new UnionTestInt0() { Id = 4 };
        var b4 = TinyhandSerializer.Deserialize<IUnionTestInt>(TinyhandSerializer.Serialize((IUnionTestInt)a4));
        b4.IsStructuralEqual(a4);

        var a5 = new UnionTestInt0() { Id = 5 };
        var b5 = TinyhandSerializer.Deserialize<IUnionTestInt>(TinyhandSerializer.Serialize((IUnionTestInt)a5));
        b5.IsStructuralEqual(a5);
    }
}

