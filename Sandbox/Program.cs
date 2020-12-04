using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using Tinyhand;

namespace Sandbox
{
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
            var t = new StringEmptyClass();
            var t2 = TinyhandSerializer.Deserialize<DefaultTestClass>(TinyhandSerializer.Serialize(t));
            var b2 = TinyhandSerializer.Serialize(t2);

            var myClass = new SampleCallback();
            var b = TinyhandSerializer.Serialize(myClass);
            var myClass2 = TinyhandSerializer.Deserialize<SampleCallback>(b);
        }
    }
}
