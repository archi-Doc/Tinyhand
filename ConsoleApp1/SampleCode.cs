using System;
using System.Collections.Generic;
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
}
