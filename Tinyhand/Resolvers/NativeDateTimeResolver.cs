// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using Tinyhand.Formatters;
using Tinyhand.Internal;

#pragma warning disable SA1403 // File may only contain a single namespace

/*namespace Tinyhand.Resolvers
{
    public sealed class NativeDateTimeResolver : IFormatterResolver
    {
        /// <summary>
        /// The singleton instance that can be used.
        /// </summary>
        public static readonly NativeDateTimeResolver Instance;

        /// <summary>
        /// A <see cref="TinyhandSerializerOptions"/> instance with this formatter pre-configured.
        /// </summary>
        public static readonly TinyhandSerializerOptions Options;

        static NativeDateTimeResolver()
        {
            Instance = new NativeDateTimeResolver();
            Options = new TinyhandSerializerOptions(Instance);
        }

        private NativeDateTimeResolver()
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
                Formatter = (ITinyhandFormatter<T>?)NativeDateTimeResolverGetFormatterHelper.GetFormatter(typeof(T));
            }
        }
    }
}

namespace Tinyhand.Internal
{
    internal static class NativeDateTimeResolverGetFormatterHelper
    {
        internal static object? GetFormatter(Type t)
        {
            if (t == typeof(DateTime))
            {
                return NativeDateTimeFormatter.Instance;
            }
            else if (t == typeof(DateTime?))
            {
                return new StaticNullableFormatter<DateTime>(NativeDateTimeFormatter.Instance);
            }
            else if (t == typeof(DateTime[]))
            {
                return NativeDateTimeArrayFormatter.Instance;
            }

            return null;
        }
    }
}*/
