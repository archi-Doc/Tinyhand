using System;
using System.Collections.Generic;
using System.ComponentModel;
using Tinyhand;

namespace QuickStart;

[TinyhandObject] // Annote a [TinyhandObject] attribute.
public partial record MyClass // partial class is required for source generator.
{
    // Key attributes take a serialization index (or string name)
    // The values must be unique and versioning has to be considered as well.
    [Key(0)]
    public int Age { get; set; }

    [Key(1)]
    public string FirstName { get; set; } = string.Empty;

    [Key(2)]
    public string LastName { get; set; } = "Doe"; // Initial value is used when creating a new instance or deserializing if the value is missing.

    // All fields or properties that should not be serialized must be annotated with [IgnoreMember].
    [IgnoreMember]
    public string FullName { get { return FirstName + LastName; } }

    [Key(3)]
    public List<string> Friends { get; set; } = [];

    [Key(4)]
    public int[]? Ids { get; set; } // Nullable value remains null

    public MyClass()
    {
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
        // TinyhandModule_ConsoleApp1.Initialize(); // Initialize() method is required on some platforms (e.g Xamarin, Native AOT) which does not support ModuleInitializer attribute.

        var myClass = new MyClass() { Age = 10, FirstName = "hoge", LastName = "huga", };
        var b = TinyhandSerializer.Serialize(myClass);
        var myClass2 = TinyhandSerializer.Deserialize<MyClass>(b);
        Console.WriteLine($"myClass2:");
        Console.WriteLine(myClass2?.ToString());
        Console.WriteLine();

        b = TinyhandSerializer.Serialize(new EmptyClass()); // Empty data
        var myClass3 = TinyhandSerializer.Deserialize<MyClass>(b); // Create an instance and set non-null values of the members.

        var myClassRecon = TinyhandSerializer.Reconstruct<MyClass>(); // Create a new instance whose members have default values.
        Console.WriteLine($"myClassRecon:");
        Console.WriteLine(myClassRecon?.ToString());
        Console.WriteLine();

        // MaxLengthTest.Test();
    }
}
