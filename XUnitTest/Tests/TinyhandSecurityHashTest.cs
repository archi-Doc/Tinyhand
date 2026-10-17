// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Tinyhand.Tests;

public class TinyhandSecurityHashTest
{
    [Fact]
    public void TrustedDataRetainsDefaultComparers()
    {
        foreach (var security in new[] { TinyhandSecurity.TrustedData, TinyhandSecurity.UntrustedData.WithHashCollisionResistant(false) })
        {
            Assert.Same(EqualityComparer<long>.Default, security.GetEqualityComparer<long>());
            Assert.Same(EqualityComparer<ulong>.Default, security.GetEqualityComparer<ulong>());
            Assert.Same(EqualityComparer<double>.Default, security.GetEqualityComparer<double>());
            Assert.Same(EqualityComparer<Guid>.Default, security.GetEqualityComparer<Guid>());
            Assert.Same(EqualityComparer<DateTime>.Default, security.GetEqualityComparer<DateTime>());
            Assert.Same(EqualityComparer<DateTimeOffset>.Default, security.GetEqualityComparer<DateTimeOffset>());
        }
    }

    [Fact]
    public void SipHashMatchesOfficialVectorsForBothSupportedLengths()
    {
        // Reference vectors: https://github.com/veorq/SipHash/blob/master/vectors.h.
        // Key bytes are 00..0f; message bytes are 00..07 or 00..0f.
        var hash = new SipHash(0x0706050403020100UL, 0x0f0e0d0c0b0a0908UL);
        Assert.Equal(0x93f5f5799a932462UL, hash.Hash64(0x0706050403020100UL));
        Assert.Equal(0x3f2acc7f57c29bdbUL, hash.Hash64(0x0706050403020100UL, 0x0f0e0d0c0b0a0908UL));
    }

    [Fact]
    public void SipHashUsesBothSecretWordsAndBothMessageWords()
    {
        var hash = new SipHash(0, 0);
        var firstKey = new SipHash(1, 0);
        var secondKey = new SipHash(0, 1);
        Assert.NotEqual(hash.Hash64(0), firstKey.Hash64(0));
        Assert.NotEqual(hash.Hash64(0), secondKey.Hash64(0));
        Assert.NotEqual(hash.Hash64(0, 0), firstKey.Hash64(0, 0));
        Assert.NotEqual(hash.Hash64(0, 0), secondKey.Hash64(0, 0));
        Assert.NotEqual(hash.Hash64(0, 0), hash.Hash64(1, 0));
        Assert.NotEqual(hash.Hash64(0, 0), hash.Hash64(0, 1));
    }

    [Fact]
    public void KnownHashCodeCollisionAttackDoesNotFloodNumericComparers()
    {
        var keys = AttackKeys().Take(4096).ToArray();
        AssertWellDistributed(keys.Select(x => unchecked((long)x)));
        AssertWellDistributed(keys);
        AssertWellDistributed(keys.Select(x => BitConverter.Int64BitsToDouble(unchecked((long)x))).Where(x => !double.IsNaN(x)));
    }

    [Fact]
    public void KnownHashCodeCollisionAttackDoesNotFloodGuidComparer()
    {
        // The old comparer added these two 32-bit words and two zero words to System.HashCode.
        var keys = AttackKeys().Take(4096).Select(x =>
        {
            Span<byte> bytes = stackalloc byte[16];
            bytes.Clear();
            BinaryPrimitives.WriteUInt32LittleEndian(bytes, (uint)(x >> 32));
            BinaryPrimitives.WriteUInt32LittleEndian(bytes[4..], unchecked((uint)x));
            return new Guid(bytes);
        });
        AssertWellDistributed(keys);
    }

    [Fact]
    public void KnownHashCodeCollisionAttackDoesNotFloodDateComparers()
    {
        var ticks = AttackKeys().Where(x => x <= (ulong)DateTime.MaxValue.Ticks).Take(4096).ToArray();
        AssertWellDistributed(ticks.Select(x => new DateTime((long)x, DateTimeKind.Utc)));
        AssertWellDistributed(ticks.Select(x => new DateTimeOffset((long)x, TimeSpan.Zero)));
    }

    [Fact]
    public void DeserializedDictionaryUsesTheResistantHash()
    {
        var source = AttackKeys().Take(4096).ToDictionary(x => unchecked((long)x), x => x);
        var options = TinyhandSerializerOptions.Standard with { Security = TinyhandSecurity.UntrustedData };
        var bytes = TinyhandSerializer.Serialize(source);
        var result = TinyhandSerializer.Deserialize<Dictionary<long, ulong>>(bytes, options)!;
        Assert.Equal(source.Count, result.Count);
        Assert.True(source.Keys.Select(result.Comparer.GetHashCode).Distinct().Count() > 4000);
        foreach (var pair in source)
        {
            Assert.Equal(pair.Value, result[pair.Key]);
        }
    }

    [Fact]
    public void HashesPreserveFloatingPointAndDateEquality()
    {
        var security = TinyhandSecurity.UntrustedData;
        var doubles = security.GetEqualityComparer<double>();
        var nan = BitConverter.Int64BitsToDouble(0x7ff0000000000001L);
        var negativeNan = BitConverter.Int64BitsToDouble(unchecked((long)0xfff123456789abcdUL));
        AssertEqualHash(doubles, 0d, BitConverter.Int64BitsToDouble(long.MinValue));
        AssertEqualHash(doubles, double.NaN, nan);
        AssertEqualHash(doubles, double.NaN, negativeNan);

        var floats = security.GetEqualityComparer<float>();
        AssertEqualHash(floats, 0f, BitConverter.Int32BitsToSingle(int.MinValue));
        AssertEqualHash(floats, float.NaN, BitConverter.Int32BitsToSingle(0x7f800001));

        var utc = new DateTime(2026, 10, 17, 12, 0, 0, DateTimeKind.Utc);
        AssertEqualHash(security.GetEqualityComparer<DateTime>(), utc, DateTime.SpecifyKind(utc, DateTimeKind.Local));
        AssertEqualHash(security.GetEqualityComparer<DateTime>(), utc, DateTime.SpecifyKind(utc, DateTimeKind.Unspecified));

        var offset = new DateTimeOffset(utc);
        AssertEqualHash(security.GetEqualityComparer<DateTimeOffset>(), offset, offset.ToOffset(TimeSpan.FromHours(9)));
        AssertEqualHash(security.GetEqualityComparer<DateTimeOffset>(), offset, offset.ToOffset(TimeSpan.FromHours(-8)));

        var guid = new Guid("00112233-4455-6677-8899-aabbccddeeff");
        AssertEqualHash(security.GetEqualityComparer<Guid>(), guid, new Guid(guid.ToByteArray()));
    }

    [Fact]
    public void ResistantHashingDoesNotAllocateAfterInitialization()
    {
        var longs = TinyhandSecurity.UntrustedData.GetEqualityComparer<long>();
        var guids = TinyhandSecurity.UntrustedData.GetEqualityComparer<Guid>();
        var guid = new Guid("00112233-4455-6677-8899-aabbccddeeff");
        var result = longs.GetHashCode(1) ^ guids.GetHashCode(guid);
        var allocated = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 4096; i++)
        {
            result ^= longs.GetHashCode(i);
            result ^= guids.GetHashCode(guid);
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - allocated);
        GC.KeepAlive(result);
    }

    private static IEnumerable<ulong> AttackKeys()
    {
        // Seed-independent collision construction published by the .NET team:
        // https://github.com/dotnet/runtime/blob/main/docs/design/security/System.HashCode.md.
        for (uint i = 0; ; i++)
        {
            var high = unchecked((i * 0xA89ED915u) << 15);
            var low = unchecked(i * 0x412BDE25u);
            yield return ((ulong)high << 32) | low;
        }
    }

    private static void AssertWellDistributed<T>(IEnumerable<T> values)
        where T : notnull
    {
        var keys = values.ToArray();
        Assert.InRange(keys.Length, 4000, 4096);
        Assert.Equal(keys.Length, keys.Distinct().Count());
        var comparer = TinyhandSecurity.UntrustedData.GetEqualityComparer<T>();
        var groups = keys.GroupBy(comparer.GetHashCode).ToArray();

        // Allow ordinary 32-bit collisions. The vulnerable hash produced only one or two groups.
        Assert.True(groups.Length > keys.Length * 0.99);
        Assert.True(groups.Max(x => x.Count()) <= 4);
    }

    private static void AssertEqualHash<T>(IEqualityComparer<T> comparer, T left, T right)
        where T : notnull
    {
        Assert.True(comparer.Equals(left, right));
        Assert.Equal(comparer.GetHashCode(left), comparer.GetHashCode(right));
        Assert.Contains(right, new HashSet<T>(new[] { left }, comparer));
    }
}
