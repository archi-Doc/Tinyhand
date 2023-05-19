// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

#pragma warning disable SA1602 // Enumeration items should be documented

namespace Tinyhand;

public enum JournalType : byte
{
    Startingpoint,
    Waypoint,
    Record,
}

public enum JournalRecord : byte
{
    Locator,
    Key,
    Value,
    Add,
    Remove,
    Clear,
}
