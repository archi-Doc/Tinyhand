// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Tinyhand.IO;

namespace Tinyhand;

/// <summary>
/// Adds a return-value deserialization overload to Tinyhand formatters.
/// </summary>
public static class ITinyhandFormatterExtension
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T? Deserialize<T>(this ITinyhandFormatter<T> f, ref TinyhandReader reader, TinyhandSerializerOptions options)
    {
        T? value = default;
        f.Deserialize(ref reader, ref value, options);
        return value;
    }
}

/// <summary>
/// Defines the common marker for Tinyhand formatters.
/// </summary>
public interface ITinyhandFormatter
{
}

/// <summary>
/// Defines serialization, deserialization, reconstruction, and cloning for a specific type.
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
    /// <param name="value">The existing value to reuse and the resulting value. Reuse and nil handling depend on the formatter.</param>
    /// <param name="options">The serialization settings to use, including the resolver to use to obtain formatters for types that make up the composite type <typeparamref name="T"/>.</param>
    void Deserialize(ref TinyhandReader reader, ref T? value, TinyhandSerializerOptions options);

    /// <summary>
    /// Reconstructs a default value.
    /// </summary>
    /// <param name="options">The serialization settings to use, including the resolver to use to obtain formatters for types that make up the composite type <typeparamref name="T"/>.</param>
    /// <returns>A default value, which may be a shared immutable instance.</returns>
    T Reconstruct(TinyhandSerializerOptions options);

    /// <summary>
    /// Copies supported mutable data while allowing immutable values to be shared.
    /// </summary>
    /// <param name="value">The value to be cloned.</param>
    /// <param name="options">The serialization settings to use, including the resolver to use to obtain formatters for types that make up the composite type <typeparamref name="T"/>.</param>
    /// <returns>The cloned value, or null when the input is null.</returns>
    [return: NotNullIfNotNull(nameof(value))]
    T? Clone(T? value, TinyhandSerializerOptions options);
}
