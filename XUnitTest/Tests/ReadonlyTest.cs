// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Tinyhand;
using Xunit;

namespace Tinyhand.Tests;

[TinyhandObject]
public readonly partial struct IdentifierReadonlyStruct
{
    [Key(0)]
    readonly ulong Id0;

    [Key(1)]
    readonly ulong Id1;

    [Key(2)]
    ulong Id2 { get; init; }

    [Key(3)]
    readonly ulong Id3 { get; init; }

    public IdentifierReadonlyStruct()
    {
        this.Id0 = 0;
        this.Id1 = 0;
        this.Id2 = 0;
        this.Id3 = 0;
    }

    public IdentifierReadonlyStruct(ulong id0, ulong id1, ulong id2, ulong id3)
    {
        this.Id0 = id0;
        this.Id1 = id1;
        this.Id2 = id2;
        this.Id3 = id3;
    }
}

[TinyhandObject]
public readonly partial struct IdentifierReadonlyStruct2
{
    [KeyAsName]
    readonly ulong Id0;

    [KeyAsName]
    readonly ulong Id1;

    [KeyAsName]
    ulong Id2 { get; init; }

    [KeyAsName]
    readonly ulong Id3 { get; init; }

    public IdentifierReadonlyStruct2()
    {
        this.Id0 = 0;
        this.Id1 = 0;
        this.Id2 = 0;
        this.Id3 = 0;
    }

    public IdentifierReadonlyStruct2(ulong id0, ulong id1, ulong id2, ulong id3)
    {
        this.Id0 = id0;
        this.Id1 = id1;
        this.Id2 = id2;
        this.Id3 = id3;
    }
}

public class ReadonlyTest
{
    delegate void FirstByRefAction<T1, T2>(in T1 arg1, T2 arg2);

    [Fact]
    public void Test1()
    {
        var r = new IdentifierReadonlyStruct(1, 2, 3, 4);

        var st = TinyhandSerializer.SerializeToString(r);
        var r3 = TinyhandSerializer.DeserializeFromString<IdentifierReadonlyStruct>(st);
        r3.Equals(r).IsTrue(); // r3.IsStructuralEqual(r);

        r3 = TinyhandSerializer.Clone(r);
        r3.Equals(r).IsTrue();
    }

    [Fact]
    public void Test2()
    {
        var r = new IdentifierReadonlyStruct2(1, 2, 3, 4);

        var st = TinyhandSerializer.SerializeToString(r);
        var r3 = TinyhandSerializer.DeserializeFromString<IdentifierReadonlyStruct2>(st);
        r3.Equals(r).IsTrue();

        r3 = TinyhandSerializer.Clone(r);
        r3.Equals(r).IsTrue();
    }
}
