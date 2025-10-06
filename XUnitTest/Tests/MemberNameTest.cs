// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace Tinyhand.Tests;

[TinyhandObject(ImplicitMemberNameAsKey = true)]
public partial class MemberNameTestClass
{
    public MemberNameTestItem?[] array = new MemberNameTestItem[10];

    public int System;
}

[TinyhandObject(ImplicitMemberNameAsKey = true)]
public partial class MemberNameTestItem
{
    public int ii;
}

public class MemberNameTest
{
    [Fact]
    public void Test1()
    {
        var c = new MemberNameTestClass();
        var b = Tinyhand.TinyhandSerializer.Serialize(c);
        var c2 = Tinyhand.TinyhandSerializer.Deserialize<MemberNameTestClass>(b);
        c2.IsStructuralEqual(c);
    }
}
