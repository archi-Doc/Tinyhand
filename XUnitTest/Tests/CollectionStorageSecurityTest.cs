// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Arc.Collections;
using Tinyhand;
using Tinyhand.Formatters;
using Tinyhand.IO;
using Xunit;

namespace XUnitTest.Tests;

public class CollectionStorageSecurityTest
{
    private static readonly TinyhandSerializerOptions Untrusted = TinyhandSerializerOptions.Standard with { Security = TinyhandSecurity.UntrustedData };

    [Fact]
    public void LargeElementsAreRejectedBeforeAllocatingDeclaredStorage()
    {
        // A 16 KiB input must not allocate an array or list containing more than 64 MiB of inline values.
        var bytes = new byte[16_388];
        bytes.AsSpan().Fill(MessagePackCode.Nil);
        bytes[0] = MessagePackCode.Array16;
        bytes[1] = 0x40;
        bytes[2] = 1;
        Assert.Equal(4096, Unsafe.SizeOf<CollectionStorageValue>());
        CheckRejectedAllocation(new ArrayFormatter<CollectionStorageValue>(), bytes);
        CheckRejectedAllocation(new ListFormatter<CollectionStorageValue>(), bytes);
        CheckRejectedAllocation(new ImmutableArrayFormatter<CollectionStorageValue>(), bytes);
        CheckRejectedAllocation(new OrderedListFormatter<CollectionStorageValue>(), bytes);
        CheckRejectedAllocation(new UnorderedListFormatter<CollectionStorageValue>(), bytes);
    }

    [Fact]
    public void GenericCollectionStorageUsesElementSizeAndChecksBeforeReuseMutation()
    {
        var element = new CollectionStorageValue { Value = 1 };
        var elementBytes = Unsafe.SizeOf<CollectionStorageValue>();
        var pairBytes = Unsafe.SizeOf<KeyValuePair<long, CollectionStorageValue>>();
        CheckBudget(new ArrayFormatter<CollectionStorageValue>(), new[] { element, element }, 2L * elementBytes);
        CheckBudget(new ListFormatter<CollectionStorageValue>(), new List<CollectionStorageValue> { element, element }, 2L * elementBytes);
        CheckBudget(new QueueFormatter<CollectionStorageValue>(), new Queue<CollectionStorageValue>(new[] { element, element }), 2L * elementBytes);
        CheckBudget(new ImmutableArrayFormatter<CollectionStorageValue>(), ImmutableArray.Create(element, element), 2L * elementBytes);
        CheckBudget(new TwoDimensionalArrayFormatter<CollectionStorageValue>(), new[,] { { element, element } }, 2L * elementBytes);
        CheckBudget(new ThreeDimensionalArrayFormatter<CollectionStorageValue>(), new[,,] { { { element, element } } }, 2L * elementBytes);
        CheckBudget(new FourDimensionalArrayFormatter<CollectionStorageValue>(), new[,,,] { { { { element, element } } } }, 2L * elementBytes);
        CheckBudget(new DictionaryFormatter<long, CollectionStorageValue>(), new Dictionary<long, CollectionStorageValue> { [1] = element }, pairBytes);
        CheckBudget(new OrderedMapFormatter<long, CollectionStorageValue>(), new OrderedMap<long, CollectionStorageValue> { { 1, element } }, pairBytes);
        CheckBudget(new OrderedMultiMapFormatter<long, CollectionStorageValue>(), new OrderedMultiMap<long, CollectionStorageValue> { { 1, element } }, pairBytes);
        CheckBudget(new UnorderedMapFormatter<long, CollectionStorageValue>(), new UnorderedMap<long, CollectionStorageValue> { { 1, element } }, pairBytes);
        CheckBudget(new KeyValueListFormatter<long, CollectionStorageValue>(), new KeyValueList<long, CollectionStorageValue> { new(1, element) }, pairBytes);
        CheckBudget(new OrderedListFormatter<CollectionStorageValue>(), new OrderedList<CollectionStorageValue> { element }, elementBytes);
        CheckBudget(new UnorderedListFormatter<CollectionStorageValue>(), new UnorderedList<CollectionStorageValue> { element }, elementBytes);

        var linkedList = new UnorderedLinkedList<CollectionStorageValue>();
        linkedList.AddLast(element);
        CheckBudget(new UnorderedLinkedListFormatter<CollectionStorageValue>(), linkedList, elementBytes);
    }

    [Fact]
    public void PrimitiveAndReferenceStorageShareTheSameBudget()
    {
        CheckBudget(Untrusted.Resolver.GetFormatter<long[]>(), new long[] { 1, 2 }, 16);
        CheckBudget(Untrusted.Resolver.GetFormatter<List<long>>(), new List<long> { 1, 2 }, 16);
        CheckBudget(Untrusted.Resolver.GetFormatter<string[]>(), new[] { "a", "b" }, 2L * IntPtr.Size);
        CheckBudget(Untrusted.Resolver.GetFormatter<List<string>>(), new List<string> { "a", "b" }, 2L * IntPtr.Size);
        CheckBudget(CharMemoryFormatter.Instance, new Memory<char>(new[] { 'a', 'b' }), 4);
        CheckBudget(CharReadOnlyMemoryFormatter.Instance, new ReadOnlyMemory<char>(new[] { 'a', 'b' }), 4);
        CheckBudget(BitArrayFormatter.Instance, new BitArray(new[] { true, false }), 2);
        CheckBudget(new ArrayFormatter<long[]>(), new[] { new long[] { 1, 2 }, new long[] { 3, 4 } }, (2L * IntPtr.Size) + 32);
    }

    [Fact]
    public void TrustedStorageBudgetRemainsConfigurable()
    {
        Assert.Equal(long.MaxValue, TinyhandSecurity.TrustedData.Limits.MaximumTotalCollectionBytes);
        Assert.Equal(64 * 1024 * 1024, TinyhandSecurity.UntrustedData.Limits.MaximumTotalCollectionBytes);
        var options = TinyhandSerializerOptions.Standard with
        {
            Security = TinyhandSecurity.TrustedData.WithLimits(TinyhandSecurity.TrustedData.Limits with { MaximumTotalCollectionBytes = 7 }),
        };
        Assert.Throws<TinyhandException>(() => TinyhandSerializer.Deserialize<long[]>(TinyhandSerializer.Serialize(new long[] { 1 }), options));
    }

    private static void CheckRejectedAllocation<T>(ITinyhandFormatter<T> formatter, byte[] bytes)
    {
        Action deserialize = () =>
        {
            var reader = new TinyhandReader(bytes);
            T? value = default;
            formatter.Deserialize(ref reader, ref value, Untrusted);
        };

        Assert.Throws<TinyhandException>(deserialize);
        var before = GC.GetAllocatedBytesForCurrentThread();
        Assert.Throws<TinyhandException>(deserialize);
        Assert.True(GC.GetAllocatedBytesForCurrentThread() - before < 128 * 1024, "Storage rejection must precede collection capacity allocation.");
    }

    private static void CheckBudget<T>(ITinyhandFormatter<T> formatter, T original, long storageBytes)
    {
        var bytes = Serialize(formatter, original);
        var options = Untrusted with
        {
            Security = Untrusted.Security.WithLimits(Untrusted.Security.Limits with { MaximumTotalCollectionBytes = storageBytes - 1 }),
        };
        T? reused = original;
        Assert.Throws<TinyhandException>(() =>
        {
            var reader = new TinyhandReader(bytes);
            formatter.Deserialize(ref reader, ref reused, options);
        });
        Assert.Equal(bytes, Serialize(formatter, original));

        options = Untrusted with
        {
            Security = Untrusted.Security.WithLimits(Untrusted.Security.Limits with { MaximumTotalCollectionBytes = storageBytes }),
        };
        T? result = default;
        var acceptedReader = new TinyhandReader(bytes);
        formatter.Deserialize(ref acceptedReader, ref result, options);
        Assert.True(acceptedReader.End);
        Assert.Equal(0, acceptedReader.Depth);
        Assert.Equal(bytes, Serialize(formatter, result));
    }

    private static byte[] Serialize<T>(ITinyhandFormatter<T> formatter, T? value)
    {
        var writer = TinyhandWriter.CreateFromBytePool();
        try
        {
            formatter.Serialize(ref writer, value, Untrusted);
            return writer.FlushAndGetArray();
        }
        finally
        {
            writer.Dispose();
        }
    }
}

[TinyhandObject]
[StructLayout(LayoutKind.Sequential, Size = 4096)]
public partial struct CollectionStorageValue : IComparable<CollectionStorageValue>
{
    [Key(0)]
    public int Value { get; set; }

    public readonly int CompareTo(CollectionStorageValue other) => this.Value.CompareTo(other.Value);
}
