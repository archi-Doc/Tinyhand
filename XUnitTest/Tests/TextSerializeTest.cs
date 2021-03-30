// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tinyhand;
using Xunit;

namespace Tinyhand.Tests
{
    [TinyhandObject(ImplicitKeyAsName = true)]
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

    public class TextSerializeTest
    {
        [Fact]
        public void Test1()
        {// Requires visual assessment.
            string st;
            var simple = TinyhandSerializerOptions.Standard.WithCompose(TinyhandComposeOption.Simple);

            var c1 = TinyhandSerializer.Reconstruct<TextSerializeClass1>();
            c1.DictionaryIntString = new(new KeyValuePair<int, string>[] { new KeyValuePair<int, string>(33, "rr") });
            c1.IDictionaryStringDouble = new Dictionary<string, double>(new KeyValuePair<string, double>[] { new KeyValuePair<string, double>("test", 33d) });

            st = TinyhandSerializer.SerializeToString(c1);
            var c2 = TinyhandSerializer.DeserializeFromString<TextSerializeClass1>(st);
            c1.IsStructuralEqual(c2);

            st = TinyhandSerializer.SerializeToString(c1, simple);
            c2 = TinyhandSerializer.DeserializeFromString<TextSerializeClass1>(st);
            c1.IsStructuralEqual(c2);
        }

        [Fact]
        public void Test2()
        {// Requires visual assessment.
            string st;
            var simple = TinyhandSerializerOptions.Standard.WithCompose(TinyhandComposeOption.Simple);

            var c1 = TinyhandSerializer.Reconstruct<SimpleIntKeyData>();
            st = TinyhandSerializer.SerializeToString(c1);
            c1.IsStructuralEqual(TinyhandSerializer.DeserializeFromString<SimpleIntKeyData>(st));
            st = TinyhandSerializer.SerializeToString(c1, simple);
            c1.IsStructuralEqual(TinyhandSerializer.DeserializeFromString<SimpleIntKeyData>(st));

            var c2 = TinyhandSerializer.Reconstruct<EmptyClass>();
            st = TinyhandSerializer.SerializeToString(c2);
            c2.IsStructuralEqual(TinyhandSerializer.DeserializeFromString<EmptyClass>(st));
            st = TinyhandSerializer.SerializeToString(c2, simple);
            c2.IsStructuralEqual(TinyhandSerializer.DeserializeFromString<EmptyClass>(st));

            var c3 = TinyhandSerializer.Reconstruct<EmptyClass2>();
            st = TinyhandSerializer.SerializeToString(c3);
            c3.IsStructuralEqual(TinyhandSerializer.DeserializeFromString<EmptyClass2>(st));
            st = TinyhandSerializer.SerializeToString(c3, simple);
            c3.IsStructuralEqual(TinyhandSerializer.DeserializeFromString<EmptyClass2>(st));

            var c4 = new FormatterResolverClass();
            st = TinyhandSerializer.SerializeToString(c4);
            var c5 = TinyhandSerializer.DeserializeFromString<FormatterResolverClass>(st);
            c5.ObjectArray = c4.ObjectArray; // avoid int != byte issue
            c5.ObjectList = c4.ObjectList; // avoid int != byte issue
            var b = MessagePack.MessagePackSerializer.Serialize<FormatterResolverClass>(c4);
            var b2 = TinyhandSerializer.Serialize<FormatterResolverClass>(c5);
            b.IsStructuralEqual(b2);

            st = TinyhandSerializer.SerializeToString(c4, simple);
            // var json = MessagePack.MessagePackSerializer.ConvertToJson(MessagePack.MessagePackSerializer.Serialize<FormatterResolverClass>(c4));
            // var cc = MessagePack.MessagePackSerializer.Deserialize<FormatterResolverClass>(MessagePack.MessagePackSerializer.ConvertFromJson(json));
            c5 = TinyhandSerializer.DeserializeFromString<FormatterResolverClass>(st);
            c5.ObjectArray = c4.ObjectArray; // avoid int != byte issue
            c5.ObjectList = c4.ObjectList; // avoid int != byte issue
            b = MessagePack.MessagePackSerializer.Serialize<FormatterResolverClass>(c4);
            b2 = TinyhandSerializer.Serialize<FormatterResolverClass>(c5);
            b.IsStructuralEqual(b2);
        }
    }
}
