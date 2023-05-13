using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;
using SandboxBase;
using Sandbox.ZenItz;
using Tinyhand;
using Tinyhand.IO;
using ValueLink;

#pragma warning disable CS0414
#pragma warning disable CS0169

namespace Sandbox;

[TinyhandObject(LockObject = "semaphore", Journaling = true)]
public partial class PropertyTestClass2 : PropertyTestClass
{
}

[TinyhandObject(Journaling = true)]
public partial class JournalingClass
{
    [IgnoreMember]
    public ITinyhandCrystal? Crystal { get; set; }

    [Key(0)]
    public int X { get; set; }

    [Key(1)]
    public PropertyTestClass Class1 { get; set; } = new();
}

[TinyhandObject]
public partial class LockObjectClassB : LockObjectClassA
{
    [Key(1)]
    public int Y { get; set; }
}

public partial class SerializationGenericClass<T>
{
    public T Value { get; set; } = default!;

    [TinyhandObject(ExplicitKeyOnly = true)] // ExplicitKeyOnly
    public partial class TestClass
    {
        [Key(0)]
        [DefaultValue(1)]
        public int A; // Serialize

        public SerializationGenericClass<T> Parent { get; set; } = default!;
    }
}

[TinyhandObject]
public partial class LockObjectClass2 : LockObjectClass<LockObjectClass2>
{
}

[TinyhandObject(LockObject = "syncObject")]
public abstract partial class LockObjectClass<T>
    where T : LockObjectClass<T>
{
    public LockObjectClass()
    {
    }

    [Key(0)]
    public int X { get; set; }

    protected object syncObject = new();
}

[ValueLinkObject]
[TinyhandObject]
public partial class TestItem
{
    [Key(0)]
    [Link(Primary = true, Type = ChainType.Ordered)]
    public int X { get; set; }

    [Key(2)]
    public GoshujinClass? Class2 { get; set; } = default!; // Error
}

public partial class NestParent
{
    [TinyhandObject]
    public partial class NestContract
    {
        [Key(0)]
        public int MyProperty { get; set; }

        [Key(1)]
        public TestItem.GoshujinClass? Class1 { get; set; } = default!;
    }

    public class NestContractless
    {
        public int MyProperty { get; set; }
    }
}

public partial interface Nested
{
    [TinyhandObject]
    public partial record TestRecord([property: Key(0)] int X);

    [TinyhandObject]
    public partial struct TestStruct
    {
        [Key(0)]
        public int X;

        [Key(1)]
        public int Y;

        [Key(2)]
        public TestRecord[] Records;
    }
}


[TinyhandObject]
public abstract class AbstractClass
{
}

[TinyhandGenerateHash("strings.tinyhand")]
public partial class Hashed
{
}

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
public partial interface ISerializableInterface
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
    internal static Type ist => typeof(Item<int>);

    [TinyhandObject]
    private sealed partial class Item<Z>
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

        [Key(2)]
        internal Z Key2 = default!;
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
            setterDelegate = Expression.Lambda<Action<InternalTestClass2<T>, T>>(Expression.Assign(Expression.PropertyOrField(exp, "PrivatePrivate"), exp2), exp, exp2).Compile();
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

[TinyhandObject]
public partial class MaxLengthClass
{
    [Key(0, PropertyName = "X")]
    // [MaxLength(3)]
    private int _x;

    [Key(1, PropertyName = "Name")]
    [MaxLength(3)]
    private string _name = default!;

    [Key(2, PropertyName = "Ids")]
    [MaxLength(3)]
    private int[] _ids = default!;

    [Key(3, PropertyName = "StringArray")]
    [MaxLength(3, 4)]
    private string[] _stringArray = default!;

    [Key(4, PropertyName = "StringList")]
    [MaxLength(4, 3)]
    private List<string> _stringList = default!;
}

[TinyhandObject]
public partial class MaxLengthClass2 : MaxLengthClass, ITinyhandDefault<int>
{
    [Key(5, PropertyName = "Byte")]
    [MaxLength(4)]
    private byte[] _byte = default!;

    [Key(6, PropertyName = "ByteArray")]
    [MaxLength(2, 3)]
    private byte[][] _byteArray = default!;

    [Key(7, PropertyName = "ByteList", PropertyAccessibility = PropertyAccessibility.ProtectedSetter)]
    [MaxLength(3, 2)]
    private List<byte[]> _byteList = default!;

    public bool CanSkipSerialization()
        => false;

    public void SetDefaultValue(int defaultValue)
    {
    }
}

[TinyhandObject]
public partial class MaxLengthClass3
{
    [Key(0)]
    [DefaultValue(1)]
    MaxLengthClass2? Class { get; set; }
}

[TinyhandObject]
public partial class GenericTestClass<T>
{
    [KeyAsName]
    private int id;

    [KeyAsName]
    private T value = default!;

    [KeyAsName]
    private NestedClass<double, int> nested = default!;

    public GenericTestClass()
    {
    }

    public GenericTestClass(int id, T value, NestedClass<double, int> nested)
    {
        this.id = id;
        this.value = value;
        this.nested = nested;
    }

    [TinyhandObject]
    public partial class NestedClass<X, Y>
    {
        [KeyAsName]
        private string name = default!;

        [KeyAsName]
        private X xvalue = default!;

        [KeyAsName]
        private Y yvalue = default!;

        public NestedClass()
        {
        }

        public NestedClass(string name, X xvalue, Y yvalue)
        {
            this.name = name;
            this.xvalue = xvalue;
            this.yvalue = yvalue;
        }
    }
}


class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Sandbox");
        Console.WriteLine();

        var ship = new ZenItz.Itz<int>.DefaultShip<Payload>();
        ship.Test();

        var pt = Type.GetType("Sandbox.ZenItz.Itz`1+DefaultShip`1+Item"); // "ZenItz.Itz<>.DefaultShip<>.Item", "Sandbox.ZenItz.Itz`1+DefaultShip`1"
        pt = typeof(ZenItz.Itz<>.DefaultShip<>);
        pt = Type.GetType("Sandbox.ZenItz.Itz`1+DefaultShip`1");

        var ts = default(Nested.TestStruct);
        ts.X = 1;
        ts.Y = 2;
        var tsb = TinyhandSerializer.SerializeObject(ts);
        var ts2 = TinyhandSerializer.DeserializeObject<Nested.TestStruct>(tsb);
        ts2 = default;
        var reader = new TinyhandReader(tsb);
        var ts3 = (ITinyhandSerialize)ts2;
        ts3.Deserialize(ref reader, TinyhandSerializerOptions.Standard);

        var gc = new GenericTestClass<int>();
        var gc2 = TinyhandSerializer.Deserialize<GenericTestClass<int>>(TinyhandSerializer.Serialize(gc));

        var ntc = new GenericTestClass<string>.NestedClass<int, double>("test", 1, 100d);
        var ntc2 = TinyhandSerializer.Deserialize<GenericTestClass<string>.NestedClass<int, double>>(TinyhandSerializer.Serialize(ntc));

        var ntc3 = new GenericTestClass<string>.NestedClass<int, long>("test2", 2, 200);
        var ntc4 = TinyhandSerializer.Deserialize<GenericTestClass<string>.NestedClass<int, long>>(TinyhandSerializer.Serialize(ntc3));

        var pk = new PublicKey(2, new byte[] { 0, 1, });
        pk.Test(new byte[] { });

        var tc = new MaxLengthClass();
        tc.X = 1;
        tc.Name = "Fuga";
        tc.Ids = new int[] { 1, 2, 3, 4, };
        tc.StringArray = new[] { "11", "2222", "333333", "44444444", "5", };
        tc.StringList = new(tc.StringArray);
        var tc2 = TinyhandSerializer.Deserialize<MaxLengthClass>(TinyhandSerializer.Serialize(tc));

        var asm = System.Reflection.Assembly.GetExecutingAssembly();
        HashedString.LoadAssembly(null, asm, "strings.tinyhand");
        HashedString.LoadAssembly(null, asm, "Sub.strings2.tinyhand");
        var t = HashedString.Get(Hashed.Dialog.Ok);
        t = HashedString.Get(Hashed.EscapeTest);
        t = HashedString.Get(Hashed.StringFormatTest, "1", "2");

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

        var hash = Hashed.Application.Description9;
    }
}
