﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.ComponentModel;
using Arc.Crypto;
using BenchmarkDotNet.Attributes;
using Tinyhand;
using Tinyhand.IO;

namespace Benchmark.Generics;

[TinyhandObject]
[MessagePack.MessagePackObject]
public partial class GenericsIntClass<T>
{
    [Key(0)]
    [MessagePack.Key(0)]
    public T X { get; set; } = default!;

    [Key(1)]
    [MessagePack.Key(1)]
    public T Y { get; set; } = default!;

    [Key(2)]
    [MessagePack.Key(2)]
    public T[] A { get; set; } = default!;


    public GenericsIntClass(T x, T y, T[] a)
    {
        this.X = x;
        this.Y = y;
        this.A = a;
    }

    public GenericsIntClass()
    {
    }
}

[TinyhandObject]
[MessagePack.MessagePackObject]
public partial class NormalIntClass
{
    [Key(0)]
    [MessagePack.Key(0)]
    public int X { get; set; }

    [Key(1)]
    [DefaultValue(22)]
    [MessagePack.Key(1)]
    public int Y { get; set; }

    [Key(2)]
    [MessagePack.Key(2)]
    public int[] A { get; set; } = default!;


    public NormalIntClass(int x, int y, int[] a)
    {
        this.X = x;
        this.Y = y;
        this.A = a;
    }

    public NormalIntClass()
    {
    }
}

[TinyhandObject]
public partial class CustomIntClass : ITinyhandSerializable<CustomIntClass>
{
    public int X { get; set; }

    public int Y { get; set; }

    public int[] A { get; set; } = default!;


    public CustomIntClass(int x, int y, int[] a)
    {
        this.X = x;
        this.Y = y;
        this.A = a;
    }

    public CustomIntClass()
    {
    }

    public static void Serialize(ref TinyhandWriter writer, scoped ref CustomIntClass? value, TinyhandSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNil();
            return;
        }

        writer.WriteArrayHeader(3);
        options.Resolver.GetFormatter<int>().Serialize(ref writer, value.X, options);
        options.Resolver.GetFormatter<int>().Serialize(ref writer, value.Y, options);
        options.Resolver.GetFormatter<int[]>().Serialize(ref writer, value.A, options);
    }

    public static void Deserialize(ref TinyhandReader reader, scoped ref CustomIntClass? value, TinyhandSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
            value = default;
            return;
        }

        value ??= new();
        var numberOfData = reader.ReadArrayHeader();
        options.Security.DepthStep(ref reader);
        try
        {
            if (numberOfData-- > 0 && !reader.TryReadNil())
            {
                value.X = options.Resolver.GetFormatter<int>().Deserialize(ref reader, options);
            }
            if (numberOfData-- > 0 && !reader.TryReadNil())
            {
                value.Y = options.Resolver.GetFormatter<int>().Deserialize(ref reader, options);
            }
            if (numberOfData-- > 0 && !reader.TryReadNil())
            {
                value.A = options.Resolver.GetFormatter<int[]>().Deserialize(ref reader, options)!;
            }
            else
            {
                value.A = new int[0];
            }
            while (numberOfData-- > 0) reader.Skip();
        }
        finally { reader.Depth--; }
    }

    public static ulong GetTypeIdentifier() => FarmHash.Hash64(typeof(CustomIntClass).FullName ?? string.Empty);
}

[TinyhandObject(ImplicitKeyAsName = true)]
[MessagePack.MessagePackObject(true)]
public partial class GenericsStringClass<T>
{
    public T X { get; set; } = default!;

    public T Y { get; set; } = default!;

    public T[] A { get; set; } = default!;

    public GenericsStringClass(T x, T y, T[] a)
    {
        this.X = x;
        this.Y = y;
        this.A = a;
    }

    public GenericsStringClass()
    {
    }
}

[TinyhandObject(ImplicitKeyAsName = true)]
[MessagePack.MessagePackObject(true)]
public partial class NormalStringClass
{
    public int X { get; set; }

    [DefaultValue(32)]
    public int Y { get; set; }

    public int[] A { get; set; } = default!;


    public NormalStringClass(int x, int y, int[] a)
    {
        this.X = x;
        this.Y = y;
        this.A = a;
    }

    public NormalStringClass()
    {
    }
}

public class NonSerializeClass
{
}

// [TinyhandObject]
public partial record NormalIntRecord(int X, int Y, string A, string B);

[Config(typeof(BenchmarkConfig))]
public class GenericsBenchmark
{
    private int[] intArray = default!;

    private byte[] intByte = default!;
    private byte[] intByteMp = default!;

    private byte[] stringByte = default!;
    private byte[] stringByteMp = default!;


    public GenericsBenchmark()
    {
    }

    [GlobalSetup]
    public void Setup()
    {
        this.intArray = new int[] { 0, 1, -1, 2, -2, 3, -30, 400, 5000, 10000, -20000, 50000 };
        this.intByte = TinyhandSerializer.Serialize(new GenericsIntClass<int>(10, 200, this.intArray));
        this.stringByte = TinyhandSerializer.Serialize(new GenericsStringClass<int>(10, 200, this.intArray));
        this.intByteMp = MessagePack.MessagePackSerializer.Serialize(new GenericsIntClass<int>(10, 200, this.intArray));
        this.stringByteMp = MessagePack.MessagePackSerializer.Serialize(new GenericsStringClass<int>(10, 200, this.intArray));

        // this.intByte = TinyhandSerializer.Serialize(new GenericsIntClass<NonSerializeClass>());
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
        var c = (GenericsIntClass<double>)Activator.CreateInstance(typeof(GenericsIntClass<>).MakeGenericType(new Type[] { typeof(double), }));
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
        var st = TinyhandSerializer.SerializeToString(c);
    }

    [Benchmark]
    public byte[] Serialize_NormalInt_Tinyhand()
    {
        return TinyhandSerializer.Serialize(new NormalIntClass(10, 200, this.intArray));
    }

    [Benchmark]
    public byte[] Serialize_CustomInt_Tinyhand()
    {
        return TinyhandSerializer.Serialize(new CustomIntClass(10, 200, this.intArray));
    }

    [Benchmark]
    public byte[] Serialize_NormalInt_MessagePack()
    {
        return MessagePack.MessagePackSerializer.Serialize(new NormalIntClass(10, 200, this.intArray));
    }

    [Benchmark]
    public byte[] Serialize_GenericsInt_Tinyhand()
    {
        return TinyhandSerializer.Serialize(new GenericsIntClass<int>(10, 200, this.intArray));
    }

    [Benchmark]
    public byte[] Serialize_GenericsInt_MessagePack()
    {
        return MessagePack.MessagePackSerializer.Serialize(new GenericsIntClass<int>(10, 200, this.intArray));
    }

    [Benchmark]
    public byte[] Serialize_NormalString_Tinyhand()
    {
        return TinyhandSerializer.Serialize(new NormalStringClass(10, 200, this.intArray));
    }

    [Benchmark]
    public byte[] Serialize_NormalString_MessagePack()
    {
        return MessagePack.MessagePackSerializer.Serialize(new NormalStringClass(10, 200, this.intArray));
    }

    [Benchmark]
    public byte[] Serialize_GenericsString_Tinyhand()
    {
        return TinyhandSerializer.Serialize(new GenericsStringClass<int>(10, 200, this.intArray));
    }

    [Benchmark]
    public byte[] Serialize_GenericsString_MessagePack()
    {
        return MessagePack.MessagePackSerializer.Serialize(new GenericsStringClass<int>(10, 200, this.intArray));
    }

    [Benchmark]
    public NormalIntClass? Deserialize_NormalInt_Tinyhand()
    {
        return TinyhandSerializer.Deserialize<NormalIntClass>(this.intByte);
    }

    [Benchmark]
    public CustomIntClass? Deserialize_CustomInt_Tinyhand()
    {
        return TinyhandSerializer.Deserialize<CustomIntClass>(this.intByte);
    }

    [Benchmark]
    public GenericsIntClass<int>? Deserialize_GenericsInt_Tinyhand()
    {
        return TinyhandSerializer.Deserialize<GenericsIntClass<int>>(this.intByte);
    }

    [Benchmark]
    public NormalStringClass? Deserialize_NormalString_Tinyhand()
    {
        return TinyhandSerializer.Deserialize<NormalStringClass>(this.stringByte);
    }

    [Benchmark]
    public GenericsStringClass<int>? Deserialize_GenericsString_Tinyhand()
    {
        return TinyhandSerializer.Deserialize<GenericsStringClass<int>>(this.stringByte);
    }
}
