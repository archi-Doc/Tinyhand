// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Threading;
using Tinyhand.IO;
using Xunit;

namespace Tinyhand.Tests;

[TinyhandObject]
public readonly partial struct JournalIdentifier
{
    [Key(0)]
    public readonly int Id0;

    [Key(1)]
    public readonly int Id1;
}

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

[TinyhandObject(Journaling = true)]
public partial class JournalingClass2B
{
    [Key(1)]
    public JournalingClass2 Class1 { get; set; }

    [Key(2)]
    public JournalingClass2 Class2 { get; set; }
}

[TinyhandObject(Journaling = true, LockObject = "syncObject")]
public partial class JournalingClass3 : ITinyhandCustomJournal
{
    [Key(0)]
    public int X0 { get; set; }

    [Key(1, AddProperty = "X1")]
    private int x1;

    [Key(2, AddProperty = "X2")]
    private JournalIdentifier x2;

    private object syncObject = new();

    public void WriteCustomLocator(ref TinyhandWriter writer)
    {
    }

    bool ITinyhandCustomJournal.ReadCustomRecord(ref TinyhandReader reader)
    {
        return false;
    }
}

[TinyhandObject(Journaling = true)]
public partial class JournalingTestClass
{
    public JournalingTestClass()
    {
    }

    public JournalingTestClass(int id, string name)
    {
        this.id = id;
        this.name = name;
    }

    [Key(0, AddProperty = "Id")]
    private int id;

    [Key(1, AddProperty = "Name")]
    private string name = string.Empty;
}

public class JournalingTest
{
    [Fact]
    public void Test1()
    {
        var tester = new JournalTester();
        var c = new JournalingTestClass(1, "one");

        var cc = new JournalingTestClass();
        cc.Journal = tester;
        cc.Id = c.Id;
        cc.Name = c.Name;

        var journal = tester.GetJournal();
        var c2 = new JournalingTestClass();
        JournalHelper.ReadJournal(c2, journal).IsTrue();

        c2.IsStructuralEqual(c);
    }

    [Fact]
    public void Test2()
    {
        var tester = new JournalTester();
        var c = new JournalingClass2B();
        c.Journal = tester;

        var journal = tester.GetJournal();
        var c2 = new JournalingTestClass();
        JournalHelper.ReadJournal(c2, journal).IsTrue();

        c2.IsStructuralEqual(c);
    }
}
