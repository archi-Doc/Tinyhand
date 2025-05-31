// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Runtime.CompilerServices;
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

    [Key(3)]
    public readonly string Name;

    public PartialImplementationStruct(int partial, int total, string name)
    {
        this.Partial = partial;
        this.Total = total;
        this.Name = name;
    }

    public PartialImplementationStruct()
    {
        this.Name = "Default";
    }

    static void ITinyhandSerializable<PartialImplementationStruct>.Serialize(ref TinyhandWriter writer, scoped ref PartialImplementationStruct v, TinyhandSerializerOptions options)
    {
        if (options.IsSignatureMode)
        {
        }
        else
        {
            writer.WriteArrayHeader(4);

            writer.WriteNil();
            writer.Write(v.Partial);
            writer.Write(v.Total);
            writer.Write(v.Name);
        }
    }

    static unsafe void ITinyhandSerializable<PartialImplementationStruct>.Deserialize(ref TinyhandReader reader, scoped ref PartialImplementationStruct v, TinyhandSerializerOptions options)
    {
        if (reader.TryReadNil()) throw new TinyhandException("Data is Nil, struct can not be null.");
        var numberOfData = reader.ReadArrayHeader();
        options.Security.DepthStep(ref reader);
        try
        {
            if (numberOfData-- > 0) reader.Skip();
            if (numberOfData-- > 0 && !reader.TryReadNil())
            {
                int vd;
                vd = reader.ReadInt32();
                fixed (int* ptr = &v.Partial)*ptr = vd;
            }
            if (numberOfData-- > 0 && !reader.TryReadNil())
            {
                int vd;
                vd = reader.ReadInt32();
                fixed (int* ptr = &v.Total)*ptr = vd;
            }
            if (numberOfData-- > 0 && !reader.TryReadNil())
            {
                string vd;
                vd = reader.ReadString() ?? string.Empty;
                Unsafe.AsRef(in v.Name) = vd;
            }
            else
            {
                string vd;
                vd = string.Empty;
                Unsafe.AsRef(in v.Name) = vd;
            }
            while (numberOfData-- > 0) reader.Skip();
        }
        finally
        {
            reader.Depth--;
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
        var c = new PartialImplementationStruct(1, 100, "Abc");
        var binary = TinyhandSerializer.Serialize(c);
        var c2 = TinyhandSerializer.Deserialize<PartialImplementationStruct>(binary);
        c2.Partial.Is(1);
        c2.Total.Is(100);
        c2.Name.Is("Abc");
    }
}
