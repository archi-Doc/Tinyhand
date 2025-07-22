// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Linq;
using Tinyhand.Formatters;

namespace Tinyhand.Resolvers;

/// <summary>
/// Default composited resolver.
/// </summary>
public sealed class StandardResolver : IFormatterResolver
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
    };

    private StandardResolver()
    {
    }

    public ITinyhandFormatter<T>? TryGetFormatter<T>()
    {
        return FormatterCache<T>.Formatter;
    }

    public Type[] GetInstantiableTypes()
        => Resolvers.SelectMany(x => x.GetInstantiableTypes()).ToArray();

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
}
