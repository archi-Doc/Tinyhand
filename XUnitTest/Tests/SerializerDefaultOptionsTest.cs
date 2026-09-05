// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Tinyhand;
using Xunit;

namespace XUnitTest;

[CollectionDefinition("SerializerDefaults", DisableParallelization = true)]
public class SerializerDefaultsCollection;

[Collection("SerializerDefaults")]
public class SerializerDefaultOptionsTest
{
    [Fact]
    public void ParameterlessDeserializationHonorsDefaultCompression()
    {
        var original = TinyhandSerializer.DefaultOptions;
        try
        {
            TinyhandSerializer.DefaultOptions = TinyhandSerializerOptions.Lz4;
            var value = new string('x', 1024);
            var bytes = TinyhandSerializer.Serialize(value);
            Assert.Equal(value, TinyhandSerializer.Deserialize<string>(bytes));
            Assert.Equal(value, TinyhandSerializer.Deserialize<string>(bytes, options: null));
            Assert.True(TinyhandSerializer.TryDeserialize<string>(bytes, out var copy));
            Assert.Equal(value, copy);
            Assert.Equal(123, TinyhandSerializer.Deserialize<int>(TinyhandSerializer.Serialize(123)));
        }
        finally
        {
            TinyhandSerializer.DefaultOptions = original;
        }
    }
}
