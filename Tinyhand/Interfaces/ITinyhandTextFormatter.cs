// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Tinyhand.IO;
using Tinyhand.Tree;

namespace Tinyhand
{
    /// <summary>
    /// A base interface for <see cref="ITinyhandTextFormatter{T}"/> so that all generic implementations
    /// can be detected by a common base type.
    /// </summary>
    public interface ITinyhandTextFormatter
    {
    }

    /// <summary>
    /// The contract for text serialization of some specific type.
    /// </summary>
    /// <typeparam name="T">The type to be serialized or deserialized.</typeparam>
    public interface ITinyhandTextFormatter<T> : ITinyhandTextFormatter
    {
        /// <summary>
        /// Serializes a value (convert a value to a Tinyhand element).
        /// </summary>
        /// <param name="element">The serialized Tinyhand element.</param>
        /// <param name="value">The value to be serialized.</param>
        /// <param name="options">The serialization settings to use, including the resolver to use to obtain formatters for types that make up the composite type <typeparamref name="T"/>.</param>
        void Serialize(out Element element, T? value, TinyhandTextSerializerOptions options);

        /// <summary>
        /// Deserializes a value (convert a Tinyhand element to a value).
        /// </summary>
        /// <param name="element">The Tinyhand element to deserialize from.</param>
        /// <param name="options">The serialization settings to use, including the resolver to use to obtain formatters for types that make up the composite type <typeparamref name="T"/>.</param>
        /// <returns>The deserialized value.</returns>
        T? Deserialize(Element element, TinyhandTextSerializerOptions options);
    }
}
