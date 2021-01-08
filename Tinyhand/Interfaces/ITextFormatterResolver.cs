// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

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
    public interface ITextFormatterResolver
    {
        /// <summary>
        /// Gets an <see cref="ITinyhandTextFormatter{T}"/> instance that can serialize or deserialize some type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of value to be serialized or deserialized.</typeparam>
        /// <returns>A formatter, if this resolver supplies one for type <typeparamref name="T"/>; otherwise <c>null</c>.</returns>
        ITinyhandTextFormatter<T>? TryGetFormatter<T>();
    }

    public static partial class ResolverExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ITinyhandTextFormatter<T> GetFormatter<T>(this ITextFormatterResolver resolver)
        {
            ITinyhandTextFormatter<T>? formatter;

            formatter = resolver.TryGetFormatter<T>();
            if (formatter == null)
            {
                Throw(typeof(T), resolver);
            }

            return formatter!;
        }

        private static void Throw(Type t, ITextFormatterResolver resolver)
        {
            throw new FormatterNotRegisteredException(t.FullName + " is not registered in resolver: " + resolver.GetType());
        }
    }
}
