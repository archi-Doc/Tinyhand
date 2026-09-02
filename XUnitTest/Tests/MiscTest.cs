// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Tinyhand;
using Xunit;

namespace XUnitTest.Tests;

public class ThreadsafeTypeKeyHashtableTest2
{
    private static Type[] CreateTypes(int count)
    {
        var types = new Type[count];
        var t = typeof(int);
        for (var i = 0; i < count; i++)
        {
            t = t.MakeArrayType();
            types[i] = t;
        }

        return types;
    }

    [Fact]
    public void ResizeKeepsAllEntries()
    {
        var types = CreateTypes(64);
        var table = new ThreadsafeTypeKeyHashtable<int>();

        for (var i = 0; i < types.Length; i++)
        {
            table.TryAdd(types[i], i).IsTrue();

            // Re-adding an existing key must not add anything, and must not lose any entry
            // even when it happens to trigger a rehash.
            table.TryAdd(types[0], -1).IsFalse();
            table.TryAdd(types[i], -1).IsFalse();

            for (var j = 0; j <= i; j++)
            {
                table.TryGetValue(types[j], out var value).IsTrue();
                value.Is(j);
            }
        }

        table.Keys.Length.Is(types.Length);
        table.Values.Length.Is(types.Length);
        table.ToArray().Length.Is(types.Length);
    }

    [Fact]
    public void GetOrAddReturnsExistingValue()
    {
        var types = CreateTypes(32);
        var table = new ThreadsafeTypeKeyHashtable<int>();

        for (var i = 0; i < types.Length; i++)
        {
            table.GetOrAdd(types[i], _ => i).Is(i);
        }

        for (var i = 0; i < types.Length; i++)
        {
            table.GetOrAdd(types[i], _ => -1).Is(i);
        }
    }
}

public class Utf8StringTest2
{
    [Fact]
    public void EqualityIsConsistent()
    {
        var a = new Utf8String("test"u8);
        var b = new Utf8String("test"u8);
        var c = new Utf8String("Test"u8);

        a.Equals(b).IsTrue();
        a.Equals(c).IsFalse();
        a.GetHashCode().Is(b.GetHashCode());

        // object.Equals must agree with IEquatable<Utf8String>.Equals.
        a.Equals((object)b).IsTrue();
        a.Equals((object)c).IsFalse();
        (a == b).IsTrue();
        (a != c).IsTrue();

        var set = new HashSet<Utf8String> { a, };
        set.Contains(b).IsTrue();
        set.Contains(c).IsFalse();
    }

    [Fact]
    public void DefaultInstanceIsUsable()
    {
        var d = default(Utf8String);
        var empty = new Utf8String();

        d.Equals(empty).IsTrue();
        empty.Equals(d).IsTrue();
        d.GetHashCode().Is(empty.GetHashCode());
        d.ToString().Is(string.Empty);
        d.Equals(new Utf8String("a"u8)).IsFalse();
    }

    [Fact]
    public void VariousLengths()
    {
        for (var length = 0; length < 40; length++)
        {
            var bytes = new byte[length];
            new Random(length).NextBytes(bytes);
            var a = new Utf8String(bytes);
            var b = new Utf8String((byte[])bytes.Clone());

            a.Equals(b).IsTrue();
            a.GetHashCode().Is(b.GetHashCode());
        }
    }
}

[TinyhandObject]
public partial class Lz4Class
{
    [Key(0)]
    public string Text { get; set; } = string.Empty;

    [Key(1)]
    public int[] Numbers { get; set; } = [];
}

public class TinyhandSerializerLz4Test
{
    [Fact]
    public void RoundTrip()
    {
        var c = new Lz4Class
        {
            Text = new string('a', 10_000),
            Numbers = new int[5_000],
        };

        for (var i = 0; i < c.Numbers.Length; i++)
        {
            c.Numbers[i] = i;
        }

        var options = TinyhandSerializerOptions.Lz4;
        var bin = TinyhandSerializer.Serialize(c, options);
        var c2 = TinyhandSerializer.Deserialize<Lz4Class>(bin, options);

        c2!.Text.Is(c.Text);
        c2.Numbers.SequenceEqual(c.Numbers).IsTrue();

        // Uncompressed data can still be read with the Lz4 option.
        var bin2 = TinyhandSerializer.Serialize(c);
        var c3 = TinyhandSerializer.Deserialize<Lz4Class>(bin2, options);
        c3!.Text.Is(c.Text);
    }
}

public class TinyhandSerializerStreamTest
{
    [Fact]
    public void SerializeAndDeserializeStream()
    {
        var value = new KeyValuePair<int, string>(1, "test");
        using var ms = new System.IO.MemoryStream();
        TinyhandSerializer.Serialize(ms, value);

        ms.Position = 0;
        var value2 = TinyhandSerializer.Deserialize<KeyValuePair<int, string>>(ms);
        value2.Is(value);
    }

    [Fact]
    public void DeserializeReportsBytesRead()
    {
        var bin = TinyhandSerializer.Serialize(123);
        var buffer = new byte[bin.Length + 4];
        bin.CopyTo(buffer, 0);

        var value = TinyhandSerializer.Deserialize<int>(buffer, out var bytesRead, null);
        value.Is(123);
        bytesRead.Is(bin.Length);
    }
}
