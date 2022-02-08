// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using Tinyhand;
using Tinyhand.IO;

namespace Benchmark;

[TinyhandObject]
public partial class IdentifierClass
{
    [Key(0)]
    ulong Id0;

    [Key(1)]
    ulong Id1;

    [Key(2)]
    ulong Id2;

    [Key(3)]
    ulong Id3;

    public IdentifierClass()
    {
    }

    public IdentifierClass(ulong id0, ulong id1, ulong id2, ulong id3)
    {
        this.Id0 = id0;
        this.Id1 = id1;
        this.Id2 = id2;
        this.Id3 = id3;
    }
}

[TinyhandObject]
public partial struct IdentifierStruct
{
    [Key(0)]
    ulong Id0;

    [Key(1)]
    ulong Id1;

    [Key(2)]
    ulong Id2;

    [Key(3)]
    ulong Id3;

    public IdentifierStruct()
    {
        this.Id0 = 0;
        this.Id1 = 0;
        this.Id2 = 0;
        this.Id3 = 0;
    }

    public IdentifierStruct(ulong id0, ulong id1, ulong id2, ulong id3)
    {
        this.Id0 = id0;
        this.Id1 = id1;
        this.Id2 = id2;
        this.Id3 = id3;
    }
}

[TinyhandObject]
public readonly partial struct IdentifierReadonlyStruct
{
    [Key(0)]
    readonly ulong Id0;

    [Key(1)]
    readonly ulong Id1;

    [Key(2)]
    readonly ulong Id2;

    [Key(3)]
    readonly ulong Id3;

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

[Config(typeof(BenchmarkConfig))]
public class SerializeIdentifierTest
{
    public SerializeIdentifierTest()
    {
    }

    [GlobalSetup]
    public void Setup()
    {
    }

    [Benchmark]
    public IdentifierClass? SerializeDeserializeClass()
    {
        var t = new IdentifierClass(1234, 5678, 9101112, 13141516);
        return TinyhandSerializer.Deserialize<IdentifierClass>(TinyhandSerializer.Serialize(t));
    }

    [Benchmark]
    public IdentifierStruct SerializeDeserializeStruct()
    {
        var t = new IdentifierStruct(1234, 5678, 9101112, 13141516);
        return TinyhandSerializer.Deserialize<IdentifierStruct>(TinyhandSerializer.Serialize(t));
    }

    [Benchmark]
    public IdentifierReadonlyStruct SerializeDeserializeReadonlyStruct()
    {
        var t = new IdentifierReadonlyStruct(1234, 5678, 9101112, 13141516);
        return TinyhandSerializer.Deserialize<IdentifierReadonlyStruct>(TinyhandSerializer.Serialize(t));
    }
}
