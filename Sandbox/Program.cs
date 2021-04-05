using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using CrossLink;
using Tinyhand;

namespace Sandbox
{
    [CrossLinkObject]
    [TinyhandObject]
    public partial class TestClass1
    {
        [Link(Type = LinkType.Ordered)]
        [KeyAsName]
        private int id;

        [Link(Type = LinkType.Ordered)]
        [Key("NM")]
        private string name = default!;

        [Link(Type = LinkType.Ordered)]
        [KeyAsName]
        private byte age;

        [Link(Type = LinkType.StackList, Primary = true, Name = "Stack")]
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

        public void SetA(string a) => this.A = a;
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

        }
    }
}
