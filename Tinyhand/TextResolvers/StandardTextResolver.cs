// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Tinyhand.TextResolvers
{
    /// <summary>
    /// Default composited resolver.
    /// </summary>
    public sealed class StandardTextResolver : ITextFormatterResolver
    {
        /// <summary>
        /// The singleton instance that can be used.
        /// </summary>
        public static readonly StandardTextResolver Instance = new StandardTextResolver();

        private static readonly ITextFormatterResolver[] Resolvers = new ITextFormatterResolver[]
        {
            GeneratedTextResolver.Instance,
        };

        private StandardTextResolver()
        {
        }

        public ITinyhandTextFormatter<T>? TryGetFormatter<T>()
        {
            return FormatterCache<T>.Formatter;
        }

        private static class FormatterCache<T>
        {
            public static readonly ITinyhandTextFormatter<T>? Formatter;

            static FormatterCache()
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
