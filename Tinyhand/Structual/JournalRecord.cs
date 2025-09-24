// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

#pragma warning disable SA1602 // Enumeration items should be documented

namespace Tinyhand;

/// <summary>
/// Represents the type of a journal entry.
/// </summary>
public enum JournalType : byte
{
    /// <summary>
    /// Indicates a record entry in the journal.
    /// </summary>
    Record,

    /// <summary>
    /// Indicates a waypoint in the journal.
    /// </summary>
    Waypoint,

    /*/// <summary>
    /// Indicates the starting point of a journal.
    /// </summary>
    Startingpoint, */
}

/// <summary>
/// Represents the specific record type within a journal.<br/>
/// It depends on the implementation of the object whether a record is processed by the current object or by a child object (according to the design philosophy of each).
/// </summary>
public enum JournalRecord : byte
{
    /// <summary>
    /// Represents a locator for descendant objects.
    /// </summary>
    Locator,

    /// <summary>
    /// Represents a key for descendant objects.
    /// </summary>
    Key,

    /// <summary>
    /// Represents the value.
    /// </summary>
    Value,

    /// <summary>
    /// Represents the operation of deleting this object.
    /// </summary>
    Delete,

    /// <summary>
    /// Represents the operation of adding an item to a collection.
    /// </summary>
    AddItem,

    /// <summary>
    /// Represents the operation of deleting an item from a collection.
    /// </summary>
    DeleteItem,

    /// <summary>
    /// Represents a custom add operation in the object.
    /// </summary>
    AddCustom,

    /// <summary>
    /// Represents a custom delete operation in the object.
    /// </summary>
    DeleteCustom,

    /// <summary>
    /// Represents an invalid journal record type.
    /// </summary>
    Invalid = 255,
}
