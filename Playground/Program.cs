﻿using System;
using System.ComponentModel;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using Tinyhand;
using Tinyhand.IO;
using ValueLink;

namespace Playground;

[TinyhandObject]
public partial struct DesignStruct
{
    [Key(0)]
    private int X { get; set; }

    [Key(1)]
    private int Y;

    [Key(2)]
    private readonly int Z;

    [TinyhandOnSerializing]
    public void OnSerializing()
    {
    }
}

[TinyhandObject(LockObject = "lockObject")]
public partial class DesignBaseClass
{
    protected DesignBaseClass()
    {
    }

    protected readonly Lock lockObject = new();

    [Key(0)]
    private int X { get; set; }

    [Key(1)]
    private int Y;

    [Key(2)]
    private readonly int Z;

    [Key(3)]
    public int A { get; set; }

    [TinyhandOnSerializing]
    private void OnSerializing()
    {
    }

    [TinyhandOnSerialized]
    protected void OnSerialized()
    {
    }

    [TinyhandOnDeserializing]
    protected void OnDeserializing()
    {
    }

    [TinyhandOnDeserialized]
    protected void OnDeserialized()
    {
    }

    [TinyhandOnReconstructing]
    protected void OnReconstructing()
    {
    }

    [TinyhandOnReconstructed]
    protected void OnReconstructed()
    {
    }
}

[TinyhandObject]
public partial class DesignDerivedClass : DesignBaseClass
{
    public static DesignDerivedClass New()
        => new();

    protected DesignDerivedClass()
    {
    }
}

/*[TinyhandObject]
public partial class TestClass3<T>
{
    [TinyhandObject]
    [ValueLinkObject]
    public partial class Item
    {
        [Link(Primary = true, Type = ChainType.QueueList, Name = "Queue")]
        public Item()
        {
            this.Value = default!;
        }

        public Item(T value)
        {
            this.Value = value;
        }

        [Key(0)]
        public T Value { get; set; }

        public override string ToString()
            => $"Item {this.Value?.ToString()}";
    }

    [Key(0)]
    public Item.GoshujinClass Items = new();

    [Key(1)]
    public bool B { get; set; }

    [Key(2)]
    public byte[] C { get; set; } = Array.Empty<byte>();
}*/

[TinyhandObject]
public partial class TestClass2 : TestAssembly.TestClass
{
    [Key(2)]
    public int Age2 { get; private set; }
}

[TinyhandObject]
public partial class Credential : CertificateToken<TestClass>
{
    public Credential()
    {
    }
}

[TinyhandObject]
public partial class CertificateToken<T>
    where T : ITinyhandSerializable<T>
{
    private const char Identifier = 'C';

    public CertificateToken()
    {
        this.Target = default!;
    }

    public CertificateToken(T target)
    {
        this.Target = target;
    }

    public static int MaxStringLength => 256;

    [Key(0)]
    public char TokenIdentifier { get; private set; } = Identifier;//

    [Key(2, Level = 1)]
    public byte[] Signature { get; set; } = Array.Empty<byte>();

    [Key(3)]
    public long SignedMics { get; set; }

    [Key(4)]
    public ulong Salt { get; set; }

    [Key(5)]
    public T Target
    {
        get; set;
    }
}

[TinyhandObject]
public partial class TestClass
{
    public TestClass()
    {
    }

    [Key(0)]
    public string Name { get; set; } = string.Empty;
}

/*[TinyhandObject]
[ValueLinkObject(Integrality = true)]
public partial class GenericIntegralityClass2<T>
    where T : ITinyhandSerializable<T>
{
    [Link(Primary = true, Unique = true, Type = ChainType.Unordered, TargetMember = "Id2")]
    public GenericIntegralityClass2()
    {
    }

    public int Id2 => this.id2;

    [Key(1)]
    public T Value { get; set; } = default!;

    [Key(0)]
    private int id2;

    [Key(2, AddProperty = "Name")]
    private string name = string.Empty;//
}*/

[TinyhandObject]
public partial class GenericTestClass3 : GenericTestClass2<TestClass>
{
}

[TinyhandObject]
public partial class GenericTestClass2<T>
    where T : ITinyhandSerializable<T>
{
    public GenericTestClass2()
    {
    }

    [Key(0)]
    public char Id { get; private set; }

    [Key(1)]
    public T Value { get; set; } = default!;
}

[TinyhandObject]
public partial class IdentifierClass<T>
{
    public IdentifierClass()
    {
    }

    public IdentifierClass(T value)
    {
        this.Value = value;
    }

    [Key(0)]
    public T Value { get; set; } = default!;
}


internal class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
    }
}
