// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using Tinyhand.Formatters;

namespace Tinyhand.Resolvers;

public sealed class NativeDecimalResolver : IFormatterResolver
{
    /// <summary>
    /// The singleton instance that can be used.
    /// </summary>
    public static readonly NativeDecimalResolver Instance = new NativeDecimalResolver();

    private NativeDecimalResolver()
    {
    }

    public ITinyhandFormatter<T>? TryGetFormatter<T>()
    {
        return FormatterCache<T>.Formatter;
    }

    public Type[] GetInstantiableTypes()
        => [typeof(decimal), typeof(decimal?)];

    private static object? GetFormatterHelper(Type t)
    {
        if (t == typeof(decimal))
        {
            return NativeDecimalFormatter.Instance;
        }
        else if (t == typeof(decimal?))
        {
            return new StaticNullableFormatter<decimal>(NativeDecimalFormatter.Instance);
        }

        return null;
    }

    private static class FormatterCache<T>
    {
        public static readonly ITinyhandFormatter<T>? Formatter;

        static FormatterCache()
        {
            Formatter = (ITinyhandFormatter<T>?)GetFormatterHelper(typeof(T));
        }
    }
}
