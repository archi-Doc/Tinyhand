// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Tinyhand.Formatters;
using Tinyhand.IO;
using Xunit;

namespace Tinyhand.Tests;

[TinyhandObject(ImplicitKeyAsName = true)]
public partial class MemberNameTestClass
{
    public MemberNameTestItem?[] array = new MemberNameTestItem[10];

    public int System;
}

[TinyhandObject(ImplicitKeyAsName = true)]
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
