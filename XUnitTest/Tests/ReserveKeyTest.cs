// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Xunit;

namespace Tinyhand.Tests;

[TinyhandObject(ReservedKeyCount = ReserveKeyClass.ReservedKeyCount)]
public partial class ReserveKeyClass
{
    public const int ReservedKeyCount = 8;
    [Key(0)]
    public int X { get; set; }

    [Key(5)]
    public int Y { get; set; }
}

[TinyhandObject]
public partial class ReserveKeyDerived : ReserveKeyClass
{
    // [Key(ReserveKeyClass.ReservedKeyCount - 1)]
    // public int A { get; set; }

    [Key(ReserveKeyClass.ReservedKeyCount)]
    public int B { get; set; }

    [Key(3, IgnoreKeyReservation = true)]
    public int C { get; set; }
}

public class ReserveKeyTest
{
    [Fact]
    public void Test1()
    {
    }
}
