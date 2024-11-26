// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using BenchmarkDotNet.Attributes;
using Tinyhand;

namespace Benchmark.Union2;

[TinyhandUnion(0, typeof(Class0))]
[TinyhandUnion(1, typeof(Class1))]
[TinyhandUnion(2, typeof(Class2))]
[TinyhandUnion(3, typeof(Class3))]
[TinyhandUnion(4, typeof(Class4))]
[TinyhandUnion(5, typeof(Class5))]
[TinyhandUnion(6, typeof(Class6))]
[TinyhandUnion(7, typeof(Class7))]
[TinyhandUnion(8, typeof(Class8))]
[TinyhandUnion(9, typeof(Class9<int>))]
public partial interface IUnion
{
}

[TinyhandObject]
public partial class BaseClass
{
    [Key(0)]
    public int X { get; set; }
}

[TinyhandObject]
public partial class Class0 : BaseClass, IUnion
{
}

[TinyhandObject]
public partial class Class1 : BaseClass, IUnion
{
}

[TinyhandObject]
public partial class Class2 : BaseClass, IUnion
{
}

[TinyhandObject]
public partial class Class3 : BaseClass, IUnion
{
}

[TinyhandObject]
public partial class Class4 : BaseClass, IUnion
{
}

[TinyhandObject]
public partial class Class5 : BaseClass, IUnion
{
}

[TinyhandObject]
public partial class Class6 : BaseClass, IUnion
{
}

[TinyhandObject]
public partial class Class7 : BaseClass, IUnion
{
}

[TinyhandObject]
public partial class Class8 : BaseClass, IUnion
{
}

[TinyhandObject]
public partial class Class9<T> : BaseClass, IUnion
{
    public Class9()
    {
        this.Value = default!;
    }

    public Class9(T value)
    {
        this.Value = value;
    }

    [Key(1)]
    public T Value { get; set; }

    // private static ulong identifier;

    // public static ulong GetTypeIdentifier() => identifier != 0 ? identifier : (identifier = Arc.Crypto.FarmHash.Hash64(typeof(Class9<T>).Name!));
}

[Config(typeof(BenchmarkConfig))]
public class UnionBenchmark2
{
    private IUnion union;
    private byte[] bytes;

    public UnionBenchmark2()
    {
        this.union = (IUnion)new Class9<int>(2);
        this.bytes = TinyhandSerializer.Serialize(this.union);
        var u = TinyhandSerializer.DeserializeObject<IUnion>(this.bytes);
    }

    [GlobalSetup]
    public void Setup()
    {
    }

    [Benchmark]
    public byte[] SerializeUnion()
    {
        return TinyhandSerializer.SerializeObject(this.union);
    }

    [Benchmark]
    public IUnion? DeserializeUnion()
    {
        return TinyhandSerializer.DeserializeObject<IUnion>(this.bytes);
    }
}
