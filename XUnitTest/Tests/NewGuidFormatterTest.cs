// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Xunit;

namespace Tinyhand.Tests;

public partial class NewGuidFormatterTest
{
    [TinyhandObject(ImplicitMemberNameAsKey = true)]
    public partial class InClass
    {
        public int MyProperty { get; set; }

        public Guid Guid { get; set; }
    }

    [Fact]
    public void GeneratedGuidMemberRoundtrips()
    {
        var c = new InClass() { MyProperty = 3414141, Guid = Guid.NewGuid() };
        var c2 = TinyhandSerializer.Deserialize<InClass>(TinyhandSerializer.Serialize(c));
        Assert.NotNull(c2);
        Assert.Equal(c.MyProperty, c2.MyProperty);
        Assert.Equal(c.Guid, c2.Guid);
    }
}
