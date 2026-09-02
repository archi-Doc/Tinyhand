// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Tinyhand.Formatters;

#pragma warning disable SA1401 // Fields should be private

namespace Tinyhand.Resolvers;

/// <summary>
/// Source Generated resolver.
/// </summary>
public sealed class GeneratedResolver : IFormatterResolver
{
    /// <summary>
    /// The singleton instance that can be used.
    /// </summary>
    public static readonly GeneratedResolver Instance = new();

    private ThreadsafeTypeKeyHashtable<FormatterGeneratorInfo> formatterGenerator = new();

    internal class FormatterGeneratorInfo
    {
        public Type GenericType { get; }

        public Func<Type, Type[], ITinyhandFormatter> Generator { get; set; }

        // Type[] has reference equality by default, and GetGenericArguments() returns a fresh array
        // on every call, so a structural comparer is required for the cache to ever hit.
        private readonly Dictionary<Type[], ITinyhandFormatter> formatterCache = new(TypeArrayComparer.Instance);
        private readonly Lock lockObject = new();

        public FormatterGeneratorInfo(Type genericType, Func<Type, Type[], ITinyhandFormatter> generator)
        {
            this.GenericType = genericType;
            this.Generator = generator;
        }

        public ITinyhandFormatter GetOrCreate(Type type, Type[] genericArguments)
        {
            using (this.lockObject.EnterScope())
            {
                if (!this.formatterCache.TryGetValue(genericArguments, out var formatter))
                {
                    formatter = this.Generator(type, genericArguments);
                    this.formatterCache[genericArguments] = formatter;
                }

                return formatter;
            }
        }
    }

    private sealed class TypeArrayComparer : IEqualityComparer<Type[]>
    {
        public static readonly TypeArrayComparer Instance = new();

        public bool Equals(Type[]? x, Type[]? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x is null || y is null || x.Length != y.Length)
            {
                return false;
            }

            for (var i = 0; i < x.Length; i++)
            {
                if (x[i] != y[i])
                {
                    return false;
                }
            }

            return true;
        }

        public int GetHashCode(Type[] obj)
        {
            var hash = default(HashCode);
            foreach (var x in obj)
            {
                hash.Add(x);
            }

            return hash.ToHashCode();
        }
    }

    private GeneratedResolver()
    {
    }

    public ITinyhandFormatter<T>? TryGetFormatter<T>()
    {
        var formatter = FormatterCache<T>.Formatter;
        if (formatter != null)
        {
            return formatter;
        }

        var targetType = typeof(T);
        if (!targetType.IsGenericType)
        {
            if (this.formatterGenerator.TryGetValue(targetType, out var info))
            {
                return (ITinyhandFormatter<T>)info.GetOrCreate(targetType, Array.Empty<Type>());
            }

            return null;
        }

        try
        {
            var genericType = targetType.GetGenericTypeDefinition();
            if (this.formatterGenerator.TryGetValue(genericType, out var info))
            {
                return (ITinyhandFormatter<T>)info.GetOrCreate(genericType, targetType.GetGenericArguments());
            }
        }
        catch
        {
        }

        return null;
    }

    public void RegisterInstantiableTypes()
    {
    }

    public void SetFormatterGenerator(Type genericType, Func<Type, Type[], ITinyhandFormatter> generator)
    {
        var info = new FormatterGeneratorInfo(genericType, generator);
        this.formatterGenerator.TryAdd(genericType, info);
    }

    public void SetFormatter<T>(ITinyhandFormatter<T> formatter)
    {
        TinyhandTypeIdentifier.Register(typeof(T));
        TinyhandTypeIdentifier.Register(typeof(T?));
        FormatterCache<T>.Formatter = formatter;
    }

    private static class FormatterCache<T>
    {
        public static ITinyhandFormatter<T>? Formatter;

        static FormatterCache()
        {
        }
    }
}
