// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tinyhand;
using Xunit;

namespace Tinyhand.Tests;

public class PrimitiveTest
{
    [Fact]
    public void PrimitiveIntKeyTest()
    {
        var t = new PrimitiveIntKeyClass();
        var t2 = TestHelper.TestWithMessagePack(t);
    }

    [Fact]
    public void PrimitiveStringKeyTest()
    {
        var t = new PrimitiveStringKeyClass();
        var t2 = TestHelper.TestWithMessagePack(t);
    }

    [Fact]
    public void PrimitiveArrayTest()
    {
        var t = new PrimitiveArrayClass();
        var t2 = TestHelper.TestWithMessagePack(t);
    }

    [Fact]
    public void PrimitiveNullableArrayTest()
    {
        var t = new PrimitiveNullableArrayClass();
        var t2 = TestHelper.TestWithMessagePack(t);
    }

    [Fact]
    public void PrimitiveNullableArray2Test()
    {
        var t = new PrimitiveNullableArrayClass2();
        var t2 = TestHelper.TestWithMessagePack(t);
    }

    [Fact]
    public void PrimitiveEmptyTest()
    {
        var t = new EmptyClass();
        TestHelper.TestWithMessagePack(t);

        var t2 = new EmptyClass2();
        TestHelper.TestWithMessagePack(t2);

        var t3 = new EmptyStruct();
        TestHelper.TestWithMessagePack(t3);

        var t4 = new EmptyStruct2();
        TestHelper.TestWithMessagePack(t4);
    }
}
