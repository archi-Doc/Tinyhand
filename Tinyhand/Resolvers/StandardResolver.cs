// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Tinyhand;

namespace Tinyhand.Resolvers
{
    /// <summary>
    /// Default composited resolver.
    /// </summary>
    public sealed class StandardResolver : IFormatterResolver
    {
        /// <summary>
        /// The singleton instance that can be used.
        /// </summary>
        public static readonly StandardResolver Instance = new StandardResolver();

        private static readonly IFormatterResolver[] Resolvers = new IFormatterResolver[]
        {
            BuiltinResolver.Instance,
            GenericsResolver.Instance,
            GeneratedResolver.Instance,
        };

        private StandardResolver()
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
                if (typeof(T) == typeof(object))
                {
                }
                else
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
}
