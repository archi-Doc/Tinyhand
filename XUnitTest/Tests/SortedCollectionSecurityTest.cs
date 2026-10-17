// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Arc.Collections;
using Tinyhand;
using Tinyhand.Formatters;
using Tinyhand.IO;
using Xunit;

namespace XUnitTest.Tests;

public class SortedCollectionSecurityTest
{
    private static readonly TinyhandSerializerOptions Untrusted = TinyhandSerializerOptions.Standard with { Security = TinyhandSecurity.UntrustedData };

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReverseInputPreservesSortedListComparerAndReuse(bool reverse)
    {
        var comparer = reverse ? Comparer<int>.Create((x, y) => y.CompareTo(x)) : Comparer<int>.Default;
        var original = new SortedList<int, int>(comparer) { [-1] = -1 };
        SortedList<int, int>? result = original;
        var input = reverse ? Enumerable.Range(0, 4096) : Enumerable.Range(0, 4096).Reverse();
        var reader = new TinyhandReader(CreateMap(input.Select(x => new KeyValuePair<int, int>(x, x)).ToArray()));
        new SortedListFormatter<int, int>().Deserialize(ref reader, ref result, Untrusted);
        Assert.Same(original, result);
        Assert.Same(comparer, result!.Comparer);
        Assert.Equal(reverse ? Enumerable.Range(0, 4096).Reverse() : Enumerable.Range(0, 4096), result.Keys);
        Assert.True(reader.End);
    }

    [Fact]
    public void ArcSortedListsPreserveComparersAndStableDuplicateOrder()
    {
        var lengthComparer = Comparer<string>.Create((x, y) => x.Length.CompareTo(y.Length));
        var originalList = new OrderedList<string>(lengthComparer) { "old" };
        OrderedList<string>? list = originalList;
        var reader = new TinyhandReader(TinyhandSerializer.Serialize(new[] { "bbb", "a", "bb", "c" }));
        new OrderedListFormatter<string>().Deserialize(ref reader, ref list, Untrusted);
        Assert.Same(originalList, list);
        Assert.Same(lengthComparer, list!.Comparer);
        Assert.Equal(new[] { "a", "c", "bb", "bbb" }, list);

        var reverse = Comparer<int>.Create((x, y) => y.CompareTo(x));
        var originalMap = new OrderedKeyValueList<int, int>(reverse) { { -1, -1 } };
        OrderedKeyValueList<int, int>? map = originalMap;
        reader = new TinyhandReader(CreateMap([new(3, 30), new(1, 10), new(2, 20), new(1, 11)]));
        new OrderedKeyValueListFormatter<int, int>().Deserialize(ref reader, ref map, Untrusted);
        Assert.Same(originalMap, map);
        Assert.Same(reverse, map!.Comparer);
        Assert.Equal(new[] { 3, 2, 1, 1 }, map.Select(x => x.Key));
        Assert.Equal(new[] { 30, 20, 10, 11 }, map.Select(x => x.Value));
    }

    [Fact]
    public void TemporarySortStorageIsReservedBeforeReusedCollectionsAreChanged()
    {
        byte[] array = [0x92, 2, 1];
        byte[] map = [0x82, 2, 2, 1, 1];
        var arrayStorage = 2L * (sizeof(int) + Unsafe.SizeOf<CollectionSortEntry<int>>());
        var mapStorage = 2L * (Unsafe.SizeOf<KeyValuePair<int, int>>() + Unsafe.SizeOf<CollectionSortEntry<KeyValuePair<int, int>>>());
        CheckStorage(new OrderedListFormatter<int>(), new OrderedList<int> { 99 }, array, arrayStorage);
        CheckStorage(new SortedListFormatter<int, int>(), new SortedList<int, int> { [99] = 99 }, map, mapStorage);
        CheckStorage(new OrderedKeyValueListFormatter<int, int>(), new OrderedKeyValueList<int, int> { { 99, 99 } }, map, mapStorage);
    }

    [Fact]
    public void AllowingDuplicateKeysDoesNotDisableSortStorageChecks()
    {
        byte[] data = [0x82, 1, 10, 1, 20];
        var storage = 2L * (Unsafe.SizeOf<KeyValuePair<int, int>>() + Unsafe.SizeOf<CollectionSortEntry<KeyValuePair<int, int>>>());
        var options = Options(storage - 1);
        options = options with { Security = options.Security.WithRejectDuplicateKeys(false) };
        Assert.Throws<TinyhandException>(() =>
        {
            var reader = new TinyhandReader(data);
            SortedList<int, int>? result = null;
            new SortedListFormatter<int, int>().Deserialize(ref reader, ref result, options);
        });
    }

    [Fact]
    public void ArcRuntimeComparableOrderingMatchesIndividualInsertions()
    {
        object?[] input = [new RuntimeComparable(3), new RuntimeComparable(1), null, new RuntimeComparable(2), new RuntimeComparable(1)];
        var expected = new OrderedList<object?>();
        foreach (var value in input)
        {
            expected.Add(value);
        }

        var entries = input.Select((value, index) => new CollectionSortEntry<object?>(value, index)).ToArray();
        CollectionSortHelper.Sort(entries, static x => x, Comparer<object?>.Default, true);
        Assert.Equal(expected, entries.Select(x => x.Value));
    }

    private static void CheckStorage<T>(ITinyhandFormatter<T> formatter, T original, byte[] data, long bytes)
        where T : class
    {
        var preserved = Serialize(formatter, original);
        var options = Options(bytes - 1);
        T? result = original;
        Assert.Throws<TinyhandException>(() =>
        {
            var reader = new TinyhandReader(data);
            formatter.Deserialize(ref reader, ref result, options);
        });
        Assert.Same(original, result);
        Assert.Equal(preserved, Serialize(formatter, original));

        var acceptedReader = new TinyhandReader(data);
        formatter.Deserialize(ref acceptedReader, ref result, Options(bytes));
        Assert.Same(original, result);
        Assert.True(acceptedReader.End);
        Assert.Equal(0, acceptedReader.Depth);
    }

    private static TinyhandSerializerOptions Options(long bytes)
        => Untrusted with { Security = Untrusted.Security.WithLimits(Untrusted.Security.Limits with { MaximumTotalCollectionBytes = bytes }) };

    private static byte[] CreateMap(KeyValuePair<int, int>[] entries)
    {
        var writer = TinyhandWriter.CreateFromBytePool();
        try
        {
            writer.WriteMapHeader(entries.Length);
            foreach (var entry in entries)
            {
                writer.Write(entry.Key);
                writer.Write(entry.Value);
            }

            return writer.FlushAndGetArray();
        }
        finally
        {
            writer.Dispose();
        }
    }

    private static byte[] Serialize<T>(ITinyhandFormatter<T> formatter, T value)
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

    private sealed class RuntimeComparable(int value) : IComparable<object>
    {
        private int Value => value;

        public int CompareTo(object? other)
        {
            ArgumentNullException.ThrowIfNull(other);
            return value.CompareTo(((RuntimeComparable)other).Value);
        }
    }
}
