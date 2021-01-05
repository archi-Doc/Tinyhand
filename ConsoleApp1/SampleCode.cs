using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tinyhand;

#pragma warning disable CS0169

namespace ConsoleApp1
{
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

    [TinyhandObject(KeyAsPropertyName = true)]
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

    [TinyhandObject(KeyAsPropertyName = true)]
    public partial class DefaultTestClass
    {
        [DefaultValue(true)]
        public bool Bool { get; set; }

        [DefaultValue(77)]
        public int Int { get; set; }

        [DefaultValue("test")]
        public string String { get; set; } = default!;
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

        public void Print() => Console.WriteLine($"B: {this.Name}");
    }

    public static class UnionTest
    {
        public static void Test()
        {
            var classA = new UnionTestClassA() { X = 10, };
            var classB = new UnionTestClassB() { Name = "test" , };

            var b = TinyhandSerializer.Serialize((IUnionTestInterface)classA);
            var i = TinyhandSerializer.Deserialize<IUnionTestInterface>(b);
            i?.Print(); // A: 10

            b = TinyhandSerializer.Serialize((IUnionTestInterface)classB);
            i = TinyhandSerializer.Deserialize<IUnionTestInterface>(b);
            i?.Print(); // B: test
        }
    }
}
