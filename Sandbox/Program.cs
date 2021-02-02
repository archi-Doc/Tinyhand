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

        public float Float { get; set; }

        [DefaultValue(1d)]
        public double Double { get; set; }

        public DateTime Date { get; set; } = DateTime.UtcNow;
    }

    class Program
    {
        static void Main(string[] args)
        {
            var classB = TinyhandSerializer.Reconstruct<DefaultTestClass>();

            var st = TinyhandSerializer.SerializeToString(classB);
            var classA2 = TinyhandSerializer.DeserializeFromString<DefaultTestClass>(st);
        }
    }
}
