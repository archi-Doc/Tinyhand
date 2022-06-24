// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Tinyhand.Formatters;

namespace Tinyhand.Resolvers;

/// <summary>
/// Compatible composited resolver.
/// </summary>
public sealed class CompatibleResolver : IFormatterResolver
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
    };

    private CompatibleResolver()
    {
    }

    public ITinyhandFormatter<T>? TryGetFormatter<T>()
    {
        return FormatterCache<T>.Formatter;
    }

    public ITinyhandFormatterExtra<T>? TryGetFormatterExtra<T>()
    {
        return FormatterExtraCache<T>.Formatter;
    }

    private static class FormatterCache<T>
    {
        public static readonly ITinyhandFormatter<T>? Formatter;

        static FormatterCache()
        {
            if (typeof(T) == typeof(object))
            {
                // final fallback
                Formatter = (ITinyhandFormatter<T>)Tinyhand.Formatters.DynamicObjectTypeFallbackFormatter.Instance;
            }
            else
            {
                foreach (var x in Resolvers)
                {
                    var f = x.TryGetFormatter<T>();
                    if (f != null)
                    {
                        Formatter = f;
                        return;
                    }
                }
            }
        }
    }

    private static class FormatterExtraCache<T>
    {
        public static readonly ITinyhandFormatterExtra<T>? Formatter;

        static FormatterExtraCache()
        {
            foreach (var x in Resolvers)
            {
                var f = x.TryGetFormatterExtra<T>();
                if (f != null)
                {
                    Formatter = f;
                    return;
                }
            }
        }
    }
}
