// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

#pragma warning disable SA1602 // Enumeration items should be documented

namespace Tinyhand;

/// <summary>
/// Represents the type of a journal entry.
/// </summary>
public enum JournalType : byte
{
    /// <summary>
    /// Indicates the starting point of a journal.
    /// </summary>
    Startingpoint,

    /// <summary>
    /// Indicates a waypoint in the journal.
    /// </summary>
    Waypoint,

    /// <summary>
    /// Indicates a record entry in the journal.
    /// </summary>
    Record,
}

/// <summary>
/// Represents the specific record type within a journal.
/// </summary>
public enum JournalRecord : byte
{
    /// <summary>
    /// Represents a locator for descendant objects.<br/>
    /// Subsequent processing is delegated to the descendant objects.
    /// </summary>
    Locator,

    /// <summary>
    /// Represents a key for descendant objects.<br/>
    /// Subsequent processing is delegated to the descendant objects.
    /// </summary>
    Key,

    /// <summary>
    /// Represents the value.<br/>
    /// This entry is intended to be processed by this object.
    /// </summary>
    Value,

    /// <summary>
    /// Represents the operation of deleting this object.<br/>
    /// This entry is intended to be processed by this object.
    /// </summary>
    Delete,

    /// <summary>
    /// Represents the operation of adding an item to a collection.<br/>
    /// This entry is intended to be processed by this object.
    /// </summary>
    AddItem,

    /// <summary>
    /// Represents the operation of deleting an item from a collection.<br/>
    /// This entry is intended to be processed by this object.
    /// </summary>
    DeleteItem,

    /// <summary>
    /// Represents an invalid journal record type.
    /// </summary>
    Invalid = 255,
}
