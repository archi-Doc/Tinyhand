// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using Tinyhand;
using Tinyhand.IO;

[assembly: TinyhandRegister(typeof(Dictionary<string, Envelope<Item[]>>))]

if (RuntimeFeature.IsDynamicCodeSupported)
{
    throw new InvalidOperationException("This test must run as a NativeAOT executable.");
}

var item = new Item { Number = 42, Text = "NativeAOT", Id = Guid.Parse("00112233-4455-6677-8899-aabbccddeeff"), Amount = 123.45m };
foreach (var options in new[] { TinyhandSerializerOptions.Standard, TinyhandSerializerOptions.Lz4, TinyhandSerializerOptions.Standard with { Security = TinyhandSecurity.UntrustedData } })
{
    var source = new Envelope<Item[]> { Value = [item], Map = new() { ["key"] = [item] } };
    var restored = TinyhandSerializer.Deserialize<Envelope<Item[]>>(TinyhandSerializer.Serialize(source, options), options)!;
    Check(restored.Value[0].Number == 42 && restored.Map["key"][0].Amount == 123.45m, "generic members");
    var clone = TinyhandSerializer.Clone(source, options)!;
    Check(!ReferenceEquals(source.Value, clone.Value) && clone.Value[0].Id == item.Id, "clone");
}

var text = TinyhandSerializer.SerializeToString(item);
Check(TinyhandSerializer.DeserializeFromString<Item>(text)!.Text == item.Text, "text");
var identifier = TinyhandTypeIdentifier.GetTypeIdentifier<Item>();
var encoded = TinyhandTypeIdentifier.TrySerialize(identifier, item);
Check(TinyhandTypeIdentifier.TryDeserialize(identifier, encoded.ByteArray) is Item { Number: 42 }, "type identifier");
Check(TinyhandTypeIdentifier.TryReconstruct(identifier) is Item, "identifier reconstruction");
var writer = TinyhandWriter.CreateFromThreadStaticBuffer();
try
{
    Check(TinyhandTypeIdentifier.TrySerializeWriter(ref writer, identifier, item), "writer adapter");
    var reader = new TinyhandReader(writer.FlushAndGetArray());
    Check(TinyhandTypeIdentifier.TryDeserializeReader(identifier, ref reader) is Item { Number: 42 }, "reader adapter");
}
finally
{
    writer.Dispose();
}

var dictionaryOptions = TinyhandSerializerOptions.Standard with { Security = TinyhandSecurity.UntrustedData };
var custom = new CustomDictionary { [123] = item };
var customResult = TinyhandSerializer.Deserialize<CustomDictionary>(TinyhandSerializer.Serialize(custom), dictionaryOptions)!;
Check(customResult[123].Number == 42 && ReferenceEquals(customResult.Comparer, dictionaryOptions.Security.GetEqualityComparer<long>()), "dictionary factory comparer");
Check(Roundtrip((item, 123L, LargeEnum.Maximum)).Item3 == LargeEnum.Maximum, "tuple and enum");
Check(Roundtrip<LargeEnum?>(LargeEnum.Maximum) == LargeEnum.Maximum, "nullable enum");
Check(Roundtrip(new Item[,] { { item } })[0, 0].Number == 42, "multidimensional array");
Check(Roundtrip(new ReadOnlyMemory<Item>([item])).Span[0].Number == 42, "memory dependency");
Check(Roundtrip(ImmutableDictionary<string, Item>.Empty.Add("key", item))["key"].Number == 42, "immutable dictionary");
Check(Roundtrip<ILookup<string, Item>>(new[] { item }.ToLookup(x => x.Text))[item.Text].Single().Number == 42, "lookup dependency");
Check(Roundtrip<IUnion>(new UnionItem { Number = 7 }) is UnionItem { Number: 7 }, "union");
Check(Roundtrip(new ReadonlyValue<int>(314)).GetValue() == 314, "readonly generic field accessor");
var stringIdentifier = TinyhandTypeIdentifier.GetTypeIdentifier<NumberString>();
Check(TinyhandTypeIdentifier.TryParseOrDeserializeFromString(stringIdentifier, "123") is NumberString { Number: 123 }, "static parser delegate");
Check(TinyhandTypeIdentifier.TryParseOrDeserializeFromString(stringIdentifier, "{456}") is NumberString { Number: 456 }, "braced text through static adapter");
PrivateTypes.Verify();
GenericScope<int>.Verify();
GenericHelpers.Verify(new HelperItem { Number = 91 });
var named = Roundtrip(new NamedCollections { Values = [1, 2, 3] });
Check(named.Values.SequenceEqual([1, 2, 3]), "constant string keys and primitive list");
Check(ReferenceEquals(named.Empty, Array.Empty<Item>()), "empty generated array");
Check(ReferenceEquals(TinyhandSerializer.Clone(named)!.Empty, Array.Empty<Item>()), "empty array clone");
Check(Roundtrip(ImmutableArray<int>.Empty).IsEmpty, "empty immutable array");
Check(TinyhandSerializer.Clone(default(ImmutableArray<int>)).IsDefault, "default immutable array clone");
Check(Roundtrip(new List<byte> { 1, 2, 3 }).SequenceEqual(new byte[] { 1, 2, 3 }), "binary byte list");
foreach (var options in new[] { TinyhandSerializerOptions.Standard, TinyhandSerializerOptions.Lz4 })
{
    var nested = new Envelope<NestedCallback> { Value = new() { Number = 123 } };
    var nestedCopy = TinyhandSerializer.Deserialize<Envelope<NestedCallback>>(TinyhandSerializer.Serialize(nested, options), options)!;
    Check(nestedCopy.Value.Number == 123, "nested serialization callback");
}

Console.WriteLine("NativeAOT serialization checks passed.");

static T Roundtrip<T>(T value) => TinyhandSerializer.Deserialize<T>(TinyhandSerializer.Serialize(value))!;

static void Check(bool success, string operation)
{
    if (!success)
    {
        throw new InvalidOperationException(operation);
    }
}

[TinyhandObject(SkipDefaultValues = false)]
public partial class NamedCollections
{
    [Key("値\"\\\n")] public List<int> Values { get; set; } = [];
    [Key("Empty")] public Item[] Empty { get; set; } = [];
}

[TinyhandObject]
public partial class NestedCallback
{
    [Key(0)] public int Number { get; set; }

    [TinyhandOnSerializing]
    private void Serializing()
    {
        _ = TinyhandSerializer.Serialize(new string('x', 512), TinyhandSerializerOptions.Lz4);
        _ = TinyhandSerializer.SerializeToUtf8("nested text");
    }
}

[TinyhandObject]
public partial class Item
{
    [Key(0)] public int Number { get; set; }
    [Key(1)] public string Text { get; set; } = string.Empty;
    [Key(2)] public Guid Id { get; set; }
    [Key(3)] public decimal Amount { get; set; }
}

[TinyhandObject]
public partial class Envelope<T>
{
    [Key(0)] public T Value { get; set; } = default!;
    [Key(1)] public Dictionary<string, T> Map { get; set; } = new();
}

public sealed class CustomDictionary : Dictionary<long, Item>
{
    public CustomDictionary() { }
    public CustomDictionary(int capacity, IEqualityComparer<long> comparer) : base(capacity, comparer) { }
}

public enum LargeEnum : ulong
{
    Maximum = ulong.MaxValue,
}

[TinyhandObject]
[TinyhandUnion(0, typeof(UnionItem))]
public partial interface IUnion { }

[TinyhandObject]
public partial class UnionItem : IUnion
{
    [Key(0)] public int Number { get; set; }
}

public static partial class PrivateTypes
{
    [TinyhandObject]
    private partial class Hidden
    {
        [Key(0)] public Dictionary<string, List<Item>> Items { get; set; } = new();
    }

    public static void Verify()
    {
        var value = new Hidden { Items = new() { ["key"] = [new Item { Number = 3 }] } };
        var restored = TinyhandSerializer.Deserialize<Hidden>(TinyhandSerializer.Serialize(value))!;
        if (restored.Items["key"][0].Number != 3) throw new InvalidOperationException("private type");
    }
}

public static partial class GenericScope<T>
{
    [TinyhandObject]
    private partial class Hidden
    {
        [Key(0)] public T Value { get; set; } = default!;
    }

    public static void Verify()
    {
        var restored = TinyhandSerializer.Deserialize<Hidden>(TinyhandSerializer.Serialize(new Hidden()));
        if (restored is null) throw new InvalidOperationException("private generic type");
    }
}

[TinyhandObject]
public partial class HelperItem
{
    [Key(0)] public int Number { get; set; }
}

public static class GenericHelpers
{
    public static void Verify<T>(T value) => VerifyInner(value);

    private static void VerifyInner<T>(T value)
    {
        var values = new List<T[]> { new[] { value } };
        var restored = TinyhandSerializer.Deserialize<List<T[]>>(TinyhandSerializer.Serialize(values));
        if (restored is null || restored.Count != 1 || restored[0].Length != 1)
        {
            throw new InvalidOperationException("generic helper chain");
        }
    }
}

[TinyhandObject]
public readonly partial struct ReadonlyValue<T>
{
    [Key(0)] private readonly T value;

    public ReadonlyValue(T value) => this.value = value;

    public T GetValue() => this.value;
}

[TinyhandObject]
public partial class NumberString : Arc.IStringConvertible<NumberString>
{
    public static int MaxStringLength => 11;

    [Key(0)] public int Number { get; set; }

    public int GetStringLength() => MaxStringLength;

    public bool TryFormat(Span<char> destination, out int written, Arc.IConversionOptions? conversionOptions = null)
        => this.Number.TryFormat(destination, out written);

    public static bool TryParse(ReadOnlySpan<char> source, out NumberString? instance, out int read, Arc.IConversionOptions? conversionOptions = null)
    {
        if (int.TryParse(source, out var number))
        {
            instance = new() { Number = number };
            read = source.Length;
            return true;
        }

        instance = null;
        read = 0;
        return false;
    }
}
