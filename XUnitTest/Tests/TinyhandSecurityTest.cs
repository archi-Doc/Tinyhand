// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Tinyhand.IO;
using Xunit;

namespace Tinyhand.Tests;

public class TinyhandSecurityTest
{
    [Fact]
    public void CollisionResistantHashesAgreeWithEquality()
    {
        var security = TinyhandSecurity.UntrustedData;

        // The hash of a float must depend on its four bytes only.
        var floats = security.GetEqualityComparer<float>();
        var floatSet = new HashSet<float>(floats);
        for (var i = 0; i < 1000; i++)
        {
            floatSet.Add(i * 1.5f);
        }

        for (var i = 0; i < 1000; i++)
        {
            Assert.Contains(i * 1.5f, floatSet);
            Assert.Equal(floats.GetHashCode(i * 1.5f), floats.GetHashCode(i * 1.5f));
        }

        Assert.Equal(floats.GetHashCode(0f), floats.GetHashCode(-0f));
        Assert.Equal(floats.GetHashCode(float.NaN), floats.GetHashCode(BitConverter.Int32BitsToSingle(-1)));

        var doubles = security.GetEqualityComparer<double>();
        Assert.Equal(doubles.GetHashCode(0d), doubles.GetHashCode(-0d));
        Assert.Equal(doubles.GetHashCode(1.5d), doubles.GetHashCode(1.5d));

        // DateTime.Equals ignores Kind, so the hash must ignore it too.
        var dateTimes = security.GetEqualityComparer<DateTime>();
        var utc = new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        var local = DateTime.SpecifyKind(utc, DateTimeKind.Local);
        Assert.True(dateTimes.Equals(utc, local));
        Assert.Equal(dateTimes.GetHashCode(utc), dateTimes.GetHashCode(local));
        Assert.Contains(local, new HashSet<DateTime>(new[] { utc }, dateTimes));
    }

    [Fact]
    public void CollisionResistanceRejectsObjectComparers()
    {
        foreach (var security in new[]
        {
            TinyhandSecurity.UntrustedData,
            TinyhandSecurity.TrustedData.WithHashCollisionResistant(true),
            TinyhandSecurity.UntrustedData.WithMaximumObjectGraphDepth(10),
        })
        {
            Assert.Throws<TypeAccessException>(() => security.GetEqualityComparer<object>());
        }
    }

    [Fact]
    public void TrustedDataPreservesObjectComparers()
    {
        Assert.Same(EqualityComparer<object>.Default, TinyhandSecurity.TrustedData.GetEqualityComparer<object>());
        Assert.Same(EqualityComparer<object>.Default, TinyhandSecurity.UntrustedData.WithHashCollisionResistant(false).GetEqualityComparer<object>());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UntrustedDataRejectsObjectKeyDictionaries(bool reuse)
    {
        var dictionary = new Dictionary<object, int> { ["key"] = 1 };
        AssertObjectKeysRejected(dictionary, reuse);
        AssertObjectKeysRejected<IDictionary<object, int>>(dictionary, reuse);
        AssertObjectKeysRejected<IReadOnlyDictionary<object, int>>(dictionary, reuse);
        AssertObjectKeysRejected(new ReadOnlyDictionary<object, int>(dictionary), reuse);
        AssertObjectKeysRejected(new ConcurrentDictionary<object, int>(dictionary), reuse);
        AssertObjectKeysRejected(new Dictionary<object, int>(), reuse);
    }

    [Fact]
    public void UntrustedDataRejectsObjectKeySets()
    {
        AssertObjectKeysRejected(new HashSet<object> { 1, "key" }, false);
    }

    [Fact]
    public void UntrustedDataRejectsObjectKeyLookups()
    {
        var lookup = new[] { 1 }.ToLookup(x => (object)"key");
        AssertObjectKeysRejected(lookup, false);

        var options = TinyhandSerializerOptions.Standard with { Security = TinyhandSecurity.UntrustedData };
        Assert.Throws<TypeAccessException>(() => TinyhandSerializer.Clone(lookup, options));
        Assert.Throws<TypeAccessException>(() => TinyhandSerializer.Reconstruct<ILookup<object, int>>(options));
    }

    [Fact]
    public void UntrustedDataRejectsUntypedMaps()
    {
        object map = new Dictionary<string, int> { ["key"] = 1 };
        AssertObjectKeysRejected(map, false);

        var options = TinyhandSerializerOptions.Standard with { Security = TinyhandSecurity.UntrustedData };
        Assert.Throws<TypeAccessException>(() => TinyhandSerializer.Clone(map, options));
        Assert.Throws<TypeAccessException>(() => TinyhandSerializer.Reconstruct<Dictionary<object, int>>(options));
    }

    [Fact]
    public void UntrustedDataSupportsTypedKeysAndObjectValues()
    {
        var options = TinyhandSerializerOptions.Standard with { Security = TinyhandSecurity.UntrustedData };
        var dictionary = new Dictionary<string, object> { ["key"] = 123 };
        var bytes = TinyhandSerializer.Serialize(dictionary, options);
        var result = TinyhandSerializer.Deserialize<Dictionary<string, object>>(bytes, options)!;
        Assert.Equal(123, Assert.IsType<int>(result["key"]));

        var longKeys = new Dictionary<long, string> { [long.MaxValue] = "value" };
        bytes = TinyhandSerializer.Serialize(longKeys, options);
        Assert.Equal("value", TinyhandSerializer.Deserialize<Dictionary<long, string>>(bytes, options)![long.MaxValue]);

        var array = new object?[] { 1, "value", null };
        bytes = TinyhandSerializer.Serialize(array, options);
        Assert.Equal(array, TinyhandSerializer.Deserialize<object?[]>(bytes, options));
    }

    private static void AssertObjectKeysRejected<T>(T source, bool reuse)
        where T : class
    {
        var bytes = TinyhandSerializer.Serialize(source, TinyhandSerializerOptions.Standard);
        var options = TinyhandSerializerOptions.Standard with { Security = TinyhandSecurity.UntrustedData };
        var formatter = options.Resolver.GetFormatter<T>();

        Assert.Throws<TypeAccessException>(() =>
        {
            var reader = new TinyhandReader(bytes);
            T? value = reuse ? source : null;
            formatter.Deserialize(ref reader, ref value, options);
        });
    }
}
