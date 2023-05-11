// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Xunit;

namespace Tinyhand.Tests;

[TinyhandObject(Journaling = true)]
public partial class JournalingClass
{
    [Key(0)]
    public int X0 { get; set; }

    [Key(1)]
    public int X1 { get; set; }

    [Key(2)]
    public int X2 { get; set; }

    [Key(3)]
    public int X3 { get; set; }

    [Key(4)]
    public int X4 { get; set; }

    [Key(5)]
    public int X6 { get; set; }
}

[TinyhandObject(Journaling = true)]
public partial class JournalingClass2
{
    [KeyAsName]
    public int X0 { get; set; }

    [KeyAsName]
    public int X1ABCDEFGHIJKLMN { get; set; }

    [KeyAsName]
    public int X2ABCDEFGHIJKLMNOPQRSTU { get; set; }

    [KeyAsName]
    public int X3 { get; set; }

    [KeyAsName]
    public int X4 { get; set; }

    [KeyAsName]
    public int X6 { get; set; }
}

public class JournalingTest
{
    [Fact]
    public void Test1()
    {
    }
}
