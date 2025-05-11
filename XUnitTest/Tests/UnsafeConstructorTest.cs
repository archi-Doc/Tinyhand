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

/*[TinyhandObject]
public partial class UnsafeConstructorTestClass2<T>
{
    private UnsafeConstructorTestClass2()
    {
    }
}*/

public class UnsafeConstructorTest
{
    [Fact]
    public void Test1()
    {
    }
}
