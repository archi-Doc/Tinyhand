// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace Tinyhand.Tests;

[TinyhandObject(ExplicitKeysOnly = true)]
public partial class MaxLengthClass
{
    [Key(0)]
    // [MaxLength(3)]
    public partial int X { get; set; }

    [Key(1, AddProperty = "Name")]
    [MaxLength(3)]
    private string _name = default!;

    [Key(2, AddProperty = "Ids")]
    [MaxLength(3)]
    private int[] _ids = default!;

    [Key(3)]
    [MaxLength(3, 4)]
    public partial string[] StringArray { get; set; } = default!;

    [Key(4, AddProperty = "StringList")]
    [MaxLength(4, 3)]
    private List<string> _stringList = default!;

    // [IgnoreMember]
    public int DataId;

    [Key(8, AddProperty = "TestId")]
    public int testId;
}

[TinyhandObject(ExplicitKeysOnly = false)]
public partial class MaxLengthClass2 : MaxLengthClass
{
    [Key(5)]
    [MaxLength(4)]
    public partial byte[] Byte { get; set; } = default!;

    [Key(6, AddProperty = "ByteArray")]
    [MaxLength(2, 3)]
    private byte[][] _byteArray = default!;

    [Key(7, AddProperty = "ByteList")]
    [MaxLength(3, 2)]
    private List<byte[]> _byteList = default!;

    [IgnoreMember]
    public int DataId2;
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

        var td = new MaxLengthClass2()
        {
            X = 1,
            Name = "Fuga",
            Ids = new int[] { 1, 2, 3, 4, },
            StringArray = new[] { "11", "2222", "333333", "44444444", "5", },
            StringList = new(new[] { "11", "2222", "333333", "44444444", "5", }),
            Byte = new byte[] { 0, 1, 2, 3, 4, },
            ByteArray = new byte[][] { new byte[] { 0, 1, 2, }, new byte[] { 0, 1, 2, 3, }, new byte[] { 0, 1, 2, 3, 4, }, },
            ByteList = new(new byte[][] { new byte[] { 0, 1, 2, }, new byte[] { 0, 1, 2, 3, }, new byte[] { 0, 1, 2, 3, 4, }, new byte[] { 0, 1, 2, }, }),
        };

        var td2 = new MaxLengthClass2()
        {
            X = 1,
            Name = "Fug",
            Ids = new int[] { 1, 2, 3, },
            StringArray = new[] { "11", "2222", "3333", },
            StringList = new(new[] { "11", "222", "333", "444", }),
            Byte = new byte[] { 0, 1, 2, 3, },
            ByteArray = new byte[][] { new byte[] { 0, 1, 2, }, new byte[] { 0, 1, 2, }, },
            ByteList = new(new byte[][] { new byte[] { 0, 1, }, new byte[] { 0, 1, }, new byte[] { 0, 1, }, }),
        };

        b = TinyhandSerializer.Serialize(td);
        var td3 = TinyhandSerializer.Deserialize<MaxLengthClass2>(b);
        td3.IsStructuralEqual(td2);
    }
}
