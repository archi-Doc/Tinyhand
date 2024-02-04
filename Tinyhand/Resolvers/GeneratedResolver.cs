// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;

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

        public Dictionary<Type[], ITinyhandFormatter> FormatterCache { get; } = new();

        public FormatterGeneratorInfo(Type genericType, Func<Type, Type[], ITinyhandFormatter> generator)
        {
            this.GenericType = genericType;
            this.Generator = generator;
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
                var key = Array.Empty<Type>();
                if (!info.FormatterCache.TryGetValue(key, out var f))
                {
                    f = info.Generator(targetType, key);
                    info.FormatterCache[key] = f;
                }

                return (ITinyhandFormatter<T>)f;
            }
        }

        try
        {
            var genericType = targetType.GetGenericTypeDefinition();
            if (this.formatterGenerator.TryGetValue(genericType, out var info))
            {
                var genericArguments = targetType.GetGenericArguments();
                if (!info.FormatterCache.TryGetValue(genericArguments, out var f))
                {
                    f = info.Generator(genericType, genericArguments);
                    info.FormatterCache[genericArguments] = f;
                }

                return (ITinyhandFormatter<T>)f;
            }
        }
        catch
        {
        }

        return null;
    }

    public void SetFormatterGenerator(Type genericType, Func<Type, Type[], ITinyhandFormatter> generator)
    {
        var info = new FormatterGeneratorInfo(genericType, generator);
        this.formatterGenerator.TryAdd(genericType, info);
    }

    public void SetFormatter<T>(ITinyhandFormatter<T> formatter)
    {
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
