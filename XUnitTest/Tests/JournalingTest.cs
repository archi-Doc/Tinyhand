// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Xunit;

namespace Tinyhand.Tests;

[TinyhandObject(Journaling = true)]
public partial class JournalingClass
{
    [Key(0)]
    public int X { get; set; }
}

public class JournalingTest
{
    [Fact]
    public void Test1()
    {
    }
}
