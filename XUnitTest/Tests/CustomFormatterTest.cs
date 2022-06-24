// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tinyhand;
using Tinyhand.IO;
using Xunit;

namespace Tinyhand.Tests;

[TinyhandObject]
public partial class CustomFormatterClass : ITinyhandSerialize
{
    public int ID { get; set; }

    public string Name { get; set; } = default!;

    void ITinyhandSerialize.Deserialize(ref TinyhandReader reader, TinyhandSerializerOptions options)
    {
        if (!reader.TryReadNil())
        {
            this.ID = reader.ReadInt32();
        }

        if (!reader.TryReadNil())
        {
            this.Name = reader.ReadString() ?? string.Empty;
        }
        else
        {
            this.Name = string.Empty;
        }
    }
    
    void ITinyhandSerialize.Serialize(ref TinyhandWriter writer, TinyhandSerializerOptions options)
    {
        writer.Write(this.ID + 1);
        writer.Write(this.Name + "Mock");
    }

    /*public void Reconstruct(TinyhandSerializerOptions options)
    {
        if (this.Name == null)
        {
            this.Name = string.Empty;
        }
    }*/
}

[TinyhandObject]
public partial class CustomFormatterGenericClass<T> : ITinyhandSerialize
{
    public int ID { get; set; }

    public T TValue { get; set; }

    void ITinyhandSerialize.Deserialize(ref TinyhandReader reader, TinyhandSerializerOptions options)
    {
        if (!reader.TryReadNil())
        {
            this.ID = reader.ReadInt32();
        }

        if (!reader.TryReadNil())
        {
            this.TValue = options.Resolver.GetFormatter<T>().Deserialize(ref reader, options);
        }
        else
        {
            this.TValue = options.Resolver.GetFormatter<T>().Reconstruct(options);
        }
    }

    void ITinyhandSerialize.Serialize(ref TinyhandWriter writer, TinyhandSerializerOptions options)
    {
        writer.Write(this.ID + 1);
        options.Resolver.GetFormatter<T>().Serialize(ref writer, TValue, options);
    }

    /*public void Reconstruct(TinyhandSerializerOptions options)
    {
        if (this.Name == null)
        {
            this.Name = string.Empty;
        }
    }*/
}

public class CustomFormatterTest
{
    [Fact]
    public void Test()
    {
        var tc = new CustomFormatterClass() { ID = 10, Name = "John", };
        var tc2 = TinyhandSerializer.Deserialize<CustomFormatterClass>(TinyhandSerializer.Serialize(tc));
        tc2.ID.Is(11);
        tc2.Name.Is("JohnMock");
    }

    [Fact]
    public void GenericTest()
    {
        var tc = new CustomFormatterGenericClass<long>() { ID = 10, TValue = -9, };
        var tc2 = TinyhandSerializer.Deserialize<CustomFormatterGenericClass<long>>(TinyhandSerializer.Serialize(tc));
        tc2.ID.Is(11);
        tc2.TValue.Is(-9);
    }
}
