using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using CrystalData;
using Tinyhand;
using Tinyhand.IO;

namespace Playground;

public enum TestEnum
{
    A,
    B,
    C,
    D,
}

[TinyhandObject]
public partial class DefaultValueTestClass
{
    [Key(0)]
    private int a = 0;

    [Key(1)]
    private short b = -1;

    [Key(2)]
    private long c = 1 + 2 + 3;

    [Key(3)]
    private float d = 0.1f;

    [Key(4)]
    public double e = 0.1 + 123d;

    [Key(5)]
    public double f = double.PositiveInfinity;

    [Key(6)]
    public uint g = default;

    [Key(7)]
    public uint h = default!;

    [Key(8)]
    public string i = string.Empty;

    [Key(9)]
    public string? j = null;

    [Key(10)]
    public string k = "Test";

    [Key(11)]
    public string l = "test\"\"\"e";

    [Key(12)]
    public byte[] m = [];

    [Key(13)]
    public byte[]? n = null;

    [Key(14)]
    public byte[]? o = default;

    [Key(15)]
    public TestEnum p { get; set; } = TestEnum.C;
}

[TinyhandObject(AddImmutable = true)]
public partial class PartialImplementationClass
{
    /*public sealed class Immutable : ITinyhandSerializable<Immutable>, ITinyhandReconstructable<Immutable>, ITinyhandCloneable<Immutable>
    {
        private readonly PartialImplementationClass underlyingObject;

        public Immutable(PartialImplementationClass obj)
        {
            this.underlyingObject = obj;
        }

        public PartialImplementationClass GetUnderlyingObject() => this.underlyingObject;

        static void ITinyhandSerializable<Immutable>.Serialize(ref TinyhandWriter writer, scoped ref Immutable? value, TinyhandSerializerOptions options)
        {
            if (value is null) writer.WriteNil();
            else TinyhandSerializer.SerializeObject(ref writer, in value.underlyingObject, options);
        }

        static void ITinyhandSerializable<Immutable>.Deserialize(ref TinyhandReader reader, scoped ref Immutable? value, TinyhandSerializerOptions options)
        {
            if (TinyhandSerializer.DeserializeObject<PartialImplementationClass>(ref reader, options) is { } obj) value = new(obj);
        }

        static Immutable? ITinyhandCloneable<Immutable>.Clone(scoped ref Immutable? value, TinyhandSerializerOptions options)
        {
            if (TinyhandSerializer.CloneObject(value?.underlyingObject, options) is { } obj) return new(obj);
            else return default;
        }

        static void ITinyhandReconstructable<Immutable>.Reconstruct([NotNull] scoped ref Immutable? value, TinyhandSerializerOptions options)
        {
            value ??= new(TinyhandSerializer.ReconstructObject<PartialImplementationClass>(options));
        }

        public string Name => this.underlyingObject.Name;
    }

    public Immutable ToImmutable() => new(this);

    public Immutable CloneAndToImmutable() => new(TinyhandSerializer.CloneObject(this));*/

    public PartialImplementationClass()
    {
    }

    [Key(0)]
    [MaxLength(100)]
    [DefaultValue("Test2")]
    public partial string Name { get; set; } = "Test" + "2";

    [Key(1)]
    public int Id { get; set; }

    [Key(2)]
    public StoragePoint<int> IntStorage { get; set; } = new();

    static void ITinyhandSerializable<PartialImplementationClass>.Serialize(ref TinyhandWriter writer, scoped ref PartialImplementationClass? v, TinyhandSerializerOptions options)
    {
        if (v == null)
        {
            writer.WriteNil();
            return;
        }

        if (!options.IsSignatureMode) writer.WriteArrayHeader(2);
        writer.Write(v.Name);
        writer.Write(v.Id);
    }
}

internal class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
    }
}
