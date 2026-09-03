// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using Tinyhand.Formatters;
using Tinyhand.IO;
using Tinyhand.Resolvers;
using Xunit;

namespace Tinyhand.Tests;

public class StaticRegistrationTest
{
    [Fact]
    public void MissingFormatterCanBeRegisteredAfterLookup()
    {
        var resolver = TinyhandSerializerOptions.Standard.Resolver;
        Assert.Null(resolver.TryGetFormatter<ManuallyRegistered>());
        var formatter = new ManualFormatter();
        GeneratedResolver.Instance.SetFormatter(formatter);
        Assert.Same(formatter, resolver.GetFormatter<ManuallyRegistered>());
        Assert.True(TinyhandTypeIdentifier.IsRegistered<ManuallyRegistered>());
        var value = new ManuallyRegistered { Number = 123 };
        var encoded = TinyhandTypeIdentifier.TrySerialize(value);
        Assert.Equal(123, Assert.IsType<ManuallyRegistered>(TinyhandTypeIdentifier.TryDeserialize(encoded.TypeIdentifier, encoded.ByteArray)).Number);
    }

    [Fact]
    public void IdentifierUsesRegisteredStaticStringParser()
    {
        var value = new StringConvertibleTestClass { Byte16 = new byte[16] };
        var identifier = TinyhandTypeIdentifier.GetTypeIdentifier<StringConvertibleTestClass>();
        var parsed = TinyhandTypeIdentifier.TryParseOrDeserializeFromString(identifier, "@AAAAAAAAAAAAAAAAAAAAAA");
        Assert.Equal(value, Assert.IsType<StringConvertibleTestClass>(parsed));
        var serialized = "{" + TinyhandSerializer.SerializeToString(value) + "}";
        Assert.Equal(value, Assert.IsType<StringConvertibleTestClass>(TinyhandTypeIdentifier.TryParseOrDeserializeFromString(identifier, serialized)));
    }

    [Fact]
    public void UnknownIdentifiersDoNotAllocateOrCacheMisses()
    {
        uint identifier = uint.MaxValue;
        while (TinyhandTypeIdentifier.IsRegistered(identifier))
        {
            identifier--;
        }

        Assert.Equal(0, Measure(() =>
        {
            _ = TinyhandTypeIdentifier.TryDeserialize(identifier, ReadOnlySpan<byte>.Empty);
            _ = TinyhandTypeIdentifier.TryDeserializeFromString(identifier, "");
            _ = TinyhandTypeIdentifier.TryReconstruct(identifier);
        }));
    }

    [Fact]
    public void TypedIdentifierSerializationDoesNotBoxValues()
    {
        var direct = Measure(() => GC.KeepAlive(TinyhandSerializer.Serialize(123)));
        var byIdentifier = Measure(() => GC.KeepAlive(TinyhandTypeIdentifier.TrySerialize(123).ByteArray));
        Assert.Equal(direct, byIdentifier);
    }

    [Fact]
    public void MemoryCloningAllocatesOnlyTheResultArray()
    {
        var source = new int[64];
        var memory = new Memory<int>(source);
        var readOnly = new ReadOnlyMemory<int>(source);
        var sequence = new ReadOnlySequence<int>(source);
        var segment = new ArraySegment<int>(source);
        var baseline = Measure(() => GC.KeepAlive(TinyhandSerializer.Clone(source)));
        Assert.Equal(baseline, Measure(() => CheckLength(source.Length, TinyhandSerializer.Clone(memory).Length)));
        Assert.Equal(baseline, Measure(() => CheckLength(source.Length, TinyhandSerializer.Clone(readOnly).Length)));
        Assert.Equal(baseline, Measure(() => CheckLength(source.Length, TinyhandSerializer.Clone(sequence).Length)));
        Assert.Equal(baseline, Measure(() => CheckLength(source.Length, TinyhandSerializer.Clone(segment).Count)));
    }

    [Fact]
    public void MemoryCloningUsesElementFormatterAndCopiesOnlyTheSlice()
    {
        var original = new ManuallyRegistered { Number = 4 };
        var formatter = new ManualFormatter();
        var options = new TinyhandSerializerOptions(CompositeResolver.Create(formatter, new MemoryFormatter<ManuallyRegistered>(), new ReadOnlySequenceFormatter<ManuallyRegistered>()));
        var memory = new Memory<ManuallyRegistered>([new(), original, new()], 1, 1);
        var clone = TinyhandSerializer.Clone(memory, options);
        Assert.Single(clone.ToArray());
        Assert.Equal(4, clone.Span[0].Number);
        Assert.NotSame(original, clone.Span[0]);

        var first = new Segment<ManuallyRegistered>(new[] { original });
        var last = first.Append(new[] { original });
        var sequence = new ReadOnlySequence<ManuallyRegistered>(first, 0, last, 1);
        var cloned = TinyhandSerializer.Clone(sequence, options).ToArray();
        Assert.Equal(2, cloned.Length);
        Assert.NotSame(original, cloned[0]);
        Assert.NotSame(cloned[0], cloned[1]);
    }

    [Fact]
    public void DictionaryFactoryHasNoReflectionArgumentAllocations()
    {
        var comparer = TinyhandSecurity.TrustedData.GetEqualityComparer<long>();
        var baseline = Measure(() => GC.KeepAlive(new GenericDictionaryFormatterTest.ComparerDictionary<long>(0, comparer)));
        var actual = Measure(() => GC.KeepAlive(TinyhandSerializer.Reconstruct<GenericDictionaryFormatterTest.ComparerDictionary<long>>()));
        Assert.Equal(baseline, actual);
    }

    private static long Measure(Action action)
    {
        for (var i = 0; i < 100; i++)
        {
            action();
        }

        var start = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1000; i++)
        {
            action();
        }

        return GC.GetAllocatedBytesForCurrentThread() - start;
    }

    private static void CheckLength(long expected, long actual)
    {
        if (expected != actual)
        {
            throw new InvalidOperationException("Unexpected clone length.");
        }
    }

    public sealed class ManuallyRegistered
    {
        public int Number { get; set; }
    }

    private sealed class ManualFormatter : ITinyhandFormatter<ManuallyRegistered>
    {
        public void Serialize(ref TinyhandWriter writer, ManuallyRegistered? value, TinyhandSerializerOptions options) => writer.Write(value!.Number);

        public void Deserialize(ref TinyhandReader reader, ref ManuallyRegistered? value, TinyhandSerializerOptions options) => value = new() { Number = reader.ReadInt32() };

        public ManuallyRegistered Reconstruct(TinyhandSerializerOptions options) => new();

        public ManuallyRegistered? Clone(ManuallyRegistered? value, TinyhandSerializerOptions options) => value is null ? null : new() { Number = value.Number };
    }

    private sealed class Segment<T> : ReadOnlySequenceSegment<T>
    {
        public Segment(ReadOnlyMemory<T> memory) => this.Memory = memory;

        public Segment<T> Append(ReadOnlyMemory<T> memory)
        {
            var next = new Segment<T>(memory) { RunningIndex = this.RunningIndex + this.Memory.Length };
            this.Next = next;
            return next;
        }
    }
}
