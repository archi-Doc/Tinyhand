// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Xunit;

namespace Tinyhand.Tests;

public class FormatterResolverTest
{
    [Fact]
    public void Test1()
    {
        var t = new FormatterResolverClass();
        var t2 = TestHelper.TestWithMessagePackWithoutCompareObject(t, false);
    }
}
