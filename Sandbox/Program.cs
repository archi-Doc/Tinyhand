using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using Tinyhand;

namespace Sandbox
{
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

    class Program
    {
        static void Main(string[] args)
        {
            var classA = new DefaultTestClass();
            var classB = TinyhandSerializer.Reconstruct<DefaultTestClass>();

            var st = TinyhandSerializer.SerializeToString(classA);
            var classA2 = TinyhandSerializer.DeserializeFromString<DefaultTestClass>(st);

            st = TinyhandSerializer.SerializeToString(classB);
            classA2 = TinyhandSerializer.DeserializeFromString<DefaultTestClass>(st);
        }
    }
}
