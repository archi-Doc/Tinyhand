using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using Tinyhand;

namespace Sandbox
{
    [TinyhandObject(KeyAsPropertyName = true)]
    public partial class TextSerializeClass1
    {
        [Key("2int-string")]
        public Dictionary<int, string> DictionaryIntString { get; set; } = default!;

        [Key(" st d ")]
        public IDictionary<string, double> IDictionaryStringDouble { get; set; } = default!;

        [Key("double")]
        [DefaultValue(true)]
        public bool Bool { get; set; }

        public byte Byte { get; set; }

        [Key("2")]
        [DefaultValue(77)]
        public int Int { get; set; }

        public MyClass MyClass0 { get; set; } = default!;

        [Key("St{")]
        [DefaultValue("test\"\"\"e")]
        public string String { get; set; } = default!;

        [Key("3 2")]
        public float Float { get; set; }

        [DefaultValue(1d)]
        public double Double { get; set; }

        public DateTime Date { get; set; } = DateTime.UtcNow;

        public MyClass MyClass { get; set; } = default!;
    }

    class Program
    {
        static void Main(string[] args)
        {
            var classB = TinyhandSerializer.Reconstruct<TextSerializeClass1>();
            classB.DictionaryIntString = new(new KeyValuePair<int, string>[] { new KeyValuePair<int, string>(33, "rr") });
            classB.IDictionaryStringDouble = new Dictionary<string, double>(new KeyValuePair<string, double>[] { new KeyValuePair<string, double>("test", 33d) });

            var st = TinyhandSerializer.SerializeToString(classB);
            var classA2 = TinyhandSerializer.DeserializeFromString<TextSerializeClass1>(st);

            st = TinyhandSerializer.SerializeToString(classB, TinyhandSerializerOptions.Standard.WithCompose(TinyhandComposeOption.Simple));
            // st = "\"2int-string\" = {33 = \"rr\"}, \" st d \" = {test = 33}, \"double\" = 33, Byte = 0, \"2\" = 77, MyClass0 = {99, \"\", \"Doe\", {}, null}, \"St{\" = \"test\", \"3 2\" = 0, Double = 1, Date = \"2021-02-09T10:20:29.7825986Z\", MyClass = {99, \"\", \"Doe\", {}, null}";
            classA2 = TinyhandSerializer.DeserializeFromString<TextSerializeClass1>(st);
        }
    }
}
