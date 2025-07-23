// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using Tinyhand.Formatters;

namespace Tinyhand.Resolvers;

/// <summary>
/// Template code for resolver.
/// </summary>
public sealed class PrimitiveObjectResolver : IFormatterResolver
{
    /// <summary>
    /// The singleton instance that can be used.
    /// </summary>
    public static readonly PrimitiveObjectResolver Instance;

    /// <summary>
    /// A <see cref="TinyhandSerializerOptions"/> instance with this formatter pre-configured.
    /// </summary>
    public static readonly TinyhandSerializerOptions Options;

    static PrimitiveObjectResolver()
    {
        Instance = new PrimitiveObjectResolver();
        Options = new TinyhandSerializerOptions(Instance);
    }

    private PrimitiveObjectResolver()
    {
    }

    public ITinyhandFormatter<T>? TryGetFormatter<T>()
    {
        return FormatterCache<T>.Formatter;
    }

    public void RegisterInstantiableTypes()
    {
    }

    private static class FormatterCache<T>
    {
        public static readonly ITinyhandFormatter<T>? Formatter;

        static FormatterCache()
        {
            Formatter = (typeof(T) == typeof(object))
                ? (ITinyhandFormatter<T>)(object)PrimitiveObjectFormatter.Instance
                : null;
        }
    }
}
