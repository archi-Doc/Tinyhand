// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Xunit;

namespace Tinyhand.Tests;

[TinyhandObject(ImplicitKeyAsName = true)]
public partial class ReuseInstanceClass
{
    public ReuseInstanceClass()
        : this(true)
    {
    }

    public ReuseInstanceClass(bool flag)
    {
        this.ReuseClass = new(flag);
        this.ReuseClassFalse = new(flag);
        this.ReuseStruct = new(flag);
        this.ReuseStructFalse = new(flag);
    }

    public ReuseClass ReuseClass { get; set; } = default!;

    [Reuse(false)]
    public ReuseClass ReuseClassFalse { get; set; } = default!;

    public ReuseStruct ReuseStruct { get; set; } = default!;

    [Reuse(false)]
    public ReuseStruct ReuseStructFalse { get; set; } = default!;
}

[TinyhandObject]
public partial class ReuseClass
{
    public ReuseClass()
        : this(false)
    {
    }

    public ReuseClass(bool flag)
    {
        this.Flag = flag;
    }

    [Key(0)]
    public int Int { get; set; }

    [IgnoreMember]
    public bool Flag { get; set; }
}

[TinyhandObject]
public partial struct ReuseStruct
{
    public ReuseStruct(bool flag)
    {
        this.Double = 0d;
        this.Flag = flag;
    }

    [Key(0)]
    public double Double { get; set; }

    [IgnoreMember]
    public bool Flag { get; set; }
}

public class ReuseInstanceTest
{
    [Fact]
    public void Test1()
    {
        var t = TinyhandSerializer.Reconstruct<ReuseInstanceClass>();
        t.ReuseClass.Flag.Is(true);
        t.ReuseClassFalse.Flag.Is(false);
        t.ReuseStruct.Flag.Is(true);
        t.ReuseStructFalse.Flag.Is(false);

        t = TinyhandSerializer.Deserialize<ReuseInstanceClass>(TinyhandSerializer.Serialize(new Empty2()));
        t.ReuseClass.Flag.Is(true);
        t.ReuseClassFalse.Flag.Is(false);
        t.ReuseStruct.Flag.Is(true);
        t.ReuseStructFalse.Flag.Is(false);

        var t2 = new ReuseInstanceClass(false);
        TinyhandSerializer.DeserializeObject(TinyhandSerializer.Serialize(new ReuseInstanceClass()), ref t2);
        /*var t2 = new ReuseInstanceClass(false);
        var reader = new Tinyhand.IO.TinyhandReader(TinyhandSerializer.Serialize(new ReuseInstanceClass()));
        t2.Deserialize(ref reader, TinyhandSerializerOptions.Standard);*/
        t2.ReuseClass.Flag.Is(false);
        t2.ReuseClassFalse.Flag.Is(false);
        t2.ReuseStruct.Flag.Is(false);
        t2.ReuseStructFalse.Flag.Is(false);

        t2 = new ReuseInstanceClass(true);
        TinyhandSerializer.DeserializeObject(TinyhandSerializer.Serialize(new ReuseInstanceClass()), ref t2);
        t2.ReuseClass.Flag.Is(true);
        t2.ReuseClassFalse.Flag.Is(false);
        t2.ReuseStruct.Flag.Is(true);
        t2.ReuseStructFalse.Flag.Is(false);
    }

    [Fact]
    public void Test2()
    {
        var t = TinyhandSerializer.Reconstruct<ReuseInstanceClass>();
        t.ReuseClass.Flag.Is(true);
        t.ReuseClassFalse.Flag.Is(false);
        t.ReuseStruct.Flag.Is(true);
        t.ReuseStructFalse.Flag.Is(false);

        t = TinyhandSerializer.DeserializeFromUtf8<ReuseInstanceClass>(TinyhandSerializer.SerializeToUtf8(new Empty2()));
        t.ReuseClass.Flag.Is(true);
        t.ReuseClassFalse.Flag.Is(false);
        t.ReuseStruct.Flag.Is(true);
        t.ReuseStructFalse.Flag.Is(false);

        var t2 = new ReuseInstanceClass(false);
        TinyhandSerializer.DeserializeObjectFromUtf8(TinyhandSerializer.SerializeToUtf8(new ReuseInstanceClass()), ref t2);
        /*var t2 = new ReuseInstanceClass(false);
        var reader = new Tinyhand.IO.TinyhandReader(TinyhandSerializer.Serialize(new ReuseInstanceClass()));
        t2.Deserialize(ref reader, TinyhandSerializerOptions.Standard);*/
        t2.ReuseClass.Flag.Is(false);
        t2.ReuseClassFalse.Flag.Is(false);
        t2.ReuseStruct.Flag.Is(false);
        t2.ReuseStructFalse.Flag.Is(false);

        t2 = new ReuseInstanceClass(true);
        TinyhandSerializer.DeserializeObjectFromUtf8(TinyhandSerializer.SerializeToUtf8(new ReuseInstanceClass()), ref t2);
        t2.ReuseClass.Flag.Is(true);
        t2.ReuseClassFalse.Flag.Is(false);
        t2.ReuseStruct.Flag.Is(true);
        t2.ReuseStructFalse.Flag.Is(false);
    }
}
