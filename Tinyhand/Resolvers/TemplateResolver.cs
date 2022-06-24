// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Generic;
using Tinyhand;

#pragma warning disable CS0649
#pragma warning disable SA1401 // Fields should be private

namespace Tinyhand.Resolvers;

/// <summary>
/// Template code for resolver.
/// </summary>
public sealed class TemplateResolver : IFormatterResolver
{
    /// <summary>
    /// The singleton instance that can be used.
    /// </summary>
    public static readonly TemplateResolver Instance = new ();

    private TemplateResolver()
    {
    }

    public ITinyhandFormatter<T>? TryGetFormatter<T>()
    {
        return FormatterCache<T>.Formatter;
    }

    private static class FormatterCache<T>
    {
        public static readonly ITinyhandFormatter<T>? Formatter;

        static FormatterCache()
        {
        }
    }
}
