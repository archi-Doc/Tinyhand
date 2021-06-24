using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq.Expressions;
using CrossLink;
using Tinyhand;

namespace Sandbox
{
    [CrossLinkObject]
    [TinyhandObject]
    public partial class TestClass1
    {
        [Link(Type = ChainType.Ordered)]
        [KeyAsName]
        private int id;

        [Link(Type = ChainType.Ordered)]
        [Key("NM")]
        private string name = default!;

        [Link(Type = ChainType.Ordered)]
        [KeyAsName]
        private byte age;

        [Link(Type = ChainType.StackList, Primary = true, Name = "Stack")]
        public TestClass1()
        {
        }

        public TestClass1(int id, string name, byte age)
        {
            this.id = id;
            this.name = name;
            this.age = age;
        }

        public override bool Equals(object? obj)
        {
            var t = obj as TestClass1;
            if (t == null)
            {
                return false;
            }

            return this.id == t.id && this.name == t.name && this.age == t.age;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(this.id, this.name, this.age);
        }
    }

    [TinyhandObject(ImplicitKeyAsName = true)]
    public partial class TestClass2
    {
        public int N { get; set; }

        [KeyAsName]
        public TestClass1.GoshujinClass G { get; set; } = default!;
    }

    [TinyhandObject(ImplicitKeyAsName = true, IncludePrivateMembers = true)]
    public partial record TestRecord
    {
        public int X { get; set; }

        public int Y { get; init; }

        private string A { get; set; } = string.Empty;

        private string B = string.Empty;

        public TestRecord(int x, int y, string a, string b)
        {
            this.X = x;
            this.Y = y;
            this.A = a;
            this.B = b;
        }

        public TestRecord()
        {
        }
    }

    [TinyhandObject]
    public partial class GenericTestClass2<T>
    {
        [KeyAsName]
        private int id;

        [KeyAsName]
        private T value = default!;

        [KeyAsName]
        private NestedClass<double, int> nested = default!;

        public GenericTestClass2()
        {
        }

        public GenericTestClass2(int id, T value, NestedClass<double, int> nested)
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

    public partial class NotSerializeClass
    {
    }

    [TinyhandObject]
    public partial class SerializeClass
    {
        [Key(0)]
        public int Id { get; set; }

        public SerializeClass()
        {
        }

        public SerializeClass(int id)
        {
            this.Id = id;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var g = new TestClass1.GoshujinClass();

            new TestClass1(0, "A", 100).Goshujin = g;
            new TestClass1(1, "Z", 12).Goshujin = g;
            new TestClass1(2, "1", 15).Goshujin = g;

            var st = TinyhandSerializer.SerializeToString(g);
            var g2 = TinyhandSerializer.Deserialize<TestClass1.GoshujinClass>(TinyhandSerializer.Serialize(g));

            var tc2 = new TestClass2();
            tc2.N = 100;
            tc2.G = g;
            st = TinyhandSerializer.SerializeToString(tc2);
            var tc2a = TinyhandSerializer.Deserialize<TestClass2>(TinyhandSerializer.Serialize(tc2));
            var tc2b = TinyhandSerializer.Reconstruct<TestClass2>();

            var r = new TestRecord(1, 2, "a", "b");
            var r2 = r with { X = 3, };
            st = TinyhandSerializer.SerializeToString(r);
            var r3 = TinyhandSerializer.Deserialize<TestRecord>(TinyhandSerializer.Serialize(r));

            /*var classB = TinyhandSerializer.Reconstruct<TextSerializeClass1>();
            classB.DictionaryIntString = new(new KeyValuePair<int, string>[] { new KeyValuePair<int, string>(33, "rr") });
            classB.IDictionaryStringDouble = new Dictionary<string, double>(new KeyValuePair<string, double>[] { new KeyValuePair<string, double>("test", 33d) });

            var st = TinyhandSerializer.SerializeToString(classB);
            var classA2 = TinyhandSerializer.DeserializeFromString<TextSerializeClass1>(st);

            st = TinyhandSerializer.SerializeToString(classB, TinyhandSerializerOptions.Standard.WithCompose(TinyhandComposeOption.Simple));
            st = "{\"2int-string\" = {33 = \"rr\"}, \" st d \" = {test = 33}, \"double\" = false, Byte = 0, \"2\" = 77, MyClass0 = {99, \"\", \"Doe\", {}, null}, \"St{\" = \"test\", \"3 2\" = 0, Double = 1, Date = \"2021-02-09T10:20:29.7825986Z\", MyClass = {99, \"\", \"Doe\", {}, null}";
            Console.WriteLine(st);
            classA2 = TinyhandSerializer.DeserializeFromString<TextSerializeClass1>(st);*/

            var gt = new GenericTestClass2<int>(0, 1, new GenericTestClass2<int>.NestedClass<double, int>("0", 1.2, 3));
            st = TinyhandSerializer.SerializeToString(gt);
            gt = TinyhandSerializer.Deserialize<GenericTestClass2<int>>(TinyhandSerializer.Serialize(gt));

            var gt2 = new GenericTestClass2<double>(2, 3.1, new GenericTestClass2<double>.NestedClass<double, int>("1", 1.2, 3));
            st = TinyhandSerializer.SerializeToString(gt2);
            gt2 = TinyhandSerializer.Deserialize<GenericTestClass2<double>>(TinyhandSerializer.Serialize(gt2));

            /*var gt3 = new GenericTestClass2<NotSerializeClass>();
            st = TinyhandSerializer.SerializeToString(gt3);
            gt3 = TinyhandSerializer.Deserialize<GenericTestClass2<NotSerializeClass>>(TinyhandSerializer.Serialize(gt3));*/

            var gt3 = new GenericTestClass2<SerializeClass>(2, new SerializeClass(11), new GenericTestClass2<SerializeClass>.NestedClass<double, int>("1", 1.2, 3));
            st = TinyhandSerializer.SerializeToString(gt3);
            gt3 = TinyhandSerializer.Deserialize<GenericTestClass2<SerializeClass>>(TinyhandSerializer.Serialize(gt3));
        }
    }
}
