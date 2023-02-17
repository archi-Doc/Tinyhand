// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

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

[TinyhandObject]
public partial class LockObjectClass2b : LockObjectClass2
{
    public LockObjectClass2b()
    {
    }

    [Key(2)]
    public int Z { get; set; }
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

[TinyhandObject(LockObject = "semaphore")]
public partial class LockObjectClass3
{
    [Key(0)]
    public int X { get; set; }

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
