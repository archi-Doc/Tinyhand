// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Tinyhand.TextResolvers;
using Tinyhand.Tree;

#pragma warning disable SA1602 // Enumeration items should be documented

namespace Tinyhand
{
    /* public static class TinyhandTextSerializerOptionsExtension
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T DeserializeAndReconstruct<T>(this TinyhandTextSerializerOptions options, Element element)
        {
            ITinyhandTextFormatter<T>? formatter;

            formatter = options.Resolver.TryGetFormatter<T>();
            if (formatter == null)
            {
                Throw(typeof(T), options.Resolver);
            }

            return formatter!.Deserialize(element, options) ?? formatter!.Reconstruct(options);
        }

        private static void Throw(Type t, ITextFormatterResolver resolver)
        {
            throw new FormatterNotRegisteredException(t.FullName + " is not registered in resolver: " + resolver.GetType());
        }
    }

    public class TinyhandTextSerializerOptions
    {
        public static TinyhandTextSerializerOptions Standard => new TinyhandTextSerializerOptions(StandardTextResolver.Instance);

        /// <summary>
        /// Initializes a new instance of the <see cref="TinyhandTextSerializerOptions"/> class.
        /// </summary>
        /// <param name="resolver">The new value for the <see cref="TextResolver"/>.</param>
        protected internal TinyhandTextSerializerOptions(ITextFormatterResolver resolver)
        {
            this.TextResolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TinyhandTextSerializerOptions"/> class
        /// with members initialized from an existing instance.
        /// </summary>
        /// <param name="copyFrom">The options to copy from.</param>
        protected TinyhandTextSerializerOptions(TinyhandTextSerializerOptions copyFrom)
        {
            this.TextResolver = copyFrom.TextResolver;
            this.Compose = copyFrom.Compose;
        }

        /// <summary>
        /// Gets the resolver to use for complex types.
        /// </summary>
        /// <value>An instance of <see cref="ITextFormatterResolver"/>. Never <c>null</c>.</value>
        /// <exception cref="ArgumentNullException">Thrown if an attempt is made to set this property to <c>null</c>.</exception>
        public ITextFormatterResolver TextResolver { get; private set; }

        /// <summary>
        /// Gets the compose option.
        /// </summary>
        public TinyhandComposeOption Compose { get; private set; }

        /// <summary>
        /// Gets a copy of these options with the <see cref="TextResolver"/> property set to a new value.
        /// </summary>
        /// <param name="resolver">The new value for the <see cref="TextResolver"/>.</param>
        /// <returns>The new instance; or the original if the value is unchanged.</returns>
        public TinyhandTextSerializerOptions WithResolver(ITextFormatterResolver resolver)
        {
            if (this.TextResolver == resolver)
            {
                return this;
            }

            var result = this.Clone();
            result.TextResolver = resolver;
            return result;
        }

        /// <summary>
        /// Gets a copy of these options with the <see cref="Compose"/> property set to a new value.
        /// </summary>
        /// <param name="compose">The new value for the <see cref="Compose"/> property.</param>
        /// <returns>The new instance; or the original if the value is unchanged.</returns>
        public TinyhandTextSerializerOptions WithCompose(TinyhandComposeOption compose)
        {
            if (this.Compose == compose)
            {
                return this;
            }

            var result = this.Clone();
            result.Compose = compose;
            return result;
        }

        /// <summary>
        /// Creates a clone of this instance with the same properties set.
        /// </summary>
        /// <returns>The cloned instance. Guaranteed to be a new instance.</returns>
        /// <exception cref="NotSupportedException">Thrown if this instance is a derived type that doesn't override this method.</exception>
        protected virtual TinyhandTextSerializerOptions Clone()
        {
            if (this.GetType() != typeof(TinyhandTextSerializerOptions))
            {
                throw new NotSupportedException($"The derived type {this.GetType().FullName} did not override the {nameof(this.Clone)} method as required.");
            }

            return new TinyhandTextSerializerOptions(this);
        }
    }*/
}
