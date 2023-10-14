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

[TinyhandObject(Tree = true)]
public partial class JournalClass
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

    [Key(6)]
    public readonly int Id3;
}

[TinyhandObject(Tree = true, LockObject = "semaphore")]
public partial class JournalClass2
{
    public JournalClass2()
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

[TinyhandObject(Tree = true)]
public partial class JournalClass2B
{
    [Key(1)]
    public JournalClass2 Class1 { get; set; }

    [Key(2)]
    public JournalTestClass Class2 { get; set; }
}

[TinyhandObject(Tree = true, LockObject = "syncObject")]
public partial class JournalClass3 : ITinyhandCustomJournal
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

[TinyhandObject(Tree = true)]
public partial class JournalTestClass
{
    public JournalTestClass()
    {
    }

    public JournalTestClass(int id, string name)
    {
        this.id = id;
        this.name = name;
    }

    [Key(0, AddProperty = "Id")]
    private int id;

    [Key(1, AddProperty = "Name")]
    private string name = string.Empty;
}

public class JournalTest
{
    [Fact]
    public void Test1()
    {
        var tester = new JournalTester();
        var c = new JournalTestClass(1, "one");

        var cc = new JournalTestClass();
        cc.TreeRoot = tester;
        cc.Id = c.Id;
        cc.Name = c.Name;

        var journal = tester.GetJournal();
        var c2 = new JournalTestClass();
        JournalHelper.ReadJournal(c2, journal).IsTrue();

        c2.IsStructuralEqual(c);
    }

    [Fact]
    public void Test2()
    {
        var tester = new JournalTester();
        var c = TinyhandSerializer.Reconstruct<JournalClass2B>();
        c.TreeRoot = tester;

        c.Class1.X7 = 77;
        c.Class2.Id = 21;
        c.Class2.Name = "AA";

        var journal = tester.GetJournal();
        var c2 = TinyhandSerializer.Reconstruct<JournalClass2B>();
        JournalHelper.ReadJournal(c2, journal).IsTrue();

        c.Class1.X7.Is(c2.Class1.X7);
        c.Class2.Id.Is(c2.Class2.Id);
        c.Class2.Name.Is(c2.Class2.Name);
    }
}
