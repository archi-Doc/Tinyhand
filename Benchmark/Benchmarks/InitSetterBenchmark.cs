// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
// using FastExpressionCompiler;
using Tinyhand;

#pragma warning disable CS0169

namespace Benchmark.InitOnly;

public delegate void TestByRefAction<T1, T2>(ref T1 arg1, T2 arg2);

public delegate void TestByInAction<T1, T2>(in T1 arg1, T2 arg2);

public delegate T2 TestByRefFunc<T1, T2>(ref T1 arg1);

public delegate ref V TestStructRefField<T, V>(in T obj);

public delegate ref V TestClassRefField<T, V>(T obj);

public static class VisceralHelper
{
    public const BindingFlags TargetBindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;

    public static TestStructRefField<T, V> CreateStructRefField<T, V>(string fieldName)
        where T : struct
    {
        var fieldInfo = typeof(T).GetField(fieldName, TargetBindingFlags)!;
        var name = "__refget_" + typeof(T).Name + "_fi_" + fieldInfo.Name;
        var method = new DynamicMethod(name, typeof(V).MakeByRefType(), [typeof(T).MakeByRefType(),], typeof(T), true);

        var il = method.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldflda, fieldInfo);
        il.Emit(OpCodes.Ret);

        return (TestStructRefField<T, V>)method.CreateDelegate(typeof(TestStructRefField<T, V>));
    }

    public static TestClassRefField<T, V> CreateClassRefField<T, V>(string fieldName)
        where T : class
    {
        var fieldInfo = typeof(T).GetField(fieldName, TargetBindingFlags)!;
        var name = "__refget_" + typeof(T).Name + "_fi_" + fieldInfo.Name;
        var method = new DynamicMethod(name, typeof(V).MakeByRefType(), [typeof(T),], typeof(T), true);

        var il = method.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldflda, fieldInfo);
        il.Emit(OpCodes.Ret);

        return (TestClassRefField<T, V>)method.CreateDelegate(typeof(TestClassRefField<T, V>));
    }
}

public struct DesignStruct
{
    private int X { get; set; }

    private int Y;

    private readonly int Z;

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "Y")]
    public static extern ref int RefY(in DesignStruct obj);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "Z")]
    public static extern ref int RefZ(in DesignStruct obj);
}

public class DesignBaseClass
{
    protected DesignBaseClass()
    {
    }

    private int X { get; set; }

    private int Y;

    private readonly int Z;

    [Key(0)]
    public int A { get; set; }
}

[TinyhandObject]
public partial class DesignDerivedClass : DesignBaseClass
{
    public static DesignDerivedClass New()
        => new();

    protected DesignDerivedClass()
    {
    }

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "Y")]
    public static extern ref int BaseY(DesignBaseClass obj);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "Z")]
    public static extern ref int BaseZ(DesignBaseClass obj);
}

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
    public string A { get; set; } = string.Empty;

    [Key(3)]
    [MessagePack.Key(3)]
    public string B { get; set; } = string.Empty;

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

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "<X>k__BackingField")]
    public static extern ref int UnsafeX(InitIntClass obj);
}

[TinyhandObject]
[MessagePack.MessagePackObject]
public partial struct InitIntStruct
{
    [Key(0)]
    [MessagePack.Key(0)]
    public int X { get; init; }

    [Key(1)]
    [MessagePack.Key(1)]
    public int Y { get; init; }

    [Key(2)]
    [MessagePack.Key(2)]
    public string A { get; set; } = string.Empty;

    [Key(3)]
    [MessagePack.Key(3)]
    public string B { get; set; } = string.Empty;

    public InitIntStruct(int x, int y, string a, string b)
    {
        this.X = x;
        this.Y = y;
        this.A = a;
        this.B = b;
    }

    public InitIntStruct()
    {
    }
}

[TinyhandObject]
public partial class PrivateIntClass
{
    [Key(0)]
    private int X { get; set; }

    [Key(1)]
    private int Y;

    [Key(2)]
    private string A = default!;

    [Key(3)]
    private string B = default!;

    public PrivateIntClass(int x, int y, string a, string b)
    {
        this.X = x;
        this.Y = y;
        this.A = a;
        this.B = b;
    }

    public PrivateIntClass()
    {
    }
}

[TinyhandObject]
public partial class DerivedPrivateIntClass : PrivateIntClass
{
    public DerivedPrivateIntClass(int x, int y, string a, string b)
        : base(x, y, a, b)
    {
    }

    public DerivedPrivateIntClass()
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

    private static class __visceral__
    {
        static __visceral__()
        {
        }
    }
}

[Config(typeof(BenchmarkConfig))]
public class InitOnlyBenchmark
{
    private NormalIntClass normalInt = default!;
    private byte[] normalIntByte = default!;
    private InitIntClass initInt = default!;
    private byte[] initIntByte = default!;
    private byte[] privateIntByte = default!;
    private RecordClass recordClass = default!;
    private byte[] recordClassByte = default!;
    private InitIntStruct initIntStruct;

    private readonly Action<InitIntClass, int> setDelegate;
    // private readonly Action<InitIntClass, int> setDelegateFast;
    private readonly Action<InitIntClass, int> setDelegate2;

    private readonly TestByInAction<InitIntStruct, int> setStructDelegate;
    private readonly TestByRefAction<InitIntStruct, int> setStructDelegate2;

    private readonly DesignStruct designStruct;
    private readonly DesignDerivedClass designDerivedClass;
    private readonly TestByInAction<DesignStruct, int> designStructSetter;
    private readonly Action<DesignBaseClass, int> designClassSetter;
    private readonly TestStructRefField<DesignStruct, int> structRefField;
    private readonly TestClassRefField<DesignBaseClass, int> classRefField;

    unsafe public InitOnlyBenchmark()
    {
        var designStruct = new DesignStruct();
        this.designStruct = designStruct;
        var designDerivedClass = DesignDerivedClass.New();
        this.designDerivedClass = designDerivedClass;

        var setter1 = (TestByInAction<DesignStruct, int>)Delegate.CreateDelegate(typeof(TestByInAction<DesignStruct, int>), typeof(DesignStruct).GetProperty("X", VisceralHelper.TargetBindingFlags)!.GetSetMethod(true)!);
        this.designStructSetter = setter1;
        var getter1 = (TestByRefFunc<DesignStruct, int>)Delegate.CreateDelegate(typeof(TestByRefFunc<DesignStruct, int>), typeof(DesignStruct).GetProperty("X", VisceralHelper.TargetBindingFlags)!.GetGetMethod(true)!);

        int v;
        setter1(in designStruct, 1);
        v = getter1(ref designStruct);

        var structRefField = VisceralHelper.CreateStructRefField<DesignStruct, int>("Y");
        v = structRefField(in designStruct);
        structRefField(in designStruct) = 2;
        v = structRefField(in designStruct);
        structRefField = VisceralHelper.CreateStructRefField<DesignStruct, int>("Z");
        v = structRefField(in designStruct);
        structRefField(in designStruct) = 3;
        v = structRefField(in designStruct);

        DesignStruct.RefY(in designStruct) = 10;
        DesignStruct.RefZ(in designStruct) = 20;

        var setter2 = (Action<DesignBaseClass, int>)Delegate.CreateDelegate(typeof(Action<DesignBaseClass, int>), typeof(DesignBaseClass).GetProperty("X", VisceralHelper.TargetBindingFlags)!.GetSetMethod(true)!);
        var getter2 = (Func<DesignBaseClass, int>)Delegate.CreateDelegate(typeof(Func<DesignBaseClass, int>), typeof(DesignBaseClass).GetProperty("X", VisceralHelper.TargetBindingFlags)!.GetGetMethod(true)!);

        setter2(designDerivedClass, 1);
        v = getter2(designDerivedClass);

        var classRefField = VisceralHelper.CreateClassRefField<DesignBaseClass, int>("Y");
        v = classRefField(designDerivedClass);
        classRefField(designDerivedClass) = 2;
        v = classRefField(designDerivedClass);
        classRefField = VisceralHelper.CreateClassRefField<DesignBaseClass, int>("Z");
        v = classRefField(designDerivedClass);
        classRefField(designDerivedClass) = 3;
        v = classRefField(designDerivedClass);

        DesignDerivedClass.BaseY(designDerivedClass) = 4;
        v = DesignDerivedClass.BaseY(designDerivedClass);
        DesignDerivedClass.BaseZ(designDerivedClass) = 5;
        v = DesignDerivedClass.BaseZ(designDerivedClass);

        this.designStructSetter = (TestByInAction<DesignStruct, int>)Delegate.CreateDelegate(typeof(TestByInAction<DesignStruct, int>), typeof(DesignStruct).GetProperty("X", VisceralHelper.TargetBindingFlags)!.GetSetMethod(true)!);
        this.structRefField = VisceralHelper.CreateStructRefField<DesignStruct, int>("Y");
        this.designClassSetter = (Action<DesignBaseClass, int>)Delegate.CreateDelegate(typeof(Action<DesignBaseClass, int>), typeof(DesignBaseClass).GetProperty("X", VisceralHelper.TargetBindingFlags)!.GetSetMethod(true)!);
        this.classRefField = VisceralHelper.CreateClassRefField<DesignBaseClass, int>("Y");

        this.normalInt = new(1, 2, "A", "B");
        this.normalIntByte = TinyhandSerializer.Serialize(this.normalInt);
        this.initInt = new(1, 2, "A", "B");
        this.initIntByte = TinyhandSerializer.Serialize(this.initInt);
        this.recordClass = new RecordClass(1, 2, "A", "B");
        this.recordClassByte = TinyhandSerializer.Serialize(this.recordClass);
        var derivedPrivateIntClass = new DerivedPrivateIntClass(1, 2, "A", "B");
        this.privateIntByte = TinyhandSerializer.Serialize(derivedPrivateIntClass);

        var d = this.CreateDelegate2();
        var c = new InitIntClass(1, 2, "a", "b");
        d(c, 33);

        InitIntClass.UnsafeX(c) = 99;

        this.setDelegate = this.CreateDelegate();
        // this.setDelegateFast = this.CreateDelegateFast();
        this.setDelegate2 = this.CreateDelegate2();

        this.initIntStruct = new InitIntStruct(1, 2, "a", "b");
        this.setStructDelegate = this.CreateStructDelegate();
        this.setStructDelegate2 = this.CreateStructDelegate2();
    }

    [GlobalSetup]
    public void Setup()
    {
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

    /*[Benchmark]
    public Action<InitIntClass, int> CreateDelegateFast()
    {
        var type = typeof(InitIntClass);
        var expType = Expression.Parameter(type);
        var mi = type.GetMethod("set_X")!;
        var exp = Expression.Parameter(typeof(int));
        return Expression.Lambda<Action<InitIntClass, int>>(Expression.Call(expType, mi!, exp), expType, exp).CompileFast();
    }*/

    [Benchmark]
    public Action<InitIntClass, int> CreateDelegate2()
    {
        var mi = typeof(InitIntClass).GetProperty("X", VisceralHelper.TargetBindingFlags)!.GetSetMethod(true)!;
        return (Action<InitIntClass, int>)Delegate.CreateDelegate(typeof(Action<InitIntClass, int>), mi);
    }

    [Benchmark]
    public TestByInAction<InitIntStruct, int> CreateStructDelegate()
    {
        var mi = typeof(InitIntStruct).GetProperty("X", VisceralHelper.TargetBindingFlags)!.GetSetMethod(true)!;
        return (TestByInAction<InitIntStruct, int>)Delegate.CreateDelegate(typeof(TestByInAction<InitIntStruct, int>), mi);
    }

    [Benchmark]
    public TestByRefAction<InitIntStruct, int> CreateStructDelegate2()
    {
        var mi = typeof(InitIntStruct).GetProperty("X", VisceralHelper.TargetBindingFlags)!.GetSetMethod(true)!;
        return (TestByRefAction<InitIntStruct, int>)Delegate.CreateDelegate(typeof(TestByRefAction<InitIntStruct, int>), mi);
    }

    /*[Benchmark]
    public TestByRefAction<DesignStruct, int> CreateDesignStructSetter()
        => (TestByRefAction<DesignStruct, int>)Delegate.CreateDelegate(typeof(TestByRefAction<DesignStruct, int>), typeof(DesignStruct).GetProperty("X", VisceralHelper.TargetBindingFlags)!.GetSetMethod(true)!);

    [Benchmark]
    public Action<DesignBaseClass, int> CreateDesignClassSetter()
        => (Action<DesignBaseClass, int>)Delegate.CreateDelegate(typeof(Action<DesignBaseClass, int>), typeof(DesignBaseClass).GetProperty("X", VisceralHelper.TargetBindingFlags)!.GetSetMethod(true)!);

    [Benchmark]
    public TestStructRefField<DesignStruct, int> CreateStructRefField()
        => VisceralHelper.CreateStructRefField<DesignStruct, int>("Y");

    [Benchmark]
    public TestClassRefField<DesignBaseClass, int> CreateClassRefField()
        => VisceralHelper.CreateClassRefField<DesignBaseClass, int>("Y");*/

    /*[Benchmark]
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
    }*/

    /*[Benchmark]
    public int InvokeStructDelegate()
    {
        this.setStructDelegate(in this.initIntStruct, 999);
        return this.initIntStruct.X;
    }

    [Benchmark]
    public int InvokeStructDelegate2()
    {
        this.setStructDelegate2(ref this.initIntStruct, 999);
        return this.initIntStruct.X;
    }

    [Benchmark]
    public void InvokeStructSetter()
    {
        this.designStructSetter(in this.designStruct, 999);
    }

    [Benchmark]
    public DesignDerivedClass InvokeClassSetter()
    {
        this.designClassSetter(this.designDerivedClass, 999);
        return this.designDerivedClass;
    }

    [Benchmark]
    public int InvokeStructRefGet()
    {
        return this.structRefField(in this.designStruct);
    }

    [Benchmark]
    public int InvokeClassRefGet()
    {
        return this.classRefField(this.designDerivedClass);
    }

    [Benchmark]
    public void InvokeStructRefSet()
    {
        this.structRefField(in this.designStruct) = 99;
    }

    [Benchmark]
    public DesignDerivedClass InvokeClassRefSet()
    {
        this.classRefField(this.designDerivedClass) = 99;
        return this.designDerivedClass;
    }*/

    [Benchmark]
    public NormalIntClass? DeserializeNormalInt()
    {// 33
        return TinyhandSerializer.Deserialize<NormalIntClass>(this.normalIntByte);
    }

    [Benchmark]
    public InitIntClass? DeserializeInitInt()
    {// 37
        return TinyhandSerializer.Deserialize<InitIntClass>(this.initIntByte);
    }

    [Benchmark]
    public RecordClass? DeserializeRecord()
    {// 48
        return TinyhandSerializer.Deserialize<RecordClass>(this.recordClassByte);
    }

    [Benchmark]
    public RecordClass2? DeserializeRecord2()
    {// 48
        return TinyhandSerializer.Deserialize<RecordClass2>(this.recordClassByte);
    }

    [Benchmark]
    public DerivedPrivateIntClass? DeserializeDerivedPrivateInt()
    {// 33
        return TinyhandSerializer.Deserialize<DerivedPrivateIntClass>(this.privateIntByte);
    }
}
