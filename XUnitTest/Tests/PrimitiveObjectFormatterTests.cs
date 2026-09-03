// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Tinyhand.IO;
using Tinyhand.Resolvers;
using Xunit;

namespace Tinyhand.Tests;

public class PrimitiveObjectFormatterTests
{
    [Theory]
    [InlineData((sbyte)5)]
    [InlineData((byte)5)]
    [InlineData((short)5)]
    [InlineData((ushort)5)]
    [InlineData(5)]
    [InlineData(5U)]
    [InlineData(5L)]
    [InlineData(5UL)]
    public void CompressibleIntegersRetainTypeInfo<T>(T value)
    {
        var bin = TinyhandSerializer.Serialize<object?>(value, PrimitiveObjectResolver.Options);
        T result = Assert.IsType<T>(TinyhandSerializer.Deserialize<object?>(bin, PrimitiveObjectResolver.Options));
        Assert.Equal(value, result);
    }

    [Fact]
    public void IL2CPPHint()
    {
        CompressibleIntegersRetainTypeInfo<sbyte>(default);
        CompressibleIntegersRetainTypeInfo<byte>(default);
        CompressibleIntegersRetainTypeInfo<short>(default);
        CompressibleIntegersRetainTypeInfo<ushort>(default);
        CompressibleIntegersRetainTypeInfo<int>(default);
        CompressibleIntegersRetainTypeInfo<uint>(default);
        CompressibleIntegersRetainTypeInfo<long>(default);
        CompressibleIntegersRetainTypeInfo<ulong>(default);
    }

    [Fact]
    public void EnumRetainsUnderlyingType()
    {
        var bin = TinyhandSerializer.Serialize<object?>((object?)SomeEnum.SomeValue, PrimitiveObjectResolver.Options);
        var result = (SomeEnum)TinyhandSerializer.Deserialize<object?>(bin, PrimitiveObjectResolver.Options)!;
        Assert.Equal(SomeEnum.SomeValue, result);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DefaultResolversRoundtripPrimitiveCollections(bool compatible)
    {
        var options = compatible ? TinyhandSerializerOptions.Compatible : TinyhandSerializerOptions.Standard;
        object value = new object?[]
        {
            1,
            "test",
            null,
            new[] { 999, 424 },
            new Dictionary<string, int> { { "key", 100 } },
        };

        var bytes = TinyhandSerializer.Serialize(value, options);
        var result = Assert.IsType<object[]>(TinyhandSerializer.Deserialize<object>(bytes, options));

        Assert.Equal(1, Assert.IsType<int>(result[0]));
        Assert.Equal("test", result[1]);
        Assert.Null(result[2]);
        var array = Assert.IsType<object[]>(result[3]);
        Assert.Equal(999, Assert.IsType<int>(array[0]));
        Assert.Equal(424, Assert.IsType<int>(array[1]));
        var dictionary = Assert.IsType<Dictionary<object, object>>(result[4]);
        Assert.Equal(100, Assert.IsType<int>(dictionary["key"]));
    }

    [Fact]
    public void DeserializeNilClearsExistingValue()
    {
        var options = PrimitiveObjectResolver.Options;
        var bytes = TinyhandSerializer.Serialize<object?>(null, options);
        var reader = new TinyhandReader(bytes);
        object? value = "previous value";

        options.Resolver.GetFormatter<object>().Deserialize(ref reader, ref value, options);

        Assert.Null(value);
        Assert.True(reader.End);
    }

    public enum SomeEnum : ushort
    {
        None = 0,
        SomeValue = 1,
    }
}
