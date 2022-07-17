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
    // [MaxLength(3)]
    public int X { get; set; }

    [Key(1)]
    [MaxLength(3)]
    public string Name { get; set; } = default!;

    [Key(2)]
    [MaxLength(3)]
    public int[] Ids { get; set; } = default!;

    [Key(3)]
    [MaxLength(3, 4)]
    public string[] StringArray { get; set; } = default!;

    [Key(4)]
    [MaxLength(4, 3)]
    public List<string> StringList { get; init; } = default!;

    [Key(5)]
    public string Name2
    {
        get => this.name2;
        set
        {
            if (value.Length > 3)
            {
                this.name2 = value.Substring(0, 3);
            }
            else
            {
                this.name2 = value;
            }
        }
    }

    private string name2 = string.Empty;
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
            Name2 = "Hoge",
        };

        var tc2 = new MaxLengthClass()
        {
            X = 1,
            Name = "Fug",
            Ids = new int[] { 1, 2, 3, },
            StringArray = new[] { "11", "2222", "3333", },
            StringList = new(new[] { "11", "222", "333", "444", }),
            Name2 = "Hog",
        };

        var b = TinyhandSerializer.Serialize(tc);
        var tc3 = TinyhandSerializer.Deserialize<MaxLengthClass>(b);
        tc3.IsStructuralEqual(tc2);
    }
}
