using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Tinyhand;
using Tinyhand.IO;

namespace ConsoleApp1
{
    [TinyhandObject]
    public partial class TestClass
    {
        [Key(0)]
        public int x;

        [Key(2)]
        public int y;

        [Key(3)]
        public string?[] stringList = default!;

        [Key(4)]
        public string?[]? stringList2 = default!;

        [Key(5)]
        public decimal?[] DecimalArray { get; set; } = { -144m, 456m, null, 78998m, };

        [Key(6)]
        public double?[] DoubleArray { get; set; } = { -100d, 0d, 123456d, 456789d, null, };

        [Key(7)]
        public int[,,] z = { { { 0 }, { 1 } }, { { 3 }, { 2 } }, { { 10 }, { 11 } }, { { 13 }, { 12 } } };

        [Key(8)]
        public KeyValuePair<int, string> kvp = new KeyValuePair<int, string>(3, "test");

        [Key(9)]
        public Version version = new Version(1, 2);

        [Key(10)]
        public Lazy<string> ls = new Lazy<string>(() => "test2");

        [Key(11)]
        public Type type = typeof(int);

        public void Print()
        {
            Console.WriteLine(x);
        }
    }

    [TinyhandObject] // Annote a [TinyhandObject] attribute.
    public partial class MyClass // partial class is required for surce generation.
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
        public List<string> Friends { get; set; } // Non-null value will be set by TinyhandSerializer.

        [Key(4)]
        public int[]? Ids { get; set; } // Default value is null.

        public MyClass()
        {
            this.MemberNotNull(); // optional: Informs the compiler that field or property members are set non-null values by TinyhandSerializer.
            // this.Reconstruct(TinyhandSerializerOptions.Standard); // optional: Call Reconstruct() to actually create instances of members.
        }
    }

    [TinyhandObject]
    public partial class EmptyClass
    {
    }

    // [TinyhandGeneratorOption(AttachDebugger = false, GenerateToFile = true)]
    class Program
    {
        static void Main(string[] args)
        {
            var myClass = new MyClass() { Age = 10, FirstName = "hoge", LastName = "huga", };
            var b = TinyhandSerializer.Serialize(myClass);
            var myClass2 = TinyhandSerializer.Deserialize<MyClass>(b);

            b = TinyhandSerializer.Serialize(new EmptyClass()); // Empty data
            var myClass3 = TinyhandSerializer.Deserialize<MyClass>(b); // Create an instance and set non-null values of the members.

            var tc = new TestClass() { x = 10 };
            b = TinyhandSerializer.Serialize(tc);
            var tc2 = TinyhandSerializer.Deserialize<TestClass>(b);
            tc2?.Print();
        }
    }
}
