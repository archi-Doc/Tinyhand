// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Tinyhand;
using Xunit;

namespace Tinyhand.Tests;

[TinyhandObject]
public partial class MaxLengthClass
{
    [Key(0)]
    [MaxLength(3)]
    public int X { get; set; }

    [Key(1)]
    [MaxLength(3)]
    public string Name { get; set; }

    [Key(2)]
    [MaxLength(3)]
    public int[] Ids { get; set; } = default!;

    [Key(3)]
    [MaxLength(3, 4)]
    public string[] StringArray { get; set; } = default!;

    [Key(4)]
    [MaxLength(4, 3)]
    public List<string> StringList { get; set; } = default!;
}

public class MaxLengthTest
{
    [Fact]
    public void Test1()
    {
        var tc = new MaxLengthClass();
        tc.X = 1;
        tc.Name = "Fuga";
        tc.Ids = new int[] { 1, 2, 3, 4, };
        tc.StringArray = new [] { "11", "2222", "333333", "44444444", "5", };
        tc.StringList = new(tc.StringArray);

        var tc2 = new MaxLengthClass();
        tc2.X = 1;
        tc2.Name = "Fug";
        tc2.Ids = new int[] { 1, 2, 3, };
        tc2.StringArray = new[] { "11", "2222", "3333", };
        tc2.StringList = new(new[] { "11", "222", "333", "444", });

        var b = TinyhandSerializer.Serialize(tc);
        var tc3 = TinyhandSerializer.Deserialize<MaxLengthClass>(b);
        tc3.IsStructuralEqual(tc2);
    }
}
