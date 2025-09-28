// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Tinyhand;

/// <summary>
/// An interface for comparing objects.<br/>
/// Normally, objects are serialized with Tinyhand and their byte sequences are compared.<br/>
/// However, that approach cannot be used for order-insensitive collections (e.g., hash-based collections).<br/>
/// Implement this interface for such cases.
/// </summary>
public interface IEquatableObject
{
    /// <summary>
    /// Determines whether the current object is equal to another object.
    /// </summary>
    /// <param name="other">The object to compare with the current object.</param>
    /// <returns><c>true</c> if the objects are considered equal; otherwise, <c>false</c>.</returns>
    bool ObjectEquals(object other);
}
