using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using Tinyhand;

namespace Sandbox
{
    [TinyhandObject(KeyAsPropertyName = true)]
    public partial class GenericsTestClass<T>
    {
        [DefaultValue(12)]
        public int Int { get; set; } // 12

        public T TValue { get; set; } = default!;

        [TinyhandObject]
        public partial class GenericsNestedClass<U>
        {
            [Key(0)]
            [DefaultValue("TH")]
            public string String { get; set; } = default!; // 12

            [Key(1)]
            public U UValue { get; set; } = default!;
        }

        [TinyhandObject]
        public partial class GenericsNestedClass2
        {
            [Key(0)]
            public string String { get; set; } = default!; // 12
        }

        public GenericsNestedClass<double> NestedClass { get; set; } = new();

        public GenericsNestedClass2 NestedClass2 { get; set; } = new();

        public GenericsTestClass2<int> ClassInt { get; set; } = new();
    }

    [TinyhandObject(KeyAsPropertyName = true)]
    public partial class GenericsTestClass2<V>
    {
        public V VValue { get; set; } = default!;
    }

    public class GenericsTest
    {
        public void Test1()
        {
            var t = TinyhandSerializer.Reconstruct<GenericsTestClass<string>>();
            var t2 = TinyhandSerializer.Reconstruct<GenericsTestClass<long>>();
        }
    }

    [TinyhandObject]
    public partial class EmptyClass
    {
    }

    [TinyhandObject(KeyAsPropertyName = true)]
    public partial class ReconstructTestClass
    {
        [DefaultValue(12)]
        public int Int { get; set; } // 12

        public EmptyClass EmptyClass { get; set; } = default!; // new()

        [Reconstruct(false)]
        public EmptyClass EmptyClassOff { get; set; } = default!; // null

        public EmptyClass? EmptyClass2 { get; set; } // null

        [Reconstruct(true)]
        public EmptyClass? EmptyClassOn { get; set; } // new()

        /* Error. A class to be reconstructed must have a default constructor.
        [IgnoreMember]
        [Reconstruct(true)]
        public ClassWithoutDefaultConstructor WithoutClass { get; set; }*/

        [IgnoreMember]
        [Reconstruct(true)]
        public ClassWithDefaultConstructor WithClass { get; set; } = default!;
    }

    public class ClassWithoutDefaultConstructor
    {
        public string Name = string.Empty;

        public ClassWithoutDefaultConstructor(string name)
        {
            this.Name = name;
        }
    }

    public class ClassWithDefaultConstructor
    {
        public string Name = string.Empty;

        public ClassWithDefaultConstructor(string name)
        {
            this.Name = name;
        }

        public ClassWithDefaultConstructor()
            : this(string.Empty)
        {
        }
    }

    [TinyhandObject(KeyAsPropertyName = true)]
    public partial class StringEmptyClass
    {
    }

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
    public partial struct DefaultTestStruct
    {
        [DefaultValue(true)]
        public bool Bool { get; set; }

        [DefaultValue(123)]
        public int Int { get; set; }

        [DefaultValue(1.23d)]
        public DefaultTestStructDouble DoubleStruct { get; set; }
    }

    [TinyhandObject]
    public partial struct DefaultTestStructDouble
    {
        public void SetDefault(double d)
        {
            this.Double = d;
        }

        public double Double { get; private set; }

        [Key(0)]
        public EmptyClass EmptyClass { get; set; }
    }


    class Program
    {
        static void Main(string[] args)
        {
            var t3 = TinyhandSerializer.Reconstruct<DefaultTestStruct>();

            var tt = new GenericsTestClass<string>();
            var tt2 = TinyhandSerializer.Deserialize<GenericsTestClass<string>>(TinyhandSerializer.Serialize(tt));


            var myClass = new SampleCallback();
            var b = TinyhandSerializer.Serialize(myClass);
            var myClass2 = TinyhandSerializer.Deserialize<SampleCallback>(b);
        }
    }
}
