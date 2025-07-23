// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

#pragma warning disable CS0649

using System;

namespace Tinyhand.Resolvers;

/// <summary>
/// Template code for resolver.
/// </summary>
public sealed class TemplateResolver : IFormatterResolver
{
    /// <summary>
    /// The singleton instance that can be used.
    /// </summary>
    public static readonly TemplateResolver Instance = new();

    private TemplateResolver()
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
        }
    }
}
