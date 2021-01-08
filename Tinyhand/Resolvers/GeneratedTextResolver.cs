// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

#pragma warning disable SA1401 // Fields should be private

namespace Tinyhand.TextResolvers
{
    /// <summary>
    /// Source Generated resolver.
    /// </summary>
    public sealed class GeneratedTextResolver : ITextFormatterResolver
    {
        /// <summary>
        /// The singleton instance that can be used.
        /// </summary>
        public static readonly GeneratedTextResolver Instance = new();

        private GeneratedTextResolver()
        {
        }

        public ITinyhandTextFormatter<T>? TryGetFormatter<T>()
        {
            return FormatterCache<T>.Formatter;
        }

        public void SetFormatter<T>(ITinyhandTextFormatter<T> formatter)
        {
            FormatterCache<T>.Formatter = formatter;
        }

        private static class FormatterCache<T>
        {
            public static ITinyhandTextFormatter<T>? Formatter;

            static FormatterCache()
            {
            }
        }
    }
}
