// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tinyhand;
using Xunit;

#pragma warning disable SA1139

namespace Tinyhand.Tests
{
    [TinyhandObject(KeyAsPropertyName = true)]
    public partial class DefaultTestClass
    {
        [DefaultValue(true)]
        public bool Bool { get; set; }

        [DefaultValue(11)]
        public sbyte SByte { get; set; }

        [DefaultValue(22)] // [DefaultValue((sbyte)22)] disabled because of SA1139 analyzer crash
        public byte Byte { get; set; }

        [DefaultValue(33)] // [DefaultValue((ulong)33)]
        public short Short { get; set; }

        [DefaultValue(44)]
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

    [TinyhandObject(SkipSerializingDefaultValue = true)]
    public partial class DefaultTestClassSkip
    {
        [Key(0)]
        [DefaultValue("2134.44")]
        public decimal Decimal { get; set; }

        [Key(1)]
        [DefaultValue(1234)]
        public int Int { get; set; }

        [Key(2)]
        [DefaultValue("test")]
        public string String { get; set; } = default!;
    }

    [TinyhandObject(SkipSerializingDefaultValue = true)]
    public partial class DefaultTestClassSkip2
    {
        [Key(0)]
        [DefaultValue(1234)]
        public short Short { get; set; }

        [Key(1)]
        [DefaultValue("test")]
        public string String { get; set; } = default!;

        [Key(2)]
        [DefaultValue(456.789d)]
        public double Double { get; set; }
    }

    public enum DefaultTestEnum
    {
        A,
        B,
        C,
    }

    public class DefaultValueTest
    {
        [Fact]
        public void Test1()
        {
            var t = new Empty2();
            var t2 = TinyhandSerializer.Deserialize<DefaultTestClass>(TinyhandSerializer.Serialize(t));

            t2.Bool.IsTrue();
            Assert.Equal<sbyte>(11, t2.SByte);
            Assert.Equal<byte>(22, t2.Byte);
            Assert.Equal<short>(33, t2.Short);
            Assert.Equal<ushort>(44, t2.UShort);
            Assert.Equal<int>(55, t2.Int);
            Assert.Equal<uint>(66, t2.UInt);
            Assert.Equal<long>(77, t2.Long);
            Assert.Equal<ulong>(88, t2.ULong);
            Assert.Equal<float>(1.23f, t2.Float);
            Assert.Equal<double>(456.789d, t2.Double);
            Assert.Equal<decimal>(2134.44m, t2.Decimal);
            Assert.Equal<char>('c', t2.Char);
            Assert.Equal("test", t2.String);
            Assert.Equal<DefaultTestEnum>(DefaultTestEnum.B, t2.Enum);
        }

        [Fact]
        public void TestSkip()
        {
            var e = new Empty2();
            var t = TinyhandSerializer.Deserialize<DefaultTestClassSkip>(TinyhandSerializer.Serialize(e));
            var b = TinyhandSerializer.Serialize(t);

            var t2 = TinyhandSerializer.Deserialize<DefaultTestClassSkip2>(TinyhandSerializer.Serialize(e));
            var b2 = TinyhandSerializer.Serialize(t2);

            b.IsStructuralEqual(b2);
        }
        }
}
