// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using Tinyhand.Resolvers;
using Xunit;

namespace Tinyhand.Tests;

public class FormatterResolverTest
{
    [Fact]
    public void CompositeResolverTriesRemainingResolversAndCachesMisses()
    {
        var resolver = CompositeResolver.Create(PrimitiveObjectResolver.Instance, BuiltinResolver.Instance);

        Assert.Same(BuiltinResolver.Instance.GetFormatter<string>(), resolver.GetFormatter<string>());
        Assert.Same(PrimitiveObjectResolver.Instance.GetFormatter<object>(), resolver.GetFormatter<object>());
        Assert.Null(resolver.TryGetFormatter<Action>());
        Assert.Null(resolver.TryGetFormatter<Action>());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ObjectDoesNotDispatchToRuntimeTypeFormatter(bool compatible)
    {
        var options = compatible ? TinyhandSerializerOptions.Compatible : TinyhandSerializerOptions.Standard;
        var value = new Uri("https://example.com/");

        var bytes = TinyhandSerializer.Serialize(value, options);
        Assert.Equal(value, TinyhandSerializer.Deserialize<Uri>(bytes, options));
        Assert.Throws<TinyhandException>(() => TinyhandSerializer.Serialize<object>(value, options));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DefaultResolversDoNotSupportExpandoObject(bool compatible)
    {
        var options = compatible ? TinyhandSerializerOptions.Compatible : TinyhandSerializerOptions.Standard;

        Assert.Null(options.Resolver.TryGetFormatter<System.Dynamic.ExpandoObject>());
        Assert.Throws<TinyhandException>(() => TinyhandSerializer.Serialize(new System.Dynamic.ExpandoObject(), options));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DefaultResolversDoNotSupportNonGenericCollections(bool compatible)
    {
        var options = compatible ? TinyhandSerializerOptions.Compatible : TinyhandSerializerOptions.Standard;

        Assert.Null(options.Resolver.TryGetFormatter<ArrayList>());
        Assert.Null(options.Resolver.TryGetFormatter<Hashtable>());
        Assert.Throws<TinyhandException>(() => TinyhandSerializer.Serialize(new ArrayList { 1, "value" }, options));
        Assert.Throws<TinyhandException>(() => TinyhandSerializer.Serialize(new Hashtable { ["key"] = 1 }, options));

        var list = new object[] { 1, "value" };
        var listBytes = TinyhandSerializer.Serialize(list, options);
        AssertUnsupportedCollection<IEnumerable>(list, listBytes, options);
        AssertUnsupportedCollection<ICollection>(list, listBytes, options);
        AssertUnsupportedCollection<IList>(list, listBytes, options);

        var dictionary = new Dictionary<string, int> { ["key"] = 1 };
        var dictionaryBytes = TinyhandSerializer.Serialize(dictionary, options);
        AssertUnsupportedCollection<IDictionary>(dictionary, dictionaryBytes, options);
    }

    private static void AssertUnsupportedCollection<T>(T value, byte[] bytes, TinyhandSerializerOptions options)
    {
        Assert.Null(options.Resolver.TryGetFormatter<T>());
        Assert.Throws<TinyhandException>(() => TinyhandSerializer.Serialize(value, options));
        Assert.Throws<TinyhandException>(() => TinyhandSerializer.Deserialize<T>(bytes, options));
        Assert.Throws<FormatterNotRegisteredException>(() => TinyhandSerializer.Clone(value, options));
        Assert.Throws<FormatterNotRegisteredException>(() => TinyhandSerializer.Reconstruct<T>(options));
    }
}
