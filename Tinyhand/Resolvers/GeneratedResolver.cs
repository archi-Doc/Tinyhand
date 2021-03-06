﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
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

        private Internal.ThreadsafeTypeKeyHashTable<FormatterGeneratorInfo> formatterGenerator = new();

        internal class FormatterGeneratorInfo
        {
            public Type GenericType { get; }

            public Func<Type[], (ITinyhandFormatter, ITinyhandFormatterExtra)> Generator { get; set; }

            public Dictionary<Type[], (ITinyhandFormatter, ITinyhandFormatterExtra)> FormatterCache { get; } = new();

            public FormatterGeneratorInfo(Type genericType, Func<Type[], (ITinyhandFormatter, ITinyhandFormatterExtra)> generator)
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
            var genericType = targetType.GetGenericTypeDefinition();
            if (this.formatterGenerator.TryGetValue(genericType, out var info))
            {
                (ITinyhandFormatter, ITinyhandFormatterExtra) f;
                var genericArguments = targetType.GetGenericArguments();
                if (!info.FormatterCache.TryGetValue(genericArguments, out f))
                {
                    f = info.Generator(genericArguments);
                    info.FormatterCache[genericArguments] = f;
                }

                return (ITinyhandFormatter<T>)f.Item1;
            }

            return null;
        }

        public void SetFormatterGenerator(Type genericType, Func<Type[], (ITinyhandFormatter, ITinyhandFormatterExtra)> generator)
        {
            var info = new FormatterGeneratorInfo(genericType, generator);
            this.formatterGenerator.TryAdd(genericType, info);
        }

        public void SetFormatter<T>(ITinyhandFormatter<T> formatter)
        {
            FormatterCache<T>.Formatter = formatter;
        }

        public ITinyhandFormatterExtra<T>? TryGetFormatterExtra<T>()
        {
            var formatter = FormatterCacheExtra<T>.Formatter;
            if (formatter != null)
            {
                return formatter;
            }

            var targetType = typeof(T);
            var genericType = targetType.GetGenericTypeDefinition();
            if (this.formatterGenerator.TryGetValue(genericType, out var info))
            {
                (ITinyhandFormatter, ITinyhandFormatterExtra) f;
                var genericArguments = targetType.GetGenericArguments();
                if (!info.FormatterCache.TryGetValue(genericArguments, out f))
                {
                    f = info.Generator(genericArguments);
                    info.FormatterCache[genericArguments] = f;
                }

                return (ITinyhandFormatterExtra<T>)f.Item2;
            }

            return null;
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
