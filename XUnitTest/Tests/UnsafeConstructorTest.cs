// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Tinyhand;
using ValueLink;
using Xunit;

namespace XUnitTest.Tests;

[ValueLinkObject]
[TinyhandObject]
public partial class ValueLinkClass
{
    [Key(0)]
    [Link(Primary = true, Type = ChainType.Ordered)]
    public int X { get; set; } = 1;
}

[TinyhandObject]
public partial class UnsafeConstructorTestClass
{
    private UnsafeConstructorTestClass()
    {
    }

    [Key(0)]
    private readonly ValueLinkClass.GoshujinClass c1 = new();
}

[TinyhandObject]
public partial class UnsafeConstructorTestClass2<T>
{
    internal UnsafeConstructorTestClass2(int x)
    {
    }

    [Key(0)]
    private T Value { get; set; } = default!;
}

[TinyhandObject]
public partial class UnsafeConstructorTestClass3 : UnsafeConstructorTestClass2<int>
{
    private UnsafeConstructorTestClass3()
        : base(1)
    {
    }
}

public class UnsafeConstructorTest
{
    [Fact]
    public void Test1()
    {
    }
}
