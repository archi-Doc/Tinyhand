// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Tinyhand;
using Xunit;

namespace XUnitTest.Tests;

[TinyhandObject]
public partial class HiddenMemberClass
{// Check: hidden members
    public HiddenMemberClass()
    {
    }

    public HiddenMemberClass(int x, int y, int z, int z2)
    {
        this.X = x;
        this.Y = y;
        this.Z = z;
        this.Z2 = z2;
    }

    [Key(0)]
    public int X { get; set; }

    [Key(1)]
    public int Y { get; set; }

    [Key(2)]
    public readonly int Z;

    [Key(3)]
    private int Z2 { get; set; }
}

[TinyhandObject]
public partial class HiddenMemberClass2 : HiddenMemberClass
{
    public HiddenMemberClass2()
    {
    }

    public HiddenMemberClass2(int x, int y, string y2, int z, int z2)
        : base(x, y, z, z2)
    {
        this.Y = y2;
    }

    [IgnoreMember]
    public new string X { get; set; } = string.Empty;

    [Key(4)]
    public new string Y { get; set; } = string.Empty;

    [IgnoreMember]
    public new string Z { get; set; } = string.Empty;

    [IgnoreMember]
    public string Z2 { get; set; } = string.Empty;
}

[TinyhandObject]
public partial class HiddenMemberClass3 : HiddenMemberClass2
{
    public HiddenMemberClass3()
    {
    }

    public HiddenMemberClass3(int x, int y, string y2, double y3, int z, int z2)
        : base(x, y, y2, z, z2)
    {
        this.Y = y3;
    }

    [IgnoreMember]
    public new double X { get; set; }

    [Key(5)]
    public new double Y { get; set; }

    [IgnoreMember]
    public new double Z { get; set; }

    [IgnoreMember]
    public new double Z2 { get; set; }
}

public class HideMemberTest
{
    [Fact]
    public void Test1()
    {
        var tc = new HiddenMemberClass(1, 2, 3, 4);
        var td = TinyhandSerializer.Deserialize<HiddenMemberClass>(TinyhandSerializer.Serialize(tc));
        td.IsStructuralEqual(tc);

        var tc2 = new HiddenMemberClass2(1, 2, "y", 3, 4);
        var td2 = TinyhandSerializer.Deserialize<HiddenMemberClass2>(TinyhandSerializer.Serialize(tc2));
        td2.IsStructuralEqual(tc2);

        var tc3 = new HiddenMemberClass3(1, 2, "y2", 33, 3, 4);
        var td3 = TinyhandSerializer.Deserialize<HiddenMemberClass3>(TinyhandSerializer.Serialize(tc3));
        td3.IsStructuralEqual(tc3);
    }
}
