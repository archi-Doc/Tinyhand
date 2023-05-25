// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Tinyhand.IO;

namespace Tinyhand;

public interface ITinyhandCrystal
{
    bool TryGetJournalWriter(JournalType recordType, uint plane, out TinyhandWriter writer);

    ulong AddJournal(in TinyhandWriter writer);

    bool TryAddToSaveQueue();
}

public interface ITinyhandJournal
{// TinyhandGenerator, ValueLinkGenerator
    ITinyhandCrystal? Crystal { get; set; }

    uint CurrentPlane { get; set; }

    bool ReadRecord(ref TinyhandReader reader);
}

public interface ITinyhandCustomJournal
{
    void WriteCustomLocator(ref TinyhandWriter writer);

    bool ReadCustomRecord(ref TinyhandReader reader);
}
