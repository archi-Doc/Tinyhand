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
    public readonly ulong Id0;

    [Key(1)]
    public readonly ulong Id1;

    [Key(2)]
    public readonly ulong Id2;

    [Key(3)]
    public readonly ulong Id3;

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
    [Key(0)]
    public readonly byte[] Bytes;

    public IdentifierReadonlyStruct2()
    {
        this.Bytes = Array.Empty<byte>();
    }

    public IdentifierReadonlyStruct2(byte[] bytes)
    {
        this.Bytes = bytes;
    }
}

[TinyhandObject]
public readonly partial struct IdentifierReadonlyStructB
{
    [Key(0)]
    public readonly ulong Id0;

    [Key(1)]
    public readonly ulong Id1;

    [Key(2)]
    public readonly ulong Id2;

    [Key(3)]
    public readonly ulong Id3;

    [Key(4)]
    public readonly ulong Id4;

    [Key(5)]
    public readonly ulong Id5;

    [Key(6)]
    public readonly ulong Id6;

    [Key(7)]
    public readonly ulong Id7;

    public IdentifierReadonlyStructB()
    {
        this.Id0 = 0;
        this.Id1 = 0;
        this.Id2 = 0;
        this.Id3 = 0;
        this.Id4 = 0;
        this.Id5 = 0;
        this.Id6 = 0;
        this.Id7 = 0;
    }

    public IdentifierReadonlyStructB(ulong id0, ulong id1, ulong id2, ulong id3)
    {
        this.Id0 = id0;
        this.Id1 = id1;
        this.Id2 = id2;
        this.Id3 = id3;
        this.Id4 = id0;
        this.Id5 = id1;
        this.Id6 = id2;
        this.Id7 = id3;
    }
}

[TinyhandObject]
public readonly partial struct IdentifierReadonlyStructB2
{
    [Key(0)]
    public readonly byte[] Bytes;

    public IdentifierReadonlyStructB2()
    {
        this.Bytes = Array.Empty<byte>();
    }

    public IdentifierReadonlyStructB2(byte[] bytes)
    {
        this.Bytes = bytes;
    }
}

[Config(typeof(BenchmarkConfig))]
public class SerializeIdentifierTest
{
    private IdentifierReadonlyStruct identifierStruct;
    private byte[] structBytes;
    private byte[] struct2Bytes;
    private byte[] bytes;
    private byte[] bytes2;

    public SerializeIdentifierTest()
    {
        this.identifierStruct = new IdentifierReadonlyStruct(1234, 0x9d05ec80924165bf, 0x03bc49061847b041, 0x72a57785d250e46e);
        this.structBytes = TinyhandSerializer.Serialize(this.identifierStruct);

        this.bytes = new byte[32];
        var span = bytes.AsSpan();
        BitConverter.TryWriteBytes(span, this.identifierStruct.Id0);
        span = span.Slice(sizeof(ulong));
        BitConverter.TryWriteBytes(span, this.identifierStruct.Id1);
        span = span.Slice(sizeof(ulong));
        BitConverter.TryWriteBytes(span, this.identifierStruct.Id2);
        span = span.Slice(sizeof(ulong));
        BitConverter.TryWriteBytes(span, this.identifierStruct.Id3);
        span = span.Slice(sizeof(ulong));

        this.bytes2 = new byte[64];
        span = bytes.AsSpan();
        span.CopyTo(bytes2.AsSpan(0, 32));
        span.CopyTo(bytes2.AsSpan(32, 32));

        this.struct2Bytes = TinyhandSerializer.Serialize(new IdentifierReadonlyStruct2(this.bytes));
    }

    [GlobalSetup]
    public void Setup()
    {
    }

    [Benchmark]
    public IdentifierClass? SerializeDeserializeClass()
    {
        var t = new IdentifierClass(this.identifierStruct.Id0, this.identifierStruct.Id1, this.identifierStruct.Id2, this.identifierStruct.Id3);
        return TinyhandSerializer.Deserialize<IdentifierClass>(TinyhandSerializer.Serialize(t));
    }

    [Benchmark]
    public IdentifierStruct SerializeDeserializeStruct()
    {
        var t = new IdentifierStruct(this.identifierStruct.Id0, this.identifierStruct.Id1, this.identifierStruct.Id2, this.identifierStruct.Id3);
        return TinyhandSerializer.Deserialize<IdentifierStruct>(TinyhandSerializer.Serialize(t));
    }

    [Benchmark]
    public IdentifierReadonlyStruct SerializeDeserializeReadonlyStruct()
    {
        var t = new IdentifierReadonlyStruct(this.identifierStruct.Id0, this.identifierStruct.Id1, this.identifierStruct.Id2, this.identifierStruct.Id3);
        return TinyhandSerializer.Deserialize<IdentifierReadonlyStruct>(TinyhandSerializer.Serialize(t));
    }

    [Benchmark]
    public IdentifierReadonlyStruct2 SerializeDeserializeReadonlyStruct2()
    {
        var t = new IdentifierReadonlyStruct2(this.bytes);
        return TinyhandSerializer.Deserialize<IdentifierReadonlyStruct2>(TinyhandSerializer.Serialize(t));
    }

    [Benchmark]
    public IdentifierReadonlyStructB SerializeDeserializeReadonlyStructB()
    {
        var t = new IdentifierReadonlyStructB(this.identifierStruct.Id0, this.identifierStruct.Id1, this.identifierStruct.Id2, this.identifierStruct.Id3);
        return TinyhandSerializer.Deserialize<IdentifierReadonlyStructB>(TinyhandSerializer.Serialize(t));
    }

    [Benchmark]
    public IdentifierReadonlyStructB2 SerializeDeserializeReadonlyStructB2()
    {
        var t = new IdentifierReadonlyStructB2(this.bytes2);
        return TinyhandSerializer.Deserialize<IdentifierReadonlyStructB2>(TinyhandSerializer.Serialize(t));
    }

    /*[Benchmark]
    public byte[] SerializeReadonlyStruct()
    {
        var t = new IdentifierReadonlyStruct(1234, 5678, 9101112, 13141516);
        return TinyhandSerializer.Serialize(t);
    }

    [Benchmark]
    public byte[] SerializeReadonlyStruct2()
    {
        var t = new IdentifierReadonlyStruct2(1234, 5678, 9101112, 13141516, new byte[] { 0, 1, 2, 3, });
        return TinyhandSerializer.Serialize(t);
    }

    [Benchmark]
    public IdentifierReadonlyStruct DeserializeReadonlyStruct()
    {
        return TinyhandSerializer.Deserialize<IdentifierReadonlyStruct>(this.structBytes);
    }

    [Benchmark]
    public byte[] DeserializeReadonlyStructAndArray()
    {
        var s = TinyhandSerializer.Deserialize<IdentifierReadonlyStruct>(this.structBytes);

        var bytes = new byte[32];
        var span = bytes.AsSpan();
        BitConverter.TryWriteBytes(span, s.Id0);
        span = span.Slice(sizeof(ulong));
        BitConverter.TryWriteBytes(span, s.Id1);
        span = span.Slice(sizeof(ulong));
        BitConverter.TryWriteBytes(span, s.Id2);
        span = span.Slice(sizeof(ulong));
        BitConverter.TryWriteBytes(span, s.Id3);

        return bytes;
    }

    [Benchmark]
    public IdentifierReadonlyStruct2 DeserializeReadonlyStruct2()
    {
        return TinyhandSerializer.Deserialize<IdentifierReadonlyStruct2>(this.struct2Bytes);
    }*/
}
