// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Tinyhand.IO;

#pragma warning disable SA1202 // Elements should be ordered by access

namespace Tinyhand;

/// <summary>
/// Defines parent-child relationships, journal replay, and persistence operations for a structural object.
/// </summary>
public interface IStructuralObject // TinyhandGenerator, ValueLinkGenerator
{
    /// <summary>
    /// Gets or sets the root of the structure to which this object belongs.
    /// </summary>
    IStructuralRoot? StructuralRoot { get; set; }

    /// <summary>
    /// Gets or sets the parent structural object.
    /// </summary>
    IStructuralObject? StructuralParent { get; set; }

    /// <summary>
    /// Gets or sets the key that identifies this object within its parent.
    /// </summary>
    int StructuralKey { get; set; }

    /// <summary>
    /// Sets up the structure by assigning the parent and key, and propagating the root.
    /// </summary>
    /// <param name="parent">The parent structural object.</param>
    /// <param name="key">The key for this object within its parent. Default is -1.</param>
    public void SetupStructure(IStructuralObject? parent, int key = -1)
    {
        this.StructuralRoot = parent?.StructuralRoot;
        this.StructuralParent = parent;
        this.StructuralKey = key;
    }

    /// <summary>
    /// Sets the parent and key for this object, and updates the root reference.
    /// </summary>
    /// <param name="parent">The parent structural object.</param>
    /// <param name="key">The key for this object within its parent. Default is -1.</param>
    public sealed void SetParentAndKey(IStructuralObject? parent, int key = -1)
    {
        this.StructuralRoot = parent?.StructuralRoot;
        this.StructuralParent = parent;
        this.StructuralKey = key;
    }

    /// <summary>
    /// Stores the data of the current object according to the specified <see cref="StoreMode"/>.
    /// </summary>
    /// <param name="storeMode">The mode that determines how the data should be stored.</param>
    /// <returns>A <see cref="Task{Boolean}"/> representing the asynchronous save operation. Returns <c>true</c> if the save was successful; otherwise, <c>false</c>.</returns>
    /// <remarks>The default implementation completes successfully without storing data.</remarks>
    Task<bool> StoreData(StoreMode storeMode)
        => Task.FromResult(true);

    /// <summary>
    /// Requests deletion of this object's stored data, with an optional deadline for forced deletion.
    /// </summary>
    /// <param name="forceDeleteAfter">The UTC <see cref="DateTime"/> after which the object will be forcibly deleted if not already deleted.<br/>
    /// <see langword="default"/>: Do not forcibly delete; wait until all operations are finished.<br/>
    /// <see cref="DateTime.UtcNow"/> or earlier: forcibly delete data without waiting.
    /// </param>
    /// <param name="writeJournal">Indicates whether to write the deletion operation to the journal.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous delete operation.</returns>
    /// <remarks>The default implementation does nothing. Implementations provide deletion and deadline handling.</remarks>
    Task DeleteData(DateTime forceDeleteAfter = default, bool writeJournal = true)
        => Task.CompletedTask;

    /// <summary>
    /// Reads and processes the journal record using <see cref="TinyhandReader"/>.
    /// </summary>
    /// <param name="reader">The reader to read from.</param>
    /// <returns>True if a journal was read successfully; otherwise, false.</returns>
    bool ProcessJournalRecord(ref TinyhandReader reader)
        => false;

    /// <summary>
    /// Writes a locator for this object to the specified <see cref="TinyhandWriter"/>.
    /// </summary>
    /// <param name="writer">The writer to write to.</param>
    public void WriteLocator(ref TinyhandWriter writer)
    {
    }

    /// <summary>
    /// Adds a journal record for this object, optionally including the current object in the locator path.
    /// </summary>
    /// <param name="record">The journal record to add.</param>
    /// <param name="includeCurrent">Whether to include the current object in the locator path.</param>
    public void AddJournalRecord(JournalRecordType record, bool includeCurrent = true)
    {
        if (this.TryGetJournalWriter(out var root, out var writer, includeCurrent))
        {
            if (this is Tinyhand.ITinyhandCustomJournal custom)
            {
                custom.WriteCustomLocator(ref writer);
            }

            writer.Write(record);
            root.AddJournalAndDispose(ref writer);
        }
    }

    /// <summary>
    /// Writes the key or locator for this object to the specified <see cref="TinyhandWriter"/>.
    /// </summary>
    /// <param name="writer">The writer to write to.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteKeyOrLocator(ref TinyhandWriter writer)
    {
        if (this.StructuralKey >= 0)
        {
            writer.WriteKeyRecord();
            writer.Write(this.StructuralKey);
        }
        else
        {
            this.WriteLocator(ref writer);
        }
    }

    /// <summary>
    /// Attempts to get a journal writer for this object, constructing the locator path as needed.<br/>
    /// The writer instance is released when a journal is added through <see cref="IStructuralRoot.AddJournalAndDispose"/>.
    /// </summary>
    /// <param name="root">When this method returns, contains the root object if successful; otherwise, null.</param>
    /// <param name="writer">When this method returns, contains the journal writer if successful; otherwise, the default value.<br/>
    /// The Writer instance is released when a journal is added through <see cref="IStructuralRoot.AddJournalAndDispose"/>.</param>
    /// <param name="includeCurrent">Whether to include the current object in the locator path.</param>
    /// <returns>True if a journal writer was successfully obtained; otherwise, false.</returns>
    public bool TryGetJournalWriter([NotNullWhen(true)] out IStructuralRoot? root, out TinyhandWriter writer, bool includeCurrent = true)
    {
        // The top object (the one without a parent) belongs to the root, and the objects below it are located by their keys from the top down.
        var top = this;
        while (top.StructuralParent is { } parent)
        {
            top = parent;
        }

        root = top.StructuralRoot;
        if (root is null)
        {
            writer = default;
            return false;
        }
        else if (!root.TryGetJournalWriter(JournalType.Record, out writer))
        {
            return false;
        }

        if (this.StructuralParent is { } p)
        {
            WriteLocators(p, ref writer);
            if (includeCurrent)
            {
                this.WriteKeyOrLocator(ref writer);
            }
        }

        return true;
    }

    /// <summary>
    /// Writes the keys or locators of the object and its ancestors, except the top object, from the top down.
    /// </summary>
    /// <param name="obj">The object.</param>
    /// <param name="writer">The writer to write to.</param>
    private static void WriteLocators(IStructuralObject obj, ref TinyhandWriter writer)
    {
        if (obj.StructuralParent is { } parent)
        {
            WriteLocators(parent, ref writer);
            obj.WriteKeyOrLocator(ref writer);
        }
    }
}
