// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using Tinyhand.Formatters;

namespace Tinyhand.Resolvers;

public sealed class NativeGuidResolver : IFormatterResolver
{
    /// <summary>
    /// The singleton instance that can be used.
    /// </summary>
    public static readonly NativeGuidResolver Instance = new NativeGuidResolver();

    private NativeGuidResolver()
    {
    }

    public ITinyhandFormatter<T>? TryGetFormatter<T>()
    {
        return FormatterCache<T>.Formatter;
    }

    public Type[] GetInstantiableTypes()
        => [typeof(Guid), typeof(Guid?)];

    private static object? GetFormatterHelper(Type t)
    {
        if (t == typeof(Guid))
        {
            return NativeGuidFormatter.Instance;
        }
        else if (t == typeof(Guid?))
        {
            return new StaticNullableFormatter<Guid>(NativeGuidFormatter.Instance);
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
