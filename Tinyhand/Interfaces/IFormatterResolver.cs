﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Tinyhand.Internal;
using Tinyhand.IO;

namespace Tinyhand
{
    /// <summary>
    /// Allows querying for a formatter for serializing or deserializing a particular <see cref="Type" />.
    /// </summary>
    public interface IFormatterResolver
    {
        /// <summary>
        /// Gets an <see cref="ITinyhandFormatter{T}"/> instance that can serialize or deserialize some type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of value to be serialized or deserialized.</typeparam>
        /// <returns>A formatter, if this resolver supplies one for type <typeparamref name="T"/>; otherwise <c>null</c>.</returns>
        ITinyhandFormatter<T>? TryGetFormatter<T>();

        /// <summary>
        /// Gets an <see cref="ITinyhandFormatterExtra{T}"/> instance that can serialize or deserialize some type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of value to be serialized or deserialized.</typeparam>
        /// <returns>A formatter, if this resolver supplies one for type <typeparamref name="T"/>; otherwise <c>null</c>.</returns>
        ITinyhandFormatterExtra<T>? TryGetFormatterExtra<T>() => default;
    }

    public static class ResolverExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ITinyhandFormatter<T> GetFormatter<T>(this IFormatterResolver resolver)
        {
            ITinyhandFormatter<T>? formatter;

            formatter = resolver.TryGetFormatter<T>();
            if (formatter == null)
            {
                Throw(typeof(T), resolver);
            }

            return formatter!;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ITinyhandFormatterExtra<T> GetFormatterExtra<T>(this IFormatterResolver resolver)
        {
            ITinyhandFormatterExtra<T>? formatter;

            formatter = resolver.TryGetFormatterExtra<T>();
            if (formatter == null)
            {
                Throw(typeof(T), resolver);
            }

            return formatter!;
        }

        private static void Throw(Type t, IFormatterResolver resolver)
        {
            throw new FormatterNotRegisteredException(t.FullName + " is not registered in resolver: " + resolver.GetType());
        }

        private static readonly ThreadsafeTypeKeyHashTable<Func<IFormatterResolver, ITinyhandFormatter>> FormatterGetters =
            new ThreadsafeTypeKeyHashTable<Func<IFormatterResolver, ITinyhandFormatter>>();

        private static readonly MethodInfo GetFormatterRuntimeMethod = typeof(IFormatterResolver).GetRuntimeMethod(nameof(IFormatterResolver.TryGetFormatter), Type.EmptyTypes);

        public static object TryGetFormatterDynamic(this IFormatterResolver resolver, Type type)
        {
            if (resolver is null)
            {
                throw new ArgumentNullException(nameof(resolver));
            }

            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (!FormatterGetters.TryGetValue(type, out var formatterGetter))
            {
                var genericMethod = GetFormatterRuntimeMethod.MakeGenericMethod(type);
                var inputResolver = Expression.Parameter(typeof(IFormatterResolver), "inputResolver");
                formatterGetter = Expression.Lambda<Func<IFormatterResolver, ITinyhandFormatter>>(
                    Expression.Call(inputResolver, genericMethod), inputResolver).Compile();
                FormatterGetters.TryAdd(type, formatterGetter);
            }

            return formatterGetter(resolver);
        }

        internal static object GetFormatterDynamic(this IFormatterResolver resolver, Type type)
        {
            var result = TryGetFormatterDynamic(resolver, type);
            if (result == null)
            {
                Throw(type, resolver);
            }

            return result!;
        }
    }

    public class FormatterNotRegisteredException : Exception
    {
        public FormatterNotRegisteredException(string message)
            : base(message)
        {
        }
    }
}
