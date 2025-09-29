// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Tinyhand.IO;

namespace Tinyhand;

/// <summary>
/// Defines the contract for a structural root that supports journal operations and save queue management.
/// </summary>
public interface IStructuralRoot
{
    /// <summary>
    /// Attempts to obtain a <see cref="TinyhandWriter"/> for writing a journal entry of the specified <see cref="JournalType"/>.
    /// </summary>
    /// <param name="recordType">The type of the journal entry to write.</param>
    /// <param name="writer">When this method returns, contains the <see cref="TinyhandWriter"/> instance if successful; otherwise, the default value.</param>
    /// <returns><c>true</c> if a writer was successfully obtained; otherwise, <c>false</c>.</returns>
    bool TryGetJournalWriter(JournalType recordType, out TinyhandWriter writer);

    /// <summary>
    /// Adds the journal entry using the provided <see cref="TinyhandWriter"/> and disposes of the writer.
    /// </summary>
    /// <param name="writer">A reference to the <see cref="TinyhandWriter"/> to write and dispose.</param>
    /// <returns>The position of the written journal entry.</returns>
    ulong AddJournalAndDispose(ref TinyhandWriter writer);

    /// <summary>
    /// Adds the current instance to the save queue for persistence.
    /// </summary>
    /// <param name="delaySeconds">The number of seconds to delay before saving.<br/>
    /// If 0 is specified, the default delay time is used.</param>
    void AddToSaveQueue(int delaySeconds = 0);
}
