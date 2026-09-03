// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using Tinyhand.IO;
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

    [Fact]
    public void ObjectDoesNotDispatchToRuntimeTypeFormatter()
    {
        var options = TinyhandSerializerOptions.Standard;
        var value = new Uri("https://example.com/");

        var bytes = TinyhandSerializer.Serialize(value, options);
        Assert.Equal(value, TinyhandSerializer.Deserialize<Uri>(bytes, options));
        Assert.Throws<TinyhandException>(() => TinyhandSerializer.Serialize<object>(value, options));
    }

    [Fact]
    public void StandardResolverDoesNotSupportExpandoObject()
    {
        var options = TinyhandSerializerOptions.Standard;

        Assert.Null(options.Resolver.TryGetFormatter<System.Dynamic.ExpandoObject>());
        Assert.Throws<TinyhandException>(() => TinyhandSerializer.Serialize(new System.Dynamic.ExpandoObject(), options));
    }

    [Fact]
    public void StandardResolverDoesNotSupportNonGenericCollections()
    {
        var options = TinyhandSerializerOptions.Standard;

        Assert.Null(options.Resolver.TryGetFormatter<ArrayList>());
        Assert.Null(options.Resolver.TryGetFormatter<Hashtable>());
        Assert.Throws<TinyhandException>(() => TinyhandSerializer.Serialize(new ArrayList { 1, "value" }, options));
        Assert.Throws<TinyhandException>(() => TinyhandSerializer.Serialize(new Hashtable { ["key"] = 1 }, options));

        var list = new object[] { 1, "value" };
        var listBytes = TinyhandSerializer.Serialize(list, options);
        AssertUnsupportedType<IEnumerable>(list, listBytes, options);
        AssertUnsupportedType<ICollection>(list, listBytes, options);
        AssertUnsupportedType<IList>(list, listBytes, options);

        var dictionary = new Dictionary<string, int> { ["key"] = 1 };
        var dictionaryBytes = TinyhandSerializer.Serialize(dictionary, options);
        AssertUnsupportedType<IDictionary>(dictionary, dictionaryBytes, options);
    }

    [Fact]
    public void StandardResolverDoesNotSupportSystemType()
    {
        var options = TinyhandSerializerOptions.Standard;

        AssertUnsupportedType<Type>(typeof(string), TinyhandSerializer.Serialize("System.String", options), options);
        AssertUnsupportedType<Type?>(null, TinyhandSerializer.Serialize<string?>(null, options), options);
    }

    [Fact]
    public void StandardResolverPreservesGuidAndDecimalBinaryFormat()
    {
        AssertNativeRoundtrip(Guid.Parse("00112233-4455-6677-8899-aabbccddeeff"));
        AssertNativeRoundtrip(new decimal(1341, 53156, 61, true, 3));
    }

    [Fact]
    public void OptionsRejectNullResolverWhenCopied()
    {
        Assert.Throws<ArgumentNullException>(() => TinyhandSerializerOptions.Standard with { Resolver = null! });
    }

    private static void AssertNativeRoundtrip<T>(T value)
        where T : struct
    {
        var options = TinyhandSerializerOptions.Standard;
        var bytes = TinyhandSerializer.Serialize(value, options);
        Assert.Equal(18, bytes.Length);
        Assert.Equal(MessagePackCode.Bin8, bytes[0]);
        Assert.Equal(16, bytes[1]);
        Assert.Equal(value, TinyhandSerializer.Deserialize<T>(bytes, options));
        Assert.Equal(bytes, TinyhandSerializer.Serialize<T?>(value, options));
        Assert.Equal((T?)value, TinyhandSerializer.Deserialize<T?>(bytes, options));
        Assert.Null(TinyhandSerializer.Deserialize<T?>(TinyhandSerializer.Serialize<T?>(null, options), options));
        Assert.True(TinyhandTypeIdentifier.IsRegistered<T>());
        Assert.True(TinyhandTypeIdentifier.IsRegistered<T?>());
    }

    private static void AssertUnsupportedType<T>(T value, byte[] bytes, TinyhandSerializerOptions options)
    {
        Assert.Null(options.Resolver.TryGetFormatter<T>());
        Assert.Throws<TinyhandException>(() => TinyhandSerializer.Serialize(value, options));
        Assert.Throws<TinyhandException>(() => TinyhandSerializer.Deserialize<T>(bytes, options));
        Assert.Throws<FormatterNotRegisteredException>(() => TinyhandSerializer.Clone(value, options));
        Assert.Throws<FormatterNotRegisteredException>(() => TinyhandSerializer.Reconstruct<T>(options));
    }
}
