// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Tinyhand.IO;

namespace Tinyhand;

/// <summary>
/// Handles custom journal records for a structural object.
/// </summary>
public interface ITinyhandCustomJournal
{
    void WriteCustomLocator(ref TinyhandWriter writer)
    {// Considering deprecation.
    }

    bool ReadCustomRecord(ref TinyhandReader reader);
}
