// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Tinyhand.Formatters;

namespace Tinyhand.Resolvers;

/// <summary>
/// Default composited resolver.
/// </summary>
internal sealed class StandardResolver : IFormatterResolver
{
    /// <summary>
    /// The singleton instance that can be used.
    /// </summary>
    public static readonly StandardResolver Instance = new();

    private static readonly IFormatterResolver[] Resolvers = new IFormatterResolver[]
    {// NativeResolver + CompatibleResolver
        NativeGuidResolver.Instance,
        NativeDecimalResolver.Instance,
        BuiltinResolver.Instance,
        CompositeResolver.Create(ExpandoObjectFormatter.Instance),
        GenericsResolver.Instance,
        GeneratedResolver.Instance,
        PrimitiveObjectResolver.Instance,
    };

    private StandardResolver()
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
