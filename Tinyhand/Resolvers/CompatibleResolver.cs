// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Tinyhand.Formatters;

namespace Tinyhand.Resolvers;

/// <summary>
/// Compatible composited resolver.
/// </summary>
internal sealed class CompatibleResolver : IFormatterResolver
{
    /// <summary>
    /// The singleton instance that can be used.
    /// </summary>
    public static readonly CompatibleResolver Instance = new();

    private static readonly IFormatterResolver[] Resolvers = new IFormatterResolver[]
    {
        BuiltinResolver.Instance,
        CompositeResolver.Create(ExpandoObjectFormatter.Instance),
        GenericsResolver.Instance,
        GeneratedResolver.Instance,
        PrimitiveObjectResolver.Instance,
    };

    private CompatibleResolver()
    {
    }

    public ITinyhandFormatter<T>? TryGetFormatter<T>()
    {
        return FormatterCache<T>.Formatter;
    }

    public void RegisterInstantiableTypes()
    {
        foreach (var resolver in Resolvers)
        {
            resolver.RegisterInstantiableTypes();
        }
    }

    private static class FormatterCache<T>
    {
        public static readonly ITinyhandFormatter<T>? Formatter;

        static FormatterCache()
        {
            foreach (var resolver in Resolvers)
            {
                var formatter = resolver.TryGetFormatter<T>();
                if (formatter != null)
                {
                    Formatter = formatter;
                    return;
                }
            }
        }
    }
}
