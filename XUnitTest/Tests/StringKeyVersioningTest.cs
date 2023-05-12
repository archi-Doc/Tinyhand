// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Xunit;

namespace Tinyhand.Tests;

[TinyhandObject]
public partial class StringKeyVersioningTestClass
{
    [KeyAsName]
    public int Id { get; set; }

    [IgnoreMember]
    public int Id2 { get; set; }

    [KeyAsName]
    public int Id3 { get; set; }

    public StringKeyVersioningTestClass()
    {
    }

    public StringKeyVersioningTestClass(int id)
    {
        this.Id = id;
        this.Id2 = id;
        this.Id3 = id;
    }
}

[TinyhandObject]
public partial class StringKeyVersioningTestClass2
{
    [KeyAsName]
    public int Id { get; set; }

    [KeyAsName]
    public int Id2 { get; set; }

    [IgnoreMember]
    public int Id3 { get; set; }

    public StringKeyVersioningTestClass2()
    {
    }

    public StringKeyVersioningTestClass2(int id)
    {
        this.Id = id;
        this.Id2 = id;
        this.Id3 = id;
    }
}

[TinyhandObject(ImplicitKeyAsName = true)]
public partial class StringKeyVersioningTestClass3
{
    public int Id { get; set; }

    public int Id2 { get; set; }

    public int Id3 { get; set; }

    public StringKeyVersioningTestClass3()
    {
    }

    public StringKeyVersioningTestClass3(int id)
    {
        this.Id = id;
        this.Id2 = id;
        this.Id3 = id;
    }
}

public class StringKeyVersioningTest
{
    [Fact]
    public void Test2()
    {
        var c = new StringKeyVersioningTestClass3(1);

        var c2 = TinyhandSerializer.Deserialize<StringKeyVersioningTestClass>(TinyhandSerializer.Serialize(c));
    }

    [Fact]
    public void Test1()
    {
        var c = new StringKeyVersioningTestClass(1);

        // StringKeyVersioningTestClass (1,1,1) -> StringKeyVersioningTestClass (1,0,1)
        var c2 = TinyhandSerializer.Deserialize<StringKeyVersioningTestClass>(TinyhandSerializer.Serialize(c));
        c2.Id.Is(1);
        c2.Id2.Is(0);
        c2.Id3.Is(1);

        // StringKeyVersioningTestClass (1,1,1) -> StringKeyVersioningTestClass2 (1,0,0)
        var c3 = TinyhandSerializer.Deserialize<StringKeyVersioningTestClass2>(TinyhandSerializer.Serialize(c));
        c3.Id.Is(1);
        c3.Id2.Is(0);
        c3.Id3.Is(0);

        c3.Id3 = 2;
        c3.Id3 = 3;
        // StringKeyVersioningTestClass2 (1,2,3) -> StringKeyVersioningTestClass (1,0,0)
        var c4 = TinyhandSerializer.Deserialize<StringKeyVersioningTestClass>(TinyhandSerializer.Serialize(c3));
        c4.Id.Is(1);
        c4.Id2.Is(0);
        c4.Id3.Is(0);
    }
}
