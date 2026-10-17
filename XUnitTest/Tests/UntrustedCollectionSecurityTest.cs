// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Arc.Collections;
using Tinyhand.Formatters;
using Tinyhand.IO;
using Xunit;

namespace Tinyhand.Tests;

public class UntrustedCollectionSecurityTest
{
    private static readonly TinyhandSerializerOptions Untrusted = TinyhandSerializerOptions.Standard with { Security = TinyhandSecurity.UntrustedData };

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DictionaryReuseHonorsSecurityWithoutInvokingUnsafeComparer(bool trusted)
    {
        var options = trusted ? TinyhandSerializerOptions.Standard : Untrusted;
        var comparer = new CountingComparer();
        var original = new Dictionary<long, int>(comparer) { [123] = 1 };
        Dictionary<long, int>? result = original;
        var bytes = TinyhandSerializer.Serialize(CreateDictionary());
        comparer.Calls = 0;
        var reader = new TinyhandReader(bytes);
        options.Resolver.GetFormatter<Dictionary<long, int>>().Deserialize(ref reader, ref result, options);

        Assert.Equal(32, result!.Count);
        if (trusted)
        {
            Assert.Same(original, result);
            Assert.Same(comparer, result.Comparer);
            Assert.True(comparer.Calls > 0);
        }
        else
        {
            Assert.NotSame(original, result);
            Assert.Same(options.Security.GetEqualityComparer<long>(), result.Comparer);
            Assert.Equal(0, comparer.Calls);
            Assert.Equal(1, original[123]);
        }
    }

    [Fact]
    public void SafeDictionaryIsStillReused()
    {
        var original = new Dictionary<long, int>(Untrusted.Security.GetEqualityComparer<long>());
        Dictionary<long, int>? result = original;
        var reader = new TinyhandReader(TinyhandSerializer.Serialize(CreateDictionary()));
        Untrusted.Resolver.GetFormatter<Dictionary<long, int>>().Deserialize(ref reader, ref result, Untrusted);
        Assert.Same(original, result);
        Assert.Equal(32, result!.Count);
    }

    [Fact]
    public void InterfaceAndConcurrentReuseReplaceUnsafeComparers()
    {
        var bytes = TinyhandSerializer.Serialize(CreateDictionary());
        var unsafeComparer = new CountingComparer();
        IDictionary<long, int>? dictionary = new Dictionary<long, int>(unsafeComparer);
        ConcurrentDictionary<long, int>? concurrent = new(unsafeComparer);
        var reader = new TinyhandReader(bytes);
        Untrusted.Resolver.GetFormatter<IDictionary<long, int>>().Deserialize(ref reader, ref dictionary, Untrusted);
        reader = new TinyhandReader(bytes);
        Untrusted.Resolver.GetFormatter<ConcurrentDictionary<long, int>>().Deserialize(ref reader, ref concurrent, Untrusted);
        Assert.Same(Untrusted.Security.GetEqualityComparer<long>(), Assert.IsType<Dictionary<long, int>>(dictionary).Comparer);
        Assert.Same(Untrusted.Security.GetEqualityComparer<long>(), concurrent!.Comparer);
        Assert.Equal(0, unsafeComparer.Calls);
    }

    [Fact]
    public void FreshGeneratedObjectReplacesInitializedDictionaryComparer()
    {
        var result = TinyhandSerializer.Deserialize<InitializedDictionaryPayload>(TinyhandSerializer.Serialize(new InitializedDictionaryPayload { Values = CreateDictionary() }), Untrusted)!;
        Assert.Same(Untrusted.Security.GetEqualityComparer<long>(), result.Values.Comparer);
        Assert.Equal(32, result.Values.Count);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ArcCollectionsUseSecurityComparerAndPreserveDuplicatePolicy(bool reuse)
    {
        var sourceMap = new UnorderedMap<long, int>(true);
        sourceMap.Add(1, 10);
        sourceMap.Add(1, 20);
        var sourceSet = new UnorderedSet<long>(true);
        sourceSet.Add(1);
        sourceSet.Add(1);
        UnorderedMap<long, int>? map = reuse ? new UnorderedMap<long, int>(true) : null;
        UnorderedSet<long>? set = reuse ? new UnorderedSet<long>(true) : null;
        var mapBytes = TinyhandSerializer.Serialize(sourceMap);
        if (reuse)
        {
            var mapReader = new TinyhandReader(mapBytes);
            Untrusted.Resolver.GetFormatter<UnorderedMap<long, int>>().Deserialize(ref mapReader, ref map, Untrusted);
        }
        else
        {
            Assert.Throws<TinyhandException>(() =>
            {
                var mapReader = new TinyhandReader(mapBytes);
                Untrusted.Resolver.GetFormatter<UnorderedMap<long, int>>().Deserialize(ref mapReader, ref map, Untrusted);
            });
        }

        var reader = new TinyhandReader(TinyhandSerializer.Serialize(sourceSet));
        Untrusted.Resolver.GetFormatter<UnorderedSet<long>>().Deserialize(ref reader, ref set, Untrusted);
        Assert.Same(Untrusted.Security.GetEqualityComparer<long>(), map!.Comparer);
        Assert.Same(Untrusted.Security.GetEqualityComparer<long>(), set!.Comparer);
        Assert.Equal(reuse, map.AllowDuplicates);
        Assert.Equal(reuse, set.AllowDuplicates);
        Assert.Equal(reuse ? 2 : 1, map.Count);
        Assert.Equal(reuse ? 2 : 1, set.Count);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ArcCollectionsRejectObjectKeysEvenWhenReusing(bool reuse)
    {
        var sourceMap = new UnorderedMap<object, int>();
        sourceMap.Add(1, 2);
        var sourceSet = new UnorderedSet<object>();
        sourceSet.Add(1);
        var mapBytes = TinyhandSerializer.Serialize(sourceMap);
        var setBytes = TinyhandSerializer.Serialize(sourceSet);
        Assert.Throws<TypeAccessException>(() =>
        {
            var reader = new TinyhandReader(mapBytes);
            UnorderedMap<object, int>? value = reuse ? sourceMap : null;
            Untrusted.Resolver.GetFormatter<UnorderedMap<object, int>>().Deserialize(ref reader, ref value, Untrusted);
        });
        Assert.Throws<TypeAccessException>(() =>
        {
            var reader = new TinyhandReader(setBytes);
            UnorderedSet<object>? value = reuse ? sourceSet : null;
            Untrusted.Resolver.GetFormatter<UnorderedSet<object>>().Deserialize(ref reader, ref value, Untrusted);
        });
    }

    [Fact]
    public void UntrustedDataRejectsHashTablesWithoutSafeComparerSupport()
    {
        var source = new Utf16Hashtable<int>();
        source.TryAdd("key", 1);
        var bytes = TinyhandSerializer.Serialize(source);
        Assert.Throws<TinyhandException>(() => TinyhandSerializer.Deserialize<Utf16Hashtable<int>>(bytes, Untrusted));
        Assert.NotNull(TinyhandSerializer.Deserialize<Utf16Hashtable<int>>(bytes, TinyhandSerializerOptions.Standard));

        var custom = new DefaultOnlyDictionary { [1] = 1 };
        bytes = TinyhandSerializer.Serialize(custom);
        Assert.Throws<TinyhandException>(() => TinyhandSerializer.Deserialize<DefaultOnlyDictionary>(bytes, Untrusted));
        Assert.Equal(1, TinyhandSerializer.Deserialize<DefaultOnlyDictionary>(bytes, TinyhandSerializerOptions.Standard)![1]);

        var setBytes = TinyhandSerializer.Serialize(new DefaultOnlySet { 1, 2 });
        Assert.Throws<TinyhandException>(() => TinyhandSerializer.Deserialize<DefaultOnlySet>(setBytes, Untrusted));
        Assert.Equal(2, TinyhandSerializer.Deserialize<DefaultOnlySet>(setBytes, TinyhandSerializerOptions.Standard)!.Count);
    }

    [Theory]
    [InlineData(0x00000001U)]
    [InlineData(0x01000000U)]
    [InlineData(0x001d0000U)]
    [InlineData(0x00ff0000U)]
    public void InvalidDecimalFlagsAreRejectedBeforeAssigningValue(uint flags)
    {
        var bytes = TinyhandSerializer.Serialize(123m);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(2), flags);
        foreach (var options in new[] { TinyhandSerializerOptions.Standard, Untrusted })
        {
            decimal value = 456m;
            Assert.Throws<TinyhandException>(() =>
            {
                var reader = new TinyhandReader(bytes);
                NativeDecimalFormatter.Instance.Deserialize(ref reader, ref value, options);
            });
            Assert.Equal(456m, value);
            Assert.Throws<TinyhandException>(() => TinyhandSerializer.Deserialize<decimal>(bytes, options));
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ValidDecimalRepresentationsRoundTripExactly(bool untrusted)
    {
        var options = untrusted ? Untrusted : TinyhandSerializerOptions.Standard;
        foreach (var value in new[] { decimal.MinValue, decimal.MaxValue, new decimal(1, 2, 3, true, 28), new decimal(0, 0, 0, true, 28) })
        {
            var result = TinyhandSerializer.Deserialize<decimal>(TinyhandSerializer.Serialize(value), options);
            Assert.Equal(decimal.GetBits(value), decimal.GetBits(result));
        }
    }

    [Fact]
    public void DisablingHashResistanceDoesNotDisableNativeValueValidation()
    {
        var options = Untrusted with { Security = Untrusted.Security.WithHashCollisionResistant(false) };
        var bytes = TinyhandSerializer.Serialize(123m);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(2), 0x00ff0000U);
        Assert.Throws<TinyhandException>(() => TinyhandSerializer.Deserialize<decimal>(bytes, options));
    }

    private static Dictionary<long, int> CreateDictionary()
    {
        var result = new Dictionary<long, int>();
        for (var i = 0; i < 32; i++)
        {
            result.Add(0x100000001L * i, i);
        }

        return result;
    }

    private sealed class CountingComparer : IEqualityComparer<long>
    {
        public int Calls;

        public bool Equals(long x, long y)
        {
            this.Calls++;
            return x == y;
        }

        public int GetHashCode(long value)
        {
            this.Calls++;
            return 0;
        }
    }

    public sealed class DefaultOnlyDictionary : Dictionary<long, int>
    {
    }

    public sealed class DefaultOnlySet : HashSet<long>
    {
    }
}

[TinyhandObject]
public partial class InitializedDictionaryPayload
{
    [Key(0)]
    public Dictionary<long, int> Values { get; set; } = new();
}
