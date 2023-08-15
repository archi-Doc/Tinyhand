// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Tinyhand.IO;

namespace Tinyhand;

public interface IJournalObject
{// TinyhandGenerator, ValueLinkGenerator
    ITinyhandJournal? Journal { get; set; }

    IJournalObject? JournalParent { get; protected set; }

    int JournalKey { get; protected set; } // Plane or IntKey

    public void SetParent(IJournalObject? parent, int key = -1)
    {
        this.JournalParent = parent;
        this.JournalKey = key;
    }

    public void SetParentInternal(IJournalObject? parent, int key = -1)
    {
        this.JournalParent = parent;
        this.JournalKey = key;
    }

    public bool TryGetJournalWriter([NotNullWhen(true)] out ITinyhandJournal? journal, out TinyhandWriter writer, bool includeCurrent = true)
    {
        var p = this.JournalParent;
        if (p == null)
        {
            if (this.Journal is null)
            {
                journal = null;
                writer = default;
                return false;
            }
            else
            {
                journal = this.Journal;
                return journal.TryGetJournalWriter(JournalType.Record, out writer);
            }
        }
        else
        {
            var p2 = p.JournalParent;
            if (p2 is null)
            {
                if (p.Journal is null)
                {
                    journal = null;
                    writer = default;
                    return false;
                }
                else
                {
                    journal = p.Journal;
                    journal.TryGetJournalWriter(JournalType.Record, out writer);
                }

                if (includeCurrent)
                {
                    this.WriteKeyOrLocator(ref writer);
                }

                return true;
            }
            else
            {
                var p3 = p2.JournalParent;
                if (p3 is null)
                {
                    if (p2.Journal is null)
                    {
                        journal = null;
                        writer = default;
                        return false;
                    }
                    else
                    {
                        journal = p2.Journal;
                        journal.TryGetJournalWriter(JournalType.Record, out writer);
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
                    var p4 = p3.JournalParent;
                    if (p4 is null)
                    {
                        if (p3.Journal is null)
                        {
                            journal = null;
                            writer = default;
                            return false;
                        }
                        else
                        {
                            journal = p3.Journal;
                            journal.TryGetJournalWriter(JournalType.Record, out writer);
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
                        var p5 = p4.JournalParent;
                        if (p5 is null)
                        {
                            if (p4.Journal is null)
                            {
                                journal = null;
                                writer = default;
                                return false;
                            }
                            else
                            {
                                journal = p4.Journal;
                                journal.TryGetJournalWriter(JournalType.Record, out writer);
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
                            var p6 = p5.JournalParent;
                            if (p6 is null)
                            {
                                if (p5.Journal is null)
                                {
                                    journal = null;
                                    writer = default;
                                    return false;
                                }
                                else
                                {
                                    journal = p5.Journal;
                                    journal.TryGetJournalWriter(JournalType.Record, out writer);
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
                                var p7 = p6.JournalParent;
                                if (p7 is null)
                                {
                                    if (p6.Journal is null)
                                    {
                                        journal = null;
                                        writer = default;
                                        return false;
                                    }
                                    else
                                    {
                                        journal = p6.Journal;
                                        journal.TryGetJournalWriter(JournalType.Record, out writer);
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
                            }
                        }
                    }
                }
            }
        }

        journal = null;
        writer = default;
        return false;
    }

    bool ReadRecord(ref TinyhandReader reader)
        => false;

    public void WriteLocator(ref TinyhandWriter writer)
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteKeyOrLocator(ref TinyhandWriter writer)
    {
        if (this.JournalKey >= 0)
        {
            writer.Write_Key();
            writer.Write(this.JournalKey);
        }
        else
        {
            this.WriteLocator(ref writer);
        }
    }
}
