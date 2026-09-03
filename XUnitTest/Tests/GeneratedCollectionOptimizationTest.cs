// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Tinyhand;
using Tinyhand.IO;
using Tinyhand.Resolvers;
using Xunit;

namespace XUnitTest.Tests;

public partial class GeneratedCollectionOptimizationTest
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(256)]
    public void ListsPreserveValuesCountsAndCallbacks(int count)
    {
        var source = new Lists
        {
            Integers = Enumerable.Range(0, count).Select(x => x % 2 == 0 ? x : -x).ToList(),
            Enums = Enumerable.Range(0, count).Select(x => (DayOfWeek)x).ToList(),
            Points = Enumerable.Range(0, count).Select(x => new Point { Number = x }).ToList(),
            NullablePoints = Enumerable.Range(0, count).Select(x => x % 2 == 0 ? (Point?)new Point { Number = x } : null).ToList(),
            Arrays = Enumerable.Range(0, count).Select(x => x % 2 == 0 ? new[] { x } : Array.Empty<int>()).ToList(),
            Strings = Enumerable.Range(0, count).Select(x => x.ToString()).ToList(),
        };
        var clone = TinyhandSerializer.DeserializeObject<Lists>(TinyhandSerializer.SerializeObject(source))!;
        Assert.Equal(source.Integers, clone.Integers);
        Assert.Equal(source.Enums, clone.Enums);
        Assert.Equal(source.Points.Select(x => x.Number), clone.Points.Select(x => x.Number));
        Assert.All(clone.Points, x => Assert.Equal(1, x.CallbackCount));
        Assert.Equal(source.NullablePoints.Select(x => x?.Number), clone.NullablePoints.Select(x => x?.Number));
        Assert.All(clone.NullablePoints.Where(x => x.HasValue), x => Assert.Equal(1, x!.Value.CallbackCount));
        Assert.Equal(source.Arrays, clone.Arrays);
        Assert.Equal(source.Strings, clone.Strings);
        // The returned list must still support ordinary List<T> mutation.
        clone.Integers.Add(1234);
        Assert.Equal(count + 1, clone.Integers.Count);
        Assert.Equal(1234, clone.Integers[count]);
    }

    [Fact]
    public void ListsUseCustomElementFormatter()
    {
        var formatter = new CustomFormatter();
        var options = new TinyhandSerializerOptions(CompositeResolver.Create(formatter));
        var reader = new TinyhandReader(new byte[] { 0x91, 0x93, 10, 20, 30 });
        var value = TinyhandSerializer.DeserializeObject<CustomList>(ref reader, options)!;
        Assert.Equal(new[] { 110, 120, 130 }, value.Items.Select(x => x.Number));
        Assert.Equal(3, formatter.ReadCount);
        Assert.Equal(0, reader.Depth);
        Assert.True(reader.End);
    }

    [Fact]
    public void FailedListReadRestoresDepthAndKeepsExistingMember()
    {
        var formatter = new CustomFormatter { ThrowOn = 20 };
        var options = new TinyhandSerializerOptions(CompositeResolver.Create(formatter));
        var reader = new TinyhandReader(new byte[] { 0x91, 0x93, 10, 20, 30 }) { Depth = 7 };
        CustomList? value = new() { Items = [new Custom { Number = 9 }] };
        var original = value.Items;
        var threw = false;
        try
        {
            TinyhandSerializer.DeserializeObject(ref reader, ref value, options);
        }
        catch (InvalidOperationException)
        {
            threw = true;
        }

        Assert.True(threw);
        Assert.Equal(7, reader.Depth);
        Assert.Same(original, value!.Items);
        Assert.Equal(9, Assert.Single(value.Items).Number);
        Assert.Equal(2, formatter.ReadCount);
    }

    [Fact]
    public void EmptyArraysAreSharedAndNonemptyClonesAreIndependent()
    {
        var empty = TinyhandSerializer.DeserializeObject<Arrays>(new byte[] { 0x94, 0x90, 0x90, 0x90, 0x90 })!;
        CheckEmpty(empty);
        CheckEmpty(TinyhandSerializer.CloneObject(empty)!);

        var source = new Arrays { Integers = [42], Strings = ["value"], Points = [new Point { Number = 3 }], Nested = [[7]] };
        var clone = TinyhandSerializer.CloneObject(source)!;
        Assert.NotSame(source.Integers, clone.Integers);
        Assert.NotSame(source.Strings, clone.Strings);
        Assert.NotSame(source.Points, clone.Points);
        Assert.NotSame(source.Nested, clone.Nested);
        Assert.NotSame(source.Nested[0], clone.Nested[0]);
        clone.Integers[0] = 100;
        clone.Nested[0][0] = 200;
        Assert.Equal(42, source.Integers[0]);
        Assert.Equal(7, source.Nested[0][0]);
    }

    [Fact]
    public void EmptyGeneratedArrayStillEnforcesDepthLimit()
    {
        var options = TinyhandSerializerOptions.Standard with { Security = TinyhandSecurity.UntrustedData.WithMaximumObjectGraphDepth(1) };
        var reader = new TinyhandReader(new byte[] { 0x91, 0x90 });
        var threw = false;
        try
        {
            TinyhandSerializer.DeserializeObject<PointArray>(ref reader, options);
        }
        catch (InsufficientExecutionStackException)
        {
            threw = true;
        }

        Assert.True(threw);
        Assert.Equal(0, reader.Depth);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void StringConvertibleListPreservesNullsAndValues(bool convertToString)
    {
        var options = convertToString ? TinyhandSerializerOptions.ConvertToString : TinyhandSerializerOptions.Standard;
        var item = new Tinyhand.Tests.StringConvertibleTestClass { Byte16 = Enumerable.Range(0, 16).Select(x => (byte)x).ToArray() };
        var source = new ConvertibleList { Items = [item, null, item] };
        var writer = TinyhandWriter.CreateFromBytePool();
        try
        {
            TinyhandSerializer.SerializeObject(ref writer, source, options);
            var reader = new TinyhandReader(writer.FlushAndGetArray());
            var result = TinyhandSerializer.DeserializeObject<ConvertibleList>(ref reader, options)!;
            Assert.Equal(3, result.Items.Count);
            Assert.Equal(item.Byte16, result.Items[0]!.Byte16);
            Assert.Null(result.Items[1]);
            Assert.Equal(item.Byte16, result.Items[2]!.Byte16);
            Assert.NotSame(result.Items[0], result.Items[2]);
        }
        finally
        {
            writer.Dispose();
        }
    }

    private static void CheckEmpty(Arrays value)
    {
        Assert.Same(Array.Empty<int>(), value.Integers);
        Assert.Same(Array.Empty<string>(), value.Strings);
        Assert.Same(Array.Empty<Point>(), value.Points);
        Assert.Same(Array.Empty<int[]>(), value.Nested);
    }

    [TinyhandObject(SkipDefaultValues = false)]
    public partial class Lists
    {
        [Key(0)]
        public List<int> Integers { get; set; } = [];
        [Key(1)]
        public List<DayOfWeek> Enums { get; set; } = [];
        [Key(2)]
        public List<Point> Points { get; set; } = [];
        [Key(3)]
        public List<Point?> NullablePoints { get; set; } = [];
        [Key(4)]
        public List<int[]> Arrays { get; set; } = [];
        [Key(5)]
        public List<string> Strings { get; set; } = [];
    }

    [TinyhandObject]
    public partial struct Point
    {
        [Key(0)]
        public int Number;
        [IgnoreMember]
        public int CallbackCount;

        [TinyhandOnDeserialized]
        private void OnDeserialized() => this.CallbackCount++;
    }

    [TinyhandObject]
    public partial class CustomList
    {
        [Key(0)]
        public List<Custom> Items { get; set; } = [];
    }

    [TinyhandObject(UseResolver = true)]
    public partial class Custom
    {
        [Key(0)]
        public int Number;
    }

    [TinyhandObject]
    public partial class ConvertibleList
    {
        [Key(0)]
        public List<Tinyhand.Tests.StringConvertibleTestClass?> Items { get; set; } = [];
    }

    [TinyhandObject]
    public partial class Arrays
    {
        [Key(0)]
        public int[] Integers { get; set; } = [];
        [Key(1)]
        public string[] Strings { get; set; } = [];
        [Key(2)]
        public Point[] Points { get; set; } = [];
        [Key(3)]
        public int[][] Nested { get; set; } = [];
    }

    [TinyhandObject]
    public partial class PointArray
    {
        [Key(0)]
        public Point[] Points { get; set; } = [];
    }

    private sealed class CustomFormatter : ITinyhandFormatter<Custom>
    {
        public int ReadCount { get; private set; }

        public int? ThrowOn { get; init; }

        public void Serialize(ref TinyhandWriter writer, Custom? value, TinyhandSerializerOptions options) => writer.Write(value!.Number);

        public void Deserialize(ref TinyhandReader reader, ref Custom? value, TinyhandSerializerOptions options)
        {
            this.ReadCount++;
            var number = reader.ReadInt32();
            if (number == this.ThrowOn)
            {
                throw new InvalidOperationException("Element could not be decoded.");
            }

            value = new Custom { Number = number + 100 };
        }

        public Custom Reconstruct(TinyhandSerializerOptions options) => new();

        public Custom? Clone(Custom? value, TinyhandSerializerOptions options) => value is null ? null : new() { Number = value.Number };
    }
}
