using System;
using System.Diagnostics.CodeAnalysis;
using CrystalData;
using Tinyhand;
using Tinyhand.IO;

namespace Playground;

[TinyhandObject(AddImmutable = true)]
public partial class PartialImplementationClass
{
    public sealed class Immutable : ITinyhandSerializable<Immutable>, ITinyhandReconstructable<Immutable>, ITinyhandCloneable<Immutable>
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

    public Immutable CloneAndToImmutable() => new(TinyhandSerializer.CloneObject(this));


    public PartialImplementationClass()
    {
    }

    [Key(0)]
    public string Name { get; set; } = "Test";

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
