// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Tinyhand.IO;
using Xunit;

namespace Tinyhand.Tests;

[TinyhandObject]
public partial class CustomFormatterClass : ITinyhandSerialize<Tinyhand.Tests.CustomFormatterClass>
{
    public int ID { get; set; }

    public string Name { get; set; } = default!;

    static void ITinyhandSerialize<CustomFormatterClass>.Deserialize(ref TinyhandReader reader, scoped ref CustomFormatterClass? value, TinyhandSerializerOptions options)
    {
        value ??= new CustomFormatterClass();

        if (!reader.TryReadNil())
        {
            value.ID = reader.ReadInt32();
        }

        if (!reader.TryReadNil())
        {
            value.Name = reader.ReadString() ?? string.Empty;
        }
        else
        {
            value.Name = string.Empty;
        }
    }

    static void ITinyhandSerialize<CustomFormatterClass>.Serialize(ref TinyhandWriter writer, scoped ref CustomFormatterClass? value, TinyhandSerializerOptions options)
    {
        writer.Write(value.ID + 1);
        writer.Write(value.Name + "Mock");
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
public partial class CustomFormatterGenericClass<T> : ITinyhandSerialize<CustomFormatterGenericClass<T>>
{
    public int ID { get; set; }

    public T TValue { get; set; }

    public static void Deserialize(ref TinyhandReader reader, scoped ref CustomFormatterGenericClass<T>? value, TinyhandSerializerOptions options)
    {
        value ??= new CustomFormatterGenericClass<T>();

        if (!reader.TryReadNil())
        {
            value.ID = reader.ReadInt32();
        }

        if (!reader.TryReadNil())
        {
            value.TValue = options.Resolver.GetFormatter<T>().Deserialize(ref reader, options);
        }
        else
        {
            value.TValue = options.Resolver.GetFormatter<T>().Reconstruct(options);
        }
    }

    public static void Serialize(ref TinyhandWriter writer, scoped ref CustomFormatterGenericClass<T>? value, TinyhandSerializerOptions options)
    {
        writer.Write(value.ID + 1);
        options.Resolver.GetFormatter<T>().Serialize(ref writer, value.TValue, options);
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
