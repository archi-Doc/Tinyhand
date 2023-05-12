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

[TinyhandUnion("", typeof(UnionTestInt0))]
[TinyhandUnion("1", typeof(UnionTestInt1))]
[TinyhandUnion("2222", typeof(UnionTestInt2))]
[TinyhandUnion("33333333", typeof(UnionTestInt3))]
[TinyhandUnion("a4444444444444444", typeof(UnionTestInt4))]
[TinyhandUnion("555555555555555555555555", typeof(UnionTestInt5))]
public partial interface IUnionTestString
{
    public int Id { get; set; }
}

[TinyhandObject]
public partial class UnionTestInt0 : IUnionTestInt, IUnionTestString
{
    [Key(0)]
    public int Id { get; set; }

    // [Key("test")]
    // public string Name { get; set; }
}

[TinyhandObject]
public partial class UnionTestInt1 : IUnionTestInt, IUnionTestString
{
    [Key(0)]
    public int Id { get; set; }
}

[TinyhandObject]
public partial class UnionTestInt2 : IUnionTestInt, IUnionTestString
{
    [Key(0)]
    public int Id { get; set; }
}

[TinyhandObject]
public partial class UnionTestInt3 : IUnionTestInt, IUnionTestString
{
    [Key(0)]
    public int Id { get; set; }
}

[TinyhandObject]
public partial class UnionTestInt4 : IUnionTestInt, IUnionTestString
{
    [Key(0)]
    public int Id { get; set; }
}

[TinyhandObject]
public partial class UnionTestInt5 : IUnionTestInt, IUnionTestString
{
    [Key(0)]
    public int Id { get; set; }
}

public class UnionTest2
{
    private delegate void TestDelegate(ref Tinyhand.IO.TinyhandWriter writer, ref IUnionTestInt? v, TinyhandSerializerOptions options);

    [Fact]
    public void Test()
    {
        // Int key
        var a0 = new UnionTestInt0() { Id = 0 };
        var b0 = TinyhandSerializer.Deserialize<IUnionTestInt>(TinyhandSerializer.Serialize((IUnionTestInt)a0));
        b0.IsStructuralEqual(a0);

        var a1 = new UnionTestInt1() { Id = 1 };
        var b1 = TinyhandSerializer.Deserialize<IUnionTestInt>(TinyhandSerializer.Serialize((IUnionTestInt)a1));
        b1.IsStructuralEqual(a1);

        var a2 = new UnionTestInt2() { Id = 2 };
        var b2 = TinyhandSerializer.Deserialize<IUnionTestInt>(TinyhandSerializer.Serialize((IUnionTestInt)a2));
        b2.IsStructuralEqual(a2);

        var a3 = new UnionTestInt3() { Id = 3 };
        var b3 = TinyhandSerializer.Deserialize<IUnionTestInt>(TinyhandSerializer.Serialize((IUnionTestInt)a3));
        b3.IsStructuralEqual(a3);

        var a4 = new UnionTestInt4() { Id = 4 };
        var b4 = TinyhandSerializer.Deserialize<IUnionTestInt>(TinyhandSerializer.Serialize((IUnionTestInt)a4));
        b4.IsStructuralEqual(a4);

        var a5 = new UnionTestInt5() { Id = 5 };
        var b5 = TinyhandSerializer.Deserialize<IUnionTestInt>(TinyhandSerializer.Serialize((IUnionTestInt)a5));
        b5.IsStructuralEqual(a5);

        // String key
        a0 = new UnionTestInt0() { Id = 0 };
        b0 = TinyhandSerializer.Deserialize<IUnionTestInt>(TinyhandSerializer.Serialize((IUnionTestInt)a0));
        b0.IsStructuralEqual(a0);

        a1 = new UnionTestInt1() { Id = 1 };
        var c1 = TinyhandSerializer.Deserialize<IUnionTestString>(TinyhandSerializer.Serialize((IUnionTestString)a1));
        c1.IsStructuralEqual(a1);

        a2 = new UnionTestInt2() { Id = 2 };
        var c2 = TinyhandSerializer.Deserialize<IUnionTestString>(TinyhandSerializer.Serialize((IUnionTestString)a2));
        c2.IsStructuralEqual(a2);

        a3 = new UnionTestInt3() { Id = 3 };
        var c3 = TinyhandSerializer.Deserialize<IUnionTestString>(TinyhandSerializer.Serialize((IUnionTestString)a3));
        c3.IsStructuralEqual(a3);

        a4 = new UnionTestInt4() { Id = 4 };
        var c4 = TinyhandSerializer.Deserialize<IUnionTestString>(TinyhandSerializer.Serialize((IUnionTestString)a4));
        c4.IsStructuralEqual(a4);

        a5 = new UnionTestInt5() { Id = 5 };
        var c5 = TinyhandSerializer.Deserialize<IUnionTestString>(TinyhandSerializer.Serialize((IUnionTestString)a5));
        c5.IsStructuralEqual(a5);

        var st = TinyhandSerializer.SerializeToString((IUnionTestString)a4);
    }
}
