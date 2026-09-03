# Tinyhand

![NuGet](https://img.shields.io/nuget/v/Tinyhand) ![Build and Test](https://github.com/archi-Doc/Tinyhand/workflows/Build%20and%20Test/badge.svg)

Tinyhand is a data format and C# serializer based on [MessagePack for C#](https://github.com/neuecc/MessagePack-CSharp). It combines compact binary serialization, readable text, and compile-time code generation, including support for .NET NativeAOT.

[Japanese documentation](doc/README.jp.md)

## Table of Contents

- [Requirements and installation](#requirements-and-installation)
- [Quick start](#quick-start)
- [Serialization targets](#serialization-targets)
  - [Keys and member selection](#keys-and-member-selection)
  - [Readonly fields, init-only properties, and records](#readonly-fields-init-only-properties-and-records)
  - [Generated properties and length limits](#generated-properties-and-length-limits)
- [Object lifecycle](#object-lifecycle)
  - [Default values and reconstruction](#default-values-and-reconstruction)
  - [Instance reuse](#instance-reuse)
  - [Constructors and service providers](#constructors-and-service-providers)
  - [Callbacks and locking](#callbacks-and-locking)
  - [Cloning and read-only wrappers](#cloning-and-read-only-wrappers)
- [Schema and serialization options](#schema-and-serialization-options)
  - [Versioning and reserved keys](#versioning-and-reserved-keys)
  - [Unions](#unions)
  - [Alternate keys and enum names](#alternate-keys-and-enum-names)
  - [Exclusion and signatures](#exclusion-and-signatures)
- [Text serialization and syntax trees](#text-serialization-and-syntax-trees)
- [Buffers, streams, and compression](#buffers-streams-and-compression)
- [Supported types](#supported-types)
- [Deserialization security](#deserialization-security)
- [Custom serialization and formatters](#custom-serialization-and-formatters)
- [NativeAOT and type registration](#nativeaot-and-type-registration)
- [Generated members and localized strings](#generated-members-and-localized-strings)
- [Structural objects and journaling](#structural-objects-and-journaling)
- [Tinyhand Processor](#tinyhand-processor)
- [Building, testing, and benchmarks](#building-testing-and-benchmarks)

## Requirements and installation

Use .NET 10 or later and C# 14 or later. Visual Studio users need Visual Studio 2026 or later for the incremental source generator. The repository also builds with the .NET CLI.

```sh
dotnet add package Tinyhand
```

The NuGet package includes the source generator. When referencing this repository directly, reference both the library and the generator:

```xml
<ItemGroup>
  <ProjectReference Include="../Tinyhand/Tinyhand.csproj" />
  <ProjectReference Include="../TinyhandGenerator/TinyhandGenerator.csproj"
                    OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
</ItemGroup>
```

## Quick start

Annotate a partial type with `[TinyhandObject]` and assign a stable key to each serialized member. Formatters are registered automatically.

```csharp
using Tinyhand;

var person = new Person { Age = 30, FirstName = "Ada", LastName = "Lovelace" };
byte[] bytes = TinyhandSerializer.Serialize(person);
Person? restored = TinyhandSerializer.Deserialize<Person>(bytes);
string text = TinyhandSerializer.SerializeToString(person);
Person? fromText = TinyhandSerializer.DeserializeFromString<Person>(text);

[TinyhandObject]
public partial class Person
{
    [Key(0)]
    public int Age { get; set; }

    [Key(1)]
    public string FirstName { get; set; } = string.Empty;

    [Key(2)]
    public string LastName { get; set; } = string.Empty;

    [IgnoreMember]
    public string FullName => $"{FirstName} {LastName}";
}
```

`Deserialize<T>` can return null for a reference type. `TryDeserialize<T>` reports failure without throwing. `Reconstruct<T>()` invokes the type's reconstruction operation, and `Clone(value)` copies supported members without a binary round trip.

See [QuickStart](QuickStart) for additional examples.

## Serialization targets

### Keys and member selection

By default, writable public instance fields and properties are serialization targets and require `[Key]`. Properties with non-public setters and readonly fields require explicit inclusion. Static members and indexers are not targets.

| Setting or attribute | Behavior |
| --- | --- |
| `[Key(0)]` | Uses an integer array index; start at zero and avoid large gaps. |
| `[Key("name")]` | Uses a string map key. |
| `[MemberNameAsKey]` | Uses the member name as a string key. |
| `ImplicitMemberNameAsKey = true` | Uses names for eligible members without explicit keys. |
| `IncludePrivateMembers = true` | Includes eligible private and protected members. |
| `ExplicitKeysOnly = true` | Includes only explicitly keyed members. |
| `[IgnoreMember]` | Excludes a member from serialization. |

An explicit key can include a private member or a property with a private setter. Integer and string keys cannot be mixed in a normal layout. `ImplicitMemberNameAsKey` and `ExplicitKeysOnly` cannot be enabled together. Keys must be unique across a type and its base types.

```csharp
[TinyhandObject(ExplicitKeysOnly = true)]
public partial class Settings
{
    [Key(0)]
    public int Timeout { get; private set; } = 30;

    [Key(1)]
    private string endpoint = "localhost";

    public int TemporaryCount { get; set; } // Not serialized.
}
```

### Readonly fields, init-only properties, and records

Readonly fields can be explicitly keyed; enable `<AllowUnsafeBlocks>true</AllowUnsafeBlocks>` for the generated access code. Getter-only properties are not supported as serialization targets. Init-only properties, required members, record classes, and record structs are supported.

```csharp
[TinyhandObject(ImplicitMemberNameAsKey = true)]
public partial record Point(int X, int Y);
```

Generated types and their containing types must be `partial` where the generator needs to add code.

### Generated properties and length limits

`Key.AddProperty` generates a property over a field. `Key.PropertyAccessibility` selects a public setter, protected setter, or getter-only wrapper. Tinyhand also implements keyed partial properties.

`[MaxLength]` truncates strings, arrays, and lists during deserialization and in generated setters. Its second argument limits supported child strings or arrays; a negative limit leaves that dimension unrestricted. Direct writes to backing fields or ordinary setters do not apply this check.

```csharp
[TinyhandObject]
public partial class LimitedValues
{
    [Key(0, AddProperty = "Name")]
    [MaxLength(20)]
    private string name = string.Empty;

    [Key(1)]
    [MaxLength(3, 10)]
    public partial string[] Tags { get; set; } = [];
}
```

## Object lifecycle

### Default values and reconstruction

Use field and property initializers for defaults. The generator recognizes supported constant initializers and some empty or null values. `SkipDefaultValues` is true by default: recognized defaults can be represented by nil or omitted entries, depending on the layout. Set it to false to write those values explicitly. Implement `ITinyhandDefault.CanSkipSerialization()` when a custom object needs to define its default state.

During deserialization, missing data retains initialized values where applicable. Non-nullable reference members are initialized when needed: strings become empty strings, arrays become empty arrays, and supported object members are created. Nullable members normally remain null. This does not make every collection element non-null; generic collections do not retain element nullability information.

```csharp
[TinyhandObject(ImplicitMemberNameAsKey = true)]
public partial class Defaults
{
    public int Count { get; set; } = 12;
    public string Name { get; set; } = "Guest";

    [Reconstruct(true)]
    public Person? Owner { get; set; }

    public Person? OptionalOwner { get; set; }
}
```

`ReconstructMembers` and `[Reconstruct(true/false)]` control member reconstruction eligibility. For example, the missing nullable `Owner` above is reconstructed during deserialization. These settings do not guarantee that missing non-nullable map members remain null; use nullable members when absence should be preserved. Ignored members should be initialized explicitly.

For classes, generated `Reconstruct<T>()` creates the instance if needed and runs its reconstruction callback; member values come from constructors and initializers. It does not fill every null member. Struct reconstruction also initializes eligible members. Use initializers rather than `System.ComponentModel.DefaultValueAttribute` to express defaults in current generated code.

### Instance reuse

`ReuseMembers` is true by default. Existing members of Tinyhand object types can be deserialized in place, preserving state that is not serialized. Override individual members with `[Reuse(false)]` or `[Reuse(true)]`.

To deserialize into an existing root object, use the static object API:

```csharp
Person? existing = new Person();
TinyhandSerializer.DeserializeObject(bytes, ref existing);
```

This overload calls the type's static deserializer directly. Use the regular `Deserialize<T>` APIs when compression handling is required.

### Constructors and service providers

The generator uses a public parameterless constructor when available, supports primary constructors, and can generate a construction path for other partial models. A public parameterless constructor is therefore not always required. Constructor arguments may receive defaults; use a service provider when construction requires application services.

```csharp
[TinyhandObject(UseServiceProvider = true)]
public partial class ServiceBackedModel
{
    public ServiceBackedModel(IServiceProvider services)
    {
        Services = services;
    }

    [IgnoreMember]
    public IServiceProvider Services { get; }

    [Key(0)]
    public int Value { get; set; }
}
```

Set `TinyhandSerializer.ServiceProvider` before deserialization or reconstruction. The provider must return an instance of the requested model type.

### Callbacks and locking

Annotate parameterless instance methods with `TinyhandOnSerializing`, `TinyhandOnSerialized`, `TinyhandOnDeserializing`, `TinyhandOnDeserialized`, or `TinyhandOnReconstructed`. Callbacks are not inherited by derived classes.

Set `LockObject` to the name of a lock member to synchronize serialization and deserialization. Their callbacks execute while that lock is held.

```csharp
[TinyhandObject(LockObject = nameof(syncObject))]
public partial class Counter
{
    private readonly object syncObject = new();

    [Key(0)]
    public int Value { get; set; }

    [TinyhandOnDeserialized]
    private void OnDeserialized() => Value = Math.Max(0, Value);
}
```

### Cloning and read-only wrappers

`TinyhandSerializer.Clone(value)` uses generated cloning code or a formatter. Supported members can be cloned even when they are not serialization targets, so cloning is not identical to a serialize/deserialize round trip. Unsupported member types are not automatically deep-cloned. Custom types can implement `ITinyhandCloneable<T>`.

For classes, `AddImmutable = true` generates a nested `Immutable` wrapper with getter-only access, plus `ToImmutable()` and `CloneAndToImmutable()`. `ToImmutable()` wraps the original instance; changes to that instance remain visible. `CloneAndToImmutable()` wraps a clone. Neither method makes mutable objects returned by getters intrinsically immutable.

## Schema and serialization options

Create options with a record `with` expression. Binary APIs normally use `TinyhandSerializer.DefaultOptions`, initially `TinyhandSerializerOptions.Standard`. Text APIs default to `TinyhandSerializerOptions.ConvertToString`. Pass options explicitly when different callers need different settings.

### Versioning and reserved keys

Unknown members are skipped, and missing members use their initialized or reconstructed values. Keep existing keys and their value types compatible. Do not renumber integer keys or reuse a removed key for a different meaning.

`ReservedKeyCount` reserves integer keys from zero through `ReservedKeyCount - 1` for a base type. Derived members should use keys above that range. `Key.IgnoreKeyReservation` suppresses reservation diagnostics when explicitly required.

### Unions

Declare known subtypes on a partial interface or abstract class with `[TinyhandUnion]`. Keys may be integers or strings, but a union must use one key kind and unique keys. Annotate the union root and concrete models with `[TinyhandObject]`.

```csharp
[TinyhandObject]
[TinyhandUnion(0, typeof(Circle))]
[TinyhandUnion(1, typeof(Rectangle))]
public partial interface IShape;

[TinyhandObject]
public partial class Circle : IShape
{
    [Key(0)] public double Radius { get; set; }
}

[TinyhandObject]
public partial class Rectangle : IShape
{
    [Key(0)] public double Width { get; set; }
    [Key(1)] public double Height { get; set; }
}
```

Serialize with the declared union type, for example `TinyhandSerializer.Serialize<IShape>(new Circle { Radius = 2 })`, and deserialize with `Deserialize<IShape>`.

### Alternate keys and enum names

`AddAlternateKey = true` keeps integer keys for ordinary binary serialization and adds string keys for text serialization. Names default to member names; `Key.Alternate` supplies a stable alternative.

```csharp
[TinyhandObject(AddAlternateKey = true)]
public partial class NamedValue
{
    [Key(0, Alternate = "value")]
    public int Value { get; set; }
}
```

Set `EnumAsString = true` on a model to serialize its enum values by name. Renaming enum members then changes their serialized representation.

### Exclusion and signatures

`[Key(0, Exclude = true)]` omits a member when using `TinyhandSerializerOptions.Exclude`. Ordinary serialization still includes it.

`SerializeSignature(value, level)` uses signature mode. `Key.Level` controls inclusion, array headers are omitted, and `AddSignatureId` controls the generated type signature identifier. Signature bytes are intended for hashing or signing and are not ordinary round-trip serialized data. `GetXxHash3` hashes serialized bytes and returns zero if serialization fails; it is not a cryptographic signature.

`TinyhandSerializerOptions.Special` lets custom serialization code select application-specific behavior.

## Text serialization and syntax trees

Tinyhand text supports values, groups (`{ ... }`), assignments (`name = value`), comments, and binary literals. It is a separate format from JSON.

```csharp
byte[] utf8 = TinyhandSerializer.SerializeToUtf8(person);
Person? copy = TinyhandSerializer.DeserializeFromUtf8<Person>(utf8);

var strict = TinyhandSerializerOptions.ConvertToStrictString;
string strictText = TinyhandSerializer.SerializeToString(person, strict);
Person? strictCopy = TinyhandSerializer.DeserializeFromString<Person>(strictText, strict);
```

`Standard` composition indents groups, `Simple` uses a compact layout, and `Strict` retains explicit outer group delimiters. Use matching composition settings when reading text whose top-level delimiters were omitted. Text options with `ConvertToString` use `Arc.IStringConvertible<T>` where supported.

For document editing, parse a syntax tree and compose it again:

```csharp
var tree = TinyhandParser.Parse(
    "name = \"Ada\" // Display name"u8,
    TinyhandParserOptions.ContextualInformation);
string document = TinyhandComposer.ComposeToString(
    tree, TinyhandComposeOption.UseContextualInformation);
```

`TinyhandParserOptions.ContextualInformation` retains comments and line breaks. `Tinyhand.Tree` exposes groups, assignments, identifiers, and scalar nodes; `TinyhandTreeHelper` provides queries. `TinyhandTreeConverter` converts between text, trees, and binary data, and `TinyhandSerializer.DeserializeFromElement<T>` deserializes a tree.

## Buffers, streams, and compression

Binary serialization accepts `IBufferWriter<byte>`, `Stream`, and `ref TinyhandWriter`. Deserialization accepts byte spans, streams, and `ref TinyhandReader`; an overload reports consumed bytes. `SerializeAsync` and `DeserializeAsync` support streams.

Stream deserialization reads to the end and returns the first value. Seekable streams are repositioned after that value; non-seekable streams need application-level framing if multiple messages share a stream.

`SerializeToRentMemory` returns pooled bytes. Return the memory after use:

```csharp
var rented = TinyhandSerializer.SerializeToRentMemory(person);
try
{
    Person? copy = TinyhandSerializer.Deserialize<Person>(rented.Span);
}
finally
{
    rented.Return();
}
```

Dispose manually created writers to release their owned buffers. Spans and sequences that refer to writer storage must not outlive its reuse or disposal.

Enable LZ4 on both sides:

```csharp
byte[] compressed = TinyhandSerializer.Serialize(person, TinyhandSerializerOptions.Lz4);
Person? copy = TinyhandSerializer.Deserialize<Person>(compressed, TinyhandSerializerOptions.Lz4);
```

LZ4 options can also read uncompressed data. Small payloads may be emitted without compression. To combine settings, use `with`, such as `TinyhandSerializerOptions.Lz4 with { Security = TinyhandSecurity.UntrustedData }`.

## Supported types

The standard resolver supports these families when the required closed generic types are registered:

| Family | Types |
| --- | --- |
| Values | Primitive numeric types, `Int128`, `UInt128`, `bool`, `char`, `string`, enums, nullable values, `Nil` |
| Time and identifiers | `DateTime`, `DateTimeOffset`, `TimeSpan`, `Guid` |
| Other values | `decimal`, `BigInteger`, `Complex`, `Uri`, `Version`, `StringBuilder`, `IPAddress`, `IPEndPoint` |
| Buffers and arrays | Arrays of rank 1–4, `ArraySegment<T>`, `Memory<T>`, `ReadOnlyMemory<T>`, `ReadOnlySequence<T>`, `BitArray` |
| Tuples | `KeyValuePair<TKey, TValue>`, `Tuple<...>`, `ValueTuple<...>` |
| Collections | Lists, linked lists, queues, stacks, sets, sorted collections, dictionaries, read-only wrappers, observable collections, concurrent collections, and supported generic interfaces |
| Collection interfaces | `IEnumerable<T>`, `ICollection<T>`, `IList<T>`, `IReadOnlyCollection<T>`, `IReadOnlyList<T>`, `ISet<T>`, `IDictionary<TKey, TValue>`, `IReadOnlyDictionary<TKey, TValue>`, `ILookup<TKey, TElement>`, `IGrouping<TKey, TElement>` |
| Immutable collections | Immutable arrays, lists, dictionaries, sets, queues, stacks, and their supported interfaces |
| Additional types | `Lazy<T>`, `Utf8String`, `KeyValueList<TKey, TValue>`, `Arc.Crypto.Struct128` / `Struct256`, pooled byte memory, and supported Arc.Collections types |

Arc.Collections support includes ordered and unordered maps, sets, and lists, ordered multimaps and multisets, unordered linked lists, and `Utf16Hashtable<T>`.

Custom `ICollection<T>` and `IDictionary<TKey, TValue>` implementations can use generated factories. A dictionary's public `(int capacity, IEqualityComparer<TKey> comparer)` constructor is preferred; otherwise, a public parameterless constructor is used. Collection factories require a public parameterless constructor.

With standard options, `object` uses primitive encoding for supported scalar values, enums, and `System.Collections.ICollection` / `System.Collections.IDictionary` instances containing supported values. It does not automatically dispatch to an arbitrary runtime model's formatter. Use the concrete model type or a declared union. When reading primitive objects, integer types are determined by the encoded bytes.

Non-generic collection types such as `IEnumerable`, `IList`, `IDictionary`, `ArrayList`, and `Hashtable` are not supported as declared serialization types. Use generic collections. `System.Type` and `ExpandoObject` do not have built-in formatters.

## Deserialization security

The default security policy is `TrustedData`. For untrusted input, select `UntrustedData`, which enables collision-resistant collection comparers and limits object-graph depth to 500:

```csharp
var options = TinyhandSerializerOptions.Standard with
{
    Security = TinyhandSecurity.UntrustedData,
};
Person? copy = TinyhandSerializer.Deserialize<Person>(bytes, options);
```

This policy rejects hash-based collections with `object` keys, including `Dictionary<object, ...>`, `HashSet<object>`, `ILookup<object, ...>`, and maps read through `object`, even when their keys happen to be strings. Use supported concrete keys such as `string` or `int`. Object scalars and arrays without nested maps remain supported.

Custom formatters that read nested values should call `options.Security.DepthStep(ref reader)` and decrement `reader.Depth` in a `finally` block. Depth and comparer policies do not impose a total input-size limit; bound input sizes in the calling application.

## Custom serialization and formatters

For an annotated model, implement the static methods of `ITinyhandSerializable<T>` to customize serialization. `ITinyhandReconstructable<T>` and `ITinyhandCloneable<T>` customize reconstruction and cloning. The generator supplies operations that are not implemented manually.

For a separate formatter, implement `ITinyhandFormatter<T>` and register it before use with `Tinyhand.Resolvers.GeneratedResolver.Instance.SetFormatter<T>()`. A formatter must encode exactly one value: wrap multiple values in an array or map and handle nil consistently.

```csharp
using System.Diagnostics.CodeAnalysis;
using Tinyhand;
using Tinyhand.IO;
using Tinyhand.Resolvers;

GeneratedResolver.Instance.SetFormatter<Label>(new LabelFormatter());
byte[] data = TinyhandSerializer.Serialize(new Label { Value = "example" });
Label? label = TinyhandSerializer.Deserialize<Label>(data);

[TinyhandObject(UseResolver = true)]
public partial class Label
{
    [Key(0)]
    public string Value { get; set; } = string.Empty;
}

public sealed class LabelFormatter : ITinyhandFormatter<Label>
{
    public void Serialize(ref TinyhandWriter writer, Label? value, TinyhandSerializerOptions options)
    {
        if (value is null)
            writer.WriteNil();
        else
            writer.Write(value.Value);
    }

    public void Deserialize(ref TinyhandReader reader, ref Label? value, TinyhandSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
            value = null;
            return;
        }

        value ??= new Label();
        value.Value = reader.ReadString() ?? string.Empty;
    }

    public Label Reconstruct(TinyhandSerializerOptions options) => new();

    [return: NotNullIfNotNull(nameof(value))]
    public Label? Clone(Label? value, TinyhandSerializerOptions options)
        => value is null ? null : new Label { Value = value.Value };
}
```

`UseResolver = true` makes generated callers resolve the annotated type through a formatter. Registration alone does not teach the generator how to handle an otherwise unsupported member type; use an annotated wrapper or a custom containing serializer.

Built-in formatters take precedence over `GeneratedResolver`. To override a built-in type, implement `IFormatterResolver`, return your formatter first, and delegate other requests to `TinyhandSerializerOptions.Standard.Resolver`. Set it with `Standard with { Resolver = yourResolver }`. Built-in resolver classes are internal.

The `SerializeObject`, `DeserializeObject`, `ReconstructObject`, and `CloneObject` APIs call static type operations directly. They are useful for generated models but do not substitute for resolver-based custom formatter dispatch.

## NativeAOT and type registration

Enable `<PublishAot>true</PublishAot>` in a .NET 10 application and publish for its runtime identifier. Generated module initializers register models, enums, and supported closed collection types automatically.

Use assembly-level registration for closed types that cannot be discovered from the caller's source, such as types used only inside another assembly's generic helpers:

```csharp
[assembly: TinyhandRegister(typeof(Dictionary<string, Person>))]
```

Place assembly attributes after `using` directives and before type declarations or top-level statements. Open generic types such as `Dictionary<,>` cannot be registered. There is no runtime factory for arbitrary closed generic types.

`TinyhandTypeIdentifier` dispatches operations for registered types by a 32-bit identifier. Generated and built-in types register automatically; manual registrations use `Register<T>()`. Use `RegisterStringConvertible<T>()` for manually registered types needing their static string parser. Identifiers derive from type names, so renaming a type affects identifier-based persistence.

See [NativeAOT setup and migration notes](doc/NativeAOT.md) for diagnostics, publishing commands, and verification details.

## Generated members and localized strings

`[TinyhandGenerateMember("data.tinyhand")]` generates initialized members and nested classes from a text file. `[TinyhandGenerateHash("strings.tinyhand")]` generates identifier hash constants. Apply these attributes to partial types; relative paths are resolved from the declaring source file.

`HashedString` loads localized strings from files, streams, or embedded resources and retrieves them by identifier or hash. Use `SetDefaultCulture` and `ChangeCulture` to select tables; lookups fall back to the default culture. `GetOrEmpty` and `GetOrAlternative` control missing-string behavior.

## Structural objects and journaling

`[TinyhandObject(Structural = true)]` generates `IStructuralObject` support for parent-child links and journal operations. Generated setters can record changes through an attached `IStructuralRoot`; direct backing-field writes bypass those setters.

The host supplies journal storage and save scheduling through `IStructuralRoot`. `ITinyhandCustomJournal` handles custom records, `JournalHelper.ReadJournal` replays records, and `JournalTester` provides an in-memory root for tests. See [journal examples](XUnitTest/Tests/JournalTest.cs).

## Tinyhand Processor

The separate `TinyhandProcessor` project executes Tinyhand process scripts. Built-in cores include text-line conversion, language-file updates, an example logger, and executable startup measurements.

```sh
dotnet run --project TinyhandProcessor/TinyhandProcessor.csproj -- script.tinyhand
```

Plugins implement `IProcessCore`. Reference the plugin assembly and call `TinyhandProcess.RegisterPlugin<MyProcessCore>("my process")` before processing. Plugins are registered statically; runtime DLL discovery is not supported. See [TestPlugin](TestPlugin) and the [NativeAOT notes](doc/NativeAOT.md) for hosting details.

## Building, testing, and benchmarks

```sh
dotnet build Tinyhand.slnx
dotnet test --project XUnitTest/XUnitTest.csproj
dotnet run --project QuickStart/QuickStart.csproj
```

NativeAOT smoke tests must be published and run as native executables; see the [NativeAOT guide](doc/NativeAOT.md).

The [Benchmark project](Benchmark) compares binary serialization, text conversion, and cloning. [Saved benchmark reports](Benchmark/ChampionData) are historical measurements, not results for every current runtime or version. Run Release benchmarks on the target hardware before drawing performance conclusions.

