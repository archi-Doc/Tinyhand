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

    [Key(1)]
    private readonly UnsafeConstructorTestClass2<int> class2 = new(1);
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
    protected UnsafeConstructorTestClass3(int x, string y)
        : base(x)
    {
    }
}

[TinyhandObject]
public partial class UnsafeConstructorTestClass4 : UnsafeConstructorTestClass3
{
    private UnsafeConstructorTestClass4(int x, string y)
        : base(x, y)
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
