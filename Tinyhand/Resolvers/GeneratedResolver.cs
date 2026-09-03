// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Threading;

#pragma warning disable SA1401 // The containing resolver accesses the generic cache.

namespace Tinyhand.Resolvers;

/// <summary>Stores statically generated formatters in a cache for each closed type.</summary>
public sealed partial class GeneratedResolver : IFormatterResolver
{
    public static readonly GeneratedResolver Instance = new();

    private GeneratedResolver()
    {
    }

    public ITinyhandFormatter<T>? TryGetFormatter<T>() => Volatile.Read(ref FormatterCache<T>.Formatter);

    public void RegisterInstantiableTypes()
    {
    }

    /// <summary>
    /// Registers or replaces a formatter in the generated-type cache and registers its type identifier.
    /// </summary>
    /// <typeparam name="T">The type handled by the formatter.</typeparam>
    /// <param name="formatter">The formatter to register.</param>
    /// <remarks>The standard resolver checks built-in formatters before this cache.</remarks>
    /// <exception cref="ArgumentNullException">The formatter is null.</exception>
    public void SetFormatter<T>(ITinyhandFormatter<T> formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);
        Volatile.Write(ref FormatterCache<T>.Formatter, formatter);
        TinyhandTypeIdentifier.Register<T>();
    }

    private static void Register<T, TFormatter>()
        where TFormatter : ITinyhandFormatter<T>, new()
    {
        if (BuiltinResolver.Instance.TryGetFormatter<T>() is null && FormatterCache<T>.Formatter is null)
        {
            Interlocked.CompareExchange(ref FormatterCache<T>.Formatter, new TFormatter(), null);
            TinyhandTypeIdentifier.Register<T>();
        }
    }

    private static class FormatterCache<T>
    {
        internal static ITinyhandFormatter<T>? Formatter;
    }
}
