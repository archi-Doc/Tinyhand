// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Tinyhand.IO;

namespace Tinyhand;

public interface IStructualRoot
{
    bool TryGetJournalWriter(JournalType recordType, out TinyhandWriter writer);

    ulong AddJournal(in TinyhandWriter writer);

    bool TryAddToSaveQueue();
}
