// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using Tinyhand;
using Tinyhand.IO;
using Xunit;

namespace XUnitTest.Tests;

[TinyhandObject]
public readonly partial struct PartialImplementationStruct
{
    [Key(1)]
    public readonly int Partial;

    [Key(2)]
    public readonly int Total;

    public PartialImplementationStruct(int partial, int total)
    {
        this.Partial = partial;
        this.Total = total;
    }

    public PartialImplementationStruct()
    {
    }

    static void ITinyhandSerializable<PartialImplementationStruct>.Serialize(ref TinyhandWriter writer, scoped ref PartialImplementationStruct v, TinyhandSerializerOptions options)
    {
        if (options.IsSignatureMode)
        {
        }
        else
        {
            writer.WriteArrayHeader(3);

            writer.WriteNil();
            writer.Write(v.Partial);
            writer.Write(v.Total);
        }
    }
}

[TinyhandObject]
public partial class PartialImplementationClass
{
    public PartialImplementationClass()
    {
    }

    [Key(0)]
    public string Name { get; set; } = "Test";

    [Key(1)]
    public int Id { get; set; }

    static void ITinyhandSerializable<PartialImplementationClass>.Serialize(ref TinyhandWriter writer, scoped ref PartialImplementationClass? v, TinyhandSerializerOptions options)
    {
        if (v == null)
        {
            writer.WriteNil();
            return;
        }

        if (!options.IsSignatureMode) writer.WriteArrayHeader(2);
        writer.Write(v.Name);
        writer.Write(v.Id);
    }
}

public class PartialImplementationTest
{
    [Fact]
    public void Test1()
    {
        var c = new PartialImplementationClass { Name = "Test", Id = 1 };
        var binary = TinyhandSerializer.Serialize(c);
        var c2 = TinyhandSerializer.Deserialize<PartialImplementationClass>(binary);
        c2.Name.Is("Test");
        c2.Id.Is(1);
    }

    [Fact]
    public void Test2()
    {
        var c = new PartialImplementationStruct(1, 100);
        var binary = TinyhandSerializer.Serialize(c);
        var c2 = TinyhandSerializer.Deserialize<PartialImplementationStruct>(binary);
        c2.Partial.Is(1);
        c2.Total.Is(100);
    }
}
