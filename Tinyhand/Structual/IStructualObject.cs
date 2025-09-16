// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Tinyhand.IO;

#pragma warning disable SA1202 // Elements should be ordered by access

namespace Tinyhand;

/// <summary>
/// Represents a structural object that can participate in a hierarchical structure,
/// support journaling, and provide serialization/deserialization capabilities.
/// </summary>
public interface IStructualObject // TinyhandGenerator, ValueLinkGenerator
{
    /// <summary>
    /// Gets or sets the root of the structure to which this object belongs.
    /// </summary>
    IStructualRoot? StructualRoot { get; set; }

    /// <summary>
    /// Gets or sets the parent structural object.
    /// </summary>
    IStructualObject? StructualParent { get; set; }

    /// <summary>
    /// Gets or sets the key that identifies this object within its parent.
    /// </summary>
    int StructualKey { get; set; }

    /// <summary>
    /// Sets up the structure by assigning the parent and key, and propagating the root.
    /// </summary>
    /// <param name="parent">The parent structural object.</param>
    /// <param name="key">The key for this object within its parent. Default is -1.</param>
    public void SetupStructure(IStructualObject? parent, int key = -1)
    {
        this.StructualRoot = parent?.StructualRoot;
        this.StructualParent = parent;
        this.StructualKey = key;
    }

    /// <summary>
    /// Sets the parent and key for this object, and updates the root reference.
    /// </summary>
    /// <param name="parent">The parent structural object.</param>
    /// <param name="key">The key for this object within its parent. Default is -1.</param>
    public sealed void SetParentAndKey(IStructualObject? parent, int key = -1)
    {
        this.StructualRoot = parent?.StructualRoot;
        this.StructualParent = parent;
        this.StructualKey = key;
    }

    /// <summary>
    /// Stores the data of the current object according to the specified <see cref="StoreMode"/>.
    /// </summary>
    /// <param name="storeMode">The mode that determines how the data should be stored.</param>
    /// <returns>A <see cref="Task{Boolean}"/> representing the asynchronous save operation. Returns <c>true</c> if the save was successful; otherwise, <c>false</c>.</returns>
    Task<bool> StoreData(StoreMode storeMode)
        => Task.FromResult(true);

    /// <summary>
    /// Delete the storage and data of the current object.<br/>
    /// This is an asynchronous function; if the elapsed time exceeds the specified timeout, the object is forcibly deleted.<br/>
    /// </summary>
    /// <param name="forceDeleteAfter">The UTC <see cref="DateTime"/> after which the object will be forcibly deleted if not already deleted.<br/>
    /// <see langword="default"/>: Do not forcibly delete; wait until all operations are finished.<br/>
    /// <see cref="DateTime.UtcNow"/> or earlier: forcibly delete data without waiting.
    /// </param>
    /// <returns>A <see cref="Task"/> representing the asynchronous delete operation.</returns>
    Task DeleteData(DateTime forceDeleteAfter = default)
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
    public void AddJournalRecord(JournalRecord record, bool includeCurrent = true)
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
        if (this.StructualKey >= 0)
        {
            writer.Write_Key();
            writer.Write(this.StructualKey);
        }
        else
        {
            this.WriteLocator(ref writer);
        }
    }

    /// <summary>
    /// Attempts to get a journal writer for this object, constructing the locator path as needed.<br/>
    /// The writer instance is released when a journal is added through <see cref="IStructualRoot.AddJournalAndDispose"/>.
    /// </summary>
    /// <param name="root">When this method returns, contains the root object if successful; otherwise, null.</param>
    /// <param name="writer">When this method returns, contains the journal writer if successful; otherwise, the default value.<br/>
    /// The Writer instance is released when a journal is added through <see cref="IStructualRoot.AddJournalAndDispose"/>.</param>
    /// <param name="includeCurrent">Whether to include the current object in the locator path.</param>
    /// <returns>True if a journal writer was successfully obtained; otherwise, false.</returns>
    public bool TryGetJournalWriter([NotNullWhen(true)] out IStructualRoot? root, out TinyhandWriter writer, bool includeCurrent = true)
    {
        var p = this.StructualParent;
        if (p == null)
        {
            if (this.StructualRoot is null)
            {
                root = null;
                writer = default;
                return false;
            }
            else
            {
                root = this.StructualRoot;
                return root.TryGetJournalWriter(JournalType.Record, out writer);
            }
        }
        else
        {
            var p2 = p.StructualParent;
            if (p2 is null)
            {
                if (p.StructualRoot is null)
                {
                    root = null;
                    writer = default;
                    return false;
                }
                else
                {
                    root = p.StructualRoot;
                    root.TryGetJournalWriter(JournalType.Record, out writer);
                }

                if (includeCurrent)
                {
                    this.WriteKeyOrLocator(ref writer);
                }

                return true;
            }
            else
            {
                var p3 = p2.StructualParent;
                if (p3 is null)
                {
                    if (p2.StructualRoot is null)
                    {
                        root = null;
                        writer = default;
                        return false;
                    }
                    else
                    {
                        root = p2.StructualRoot;
                        root.TryGetJournalWriter(JournalType.Record, out writer);
                    }

                    p.WriteKeyOrLocator(ref writer);
                    if (includeCurrent)
                    {
                        this.WriteKeyOrLocator(ref writer);
                    }

                    return true;
                }
                else
                {
                    var p4 = p3.StructualParent;
                    if (p4 is null)
                    {
                        if (p3.StructualRoot is null)
                        {
                            root = null;
                            writer = default;
                            return false;
                        }
                        else
                        {
                            root = p3.StructualRoot;
                            root.TryGetJournalWriter(JournalType.Record, out writer);
                        }

                        p2.WriteKeyOrLocator(ref writer);
                        p.WriteKeyOrLocator(ref writer);
                        if (includeCurrent)
                        {
                            this.WriteKeyOrLocator(ref writer);
                        }

                        return true;
                    }
                    else
                    {
                        var p5 = p4.StructualParent;
                        if (p5 is null)
                        {
                            if (p4.StructualRoot is null)
                            {
                                root = null;
                                writer = default;
                                return false;
                            }
                            else
                            {
                                root = p4.StructualRoot;
                                root.TryGetJournalWriter(JournalType.Record, out writer);
                            }

                            p3.WriteKeyOrLocator(ref writer);
                            p2.WriteKeyOrLocator(ref writer);
                            p.WriteKeyOrLocator(ref writer);
                            if (includeCurrent)
                            {
                                this.WriteKeyOrLocator(ref writer);
                            }

                            return true;
                        }
                        else
                        {
                            var p6 = p5.StructualParent;
                            if (p6 is null)
                            {
                                if (p5.StructualRoot is null)
                                {
                                    root = null;
                                    writer = default;
                                    return false;
                                }
                                else
                                {
                                    root = p5.StructualRoot;
                                    root.TryGetJournalWriter(JournalType.Record, out writer);
                                }

                                p4.WriteKeyOrLocator(ref writer);
                                p3.WriteKeyOrLocator(ref writer);
                                p2.WriteKeyOrLocator(ref writer);
                                p.WriteKeyOrLocator(ref writer);
                                if (includeCurrent)
                                {
                                    this.WriteKeyOrLocator(ref writer);
                                }

                                return true;
                            }
                            else
                            {
                                var p7 = p6.StructualParent;
                                if (p7 is null)
                                {
                                    if (p6.StructualRoot is null)
                                    {
                                        root = null;
                                        writer = default;
                                        return false;
                                    }
                                    else
                                    {
                                        root = p6.StructualRoot;
                                        root.TryGetJournalWriter(JournalType.Record, out writer);
                                    }

                                    p5.WriteKeyOrLocator(ref writer);
                                    p4.WriteKeyOrLocator(ref writer);
                                    p3.WriteKeyOrLocator(ref writer);
                                    p2.WriteKeyOrLocator(ref writer);
                                    p.WriteKeyOrLocator(ref writer);
                                    if (includeCurrent)
                                    {
                                        this.WriteKeyOrLocator(ref writer);
                                    }

                                    return true;
                                }
                                else
                                {
                                    var p8 = p7.StructualParent;
                                    if (p8 is null)
                                    {
                                        if (p7.StructualRoot is null)
                                        {
                                            root = null;
                                            writer = default;
                                            return false;
                                        }
                                        else
                                        {
                                            root = p7.StructualRoot;
                                            root.TryGetJournalWriter(JournalType.Record, out writer);
                                        }

                                        p6.WriteKeyOrLocator(ref writer);
                                        p5.WriteKeyOrLocator(ref writer);
                                        p4.WriteKeyOrLocator(ref writer);
                                        p3.WriteKeyOrLocator(ref writer);
                                        p2.WriteKeyOrLocator(ref writer);
                                        p.WriteKeyOrLocator(ref writer);
                                        if (includeCurrent)
                                        {
                                            this.WriteKeyOrLocator(ref writer);
                                        }

                                        return true;
                                    }
                                    else
                                    {
                                        var p9 = p8.StructualParent;
                                        if (p9 is null)
                                        {
                                            if (p8.StructualRoot is null)
                                            {
                                                root = null;
                                                writer = default;
                                                return false;
                                            }
                                            else
                                            {
                                                root = p8.StructualRoot;
                                                root.TryGetJournalWriter(JournalType.Record, out writer);
                                            }

                                            p7.WriteKeyOrLocator(ref writer);
                                            p6.WriteKeyOrLocator(ref writer);
                                            p5.WriteKeyOrLocator(ref writer);
                                            p4.WriteKeyOrLocator(ref writer);
                                            p3.WriteKeyOrLocator(ref writer);
                                            p2.WriteKeyOrLocator(ref writer);
                                            p.WriteKeyOrLocator(ref writer);
                                            if (includeCurrent)
                                            {
                                                this.WriteKeyOrLocator(ref writer);
                                            }

                                            return true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        root = null;
        writer = default;
        return false;
    }
}
