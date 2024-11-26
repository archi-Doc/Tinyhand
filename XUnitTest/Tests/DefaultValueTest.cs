// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection.Emit;
using Tinyhand;
using Xunit;

namespace Tinyhand.Tests;

[TinyhandObject(ImplicitKeyAsName = true)]
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

    [DefaultValue("Test")]
    // [DefaultValue(new[] { "1", "2" })]
    public DefaultTestClassName NameClass { get; set; }
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

[TinyhandObject]
public partial class DefaultTestClassName : ITinyhandDefault<string>
{
    public DefaultTestClassName()
    {
    }

    public void SetDefaultValue(string defaultValue)
    {
        this.Name = defaultValue;
    }

    public bool CanSkipSerialization()
        => false;

    [Key(0)]
    public string Name { get; private set; }
}

public enum DefaultTestEnum
{
    A,
    B,
    C,
}

[TinyhandObject(ImplicitKeyAsName = true)]
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
public partial struct DefaultTestStructDouble : ITinyhandDefault<double>
{
    public bool CanSkipSerialization()
        => false;

    public void SetDefaultValue(double defaultValue)
    {
        this.Double = defaultValue;
    }

    public double Double { get; private set; }

    [Key(0)]
    public EmptyClass EmptyClass { get; set; }
}

public partial class DefaultValueTest
{
    [Fact]
    public void TestClass()
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
        Assert.Equal("Test", t2.NameClass.Name);

        var t3 = TinyhandSerializer.Reconstruct<DefaultTestClass>();
        t3.IsStructuralEqual(t2);
    }

    [Fact]
    public void TestSkip()
    {
        var t = TinyhandSerializer.Reconstruct<DefaultTestClassSkip>();
        var b = TinyhandSerializer.Serialize(t);

        var t2 = TinyhandSerializer.Reconstruct<DefaultTestClassSkip2>();
        var b2 = TinyhandSerializer.Serialize(t2);

        b.IsStructuralEqual(b2);
    }

    [Fact]
    public void TestStruct()
    {
        var t = new Empty2();
        var t2 = TinyhandSerializer.Deserialize<DefaultTestStruct>(TinyhandSerializer.Serialize(t));

        t2.Bool.IsTrue();
        t2.Int.Is(123);
        t2.DoubleStruct.Double.Is(1.23d);
        t2.DoubleStruct.EmptyClass.IsNotNull();

        var t3 = TinyhandSerializer.Reconstruct<DefaultTestStruct>();
        t3.IsStructuralEqual(t2);
    }

    [TinyhandObject]
    public partial class TokenClassV1
    {
        [Key(0)]
        public int Id { get; set; }

        [Key(1)]
        public string Name { get; set; } = string.Empty;

        [Key(2, Level = 1)]
        public byte[] Signature = Array.Empty<byte>();
    }

    [TinyhandObject]
    public partial class TokenClassV2
    {
        [Key(0)]
        public int Id { get; set; }

        [Key(1)]
        public string Name { get; set; } = string.Empty;

        [Key(2, Level = 1)]
        public byte[] Signature = Array.Empty<byte>();

        [Key(3)]
        [DefaultValue(0)]
        public long AddedMember { get; set; }

        [Key(4)]
        [DefaultValue(null)]
        public byte[]? AddedBytes { get; set; } = null;
    }

    [Fact]
    public void TestToken()
    {
        var v1 = new TokenClassV1();
        v1.Id = 1;
        v1.Name = "Test";
        v1.Signature = TinyhandSerializer.SerializeSignature(v1, 0);
        v1.Signature.SequenceEqual(TinyhandSerializer.SerializeSignature(v1, 0)).IsTrue();

        var v2 = new TokenClassV2();
        v2.Id = 1;
        v2.Name = "Test";
        v2.AddedMember = 1;
        v2.Signature = TinyhandSerializer.SerializeSignature(v2, 0);
        v2.Signature.SequenceEqual(TinyhandSerializer.SerializeSignature(v2, 0)).IsTrue();
        v2.Signature.SequenceEqual(v1.Signature).IsFalse();

        var b1 = TinyhandSerializer.Serialize(v1);
        v2 = TinyhandSerializer.Deserialize<TokenClassV2>(b1);
        var sig = TinyhandSerializer.SerializeSignature(v2, 0);
        v2.Signature.SequenceEqual(sig).IsTrue();
    }
}
