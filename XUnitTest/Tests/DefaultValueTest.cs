// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.ComponentModel;
using System.Linq;
using Tinyhand;
using Xunit;

namespace Tinyhand.Tests;

[TinyhandObject(ImplicitKeyAsName = true)]
public partial class DefaultTestClass
{
    public sbyte SByte { get; set; } = 11;

    public byte Byte { get; set; } = 22;

    public short Short { get; set; } = 33;

    public ushort UShort { get; set; } = 44;

    public int Int { get; set; } = 55;

    public uint UInt { get; set; } = 66;

    public long Long { get; init; } = 77;

    public partial ulong ULong { get; init; } = 88;

    public float Float { get; set; } = 1.23f;

    public double Double { get; set; } = 456.789d;

    public decimal Decimal { get; set; } = 2134.44m;

    public char Char { get; set; } = 'c';

    public string String { get; set; } = "test";

    public partial string String2 { get; private set; } = "test2";

    public DefaultTestEnum Enum = DefaultTestEnum.B;

    public DefaultTestClassName NameClass { get; set; }

    public byte[]? ByteArray { get; set; } = null;
}

[TinyhandObject(SkipSerializingDefaultValue = true)]
public partial class DefaultTestClassSkip
{
    [Key(0)]
    public decimal Decimal { get; set; } = 2134.44m;

    [Key(1)]
    public int Int { get; set; } = 1234;

    [Key(2)]
    public string String { get; set; } = "test";
}

[TinyhandObject(SkipSerializingDefaultValue = true)]
public partial class DefaultTestClassSkip2
{
    [Key(0)]
    public short Short { get; set; } = 1234;

    [Key(1)]
    public string String { get; set; } = "test";

    [Key(2)]
    public double Double { get; set; } = 456.789d;
}

[TinyhandObject]
public partial class DefaultTestClassName : ITinyhandDefault
{
    public DefaultTestClassName()
    {
        this.Name2 = "Test2";
    }

    public bool CanSkipSerialization()
        => this.Name == "Test" && this.Name2 == "Test2";

    [Key(0)]
    public string Name { get; private set; } = "Test";

    [Key(1)]
    public string Name2 { get; private set; }
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
    public DefaultTestStruct()
    {
    }

    public int Int { get; set; } = 123;

    public DefaultTestStructDouble DoubleStruct { get; set; }
}

[TinyhandObject]
public partial struct DefaultTestStructDouble : ITinyhandDefault
{
    public DefaultTestStructDouble()
    {
    }

    public bool CanSkipSerialization()
        => this.Double == 1.23d;

    [Key(1)]
    public double Double { get; private set; } = 1.23d;

    [Key(0)]
    public EmptyClass EmptyClass { get; set; }
}

public partial class DefaultValueTest
{
    [Fact]
    public void TestClass()
    {
        var t = new Empty2();
        var b = TinyhandSerializer.Serialize(t);
        var t2 = TinyhandSerializer.Deserialize<DefaultTestClass>(b);

        // t2.Bool.IsTrue();
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
        Assert.Equal("test2", t2.String2);
        Assert.Equal<DefaultTestEnum>(DefaultTestEnum.B, t2.Enum);
        Assert.Equal("Test", t2.NameClass.Name);
        Assert.Equal("Test2", t2.NameClass.Name2);

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

        // t2.Bool.IsTrue();
        t2.Int.Is(123);
        t2.DoubleStruct.Double.Is(1.23d);
        t2.DoubleStruct.EmptyClass.IsNotNull();

        var t3 = TinyhandSerializer.Reconstruct<DefaultTestStruct>();
        t3.IsStructuralEqual(t2);
    }

    [TinyhandObject(AddSignatureId = false)]
    public partial class TokenClassV1
    {// Set to false because adding the SignatureId would result in different signatures between TokenClassV1 and TokenClassV2.
        [Key(0)]
        public int Id { get; set; }

        [Key(1)]
        public string Name { get; set; } = string.Empty;

        [Key(2, Level = 1)]
        public byte[] Signature = Array.Empty<byte>();
    }

    [TinyhandObject(AddSignatureId = false)]
    public partial class TokenClassV2
    {
        [Key(0)]
        public int Id { get; set; }

        [Key(1)]
        public string Name { get; set; } = string.Empty;

        [Key(2, Level = 1)]
        public byte[] Signature = Array.Empty<byte>();

        [Key(3)]
        public long AddedMember { get; set; } = -10;

        [Key(4)]
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
