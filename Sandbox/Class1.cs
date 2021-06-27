using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tinyhand;

namespace Sandbox
{
    [TinyhandObject(ImplicitKeyAsName = true)]
    public partial class NullableTestClass1
    {
        public string[] A = default!;
        public string[]? B = default!;
        public string?[] C = default!;
        public string?[]? D = default!;

        public KeyValuePair<int, double> X1 = default!;
        public KeyValuePair<int?, double> X2 = default!;
        public KeyValuePair<int?, double?> X3 = default!;
        public KeyValuePair<int, double>? X4 = default!;
        public KeyValuePair<int?, double>? X5 = default!;
        public KeyValuePair<int?, double?>? X6 = default!;
        public KeyValuePair<int?, KeyValuePair<int?, double?>?>? X7 = default!;
    }

    [TinyhandObject(ImplicitKeyAsName = true)]
    public partial class CloneTestClass1
    {
        public Memory<byte> MemoryByte { get; set; } = new(new byte[] { 1, 10, 20, });
        public ReadOnlyMemory<byte> ReadOnlyMemoryByte { get; set; } = new(new byte[] { 1, 10, 20, });
        public ReadOnlySequence<byte> ReadOnlySequenceByte { get; set; } = new(new byte[] { 1, 10, 20, });
    }

    /*[TinyhandObject(ImplicitKeyAsName = true)]
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

    [TinyhandObject] // Annote a [TinyhandObject] attribute.
    public partial class MyClass // partial class is required for source generator.
    {
        // Key attributes take a serialization index (or string name)
        // The values must be unique and versioning has to be considered as well.
        [Key(0)]
        [DefaultValue(99)]
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
        }
    }*/
}
