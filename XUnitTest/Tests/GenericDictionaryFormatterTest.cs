// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Tinyhand.Formatters;
using Tinyhand.IO;
using Xunit;

namespace Tinyhand.Tests;

public class GenericDictionaryFormatterTest
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CreationPreservesCapacityAndSecurityComparer(bool untrusted)
    {
        var options = TinyhandSerializerOptions.Standard with
        {
            Security = untrusted ? TinyhandSecurity.UntrustedData : TinyhandSecurity.TrustedData,
        };
        var source = new ComparerDictionary<long> { [1] = 10, [2] = 20 };
        var bytes = TinyhandSerializer.Serialize(source, options);
        var deserialized = TinyhandSerializer.Deserialize<ComparerDictionary<long>>(bytes, options)!;
        var cloned = TinyhandSerializer.Clone(source, options)!;
        var reconstructed = TinyhandSerializer.Reconstruct<ComparerDictionary<long>>(options);
        var comparer = options.Security.GetEqualityComparer<long>();

        foreach (var dictionary in new[] { deserialized, cloned })
        {
            Assert.Equal(2, dictionary.InitialCapacity);
            Assert.Same(comparer, dictionary.Comparer);
            Assert.Equal(2, dictionary.Count);
            Assert.Equal(10, dictionary[1]);
            Assert.Equal(20, dictionary[2]);
        }

        Assert.Equal(0, reconstructed.InitialCapacity);
        Assert.Same(comparer, reconstructed.Comparer);
        Assert.Empty(reconstructed);
    }

    [Fact]
    public void DeserializationReusesExistingDictionary()
    {
        var options = TinyhandSerializerOptions.Standard with { Security = TinyhandSecurity.UntrustedData };
        var source = new ComparerDictionary<long> { [1] = 10, [2] = 20 };
        var bytes = TinyhandSerializer.Serialize(source, options);
        var original = new ComparerDictionary<long>(5, options.Security.GetEqualityComparer<long>()) { [1] = 100 };
        ComparerDictionary<long>? result = original;
        var reader = new TinyhandReader(bytes);

        options.Resolver.GetFormatter<ComparerDictionary<long>>().Deserialize(ref reader, ref result, options);

        Assert.Same(original, result);
        Assert.Equal(5, result!.InitialCapacity);
        Assert.Equal(2, result.Count);
        Assert.Equal(100, result[1]);
        Assert.Equal(20, result[2]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UntrustedDataRejectsObjectKeysBeforeCreationOrReuse(bool reuse)
    {
        var options = TinyhandSerializerOptions.Standard with { Security = TinyhandSecurity.UntrustedData };
        var source = new ComparerDictionary<object>();
        var bytes = TinyhandSerializer.Serialize(source);
        var formatter = options.Resolver.GetFormatter<ComparerDictionary<object>>();

        Assert.Throws<TypeAccessException>(() =>
        {
            var reader = new TinyhandReader(bytes);
            ComparerDictionary<object>? result = reuse ? source : null;
            formatter.Deserialize(ref reader, ref result, options);
        });
    }

    [Fact]
    public void ConstructorExceptionsAreNotWrapped()
    {
        var formatter = new GenericDictionaryFormatter<long, int, ThrowingDictionary>(static (count, comparer) => new(count, comparer));

        Assert.Throws<InvalidOperationException>(() => formatter.Reconstruct(TinyhandSerializerOptions.Standard));
    }

    public sealed class ComparerDictionary<TKey> : Dictionary<TKey, int>
        where TKey : notnull
    {
        public ComparerDictionary()
        {
        }

        public ComparerDictionary(int capacity, IEqualityComparer<TKey> comparer)
            : base(capacity, comparer)
        {
            this.InitialCapacity = capacity;
        }

        public int InitialCapacity { get; } = -1;
    }

    public sealed class ThrowingDictionary : Dictionary<long, int>
    {
        public ThrowingDictionary()
        {
        }

        public ThrowingDictionary(int capacity, IEqualityComparer<long> comparer)
            : base(capacity, comparer)
        {
            throw new InvalidOperationException("Constructor failure.");
        }
    }
}
