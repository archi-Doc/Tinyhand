// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Tinyhand.Resolvers;

/// <summary>
/// Resolves built-in, generated, and primitive-object formatters in that order.
/// </summary>
internal sealed class StandardResolver : IFormatterResolver
{
    /// <summary>
    /// The singleton instance that can be used.
    /// </summary>
    public static readonly StandardResolver Instance = new();

    private StandardResolver()
    {
    }

    public ITinyhandFormatter<T>? TryGetFormatter<T>()
    {
        return BuiltinResolver.Instance.TryGetFormatter<T>()
            ?? GeneratedResolver.Instance.TryGetFormatter<T>()
            ?? PrimitiveObjectResolver.Instance.TryGetFormatter<T>();
    }

    public void RegisterInstantiableTypes() => BuiltinResolver.Instance.RegisterInstantiableTypes();
}
