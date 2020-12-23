## Tinyhand
![Nuget](https://img.shields.io/nuget/v/Tinyhand) ![Build and Test](https://github.com/archi-Doc/Tinyhand/workflows/Build%20and%20Test/badge.svg)

Tinyhand is a tiny and simple data format/serializer largely based on [MessagePack for C#](https://github.com/neuecc/MessagePack-CSharp) by neuecc, AArnott.



## Table of Contents

- [Quick Start](#quick-start)
- [Performance](#performance)
- [Serialization Target](#Serialization-Target)
  - [Readonly, Getter-only](#Readonly,-Getter-only)
  - [Include private members](#Include-private-members)
  - [Explicit key only](#Explicit-key-only)
- [Features](#features)
  - [Handling nullable reference types](#Handling-nullable-reference-types)
  - [Default value](#Default-value)
  - [Reconstruct](#Reconstruct)
  - [Reuse Instance](#Reuse-Instance)
  - [Serialization Callback](#Serialization-Callback)
  - [Built-in supported types](#built-in-supported-types)
  - [Non-Generic API](#Non-Generic-API)
- [External assembly](#External-assembly)




## Quick Start

Tinyhand uses Source Generator, so the Target Framework should be .NET 5 or later.

Install Tinyhand using Package Manager Console.

```
Install-Package Tinyhand
```

This is a small sample code to use Tinyhand.

```csharp
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Tinyhand;

namespace ConsoleApp1
{
    [TinyhandObject] // Annote a [TinyhandObject] attribute.
    public partial class MyClass // partial class is required for source generator.
    {
        // Key attributes take a serialization index (or string name)
        // The values must be unique and versioning has to be considered as well.
        [Key(0)]
        public int Age { get; set; }

        [Key(1)]
        public string FirstName { get; set; } = string.Empty;

        [Key(2)]
        [DefaultValue("Doe")] // If there is no corresponding data, the default value is set.
        public string LastName { get; set; } = string.Empty;

        // All fields or properties that should not be serialized must be annotated with [IgnoreMember].
        [IgnoreMember]
        public string FullName { get { return FirstName + LastName; } }

        [Key(3)]
        public List<string> Friends { get; set; } = default!; // Non-null value will be set by TinyhandSerializer.

        [Key(4)]
        public int[]? Ids { get; set; } // Nullable value will be set null.

        public MyClass()
        {
            this.MemberNotNull(); // optional (.NET 5): Informs the compiler that field or property members are set non-null values by TinyhandSerializer.
            // this.Reconstruct(TinyhandSerializerOptions.Standard); // optional: Call Reconstruct() to actually create instances of members.
        }
    }

    [TinyhandObject]
    public partial class EmptyClass
    {
    }

    class Program
    {
        static void Main(string[] args)
        {
            // TinyhandModule.Initialize(); // .NET Core 3.1 does not support ModuleInitializerAttribute, so you need to call TinyhandModule.Initialize() before using Tinyhand. Not required for .NET 5.
            // ClassLibrary1.TinyhandModule.Initialize(); // Initialize for external assembly.

            var myClass = new MyClass() { Age = 10, FirstName = "hoge", LastName = "huga", };
            var b = TinyhandSerializer.Serialize(myClass);
            var myClass2 = TinyhandSerializer.Deserialize<MyClass>(b);

            b = TinyhandSerializer.Serialize(new EmptyClass()); // Empty data
            var myClass3 = TinyhandSerializer.Deserialize<MyClass>(b); // Create an instance and set non-null values of the members.
            
            var myClassRecon = TinyhandSerializer.Reconstruct<MyClass>(); // Create a new instance whose members have default values.
        }
    }
}
```



## Performance

Simple benchmark with [protobuf-net](https://github.com/protobuf-net/protobuf-net) and [MessagePack for C#](https://github.com/neuecc/MessagePack-CSharp).

Tinyhand is quite fast and since it is based on Source Generator, it does not take time for dynamic code generation.

| Method                       |     Mean |   Error |  StdDev |   Median |  Gen 0 | Gen 1 | Gen 2 | Allocated |
| ---------------------------- | -------: | ------: | ------: | -------: | -----: | ----: | ----: | --------: |
| SerializeProtoBuf            | 449.3 ns | 4.31 ns | 6.04 ns | 452.5 ns | 0.0973 |     - |     - |     408 B |
| SerializeMessagePack         | 163.9 ns | 1.33 ns | 1.90 ns | 163.2 ns | 0.0134 |     - |     - |      56 B |
| SerializeTinyhand            | 140.4 ns | 3.32 ns | 4.97 ns | 141.0 ns | 0.0134 |     - |     - |      56 B |
| DeserializeProtoBuf          | 737.6 ns | 2.45 ns | 3.66 ns | 737.1 ns | 0.0763 |     - |     - |     320 B |
| DeserializeMessagePack       | 306.6 ns | 0.66 ns | 0.93 ns | 306.7 ns | 0.0668 |     - |     - |     280 B |
| DeserializeTinyhand          | 280.4 ns | 2.22 ns | 3.19 ns | 280.7 ns | 0.0668 |     - |     - |     280 B |
| SerializeMessagePackString   | 179.1 ns | 2.64 ns | 3.79 ns | 179.3 ns | 0.0153 |     - |     - |      64 B |
| SerializeTinyhandString      | 143.8 ns | 2.18 ns | 3.19 ns | 142.1 ns | 0.0153 |     - |     - |      64 B |
| DeserializeMessagePackString | 320.1 ns | 1.12 ns | 1.60 ns | 319.7 ns | 0.0668 |     - |     - |     280 B |
| DeserializeTinyhandString    | 311.3 ns | 1.31 ns | 1.84 ns | 310.8 ns | 0.0744 |     - |     - |     312 B |



## Serialization Target

All public members are serialization targets by default. Unless `KeyAsPropertyName` is set to true, you need to add `Key` attribute to public members.

```csharp
[TinyhandObject]
public partial class DefaultBehaviourClass
{
    [Key(0)]
    public int X; // Key required

    public int Y { get; private set; } // Not required since it's private setter.

    [Key(1)]
    private int Z; // By adding the Key attribute, You can add a private member to the serialization target.
}

[TinyhandObject(KeyAsPropertyName = true)]
public partial class KeyAsNameClass
{
    public int X; // Serialized with the key "X"

    public int Y { get; private set; } // Not a serialization target

    [Key("Z")]
    private int Z; // Serialized with the key "Z"
}
```

### Readonly, Getter-only

Readonly field and getter-only property are not serialization target. 

```csharp
[TinyhandObject]
public partial class ReadonlyGetteronlyClass
{
    [Key(0)]
    public readonly int X; // Error!

    [Key(1)]
    public int Y { get; } = 0; // Error!
}
```

Although it is not impossible to serialize read-only fields and getter-only properties, this feature is not supported because it requires dynamic code generation and read-only should be read-only.

### Include private members

By setting `IncludePrivateMembers` to true, you can add private and protected members to the serialization target.

```csharp
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
```

### Explicit key only

By setting `ExplicitKeyOnly` to true, only members with the Key attribute will be serialized.

```csharp
[TinyhandObject(ExplicitKeyOnly = true)]
public partial class ExplicitKeyClass
{
    public int X; // Not serialized (no error message).

    [Key(0)]
    public int Y; // Serialized
}
```



## Features

### Handling nullable reference types

Tinyhand tries to handle nullable/non-nullable reference types properly.

```csharp
[TinyhandObject(KeyAsPropertyName = true)]
public partial class NullableTestClass
{
    public int Int { get; set; } = default!; // 0

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
```



### Default value

You can specify the default value for a member using `DefaultValueAttribute `(System.ComponentModel).

If the serialized data does not have a matching data for a member, Tinyhand will set the default value for that member.

Primitive types (bool, sbyte, byte, short, ushort, int, uint, long, ulong, float, double, decimal, string, char, enum) are supported.

```csharp
[TinyhandObject(KeyAsPropertyName = true)]
public partial class DefaultTestClass
{
    [DefaultValue(true)]
    public bool Bool { get; set; }

    [DefaultValue(77)]
    public int Int { get; set; }

    [DefaultValue("test")]
    public string String { get; set; }
    
    [DefaultValue("Test")] // Default value for TinyhandObject is supported.
    public DefaultTestClassName NameClass { get; set; }
}

[TinyhandObject(KeyAsPropertyName = true)]
public partial class StringEmptyClass
{
}

[TinyhandObject]
public partial class DefaultTestClassName
{
    public DefaultTestClassName()
    {
        
    }

    public void SetDefault(string name)
    {// To receive the default value, SetDefault() is required.
        // Constructor -> SetDefault -> Deserialize or Reconstruct
        this.Name = name;
    }

    public string Name { get; private set; }
}

public class DefaultTest
{
    public void Test()
    {
        var t = new StringEmptyClass();
        var t2 = TinyhandSerializer.Deserialize<DefaultTestClass>(TinyhandSerializer.Serialize(t));
    }
}
```

You can skip serializing values if the value is identical to the default value, by using `[TinyhandObject(SkipSerializingDefaultValue = true)]`.



### Reconstruct

Tinyhand creates an instance of a member variable even if there is no matching data. By adding `[Reconstruct(false)]` or `[Reconstruct(true)]` to member attributes, you can change the behavior of whether an instance is created or not. 

```csharp
[TinyhandObject(KeyAsPropertyName = true)]
public partial class ReconstructTestClass
{
    [DefaultValue(12)]
    public int Int { get; set; } // 12

    public EmptyClass EmptyClass { get; set; } = default!; // new()

    [Reconstruct(false)]
    public EmptyClass EmptyClassOff { get; set; } = default!; // null

    public EmptyClass? EmptyClass2 { get; set; } // null

    [Reconstruct(true)]
    public EmptyClass? EmptyClassOn { get; set; } // new()

    /* Error. A class to be reconstructed must have a default constructor.
    [IgnoreMember]
    [Reconstruct(true)]
    public ClassWithoutDefaultConstructor WithoutClass { get; set; }*/

    [IgnoreMember]
    [Reconstruct(true)]
    public ClassWithDefaultConstructor WithClass { get; set; } = default!;
}

public class ClassWithoutDefaultConstructor
{
    public string Name = string.Empty;

    public ClassWithoutDefaultConstructor(string name)
    {
        this.Name = name;
    }
}

public class ClassWithDefaultConstructor
{
    public string Name = string.Empty;

    public ClassWithDefaultConstructor(string name)
    {
        this.Name = name;
    }

    public ClassWithDefaultConstructor()
        : this(string.Empty)
    {
    }
}
```

If you don't want to create an instance with default behavior, set `ReconstructMember` of `TinyhandObject` to false ` [TinyhandObject(ReconstructMember = false)]`.



### Reuse Instance

Tinyhand will reuse an instance if its members have valid values. The type of the instance to be reused must have a `TinyhandObject` attribute.

By adding `[Reuse(true)]` or `[Reuse(false)]` to member attributes, you can change the behavior of whether an instance is reused or not.

```csharp
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

[TinyhandObject(KeyAsPropertyName = true)]
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
        // t2.Flag == true
        // t2.ObjectToCreate.Name == "create", t2.ObjectToCreate.Length == 6
        // t2.ObjectToReuse.Name == "reuse", t2.ObjectToReuse.Length == 5

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
```

If you don't want to reuse an instance with default behavior, set `ReuseMember` of `TinyhandObject` to false ` [TinyhandObject(ReuseMember = false)]`.



### Serialization Callback

Objects implementing the `ITinyhandSerializationCallback` interface will receive `OnBeforeSerialize` and `OnAfterDeserialize` calls during serialization/deserialization.

```csharp
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
```



### Built-in supported types

These types can serialize by default:

* Primitives (`int`, `string`, etc...), `Enum`s, `Nullable<>`, `Lazy<>`

* `TimeSpan`,  `DateTime`, `DateTimeOffset`

* `Guid`, `Uri`, `Version`, `StringBuilder`

* `BigInteger`, `Complex`

* `Array[]`, `Array[,]`, `Array[,,]`, `Array[,,,]`, `ArraySegment<>`, `BitArray`

* `KeyValuePair<,>`, `Tuple<,...>`, `ValueTuple<,...>`

* `ArrayList`, `Hashtable`

* `List<>`, `LinkedList<>`, `Queue<>`, `Stack<>`, `HashSet<>`, `ReadOnlyCollection<>`, `SortedList<,>`

* `IList<>`, `ICollection<>`, `IEnumerable<>`, `IReadOnlyCollection<>`, `IReadOnlyList<>`

* `Dictionary<,>`, `IDictionary<,>`, `SortedDictionary<,>`, `ILookup<,>`, `IGrouping<,>`, `ReadOnlyDictionary<,>`, `IReadOnlyDictionary<,>`

* `ObservableCollection<>`, `ReadOnlyObservableCollection<>`

* `ISet<>`,

* `ConcurrentBag<>`, `ConcurrentQueue<>`, `ConcurrentStack<>`, `ConcurrentDictionary<,>`

* Immutable collections (`ImmutableList<>`, etc)

* Custom implementations of `ICollection<>` or `IDictionary<,>` with a parameterless constructor

* Custom implementations of `IList` or `IDictionary` with a parameterless constructor



### LZ4 Compression

Tinyhand has LZ4 compression support.

```csharp
var b = TinyhandSerializer.Serialize(myClass, TinyhandSerializerOptions.Lz4);
var myClass2 = TinyhandSerializer.Deserialize<MyClass>(b, TinyhandSerializerOptions.Standard.WithCompression(TinyhandCompression.Lz4)); // Same as TinyhandSerializerOptions.Lz4
```




### Non-Generic API

```csharp
var myClass = (MyClass)TinyhandSerializer.Reconstruct(typeof(MyClass));
var b = TinyhandSerializer.Serialize(myClass.GetType(), myClass);
var myClass2 = TinyhandSerializer.Deserialize(typeof(MyClass), b);
```




## External assembly

If you add a reference to a class library which uses Tinyhand, additional initialization code is required.

The namespace of `TinyhandModule` is set to the name of assembly, in order to avoid namespace conflicts.

```csharp
ClassLibrary1.TinyhandModule.Initialize(); // Initialize for external assembly.
```

This code will no longer be needed if .NET Standard supports `ModuleInitializerAttribute` in the future.

