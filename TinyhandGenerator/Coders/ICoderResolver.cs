// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Visceral;
using Tinyhand.Coders;
using Tinyhand.Generator;

namespace Tinyhand;

/// <summary>
/// Allows querying for a coder for serializing or deserializing a particular <see cref="TinyhandObject" />.
/// </summary>
public interface ICoderResolver
{
    /// <summary>
    /// Gets an <see cref="ITinyhandCoder"/> instance that can serialize or deserialize some type <see cref="TinyhandObject" />.
    /// </summary>
    /// <param name="withNullable">The <see cref="TinyhandObject" /> to be serialized or deserialized.</param>
    /// <returns>A coder, if this resolver supplies one for type <see cref="TinyhandObject" />; otherwise <c>null</c>.</returns>
    ITinyhandCoder? TryGetCoder(WithNullable<TinyhandObject> withNullable);
}
