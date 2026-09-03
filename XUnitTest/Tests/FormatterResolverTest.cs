// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
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
}
