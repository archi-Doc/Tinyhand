// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Tinyhand.IO;

namespace Tinyhand
{
    /// <summary>
    /// A base interface for <see cref="ITinyhandFormatter{T}"/> so that all generic implementations
    /// can be detected by a common base type.
    /// </summary>
    public interface ITinyhandFormatter
    {
    }

    /// <summary>
    /// The contract for serialization of some specific type.
    /// </summary>
    /// <typeparam name="T">The type to be serialized or deserialized.</typeparam>
    public interface ITinyhandFormatter<T> : ITinyhandFormatter
    {
        /// <summary>
        /// Serializes a value.
        /// </summary>
        /// <param name="writer">The writer to use when serializing the value.</param>
        /// <param name="value">The value to be serialized.</param>
        /// <param name="options">The serialization settings to use, including the resolver to use to obtain formatters for types that make up the composite type <typeparamref name="T"/>.</param>
        void Serialize(ref TinyhandWriter writer, ref T? value, TinyhandSerializerOptions options);

        /// <summary>
        /// Deserializes a value.
        /// </summary>
        /// <param name="reader">The reader to deserialize from.</param>
        /// <param name="value">The deserialized value. If non-null value is specified, Tinyhand will reuse the instance (overwrite).</param>
        /// <param name="options">The serialization settings to use, including the resolver to use to obtain formatters for types that make up the composite type <typeparamref name="T"/>.</param>
        void Deserialize(ref TinyhandReader reader, ref T? value, TinyhandSerializerOptions options);

        /// <summary>
        /// Create a new object.
        /// </summary>
        /// <param name="options">The serialization settings to use, including the resolver to use to obtain formatters for types that make up the composite type <typeparamref name="T"/>.</param>
        /// <returns>The new object.</returns>
        T Reconstruct(TinyhandSerializerOptions options);
    }
}
