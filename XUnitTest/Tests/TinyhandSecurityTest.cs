// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Tinyhand.IO;
using Xunit;

namespace Tinyhand.Tests;

public class TinyhandSecurityTest
{
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
        AssertObjectKeysRejected<IDictionary>(new Hashtable { ["key"] = 1 }, reuse);
    }

    [Fact]
    public void UntrustedDataRejectsObjectKeySets()
    {
        AssertObjectKeysRejected(new HashSet<object> { 1, "key" }, false);
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
