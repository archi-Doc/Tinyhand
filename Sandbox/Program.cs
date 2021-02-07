﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using Tinyhand;

namespace Sandbox
{
    [TinyhandObject(KeyAsPropertyName = true)]
    public partial class DefaultTestClass
    {
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
            var classB = TinyhandSerializer.Reconstruct<DefaultTestClass>();

            var st = TinyhandSerializer.SerializeToString(classB);
            var classA2 = TinyhandSerializer.DeserializeFromString<DefaultTestClass>(st);

            st = TinyhandSerializer.SerializeToString(classB, TinyhandSerializerOptions.Standard.WithCompose(TinyhandComposeOption.Simple));
            st = "double = true, Byte = 0, 2 = 77, MyClass0 = 33, \"St{\" = \"test\\\"\\\"\\\"e\", \"3 2\" = 0, Double = 1, Date = \"2021-02-06T13:17:48.7898669Z\", MyClass = {99, \"\", \"Doe\", {}, null}";
            classA2 = TinyhandSerializer.DeserializeFromString<DefaultTestClass>(st);
        }
    }
}
