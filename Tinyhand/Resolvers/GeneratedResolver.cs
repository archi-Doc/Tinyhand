﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Generic;
using Tinyhand;

#pragma warning disable SA1401 // Fields should be private

namespace Tinyhand.Resolvers
{
    /// <summary>
    /// Source Generated resolver.
    /// </summary>
    public sealed class GeneratedResolver : IFormatterResolver
    {
        /// <summary>
        /// The singleton instance that can be used.
        /// </summary>
        public static readonly GeneratedResolver Instance = new();

        private GeneratedResolver()
        {
        }

        public ITinyhandFormatter<T>? TryGetFormatter<T>()
        {
            return FormatterCache<T>.Formatter;
        }

        public void SetFormatter<T>(ITinyhandFormatter<T> formatter)
        {
            FormatterCache<T>.Formatter = formatter;
        }

        public ITinyhandFormatterExtra<T>? TryGetFormatterExtra<T>()
        {
            return FormatterCacheExtra<T>.Formatter;
        }

        public void SetFormatterExtra<T>(ITinyhandFormatterExtra<T> formatter)
        {
            FormatterCacheExtra<T>.Formatter = formatter;
        }

        private static class FormatterCache<T>
        {
            public static ITinyhandFormatter<T>? Formatter;

            static FormatterCache()
            {
            }
        }

        private static class FormatterCacheExtra<T>
        {
            public static ITinyhandFormatterExtra<T>? Formatter;

            static FormatterCacheExtra()
            {
            }
        }
    }
}
