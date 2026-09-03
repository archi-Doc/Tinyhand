// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Tinyhand;

/// <summary>
/// Defines object equality for values whose serialized bytes may differ, such as unordered collections.
/// </summary>
public interface IEquatableObject
{
    /// <summary>
    /// Determines whether the current object is equal to another object.
    /// </summary>
    /// <param name="otherObject">The object to compare with the current object.</param>
    /// <returns><c>true</c> if the objects are considered equal; otherwise, <c>false</c>.</returns>
    bool ObjectEquals(object? otherObject);
}
