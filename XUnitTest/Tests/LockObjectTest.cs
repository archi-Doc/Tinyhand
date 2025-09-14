// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Threading;
using Arc.Threading;
using Tinyhand;

namespace XUnitTest.Tests;

[TinyhandObject(LockObject = "syncObject")]
public partial class LockObjectClass
{
    [Key(0)]
    public int X { get; set; }

    protected object syncObject = new();
}

[TinyhandObject(LockObject = "syncObject")]
public partial class LockObjectClass2 : LockObjectClass
{
    public LockObjectClass2()
    {
    }

    [Key(1)]
    public int Y { get; set; }
}

[TinyhandObject(UseResolver = true)]
public partial class LockObjectClass2b : LockObjectClass2
{
    public LockObjectClass2b()
    {
    }

    [Key(2)]
    public int Z { get; set; }
}

[TinyhandObject(ImplicitKeyAsName = true)]
public partial class LockObjectCollection
{
    public LockObjectCollection()
    {
    }

    public LockObjectClass2 Class2 { get; set; } = default!;

    public LockObjectClass2b Class2b { get; set; } = default!;
}

[TinyhandObject(LockObject = "syncObject")]
public partial struct LockObjectStruct
{
    public LockObjectStruct()
    {
    }

    [Key(0)]
    public int X { get; set; }

    private object syncObject = new();
}

[TinyhandObject(Structual = true, LockObject = "semaphore")]
public partial class LockObjectClass3
{// Delete()
    [Key(0)]
    public int X { get; set; }

    [Key(1)]
    public Tinyhand.Tests.JournalClass JournalClass { get; set; } = new();

    protected SemaphoreLock semaphore = new();
}

[TinyhandObject(LockObject = "semaphore")]
public partial class LockObjectClass4
{
    [Key(0)]
    public int X { get; set; }

    protected ILockable? semaphore = new SemaphoreLock();
}

[TinyhandObject]
public partial class LockObjectClass5 : LockObjectClass4
{
}

[TinyhandObject(LockObject = "lockObject")]
public partial class LockObjectClass6
{
    [Key(0)]
    public int X { get; set; }

    protected Lock lockObject = new();

    [TinyhandOnSerializing]
    private void OnSerializing()
    {
    }

    [TinyhandOnSerialized]
    protected void OnSerialized()
    {
    }

    [TinyhandOnDeserializing]
    protected void OnDeserializing()
    {
    }

    [TinyhandOnDeserialized]
    protected void OnDeserialized()
    {
    }
}
