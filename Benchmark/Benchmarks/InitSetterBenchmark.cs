// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Linq.Expressions;
using BenchmarkDotNet.Attributes;
using FastExpressionCompiler;
using Tinyhand;

namespace Benchmark.InitOnly;

[TinyhandObject]
public partial class NormalIntClass
{
    [Key(0)]
    public int X { get; set; }

    [Key(1)]
    public int Y { get; set; }

    [Key(2)]
    public string A { get; set; } = default!;

    [Key(3)]
    public string B { get; set; } = default!;

    public NormalIntClass(int x, int y, string a, string b)
    {
        this.X = x;
        this.Y = y;
        this.A = a;
        this.B = b;
    }

    public NormalIntClass()
    {
    }
}

[TinyhandObject]
[MessagePack.MessagePackObject]
public partial class InitIntClass
{
    [Key(0)]
    [MessagePack.Key(0)]
    public int X { get; init; }

    [Key(1)]
    [MessagePack.Key(1)]
    public int Y { get; init; }

    [Key(2)]
    [MessagePack.Key(2)]
    public string A { get; init; } = default!;

    [Key(3)]
    [MessagePack.Key(3)]
    public string B { get; init; } = default!;

    public InitIntClass(int x, int y, string a, string b)
    {
        this.X = x;
        this.Y = y;
        this.A = a;
        this.B = b;
    }

    public InitIntClass()
    {
    }
}

[TinyhandObject(ImplicitKeyAsName = true)]
public partial record RecordClass(int X, int Y, string A, string B);

[TinyhandObject(ImplicitKeyAsName = true)]
public partial class RecordClass2
{
    public int X { get; init; }

    public int Y { get; init; }

    public string A { get; init; } = default!;

    public string B { get; init; } = default!;

    public RecordClass2()
    {
    }
}

[Config(typeof(BenchmarkConfig))]
public class InitOnlyBenchmark
{
    private NormalIntClass normalInt = default!;
    private byte[] normalIntByte = default!;
    private InitIntClass initInt = default!;
    private byte[] initIntByte = default!;
    private RecordClass recordClass = default!;
    private byte[] recordClassByte = default!;

    private readonly Action<InitIntClass, int> setDelegate;
    private readonly Action<InitIntClass, int> setDelegateFast;
    private readonly Action<InitIntClass, int> setDelegate2;

    public InitOnlyBenchmark()
    {
        var d = this.CreateDelegate2();
        var c = new InitIntClass(1, 2, "a", "b");
        d(c, 33);

        this.setDelegate = this.CreateDelegate();
        this.setDelegateFast = this.CreateDelegateFast();
        this.setDelegate2 = this.CreateDelegate2();
    }

    [GlobalSetup]
    public void Setup()
    {
        this.normalInt = new(1, 2, "A", "B");
        this.normalIntByte = TinyhandSerializer.Serialize(this.normalInt);
        this.initInt = new(1, 2, "A", "B");
        this.initIntByte = TinyhandSerializer.Serialize(this.initInt);
        this.recordClass = new RecordClass(1, 2, "A", "B");
        this.recordClassByte = TinyhandSerializer.Serialize(this.recordClass);
    }

    [Benchmark]
    public Action<InitIntClass, int> CreateDelegate()
    {
        var type = typeof(InitIntClass);
        var expType = Expression.Parameter(type);
        var mi = type.GetMethod("set_X")!;
        var exp = Expression.Parameter(typeof(int));
        return Expression.Lambda<Action<InitIntClass, int>>(Expression.Call(expType, mi!, exp), expType, exp).Compile();
    }

    [Benchmark]
    public Action<InitIntClass, int> CreateDelegateFast()
    {
        var type = typeof(InitIntClass);
        var expType = Expression.Parameter(type);
        var mi = type.GetMethod("set_X")!;
        var exp = Expression.Parameter(typeof(int));
        return Expression.Lambda<Action<InitIntClass, int>>(Expression.Call(expType, mi!, exp), expType, exp).CompileFast();
    }

    [Benchmark]
    public Action<InitIntClass, int> CreateDelegate2()
    {
        var mi = typeof(InitIntClass).GetProperty("X")!.GetSetMethod(true)!;
        return (Action<InitIntClass, int>)Delegate.CreateDelegate(typeof(Action<InitIntClass, int>), mi);
    }

    [Benchmark]
    public InitIntClass InvokeDelegate()
    {
        this.setDelegate(this.initInt, 999);
        return this.initInt;
    }

    [Benchmark]
    public InitIntClass InvokeDelegateFast()
    {
        this.setDelegateFast(this.initInt, 999);
        return this.initInt;
    }

    [Benchmark]
    public InitIntClass InvokeDelegate2()
    {
        this.setDelegate2(this.initInt, 999);
        return this.initInt;
    }

    [Benchmark]
    public NormalIntClass? DeserializeNormalInt()
    {
        return TinyhandSerializer.Deserialize<NormalIntClass>(this.normalIntByte);
    }

    [Benchmark]
    public InitIntClass? DeserializeInitInt()
    {
        return TinyhandSerializer.Deserialize<InitIntClass>(this.initIntByte);
    }

    [Benchmark]
    public RecordClass? DeserializeRecord()
    {
        return TinyhandSerializer.Deserialize<RecordClass>(this.recordClassByte);
    }

    [Benchmark]
    public RecordClass2? DeserializeRecord2()
    {
        return TinyhandSerializer.Deserialize<RecordClass2>(this.recordClassByte);
    }
}
