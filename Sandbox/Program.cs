using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using Tinyhand;

namespace Sandbox
{
    /*[TinyhandObject(KeyAsPropertyName = true)]
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
    }*/

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

    [TinyhandObject(KeyAsPropertyName = true, SkipSerializingDefaultValue = true)]
    public partial class DefaultTestClass
    {
        [DefaultValue(true)]
        public bool Bool { get; set; }

        [DefaultValue(11)]
        public sbyte SByte { get; set; }

        [DefaultValue((sbyte)22)]
        public byte Byte { get; set; }

        [DefaultValue((ulong)33)]
        public short Short { get; set; }

        [DefaultValue((byte)44)]
        public ushort UShort { get; set; }

        [DefaultValue(55)]
        public int Int { get; set; }

        [DefaultValue(66)]
        public uint UInt { get; set; }

        [DefaultValue(77)]
        public long Long { get; set; }

        [DefaultValue(88)]
        public ulong ULong { get; set; }

        [DefaultValue(1.23d)]
        public float Float { get; set; }

        [DefaultValue(456.789d)]
        public double Double { get; set; }

        [DefaultValue("2134.44")]
        public decimal Decimal { get; set; }

        [DefaultValue('c')]
        public char Char { get; set; }

        [DefaultValue("test")]
        public string String { get; set; } = default!;

        [DefaultValue(DefaultTestEnum.B)]
        public DefaultTestEnum Enum;
    }

    [TinyhandObject(KeyAsPropertyName = true)]
    public partial class StringEmptyClass
    {
    }

    public enum DefaultTestEnum
    {
        A,
        B,
        C,
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


    class Program
    {
        static void Main(string[] args)
        {
            //var tt = new GenericsTestClass<string>();
            //var tt2 = TinyhandSerializer.Deserialize<GenericsTestClass<string>>(TinyhandSerializer.Serialize(tt));

            var t = new StringEmptyClass();
            var t2 = TinyhandSerializer.Deserialize<DefaultTestClass>(TinyhandSerializer.Serialize(t));
            var b2 = TinyhandSerializer.Serialize(t2);

            var myClass = new SampleCallback();
            var b = TinyhandSerializer.Serialize(myClass);
            var myClass2 = TinyhandSerializer.Deserialize<SampleCallback>(b);
        }
    }
}
