## Tinyhand
![Build and Test](https://github.com/archi-Doc/Tinyhand/workflows/Build%20and%20Test/badge.svg)

Tinyhand is a tiny and simple data format/serializer largely based on [MessagePack for C#](https://github.com/neuecc/MessagePack-CSharp) by neuecc, AArnott.



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
            TinyhandModule.Initialize(); // .NET Core 3.1 does not support ModuleInitializerAttribute, so you need to call TinyhandModule.Initialize() before using Tinyhand. Not required for .NET 5.
            // ClassLibrary1.TinyhandModule.Initialize(); // Initialize for external assembly.

            var myClass = new MyClass() { Age = 10, FirstName = "hoge", LastName = "huga", };
            var b = TinyhandSerializer.Serialize(myClass);
            var myClass2 = TinyhandSerializer.Deserialize<MyClass>(b);

            b = TinyhandSerializer.Serialize(new EmptyClass()); // Empty data
            var myClass3 = TinyhandSerializer.Deserialize<MyClass>(b); // Create an instance and set non-null values of the members.
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
}

[TinyhandObject(KeyAsPropertyName = true)]
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
```

You can skip serializing values if the value is identical to the default value, by using `[TinyhandObject(SkipSerializingDefaultValue = true)]`.



### Reconstruct

Tinyhand creates an instance of a member variable even if there is no matching data. By adding `[Reconstruct(false)]` or `[Reconstruct(true)]` to member attributes, you can change the behavior of whether an instance is created or not. 

```csharp
[TinyhandObject(KeyAsPropertyName = true, ReconstructMember = false)]
    public partial class ReconstructTestClass
    {
        [DefaultValue(12)]
        public int Int { get; set; } // 12

        public EmptyClass EmptyClass { get; set; } // new()

        [Reconstruct(false)]
        public EmptyClass EmptyClassOff { get; set; } // null

        public EmptyClass? EmptyClass2 { get; set; } // null

        [Reconstruct(true)]
        public EmptyClass? EmptyClassOn { get; set; } // new()

        /*[IgnoreMember]
        [Reconstruct(true)]
        public ClassWithoutDefaultConstructor WithoutClass { get; set; }

        [IgnoreMember]
        [Reconstruct(true)]
        public ClassWithDefaultConstructor WithClass { get; set; }*/
    }
```

If you don't want to create an instance with default behavior, set `ReconstructMember` of `TinyhandObject` to false ` [TinyhandObject(ReconstructMember = false)]`.



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



## External assembly

If you add a reference to a class library which uses Tinyhand, additional initialization code is required.

The namespace of `TinyhandModule` is set to the name of assembly, in order to avoid namespace conflicts.

```csharp
ClassLibrary1.TinyhandModule.Initialize(); // Initialize for external assembly.
```

This code will no longer be needed if .NET Standard supports `ModuleInitializerAttribute` in the future.

