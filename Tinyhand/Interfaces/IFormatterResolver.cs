// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Runtime.CompilerServices;
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

        private static void Throw(Type t, IFormatterResolver resolver)
        {
            throw new FormatterNotRegisteredException(t.FullName + " is not registered in resolver: " + resolver.GetType());
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
