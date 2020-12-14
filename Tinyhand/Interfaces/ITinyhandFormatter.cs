// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

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
        void Serialize(ref TinyhandWriter writer, T? value, TinyhandSerializerOptions options);

        /// <summary>
        /// Deserializes a value.
        /// </summary>
        /// <param name="reader">The reader to deserialize from.</param>
        /// <param name="options">The serialization settings to use, including the resolver to use to obtain formatters for types that make up the composite type <typeparamref name="T"/>.</param>
        /// <returns>The deserialized value.</returns>
        T? Deserialize(ref TinyhandReader reader, TinyhandSerializerOptions options);

        /// <summary>
        /// Create a new object.
        /// </summary>
        /// <param name="options">The serialization settings to use, including the resolver to use to obtain formatters for types that make up the composite type <typeparamref name="T"/>.</param>
        /// <returns>The new object.</returns>
        T Reconstruct(TinyhandSerializerOptions options);
    }

    /// <summary>
    /// A base interface for <see cref="ITinyhandFormatterExtra{T}"/> so that all generic implementations
    /// can be detected by a common base type.
    /// </summary>
    public interface ITinyhandFormatterExtra
    {
    }

    /// <summary>
    /// The contract for serialization of some specific type.
    /// </summary>
    /// <typeparam name="T">The type to be serialized or deserialized.</typeparam>
    public interface ITinyhandFormatterExtra<T> : ITinyhandFormatterExtra
    {
        /// <summary>
        /// Reuse an existing instance and deserializes a value.
        /// </summary>
        /// <param name="reuse">The existing instance to reuse.</param>
        /// <param name="reader">The reader to deserialize from.</param>
        /// <param name="options">The serialization settings to use, including the resolver to use to obtain formatters for types that make up the composite type <typeparamref name="T"/>.</param>
        /// <returns>The deserialized value.</returns>
        T? Deserialize(T reuse, ref TinyhandReader reader, TinyhandSerializerOptions options);

        /*/// <summary>
        /// Reuse an existing instance and reconstruct a object.
        /// </summary>
        /// <param name="reuse">The existing instance to reuse.</param>
        /// <param name="options">The serialization settings to use, including the resolver to use to obtain formatters for types that make up the composite type <typeparamref name="T"/>.</param>
        /// <returns>The new object.</returns>
        T Reconstruct(T reuse, TinyhandSerializerOptions options);*/
    }
}
