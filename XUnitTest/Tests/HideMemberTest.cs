// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Tinyhand;

namespace XUnitTest.Tests;

[TinyhandObject]
public partial class HideMemberClass
{// Check: hidden members
    [Key(0)]
    public int X { get; set; }

    [Key(1)]
    public int Y { get; set; }
}

[TinyhandObject]
public partial class HideMemberClass2 : HideMemberClass
{
    public HideMemberClass2()
    {
    }

    [IgnoreMember]
    public new string X { get; set; }

    // [Key(2)]
    // public string Y { get; set; }
}

[TinyhandObject]
public partial class HideMemberClass3 : HideMemberClass2
{
    public HideMemberClass3()
    {
    }

    [IgnoreMember]
    public new double X { get; set; }

    // [Key(2)]
    // public string Y { get; set; }
}
