// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Arc.Collections;
using Tinyhand;
using Tinyhand.IO;
using Xunit;

namespace XUnitTest.Tests;

public class FormatterRegressionTest
{
    private static readonly TinyhandSerializerOptions Options = TinyhandSerializerOptions.Standard;

    [Fact]
    public void StringFormatterConsumesTheValueWhenReusing()
    {
        var writer = TinyhandWriter.CreateFromBytePool();
        try
        {
            writer.Write("new");
            writer.Write(5);
            var reader = new TinyhandReader(writer.FlushAndGetArray());
            string? value = "old";
            Options.Resolver.GetFormatter<string>().Deserialize(ref reader, ref value, Options);
            value.Is("new");
            reader.ReadInt32().Is(5);
        }
        finally
        {
            writer.Dispose();
        }
    }

    [Fact]
    public async Task ConcurrentCollectionsAreWrittenFromOneSnapshot()
    {
        // The count and the elements must agree while another thread modifies the collections.
        var dictionary = new ConcurrentDictionary<int, int>();
        var queue = new ConcurrentQueue<int>();
        var bag = new ConcurrentBag<int>();
        var stack = new ConcurrentStack<int>();
        using var cts = new CancellationTokenSource();
        var task = Task.Run(() =>
        {
            for (var i = 0; !cts.IsCancellationRequested; i++)
            {
                dictionary[i % 64] = i;
                dictionary.TryRemove((i + 32) % 64, out _);
                queue.Enqueue(i);
                stack.Push(i);
                bag.Add(i);
                if ((i & 63) == 0)
                {
                    queue.Clear();
                    stack.Clear();
                    bag.Clear();
                }
            }
        }, TestContext.Current.CancellationToken);

        try
        {
            for (var n = 0; n < 1000; n++)
            {
                SerializeAndReadBack(dictionary);
                SerializeAndReadBack(queue);
                SerializeAndReadBack(bag);
                SerializeAndReadBack(stack);
                Options.Resolver.GetFormatter<ConcurrentDictionary<int, int>>().Clone(dictionary, Options);
                Options.Resolver.GetFormatter<ConcurrentStack<int>>().Clone(stack, Options);
            }
        }
        finally
        {
            cts.Cancel();
            await task;
        }

        static void SerializeAndReadBack<T>(T value)
        {
            var formatter = Options.Resolver.GetFormatter<T>();
            var writer = TinyhandWriter.CreateFromBytePool();
            try
            {
                formatter.Serialize(ref writer, value, Options);
                writer.Write(12345);
                var reader = new TinyhandReader(writer.FlushAndGetArray());
                formatter.Deserialize(ref reader, Options);
                reader.ReadInt32().Is(12345);
                reader.End.IsTrue();
            }
            finally
            {
                writer.Dispose();
            }
        }
    }

    [Fact]
    public void ArcMapCloneCopiesTheValues()
    {
        var orderedMap = new OrderedMap<int, List<int>>();
        orderedMap.Add(1, new() { 1 });
        var orderedMapClone = TinyhandSerializer.Clone(orderedMap)!;
        orderedMapClone.TryGetValue(1, out var list).IsTrue();
        list!.IsNotSameReferenceAs(orderedMap[1]);
        list.Is(1);

        var unorderedMap = new UnorderedMap<int, List<int>>();
        unorderedMap.Add(1, new() { 1 });
        var unorderedMapClone = TinyhandSerializer.Clone(unorderedMap)!;
        unorderedMapClone.TryGetValue(1, out list).IsTrue();
        list!.IsNotSameReferenceAs(unorderedMap[1]);

        var multiMap = new OrderedMultiMap<int, List<int>>();
        multiMap.Add(1, new() { 1 });
        var multiMapClone = TinyhandSerializer.Clone(multiMap)!;
        multiMapClone.TryGetValue(1, out list).IsTrue();
        list!.IsNotSameReferenceAs(multiMap[1]);
    }

    [Fact]
    public void CloneKeepsTheComparer()
    {
        var set = TinyhandSerializer.Clone(new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "A" })!;
        set.Contains("a").IsTrue();

        var dictionary = TinyhandSerializer.Clone(new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) { ["A"] = 1 })!;
        dictionary.ContainsKey("a").IsTrue();

        var concurrent = TinyhandSerializer.Clone(new ConcurrentDictionary<string, int>(StringComparer.OrdinalIgnoreCase) { ["A"] = 1 })!;
        concurrent.ContainsKey("a").IsTrue();

        var reverse = Comparer<int>.Create((x, y) => y.CompareTo(x));
        var sorted = TinyhandSerializer.Clone(new SortedDictionary<int, int>(reverse) { [1] = 1, [2] = 2 })!;
        sorted.Keys.Is(2, 1);
        var sortedList = TinyhandSerializer.Clone(new SortedList<int, int>(reverse) { [1] = 1, [2] = 2 })!;
        sortedList.Keys.Is(2, 1);
    }

    [Fact]
    public void CloneOfUriLazyAndTuple()
    {
        var relative = new Uri("/a/b", UriKind.Relative);
        TinyhandSerializer.Clone(relative).Is(relative);

        // The clone is taken at the time of cloning.
        var lazy = new Lazy<List<int>>(() => new() { 1 });
        var lazyClone = TinyhandSerializer.Clone(lazy)!;
        lazy.Value.Add(2);
        lazyClone.Value.Is(1);

        var tuple = TinyhandSerializer.Reconstruct<Tuple<int, int, int, int, int, int, int, Tuple<int>>>();
        tuple.Rest.IsNotNull();
    }

    [Theory]
    [InlineData(new[] { -1, -1 }, 1)]
    [InlineData(new[] { -1, -1, 1 }, 1)]
    [InlineData(new[] { -1, -1, -1, -1 }, 1)]
    [InlineData(new[] { 1 << 20, 1 << 22, 1 << 22 }, 0)]
    [InlineData(new[] { 65536, 65536, 65536, 65536 }, 0)]
    public void MalformedMultiDimensionalArrayIsRejected(int[] dimensions, int count)
    {
        // Negative dimensions, or dimensions whose product wraps around to the element count, are rejected before allocating the array.
        var writer = TinyhandWriter.CreateFromBytePool();
        try
        {
            writer.WriteArrayHeader(dimensions.Length + 1);
            foreach (var x in dimensions)
            {
                writer.Write(x);
            }

            writer.WriteArrayHeader(count);
            for (var i = 0; i < count; i++)
            {
                writer.Write(0);
            }

            var bytes = writer.FlushAndGetArray();
            var ex = Assert.Throws<TinyhandException>(() => dimensions.Length switch
            {
                2 => (object?)TinyhandSerializer.Deserialize<int[,]>(bytes),
                3 => TinyhandSerializer.Deserialize<int[,,]>(bytes),
                _ => TinyhandSerializer.Deserialize<int[,,,]>(bytes),
            });

            Assert.IsType<TinyhandException>(ex.InnerException);
        }
        finally
        {
            writer.Dispose();
        }
    }
}
