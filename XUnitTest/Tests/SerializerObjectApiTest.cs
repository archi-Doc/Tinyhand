// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Tinyhand;
using Tinyhand.IO;
using Xunit;

namespace XUnitTest.Tests;

[TinyhandObject]
public partial class ObjectApiClass : IEquatable<ObjectApiClass>
{
    [Key(0)]
    public int Number { get; set; } = 1;

    [Key(1)]
    public string Text { get; set; } = "text";

    [Key(2)]
    public int[] Array { get; set; } = [1, 2, 3];

    public bool Equals(ObjectApiClass? other)
        => other is not null && this.Number == other.Number && this.Text == other.Text && this.Array.SequenceEqual(other.Array);

    public override bool Equals(object? obj) => this.Equals(obj as ObjectApiClass);

    public override int GetHashCode() => HashCode.Combine(this.Number, this.Text);
}

/// <summary>
/// The SerializeObject/DeserializeObject family is the direct path for types that implement
/// <see cref="ITinyhandSerializable{T}"/>; it bypasses the resolver, so it is checked against the
/// resolver-based API to make sure both produce the same bytes and the same value.
/// </summary>
public class SerializerObjectApiTest
{
    private static ObjectApiClass Sample => new() { Number = 42, Text = "abc", Array = [4, 5, 6], };

    [Fact]
    public void SerializeObjectMatchesSerialize()
    {
        var c = Sample;
        var expected = TinyhandSerializer.Serialize(c);

        TinyhandSerializer.SerializeObject(c).SequenceEqual(expected).IsTrue();
        TinyhandSerializer.SerializeObject(c, TinyhandSerializerOptions.Standard).SequenceEqual(expected).IsTrue();

        // The writer overload.
        var writer = new TinyhandWriter(new byte[64]);
        try
        {
            TinyhandSerializer.SerializeObject(ref writer, c);
            writer.FlushAndGetArray().SequenceEqual(expected).IsTrue();
        }
        finally
        {
            writer.Dispose();
        }

        // The pooled-memory overload returns the same bytes and must be returned to the pool.
        var rentMemory = TinyhandSerializer.SerializeObjectToRentMemory(c);
        try
        {
            rentMemory.Span.SequenceEqual(expected).IsTrue();
        }
        finally
        {
            rentMemory.Return();
        }
    }

    [Fact]
    public void DeserializeObject()
    {
        var c = Sample;
        var binary = TinyhandSerializer.SerializeObject(c);

        TinyhandSerializer.DeserializeObject<ObjectApiClass>(binary).Is(c);

        var value = default(ObjectApiClass);
        TinyhandSerializer.DeserializeObject(binary, ref value);
        value.Is(c);

        // The reader overloads.
        var reader = new TinyhandReader(binary);
        TinyhandSerializer.DeserializeObject<ObjectApiClass>(ref reader).Is(c);

        reader = new TinyhandReader(binary);
        var reconstructed = TinyhandSerializer.DeserializeAndReconstructObject<ObjectApiClass>(ref reader);
        reconstructed.Is(c);
    }

    [Fact]
    public void DeserializeAndReconstructObjectFillsNil()
    {
        // Nil leaves no data to read, so the value is reconstructed with its defaults.
        var writer = new TinyhandWriter(new byte[16]);
        writer.WriteNil();
        var nil = writer.FlushAndGetArray();

        var reader = new TinyhandReader(nil);
        var value = TinyhandSerializer.DeserializeAndReconstructObject<ObjectApiClass>(ref reader);
        value.IsNotNull();
        value.Number.Is(1);

        // Without reconstruction a nil yields null.
        reader = new TinyhandReader(nil);
        TinyhandSerializer.DeserializeObject<ObjectApiClass>(ref reader).IsNull();
    }

    [Fact]
    public void TryDeserializeObject()
    {
        var c = Sample;
        var binary = TinyhandSerializer.SerializeObject(c);

        TinyhandSerializer.TryDeserializeObject<ObjectApiClass>(binary, out var value).IsTrue();
        value.Is(c);

        // Truncated data fails instead of throwing.
        TinyhandSerializer.TryDeserializeObject<ObjectApiClass>(binary.AsSpan(0, binary.Length / 2), out var partial).IsFalse();
        partial.IsNull();

        TinyhandSerializer.TryDeserializeObject<ObjectApiClass>(Array.Empty<byte>(), out var empty).IsFalse();
        empty.IsNull();
    }

    [Fact]
    public void TryDeserialize()
    {
        var c = Sample;
        var binary = TinyhandSerializer.Serialize(c);

        TinyhandSerializer.TryDeserialize<ObjectApiClass>(binary, out var value).IsTrue();
        value.Is(c);

        TinyhandSerializer.TryDeserialize<ObjectApiClass>(binary, out var value2, out var bytesRead).IsTrue();
        value2.Is(c);
        bytesRead.Is(binary.Length);

        TinyhandSerializer.TryDeserialize<ObjectApiClass>(Array.Empty<byte>(), out var empty).IsFalse();
        empty.IsNull();
    }

    [Fact]
    public void GetXxHash3()
    {
        var a = Sample;
        var b = Sample;
        var different = new ObjectApiClass { Number = 43, };

        // The hash is derived from the serialized form, so equal values hash equally.
        TinyhandSerializer.GetXxHash3(a).Is(TinyhandSerializer.GetXxHash3(b));
        TinyhandSerializer.GetXxHash3(a).IsNot(TinyhandSerializer.GetXxHash3(different));
        TinyhandSerializer.GetXxHash3(a).IsNot(0UL);
    }

    [Fact]
    public void SerializeToRentMemory()
    {
        var c = Sample;
        var expected = TinyhandSerializer.Serialize(c);

        var rentMemory = TinyhandSerializer.SerializeToRentMemory(c);
        try
        {
            rentMemory.Span.SequenceEqual(expected).IsTrue();
            TinyhandSerializer.Deserialize<ObjectApiClass>(rentMemory.Span).Is(c);
        }
        finally
        {
            rentMemory.Return();
        }
    }

    [Fact]
    public async Task SerializeAndDeserializeAsync()
    {
        var c = Sample;

        using var stream = new MemoryStream();
        await TinyhandSerializer.SerializeAsync(stream, c);
        stream.Position = 0;

        var c2 = await TinyhandSerializer.DeserializeAsync<ObjectApiClass>(stream);
        c2.Is(c);

        // The bytes are the same as the synchronous path.
        stream.ToArray().SequenceEqual(TinyhandSerializer.Serialize(c)).IsTrue();
    }

    [Fact]
    public async Task AsyncRoundTripWithCompression()
    {
        var c = new ObjectApiClass { Array = Enumerable.Range(0, 100_000).ToArray(), };

        using var stream = new MemoryStream();
        await TinyhandSerializer.SerializeAsync(stream, c, TinyhandSerializerOptions.Lz4);
        stream.Position = 0;

        var c2 = await TinyhandSerializer.DeserializeAsync<ObjectApiClass>(stream, TinyhandSerializerOptions.Lz4);
        c2.Is(c);
    }

    [Fact]
    public void SerializeAndDeserializeStream()
    {
        var c = Sample;

        using var stream = new MemoryStream();
        TinyhandSerializer.Serialize(stream, c);
        stream.Position = 0;

        TinyhandSerializer.Deserialize<ObjectApiClass>(stream).Is(c);
    }
}
