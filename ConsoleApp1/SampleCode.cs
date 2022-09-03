using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tinyhand;
using LP;

#pragma warning disable CS0169

namespace ConsoleApp1;

public interface IItzPayload
{
}

public interface IItzShip<TPayload>
where TPayload : IItzPayload
{
}

public partial class ItzShip<T> : IItzShip<T>
    where T : IItzPayload
{
    [TinyhandObject]
    public sealed partial class Item
    {
        public Item(PrimarySecondaryIdentifier key, T value)
        {
            this.Key = key;
            this.Value = value;
        }

        public Item()
        {
        }

        [Key(0)]
        internal T Value = default!;

        [Key(1)]
        internal PrimarySecondaryIdentifier Key;
    }

    public ItzShip()
    {
    }
}

[TinyhandObject]
public partial class InternalTestClass
{
    [Key(0)]
    private int PrivateInt = 1;

    [Key(1)]
    internal int InternalInt = 2;

    public int PropertyInt { get; private set; } = 3;

    public void Clear()
    {
        this.PrivateInt = 0;
        this.InternalInt = 0;
        this.PropertyInt = 0;
    }

}

[TinyhandObject]
public partial class DefaultBehaviourClass
{
    [Key(0)]
    public int X; // Key required

    public int Y { get; private set; } // Not required since it's private setter.

    [Key(1)]
    private int Z; // By adding the Key attribute, You can add a private member to the serialization target.
}

[TinyhandObject(ImplicitKeyAsName = true)]
public partial class KeyAsNameClass
{
    public int X; // Serialized with the key "X"

    public int Y { get; private set; } // Not a serialization target

    [Key("Z")]
    private int Z; // Serialized with the key "Z"
}

[TinyhandObject(IncludePrivateMembers = true)]
public partial class IncludePrivateClass
{
    [Key(0)]
    public int X; // Key required

    [Key(1)]
    public int Y { get; private set; } // Key required

    [IgnoreMember]
    private int Z; // Add the IgnoreMember attribute to exclude from serialization targets.
}

/* [TinyhandObject]
public partial class ReadonlyGetteronlyClass
{
    [Key(0)]
    public readonly int X; // Error!

    [Key(1)]
    public int Y { get; } = 0; // Error!
}*/

[TinyhandObject(ExplicitKeyOnly = true)]
public partial class ExplicitKeyClass
{
    public int X; // No warning. Not serialized.

    [Key(0)]
    public int Y; // Serialized
}

[TinyhandObject]
public partial class SampleCallback : ITinyhandSerializationCallback
{
    [Key(0)]
    public int Key { get; set; }

    public void OnBeforeSerialize()
    {
        Console.WriteLine("OnBefore");
    }

    public void OnAfterDeserialize()
    {
        Console.WriteLine("OnAfter");
    }
}

[TinyhandObject(ImplicitKeyAsName = true)]
public partial class NullableTestClass
{
    public int Int { get; set; } = default!;// 0

    public int? NullableInt { get; set; } = default!; // null

    public string String { get; set; } = default!;
    // If this value is null, Tinyhand will automatically change the value to string.Empty.

    public string? NullableString { get; set; } = default!;
    // This is nullable type, so the value remains null.

    public NullableSimpleClass SimpleClass { get; set; } = default!; // new SimpleClass()

    public NullableSimpleClass? NullableSimpleClass { get; set; } = default!; // null

    public NullableSimpleClass[] Array { get; set; } = default!; // new NullableSimpleClass[0]

    public NullableSimpleClass[]? NullableArray { get; set; } = default!; // null

    public NullableSimpleClass[] Array2 { get; set; } = new NullableSimpleClass[] { new NullableSimpleClass(), null! };
    // null! will be change to a new instance.

    public Queue<NullableSimpleClass> Queue { get; set; } = new(new NullableSimpleClass[] { null!, null!, });
    // null! remains null because it loses information whether it is nullable or non-nullable in C# generic methods.
}

[TinyhandObject]
public partial class NullableSimpleClass
{
    [Key(0)]
    public double Double { get; set; }
}

public class NullableTest
{
    public void Test()
    {
        var t = new NullableTestClass();
        var t2 = TinyhandSerializer.Deserialize<NullableTestClass>(TinyhandSerializer.Serialize(t));
    }
}

[TinyhandObject(ImplicitKeyAsName = true)]
public partial class DefaultTestClass
{
    [DefaultValue(true)]
    public bool Bool { get; set; }

    [DefaultValue(77)]
    public int Int { get; set; }

    [DefaultValue("test")]
    public string String { get; set; } = default!;
}

[TinyhandObject(ImplicitKeyAsName = true)]
public partial class StringEmptyClass
{
}

public class DefaultTest
{
    public void Test()
    {
        var t = new StringEmptyClass();
        var t2 = TinyhandSerializer.Deserialize<DefaultTestClass>(TinyhandSerializer.Serialize(t));
    }
}

[TinyhandObject(ReuseMember = true)]
public partial class ReuseTestClass
{
    [Key(0)]
    [Reuse(false)]
    public ReuseObject ObjectToCreate { get; set; } = new("create");

    [Key(1)]
    public ReuseObject ObjectToReuse { get; set; } = new("reuse");

    [IgnoreMember]
    public bool Flag { get; set; } = false;
}

[TinyhandObject(ImplicitKeyAsName = true)]
public partial class ReuseObject
{
    public ReuseObject()
        : this(string.Empty)
    {
    }

    public ReuseObject(string name)
    {
        this.Name = name;
        this.Length = name.Length;
    }

    [IgnoreMember]
    public string Name { get; set; } // Not a serialization target

    public int Length { get; set; }
}

public class ReuseTest
{
    public void Test()
    {
        var t = new ReuseTestClass();
        t.Flag = true;

        var t2 = TinyhandSerializer.Deserialize<ReuseTestClass>(TinyhandSerializer.Serialize(t)); // Reuse member
        // t2.Flag == false
        // t2.ObjectToCreate.Name == "", t2.ObjectToCreate.Length == 6 // Note that Name is not a serialization target.
        // t2.ObjectToReuse.Name == "reuse", t2.ObjectToReuse.Length == 5

        t2 = TinyhandSerializer.DeserializeWith<ReuseTestClass>(t, TinyhandSerializer.Serialize(t)); // Reuse ReuseTestClass
        // t2.Flag == true
        // t2.ObjectToCreate.Name == "", t2.ObjectToCreate.Length == 6
        // t2.ObjectToReuse.Name == "reuse", t2.ObjectToReuse.Length == 5

        var reader = new Tinyhand.IO.TinyhandReader(TinyhandSerializer.Serialize(t));
        t.Deserialize(ref reader, TinyhandSerializerOptions.Standard); ; // Same as above
    }
}

// Annotate inheritance types
[TinyhandUnion(0, typeof(UnionTestClassA))]
[TinyhandUnion(1, typeof(UnionTestClassB))]
public interface IUnionTestInterface
{
    void Print();
}

[TinyhandObject]
public partial class UnionTestClassA : IUnionTestInterface
{
    [Key(0)]
    public int X { get; set; }

    public void Print() => Console.WriteLine($"A: {this.X.ToString()}");
}

[TinyhandObject]
public partial class UnionTestClassB : IUnionTestInterface
{
    [Key(0)]
    public string Name { get; set; } = default!;

    public virtual void Print() => Console.WriteLine($"B: {this.Name}");
}

public static class UnionTest
{
    public static void Test()
    {
        var classA = new UnionTestClassA() { X = 10, };
        var classB = new UnionTestClassB() { Name = "test", };

        var b = TinyhandSerializer.Serialize((IUnionTestInterface)classA);
        var i = TinyhandSerializer.Deserialize<IUnionTestInterface>(b);
        i?.Print(); // A: 10

        b = TinyhandSerializer.Serialize((IUnionTestInterface)classB);
        i = TinyhandSerializer.Deserialize<IUnionTestInterface>(b);
        i?.Print(); // B: test
    }
}

public static class TextSerializeTest
{
    public static void Test()
    {
        // Serialize an object to string (UTF-16 text) and deserialize from it.
        var myClass = new MyClass() { Age = 10, FirstName = "hoge", LastName = "huga", };
        var st = TinyhandSerializer.SerializeToString(myClass);
        var myClass2 = TinyhandSerializer.DeserializeFromString<MyClass>(st);

        // UTF-8 version
        var utf8 = TinyhandSerializer.SerializeToUtf8(myClass);
        var myClass3 = TinyhandSerializer.DeserializeFromUtf8<MyClass>(utf8);
    }
}

[TinyhandObject]
public partial class VersioningClass1
{
    [Key(0)]
    public int Id { get; set; }

    public override string ToString() => $"  Version 1, ID: {this.Id}";
}

[TinyhandObject]
public partial class VersioningClass2
{
    [Key(0)]
    public int Id { get; set; }

    [Key(1)]
    [DefaultValue("John")]
    public string Name { get; set; } = default!;

    public override string ToString() => $"  Version 2, ID: {this.Id} Name: {this.Name}";
}

public static class VersioningTest
{
    public static void Test()
    {
        var v1 = new VersioningClass1() { Id = 1, };
        Console.WriteLine("Original Version 1:");
        Console.WriteLine(v1.ToString());// Version 1, ID: 1

        var v12 = TinyhandSerializer.Deserialize<VersioningClass2>(TinyhandSerializer.Serialize(v1))!;
        Console.WriteLine("Serialize v1 and deserialize as v2:");
        Console.WriteLine(v12.ToString());// Version 2, ID: 1 Name: John (the default value is set)

        Console.WriteLine();

        var v2 = new VersioningClass2() { Id = 2, Name = "Fuga", };
        Console.WriteLine("Original Version 2:");
        Console.WriteLine(v2.ToString());// Version 2, ID: 2 Name: Fuga

        var v21 = TinyhandSerializer.Deserialize<VersioningClass1>(TinyhandSerializer.Serialize(v2))!;
        Console.WriteLine("Serialize v2 and deserialize as v1:");
        Console.WriteLine(v21.ToString());// Version 1, ID: 2 (Name ignored)
    }
}

[TinyhandObject(ExplicitKeyOnly = true)]
public partial class DeepCopyClass
{
    public int Id { get; set; }

    public string[] Name { get; set; } = new string[] { "A", "B", };

    public UnknownClass? UnknownClass { get; set; }

    public KnownClass? KnownClass { get; set; }
}

public class UnknownClass
{
}

[TinyhandObject]
public partial class KnownClass
{
    [Key(0)]
    public float?[] Single { get; init; } = new float?[] { 0, 1, null, };
}

public static class DeepCopyTest
{
    public static void Test()
    {
        var c = new DeepCopyClass();
        c.UnknownClass = new();
        c.KnownClass = new();

        var d = TinyhandSerializer.Clone(c);
        c.Name[1] = "C";
        Debug.Assert(c.Name[1] != d.Name[1]); // c.Name and d.Name are different since d is a deep copy.
        Debug.Assert(d.UnknownClass == null); // UnknownClass is ignored since Tinyhand doesn't know how to create a deep copy of UnknownClass.
        Debug.Assert(d.KnownClass != null); // Tinyhand can handle a class with TinyhandObjectAttribute.

        var e = TinyhandSerializer.Deserialize<DeepCopyClass>(TinyhandSerializer.Serialize(c)); // Almost the same as above, but Clone() is much faster.
    }
}

[TinyhandObject]
public partial record MaxLengthClass
{
    [Key(0, PropertyName = "Name")] // "Name" property will be created.
    [MaxLength(3)] // The maximum length of Name property.
    private string name = default!;

    [Key(1, PropertyName = "Ids")]
    [MaxLength(2)]
    private int[] id = default!;

    [Key(2, PropertyName = "Tags")]
    [MaxLength(2, 3)] // The maximum length of an array and length of a string.
    private string[] tags = default!;

    public override string ToString()
        => $"""
        Name: {this.Name}
        Ids: {string.Join(',', this.Ids)}
        Tags: {string.Join(',', this.Tags)}
        """;
}

public static class MaxLengthTest
{
    public static void Test()
    {
        var c = new MaxLengthClass();
        c.Name = "ABCD"; // "ABC"
        c.Ids = new int[] { 0, 1, 2, 3 }; // 0, 1,
        c.Tags = new string[] { "aaa", "bbbb", "cccc" }; // "aaa", "bbb",

        Console.WriteLine(c.ToString());
        Console.WriteLine();

        var st = TinyhandSerializer.SerializeToString(c);
        st = """ "ABCD", {0, 1, 2, 3}, {"aaa", "bbbb", "cccc"} """;
        var c2 = TinyhandSerializer.DeserializeFromString<MaxLengthClass>(st);

        Console.WriteLine(c2!.ToString());
        Console.WriteLine();
    }
}
