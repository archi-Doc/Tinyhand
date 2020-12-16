using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tinyhand;

namespace ConsoleApp1
{
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
}
