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
    /// Identifies a locator entry.
    /// </summary>
    Locator,

    /// <summary>
    /// Identifies a key entry.
    /// </summary>
    Key,

    /// <summary>
    /// Identifies a value entry.
    /// </summary>
    Value,

    /// <summary>
    /// Represents an add operation.
    /// </summary>
    Add,

    /// <summary>
    /// Represents a remove operation.
    /// </summary>
    Remove,

    /// <summary>
    /// Represents a remove and erase operation.
    /// </summary>
    RemoveAndErase,

    /// <summary>
    /// Represents an add storage operation.
    /// </summary>
    AddStorage,

    /// <summary>
    /// Represents an erase storage operation.
    /// </summary>
    EraseStorage,
}
