﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Tinyhand.IO;

namespace Tinyhand;

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
/// A base interface for <see cref="ITinyhandFormatter{T}"/> so that all generic implementations can be detected by a common base type.
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
    /// <param name="value">The original value before deserialization and the value after deserialization.</param>
    /// <param name="options">The serialization settings to use, including the resolver to use to obtain formatters for types that make up the composite type <typeparamref name="T"/>.</param>
    void Deserialize(ref TinyhandReader reader, ref T? value, TinyhandSerializerOptions options);

    /// <summary>
    /// Create a new object.
    /// </summary>
    /// <param name="options">The serialization settings to use, including the resolver to use to obtain formatters for types that make up the composite type <typeparamref name="T"/>.</param>
    /// <returns>The new object.</returns>
    T Reconstruct(TinyhandSerializerOptions options);

    /// <summary>
    /// Creates a deep copy of the object.
    /// </summary>
    /// <param name="value">The value to be cloned.</param>
    /// <param name="options">The serialization settings to use, including the resolver to use to obtain formatters for types that make up the composite type <typeparamref name="T"/>.</param>
    /// <returns>The new object.</returns>
    [return: NotNullIfNotNull(nameof(value))]
    T? Clone(T? value, TinyhandSerializerOptions options);
}
