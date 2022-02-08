using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using DryIoc;
using Tinyhand;
using Tinyhand.IO;

#pragma warning disable CS0414
#pragma warning disable CS0169

namespace Sandbox;

[TinyhandObject]
public partial class IdentifierReadonlyClass
{
    [Key(0)]
    readonly IntPayload2 Identifier;

    [Key(1)]
    readonly IntPayload2 Identifier2;
}

[TinyhandObject]
public partial class SerializableClass<T> : ISerializableInterface
{
    /*public void Deserialize(ref TinyhandReader reader, TinyhandSerializerOptions options)
    {
        // throw new NotImplementedException();
    }

    public void Serialize(ref TinyhandWriter writer, TinyhandSerializerOptions options)
    {
        // throw new NotImplementedException();
    }*/

    [Key(0)]
    public int Id { get; set; }

    [Key(1)]
    public T Value { get; set; } = default!;
}

[TinyhandUnion(0, typeof(SerializableClass<int>))]
public interface ISerializableInterface : ITinyhandSerialize
{
}

[TinyhandObject(ImplicitKeyAsName = true)]
public partial record class RecordClassData1(int Data1);

public interface INestedStructClass
{
}

public interface INestedStructClass2
{
}

public partial class NestedStructClass<T, U>
    where T : struct
    where U : class, INestedStructClass2
{
    internal static Type ist => typeof(Item);

    [TinyhandObject]
    private sealed partial class Item
    {
        public Item(int key, T value)
        {
            this.Key = key;
            this.Value = value;
        }

        public Item()
        {
        }

        [Key(0)]
        internal T Value;

        [Key(1)]
        internal int Key;
    }

    public NestedStructClass()
    {
    }

    // public List<Item> Items { get; } = new();
}

[TinyhandObject]
public partial class InheritanceTestBase<T>
{
    [Key(0)]
    internal int InternalInt = 0;

    [Key(1)]
    private int PrivateInt = 1;

    [Key(2)]
    public int PublicPublic { get; set; } = 2;

    [Key(3)]
    public int PublicProtected { get; protected set; } = 3;

    [Key(4)]
    protected int PrivateProtected { private get; set; } = 4;

    [Key(5)]
    public int PublicPrivate { get; private set; } = 5;

    [Key(6)]
    private T PrivatePrivate { get; set; } = default!;

    [Key(9)]
    private ConsoleApp1.MyClass? pp = default!;


    public InheritanceTestBase()
    {
    }

    public void Clear()
    {
        this.InternalInt = 0;
        this.PrivateInt = 0;
        this.PublicPublic = 0;
        this.PublicProtected = 0;
        this.PrivateProtected = 0;
        this.PublicPrivate = 0;
        this.PrivatePrivate = default!;
    }
}

[TinyhandObject]
public partial class InternalTestClass2<T> : InheritanceTestBase<T> // ConsoleApp1.InternalTestClass
{
    [Key(7)]
    internal int InternalInt2 = 3;

    [Key(8)]
    private int PrivateInt2 = 4;

    public InternalTestClass2()
    {
    }

    public void Clear2()
    {
        this.InternalInt2 = 0;
        this.PrivateInt2 = 0;
    }

    public static class __identifier
    {
        static __identifier()
        {
            var exp = Expression.Parameter(typeof(InheritanceTestBase<T>));
            var exp2 = Expression.Parameter(typeof(T));
            setterDelegate = Expression.Lambda<Action< InternalTestClass2<T>, T>>(Expression.Assign(Expression.PropertyOrField(exp, "PrivatePrivate"), exp2), exp, exp2).Compile();
        }

        public static Action<InternalTestClass2<T>, T> setterDelegate;
    }
}

[TinyhandObject]
partial class GenericsImplementedClass : ConsoleApp1.IItzPayload
{
    public GenericsImplementedClass()
    {
        this.Data = 0;
    }

    public GenericsImplementedClass(int data)
    {
        this.Data = data;
    }

    [Key(0)]
    public int Data;
}

[TinyhandObject]
partial struct GenericsImplementedStruct : ConsoleApp1.IItzPayload
{
}

[TinyhandObject(ImplicitKeyAsName = true)]
public partial record struct IntPayload2(int Data2) : ConsoleApp1.IItzPayload;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Sandbox");
        Console.WriteLine();

        var gtc = new ConsoleApp1.ItzShip<GenericsImplementedClass>();
        var gtc2 = new ConsoleApp1.ItzShip<GenericsImplementedStruct>();
        var gtc3 = new ConsoleApp1.ItzShip<IntPayload2>();

        var sc = new SerializableClass<int>();
        var dictionary = new Dictionary<uint, ISerializableInterface>();
        dictionary[12346] = sc;
        var ba = TinyhandSerializer.Serialize(dictionary);

        /*var t = typeof(InternalTestBase);
        var field = t.GetField("PrivateInt", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var targetExp = Expression.Parameter(t);
        var valueExp = Expression.Parameter(typeof(int));
        var fieldExp = Expression.Field(targetExp, field);
        var assignExp = Expression.Assign(fieldExp, valueExp);
        var setter = Expression.Lambda<Action<InternalTestClass2, int>>(assignExp, targetExp, valueExp).Compile();*/

        /*var t = typeof(InternalTestBase);
        var field = t.GetField("PrivateInt", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var exp = Expression.Parameter(t);
        var exp2 = Expression.Parameter(typeof(int));
        var setter = Expression.Lambda<Action<InternalTestClass2, int>>(Expression.Assign(Expression.Field(exp, "PrivateInt"), exp2), exp, exp2).Compile();

        var subject = new InternalTestClass2();
        setter(subject, 333);

        var getter = Expression.Lambda<Func<InternalTestClass2, int>>(Expression.Field(exp, field), exp).Compile();

        var aa = getter(subject);*/

        /*var b = new InternalTestClass2<double>();
        // var field = t.GetField("PrivateProtected", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var targetExp = Expression.Parameter(typeof(InternalTestClass2<double>));
        var valueExp = Expression.Parameter(typeof(int));
        var fieldExp = Expression.PropertyOrField(targetExp, "PrivateProtected");
        var setter = Expression.Lambda<Action<InternalTestClass2<double>, int>>(Expression.Assign(fieldExp, valueExp), targetExp, valueExp).Compile();

        setter(b, 444);

        var getter = Expression.Lambda<Func<InternalTestClass2<double>, int>>(Expression.PropertyOrField(Expression.Convert(targetExp, typeof(InheritanceTestBase)), "PrivateProtected"), targetExp).Compile();

        var c = getter(b);*/

        // var ty = typeof(NestedStructClass<,>.Item); // Cannot access Item...
        var ty = NestedStructClass<int, INestedStructClass2>.ist; // Cannot get NestedStructClass<,>.ist...

        var b = new InternalTestClass2<double>();
        InternalTestClass2<double>.__identifier.setterDelegate(b, 4.44);

        var a = new ConsoleApp1.InternalTestClass();
        var a2 = TinyhandSerializer.Deserialize<ConsoleApp1.InternalTestClass>(TinyhandSerializer.Serialize(a));
        TinyhandSerializer.TryDeserialize<ConsoleApp1.InternalTestClass>(TinyhandSerializer.Serialize(a), out var a3);

        b = new InternalTestClass2<double>();
        var b2 = TinyhandSerializer.Deserialize<InternalTestClass2<double>>(TinyhandSerializer.Serialize(b));
        b.Clear();
        var b3 = TinyhandSerializer.Deserialize<InternalTestClass2<double>>(TinyhandSerializer.Serialize(b));
    }
}
