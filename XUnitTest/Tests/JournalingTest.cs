// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Threading;
using Tinyhand.IO;
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

[TinyhandObject(Journaling = true, LockObject = "semaphore")]
public partial class JournalingClass2
{
    public JournalingClass2()
    {
    }

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

    [Key("X7", AddProperty = "X7")]
    private int x7;

    protected SemaphoreLock semaphore = new();
}

[TinyhandObject(Journaling = true, LockObject = "syncObject")]
public partial class JournalingClass3 : ITinyhandCustomJournal
{
    [Key(0)]
    public int X0 { get; set; }

    [Key(1, AddProperty = "X1")]
    private int x1;

    private object syncObject = new();

    public void WriteCustomLocator(ref TinyhandWriter writer)
    {
    }

    bool ITinyhandCustomJournal.ReadCustomRecord(ref TinyhandReader reader)
    {
        return false;
    }
}

public class JournalingTest
{
    [Fact]
    public void Test1()
    {
    }
}
