// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Arc.Collections;
using Tinyhand;
using Tinyhand.Formatters;
using Tinyhand.IO;
using Xunit;

namespace XUnitTest.Tests;

public class DuplicateMapSecurityTest
{
    private static readonly TinyhandSerializerOptions Untrusted = TinyhandSerializerOptions.Standard with { Security = TinyhandSecurity.UntrustedData };

    [Theory]
    [InlineData(10)]
    [InlineData(20)]
    public void UniqueDictionariesRejectDuplicateKeysEvenWhenValuesMatch(int secondValue)
    {
        byte[] bytes = [MessagePackCode.MinFixMap + 2, 1, 10, 1, (byte)secondValue];
        CheckUniqueMap(new DictionaryFormatter<long, int>(), bytes);
        CheckUniqueMap(new GenericDictionaryFormatter<long, int, Dictionary<long, int>>((count, comparer) => new(count, comparer)), bytes);
        CheckUniqueMap(new InterfaceDictionaryFormatter<long, int>(), bytes);
        CheckUniqueMap(new InterfaceReadOnlyDictionaryFormatter<long, int>(), bytes);
        CheckUniqueMap(new ReadOnlyDictionaryFormatter<long, int>(), bytes);
        CheckUniqueMap(new ConcurrentDictionaryFormatter<long, int>(), bytes);
        CheckUniqueMap(new SortedDictionaryFormatter<long, int>(), bytes);
        CheckUniqueMap(new SortedListFormatter<long, int>(), bytes);
        CheckUniqueMap(new ImmutableDictionaryFormatter<long, int>(), bytes);
        CheckUniqueMap(new ImmutableSortedDictionaryFormatter<long, int>(), bytes);
        CheckUniqueMap(new InterfaceImmutableDictionaryFormatter<long, int>(), bytes);
        CheckUniqueMap(new OrderedMapFormatter<long, int>(), bytes);
        CheckUniqueMap(new UnorderedMapFormatter<long, int>(), bytes);
        Assert.Throws<TinyhandException>(() => TinyhandSerializer.Deserialize<Dictionary<long, int>>(bytes, Untrusted));
    }

    [Fact]
    public void ExplicitMultimapsContinueToAcceptDuplicateKeys()
    {
        byte[] bytes = [MessagePackCode.MinFixMap + 2, 1, 10, 1, 20];
        var multiMap = Deserialize(new OrderedMultiMapFormatter<long, int>(), bytes, Untrusted);
        var pairs = Deserialize(new KeyValueListFormatter<long, int>(), bytes, Untrusted);
        var orderedPairs = Deserialize(new OrderedKeyValueListFormatter<long, int>(), bytes, Untrusted);
        Assert.Equal(2, multiMap!.Count);
        Assert.Equal(2, pairs!.Count);
        Assert.Equal(2, orderedPairs!.Count);
        Assert.Equal(new[] { 10, 20 }, pairs.Select(x => x.Value));
    }

    [Fact]
    public void HashResistanceAndDuplicateRejectionAreIndependent()
    {
        byte[] bytes = [MessagePackCode.MinFixMap + 2, MessagePackCode.MinFixStr + 1, (byte)'a', 10, MessagePackCode.MinFixStr + 1, (byte)'a', 20];
        var options = Untrusted with { Security = Untrusted.Security.WithHashCollisionResistant(false) };
        Assert.Throws<TinyhandException>(() => Deserialize(new Utf16HashtableFormatter<int>(), bytes, options));

        var trusted = Deserialize(new Utf16HashtableFormatter<int>(), bytes, TinyhandSerializerOptions.Standard);
        Assert.Equal(1, trusted!.Count);
        Assert.True(trusted.TryGetValue("a", out var lastValue));
        Assert.Equal(20, lastValue);
    }

    private static void CheckUniqueMap<T>(ITinyhandFormatter<T> formatter, byte[] bytes)
        where T : IEnumerable<KeyValuePair<long, int>>
    {
        Assert.Throws<TinyhandException>(() => Deserialize(formatter, bytes, Untrusted));
        var trusted = Deserialize(formatter, bytes, TinyhandSerializerOptions.Standard);
        Assert.Equal(new KeyValuePair<long, int>(1, 10), Assert.Single(trusted!));
        var options = Untrusted with { Security = Untrusted.Security.WithRejectDuplicateKeys(false) };
        var relaxed = Deserialize(formatter, bytes, options);
        Assert.Equal(new KeyValuePair<long, int>(1, 10), Assert.Single(relaxed!));
    }

    private static T? Deserialize<T>(ITinyhandFormatter<T> formatter, byte[] bytes, TinyhandSerializerOptions options)
    {
        var reader = new TinyhandReader(bytes);
        T? result = default;
        formatter.Deserialize(ref reader, ref result, options);
        Assert.True(reader.End);
        Assert.Equal(0, reader.Depth);
        return result;
    }
}
