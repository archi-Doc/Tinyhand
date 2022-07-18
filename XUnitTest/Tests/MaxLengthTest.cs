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
    [Key(0, PropertyName = "X")]
    // [MaxLength(3)]
    private int _x;

    [Key(1, PropertyName = "Name")]
    [MaxLength(3)]
    private string _name = default!;

    [Key(2, PropertyName = "Ids")]
    [MaxLength(3)]
    private int[] _ids = default!;

    [Key(3, PropertyName = "StringArray")]
    [MaxLength(3, 4)]
    private string[] _stringArray = default!;

    [Key(4, PropertyName = "StringList")]
    [MaxLength(4, 3)]
    private List<string> _stringList = default!;
}

public class MaxLengthTest
{
    [Fact]
    public void Test1()
    {
        var tc = new MaxLengthClass()
        {
            X = 1,
            Name = "Fuga",
            Ids = new int[] { 1, 2, 3, 4, },
            StringArray = new[] { "11", "2222", "333333", "44444444", "5", },
            StringList = new(new[] { "11", "2222", "333333", "44444444", "5", }),
        };

        var tc2 = new MaxLengthClass()
        {
            X = 1,
            Name = "Fug",
            Ids = new int[] { 1, 2, 3, },
            StringArray = new[] { "11", "2222", "3333", },
            StringList = new(new[] { "11", "222", "333", "444", }),
        };

        var b = TinyhandSerializer.Serialize(tc);
        var tc3 = TinyhandSerializer.Deserialize<MaxLengthClass>(b);
        tc3.IsStructuralEqual(tc2);
    }
}
