// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

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
