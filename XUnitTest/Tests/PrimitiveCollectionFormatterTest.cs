// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Tinyhand;
using Xunit;

namespace XUnitTest.Tests;

/// <summary>
/// Every primitive type has a dedicated array and list formatter. This checks the whole surface of
/// those formatters (serialize, deserialize, clone, reconstruct and the null representation) for
/// each element type, including the empty and single-element edge cases.
/// </summary>
public class PrimitiveCollectionFormatterTest
{
    private static void Check<T>(params T[] values)
    {
        foreach (var array in new[] { Array.Empty<T>(), values.Take(1).ToArray(), values })
        {
            // Array
            var binary = TinyhandSerializer.Serialize(array);
            TinyhandSerializer.Deserialize<T[]>(binary)!.SequenceEqual(array).IsTrue();
            TinyhandSerializer.Clone(array)!.SequenceEqual(array).IsTrue();

            // A clone is a copy, not the same instance.
            if (array.Length > 0)
            {
                ReferenceEquals(TinyhandSerializer.Clone(array), array).IsFalse();
            }

            // List
            var list = array.ToList();
            var listBinary = TinyhandSerializer.Serialize(list);
            TinyhandSerializer.Deserialize<List<T>>(listBinary)!.SequenceEqual(list).IsTrue();
            TinyhandSerializer.Clone(list)!.SequenceEqual(list).IsTrue();

            // An array and a list of the same values have the same representation.
            binary.SequenceEqual(listBinary).IsTrue();
        }

        // Null round-trips as null for both shapes.
        TinyhandSerializer.Deserialize<T[]>(TinyhandSerializer.Serialize<T[]?>(null)).IsNull();
        TinyhandSerializer.Deserialize<List<T>>(TinyhandSerializer.Serialize<List<T>?>(null)).IsNull();
        TinyhandSerializer.Clone<T[]?>(null).IsNull();
        TinyhandSerializer.Clone<List<T>?>(null).IsNull();

        // Reconstruct yields an empty collection rather than null.
        TinyhandSerializer.Reconstruct<T[]>().Length.Is(0);
        TinyhandSerializer.Reconstruct<List<T>>().Count.Is(0);
    }

    [Fact]
    public void SignedIntegers()
    {
        Check<sbyte>(0, 1, -1, sbyte.MinValue, sbyte.MaxValue);
        Check<short>(0, 1, -1, short.MinValue, short.MaxValue);
        Check<int>(0, 1, -1, int.MinValue, int.MaxValue);
        Check<long>(0, 1, -1, long.MinValue, long.MaxValue);
        Check<Int128>(0, 1, -1, Int128.MinValue, Int128.MaxValue);
    }

    [Fact]
    public void UnsignedIntegers()
    {
        Check<byte>(0, 1, byte.MaxValue);
        Check<ushort>(0, 1, ushort.MaxValue);
        Check<uint>(0, 1, uint.MaxValue);
        Check<ulong>(0, 1, ulong.MaxValue);
        Check<UInt128>(0, 1, UInt128.MaxValue);
    }

    [Fact]
    public void FloatingPointAndOthers()
    {
        Check<float>(0f, 1.5f, -1.5f, float.MinValue, float.MaxValue);
        Check<double>(0d, 1.5d, -1.5d, double.MinValue, double.MaxValue);
        Check<bool>(true, false, true);
        Check<char>('a', '\0', char.MaxValue);
        Check<string>("a", string.Empty, "日本語");
        Check<DateTime>(
            new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc),
            DateTime.UnixEpoch,
            new DateTime(1, 1, 1, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void NilElementOfAStringArrayBecomesEmpty()
    {
        // The element type of string[] is not nullable, so a nil element is read back as an empty
        // string. The binary and the text formats agree on this.
        var writer = new Tinyhand.IO.TinyhandWriter(new byte[32]);
        writer.WriteArrayHeader(3);
        writer.Write("a");
        writer.WriteNil();
        writer.Write("b");
        var binary = writer.FlushAndGetArray();

        var fromBinary = TinyhandSerializer.Deserialize<string[]>(binary)!;
        fromBinary.SequenceEqual(["a", string.Empty, "b"]).IsTrue();

        var text = TinyhandSerializer.SerializeToString(fromBinary);
        TinyhandSerializer.DeserializeFromString<string[]>(text)!.SequenceEqual(fromBinary).IsTrue();

        // A nil in place of the whole array is still null.
        TinyhandSerializer.Deserialize<string[]>(TinyhandSerializer.Serialize<string[]?>(null)).IsNull();
    }

    [Fact]
    public void ByteArrayUsesTheBinaryFormat()
    {
        // byte[] is stored as a MessagePack binary rather than as an array of integers.
        byte[] value = [0, 1, 2, 255];
        var binary = TinyhandSerializer.Serialize(value);
        binary[0].Is(MessagePackCode.Bin8);

        TinyhandSerializer.Deserialize<byte[]>(binary)!.SequenceEqual(value).IsTrue();
        TinyhandSerializer.Clone(value)!.SequenceEqual(value).IsTrue();
        TinyhandSerializer.Deserialize<byte[]>(TinyhandSerializer.Serialize<byte[]?>(null)).IsNull();

        // A list of bytes keeps the array-of-integers representation.
        List<byte> list = [0, 1, 2, 255];
        TinyhandSerializer.Deserialize<List<byte>>(TinyhandSerializer.Serialize(list))!.SequenceEqual(list).IsTrue();
    }

    [Fact]
    public void NonNullValuesSurviveTheTextFormat()
    {
        // The text format is a separate encoder, so the same collections are checked through it.
        int[] numbers = [0, 1, -1, int.MaxValue];
        var text = TinyhandSerializer.SerializeToString(numbers);
        TinyhandSerializer.DeserializeFromString<int[]>(text)!.SequenceEqual(numbers).IsTrue();

        string[] strings = ["a", string.Empty, "日本語"];
        var stringText = TinyhandSerializer.SerializeToString(strings);
        TinyhandSerializer.DeserializeFromString<string[]>(stringText)!.SequenceEqual(strings).IsTrue();

        double[] doubles = [0d, 1.5d, -1.5d];
        var doubleText = TinyhandSerializer.SerializeToString(doubles);
        TinyhandSerializer.DeserializeFromString<double[]>(doubleText)!.SequenceEqual(doubles).IsTrue();
    }
}

/// <summary>
/// Completes <see cref="Tinyhand.Tests.MultiDimensionalArrayTest"/> with the cases it does not
/// cover: cloning, the null representation, empty dimensions and a non-cubic shape.
/// </summary>
public class MultiDimensionalArrayFormatterTest
{
    [Fact]
    public void CloneAndRoundTrip()
    {
        var two = new int[2, 3];
        var three = new int[2, 3, 4];
        var four = new int[2, 3, 4, 5];
        var n = 0;
        foreach (var i in Enumerable.Range(0, 2))
        {
            foreach (var j in Enumerable.Range(0, 3))
            {
                two[i, j] = n++;
                foreach (var k in Enumerable.Range(0, 4))
                {
                    three[i, j, k] = n++;
                    foreach (var l in Enumerable.Range(0, 5))
                    {
                        four[i, j, k, l] = n++;
                    }
                }
            }
        }

        SameValues(TinyhandSerializer.Deserialize<int[,]>(TinyhandSerializer.Serialize(two))!, two);
        SameValues(TinyhandSerializer.Clone(two)!, two);
        SameValues(TinyhandSerializer.Deserialize<int[,,]>(TinyhandSerializer.Serialize(three))!, three);
        SameValues(TinyhandSerializer.Clone(three)!, three);
        SameValues(TinyhandSerializer.Deserialize<int[,,,]>(TinyhandSerializer.Serialize(four))!, four);
        SameValues(TinyhandSerializer.Clone(four)!, four);
    }

    [Fact]
    public void EmptyDimensions()
    {
        // A zero in any dimension makes the array empty but keeps the other lengths.
        foreach (var shape in new[] { (0, 3), (3, 0), (0, 0) })
        {
            var two = new int[shape.Item1, shape.Item2];
            var result = TinyhandSerializer.Deserialize<int[,]>(TinyhandSerializer.Serialize(two))!;
            result.GetLength(0).Is(two.GetLength(0));
            result.GetLength(1).Is(two.GetLength(1));
        }

        var three = new int[0, 2, 3];
        var three2 = TinyhandSerializer.Deserialize<int[,,]>(TinyhandSerializer.Serialize(three))!;
        three2.Length.Is(0);
        three2.GetLength(1).Is(2);

        var four = new int[2, 0, 3, 4];
        var four2 = TinyhandSerializer.Deserialize<int[,,,]>(TinyhandSerializer.Serialize(four))!;
        four2.Length.Is(0);
        four2.GetLength(3).Is(4);
    }

    [Fact]
    public void NullAndReconstruct()
    {
        TinyhandSerializer.Deserialize<int[,]>(TinyhandSerializer.Serialize<int[,]?>(null)).IsNull();
        TinyhandSerializer.Deserialize<int[,,]>(TinyhandSerializer.Serialize<int[,,]?>(null)).IsNull();
        TinyhandSerializer.Deserialize<int[,,,]>(TinyhandSerializer.Serialize<int[,,,]?>(null)).IsNull();

        TinyhandSerializer.Clone<int[,]?>(null).IsNull();
        TinyhandSerializer.Clone<int[,,]?>(null).IsNull();
        TinyhandSerializer.Clone<int[,,,]?>(null).IsNull();

        TinyhandSerializer.Reconstruct<int[,]>().Length.Is(0);
        TinyhandSerializer.Reconstruct<int[,,]>().Length.Is(0);
        TinyhandSerializer.Reconstruct<int[,,,]>().Length.Is(0);
    }

    [Fact]
    public void InvalidShapeIsRejected()
    {
        // The element count must match the product of the dimensions.
        var writer = new Tinyhand.IO.TinyhandWriter(new byte[64]);
        writer.WriteArrayHeader(3);
        writer.Write(2);
        writer.Write(3);
        writer.WriteArrayHeader(5); // 2 * 3 != 5
        for (var i = 0; i < 5; i++)
        {
            writer.Write(i);
        }

        var binary = writer.FlushAndGetArray();
        Assert.ThrowsAny<Exception>(() => TinyhandSerializer.Deserialize<int[,]>(binary));
    }

    private static void SameValues(int[,] actual, int[,] expected)
    {
        actual.GetLength(0).Is(expected.GetLength(0));
        actual.GetLength(1).Is(expected.GetLength(1));
        for (var i = 0; i < expected.GetLength(0); i++)
        {
            for (var j = 0; j < expected.GetLength(1); j++)
            {
                actual[i, j].Is(expected[i, j]);
            }
        }
    }

    private static void SameValues(int[,,] actual, int[,,] expected)
    {
        for (var i = 0; i < expected.GetLength(0); i++)
        {
            for (var j = 0; j < expected.GetLength(1); j++)
            {
                for (var k = 0; k < expected.GetLength(2); k++)
                {
                    actual[i, j, k].Is(expected[i, j, k]);
                }
            }
        }
    }

    private static void SameValues(int[,,,] actual, int[,,,] expected)
    {
        for (var i = 0; i < expected.GetLength(0); i++)
        {
            for (var j = 0; j < expected.GetLength(1); j++)
            {
                for (var k = 0; k < expected.GetLength(2); k++)
                {
                    for (var l = 0; l < expected.GetLength(3); l++)
                    {
                        actual[i, j, k, l].Is(expected[i, j, k, l]);
                    }
                }
            }
        }
    }
}
