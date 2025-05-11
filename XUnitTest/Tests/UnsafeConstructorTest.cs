// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Text;
using Tinyhand;
using Xunit;

namespace XUnitTest.Tests;

[TinyhandObject]
public partial class UnsafeConstructorTestClass
{
    private UnsafeConstructorTestClass()
    {
    }
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
