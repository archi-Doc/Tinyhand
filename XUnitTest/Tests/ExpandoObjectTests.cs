// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Dynamic;
using Tinyhand.IO;
using Tinyhand.Resolvers;
using Xunit;

namespace Tinyhand.Tests;

public class ExpandoObjectTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Roundtrip(bool compatible)
    {
        var options = compatible ? TinyhandSerializerOptions.Compatible : TinyhandSerializerOptions.Standard;
        var expando = new ExpandoObject();
        var properties = (IDictionary<string, object?>)expando;
        properties.Add("Name", "George");
        properties.Add("Age", 18);
        properties.Add("Other", null);

        var bin = TinyhandSerializer.Serialize(expando, options);
        var result = TinyhandSerializer.Deserialize<ExpandoObject>(bin, options)!;
        var resultProperties = (IDictionary<string, object?>)result;
        Assert.Equal("George", resultProperties["Name"]);
        Assert.Equal(18, resultProperties["Age"]);
        Assert.Null(resultProperties["Other"]);
    }

    [Fact]
    public void CloneCopiesValuesWithoutModifyingSource()
    {
        var expando = new ExpandoObject();
        var properties = (IDictionary<string, object?>)expando;
        var bytes = new byte[] { 1, 2, 3 };
        properties.Add("Name", "George");
        properties.Add("Bytes", bytes);

        var result = TinyhandSerializer.Clone(expando)!;
        var resultProperties = (IDictionary<string, object?>)result;
        Assert.NotSame(expando, result);
        Assert.Equal(2, properties.Count);
        Assert.Equal(2, resultProperties.Count);
        Assert.Equal("George", resultProperties["Name"]);
        var clonedBytes = Assert.IsType<byte[]>(resultProperties["Bytes"]);
        Assert.Equal(bytes, clonedBytes);
        Assert.NotSame(bytes, clonedBytes);
        clonedBytes[0] = 99;
        Assert.Equal((byte)1, bytes[0]);
    }

    [Fact]
    public void DeserializeNilClearsExistingValue()
    {
        ExpandoObject? value = new ExpandoObject();
        var options = ExpandoObjectResolver.Options;
        var bytes = TinyhandSerializer.Serialize<ExpandoObject>(null, options);
        var reader = new TinyhandReader(bytes);

        options.Resolver.GetFormatter<ExpandoObject>().Deserialize(ref reader, ref value, options);

        Assert.Null(value);
        Assert.True(reader.End);
    }
}
