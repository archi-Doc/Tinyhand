// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Xunit;

namespace Tinyhand.Tests;

public enum MyEnum { One, Two, Three }

[TinyhandObject]
public partial class EnumTestClass
{
    [Key(1)]
    public MyEnum Item1 { get; set; }

    [Key(2)]
    public MyEnum Item2 { get; set; }

    [Key(3)]
    public MyEnum Item3 { get; set; }
}

public class EnumTest
{
    [Fact]
    public void MultipleEnumTest()
    {
        var t = new EnumTestClass();
        t.Item2 = MyEnum.Two;
        t.Item3 = MyEnum.Three;
        var t2 = TestHelper.Convert(t);
        t2.IsStructuralEqual(t);
    }
}
