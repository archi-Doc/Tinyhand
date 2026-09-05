// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using Tinyhand;
using Tinyhand.Formatters;
using Tinyhand.IO;
using Tinyhand.Resolvers;
using ValueLink;

[assembly: TinyhandRegister(typeof(CustomExternal))]

if (args.Contains("--require-native") && RuntimeFeature.IsDynamicCodeSupported)
{
    throw new InvalidOperationException("Publish and run this test as a NativeAOT executable.");
}

var owner = new Item.GoshujinClass { new Item { Id = 12 } };
var copy = Roundtrip(owner);
Check(copy.Single().Id == 12, "owner roundtrip");
Check(TinyhandSerializer.Reconstruct<Item.GoshujinClass>().Count == 0, "owner reconstruction");
var clone = TinyhandSerializer.Clone(owner)!;
Check(clone.Single().Id == 12 && !ReferenceEquals(owner.Single(), clone.Single()), "owner clone");

var generic = new GenericItem<int>.Owners { new GenericItem<int> { Id = 23, Value = 42 } };
Check(Roundtrip(generic).Single().Value == 42, "custom generic owner roundtrip");
Check(TinyhandSerializer.Reconstruct<GenericItem<int>.Owners>().Count == 0, "custom generic owner reconstruction");
var genericClone = TinyhandSerializer.Clone(generic)!;
Check(genericClone.Single().Value == 42 && !ReferenceEquals(generic.Single(), genericClone.Single()), "custom generic owner clone");

// Use the resolver APIs so these checks require formatter registration.
var manual = new ManualExternal { Number = 51 };
Check(Roundtrip(manual).Number == 51, "manual external roundtrip");
Check(TinyhandSerializer.Reconstruct<ManualExternal>().Number == 0, "manual external reconstruction");
Check(TinyhandSerializer.Clone(manual) is { Number: 51 } manualClone && !ReferenceEquals(manual, manualClone), "manual external clone");

GeneratedResolver.Instance.SetFormatter(new CustomExternalFormatter());
var custom = new CustomExternal { Number = 73 };
Check(Roundtrip(custom).Number == 73, "custom formatter roundtrip");
Check(TinyhandSerializer.Reconstruct<CustomExternal>().Number == 0, "custom formatter reconstruction");
Check(TinyhandSerializer.Clone(custom) is { Number: 73 } customClone && !ReferenceEquals(custom, customClone), "custom formatter clone");
Console.WriteLine($"External registration checks passed ({(RuntimeFeature.IsDynamicCodeSupported ? "JIT" : "NativeAOT")}).");

static T Roundtrip<T>(T value) => TinyhandSerializer.Deserialize<T>(TinyhandSerializer.Serialize(value))!;

static void Check(bool success, string operation)
{
    if (!success)
    {
        throw new InvalidOperationException(operation);
    }
}

// These unused declarations previously caused CS0311 even without serialization calls.
[TinyhandObject(External = true)]
public partial class Orphan { }

[TinyhandObject]
public partial class Container
{
    [TinyhandObject(External = true)]
    public partial class GoshujinClass { }
}

[TinyhandObject]
[ValueLinkObject]
public partial class Item
{
    [Key(0)]
    [Link(Type = ChainType.Ordered, Primary = true)]
    public int Id { get; set; }

    [TinyhandObject(External = true)]
    public partial class GoshujinClass { }
}

[TinyhandObject]
[ValueLinkObject(GoshujinClass = "Owners")]
public partial class GenericItem<T>
{
    [Key(0)]
    [Link(Type = ChainType.Ordered, Primary = true)]
    public int Id { get; set; }

    [Key(1)]
    public T Value { get; set; } = default!;

    [TinyhandObject(External = true)]
    public partial class Owners { }
}

[TinyhandObject(External = true)]
public partial class ManualExternal : ITinyhandSerializable<ManualExternal>, ITinyhandReconstructable<ManualExternal>, ITinyhandCloneable<ManualExternal>
{
    public int Number { get; set; }

    public static void Serialize(ref TinyhandWriter writer, scoped ref ManualExternal? value, TinyhandSerializerOptions options) => writer.Write(value!.Number);

    public static void Deserialize(ref TinyhandReader reader, scoped ref ManualExternal? value, TinyhandSerializerOptions options) => value = new() { Number = reader.ReadInt32() };

    public static void Reconstruct([NotNull] scoped ref ManualExternal? value, TinyhandSerializerOptions options) => value = new();

    public static ManualExternal? Clone(scoped ref ManualExternal? value, TinyhandSerializerOptions options) => value is null ? null : new() { Number = value.Number };
}

[TinyhandObject(External = true)]
public partial class CustomExternal
{
    public int Number { get; set; }
}

public sealed class CustomExternalFormatter : ITinyhandFormatter<CustomExternal>
{
    public void Serialize(ref TinyhandWriter writer, CustomExternal? value, TinyhandSerializerOptions options) => writer.Write(value!.Number);

    public void Deserialize(ref TinyhandReader reader, ref CustomExternal? value, TinyhandSerializerOptions options) => value = new() { Number = reader.ReadInt32() };

    public CustomExternal Reconstruct(TinyhandSerializerOptions options) => new();

    public CustomExternal? Clone(CustomExternal? value, TinyhandSerializerOptions options) => value is null ? null : new() { Number = value.Number };
}
