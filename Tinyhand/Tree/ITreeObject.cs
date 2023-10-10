// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Tinyhand.IO;

namespace Tinyhand;

public interface ITreeObject
{// TinyhandGenerator, ValueLinkGenerator
    ITreeRoot? TreeRoot { get; set; }

    ITreeObject? TreeParent { get; protected set; }

    int TreeKey { get; protected set; } // Plane or IntKey

    public void SetParent(ITreeObject? parent, int key = -1)
    {
        this.TreeRoot = parent?.TreeRoot;
        this.TreeParent = parent;
        this.TreeKey = key;
    }

    public void SetParentActual(ITreeObject? parent, int key = -1)
    {
        this.TreeRoot = parent?.TreeRoot;
        this.TreeParent = parent;
        this.TreeKey = key;
    }

    Task<bool> Save(UnloadMode unloadMode)
        => Task.FromResult(true);

    void Delete()
    {
    }

    void NotifyDataChanged()
    {
    }

    bool ReadRecord(ref TinyhandReader reader)
        => false;

    public void WriteLocator(ref TinyhandWriter writer)
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteKeyOrLocator(ref TinyhandWriter writer)
    {
        if (this.TreeKey >= 0)
        {
            writer.Write_Key();
            writer.Write(this.TreeKey);
        }
        else
        {
            this.WriteLocator(ref writer);
        }
    }

    public bool TryGetJournalWriter([NotNullWhen(true)] out ITreeRoot? journal, out TinyhandWriter writer, bool includeCurrent = true)
    {
        var p = this.TreeParent;
        if (p == null)
        {
            if (this.TreeRoot is null)
            {
                journal = null;
                writer = default;
                return false;
            }
            else
            {
                journal = this.TreeRoot;
                return journal.TryGetJournalWriter(JournalType.Record, out writer);
            }
        }
        else
        {
            var p2 = p.TreeParent;
            if (p2 is null)
            {
                if (p.TreeRoot is null)
                {
                    journal = null;
                    writer = default;
                    return false;
                }
                else
                {
                    journal = p.TreeRoot;
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
                var p3 = p2.TreeParent;
                if (p3 is null)
                {
                    if (p2.TreeRoot is null)
                    {
                        journal = null;
                        writer = default;
                        return false;
                    }
                    else
                    {
                        journal = p2.TreeRoot;
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
                    var p4 = p3.TreeParent;
                    if (p4 is null)
                    {
                        if (p3.TreeRoot is null)
                        {
                            journal = null;
                            writer = default;
                            return false;
                        }
                        else
                        {
                            journal = p3.TreeRoot;
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
                        var p5 = p4.TreeParent;
                        if (p5 is null)
                        {
                            if (p4.TreeRoot is null)
                            {
                                journal = null;
                                writer = default;
                                return false;
                            }
                            else
                            {
                                journal = p4.TreeRoot;
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
                            var p6 = p5.TreeParent;
                            if (p6 is null)
                            {
                                if (p5.TreeRoot is null)
                                {
                                    journal = null;
                                    writer = default;
                                    return false;
                                }
                                else
                                {
                                    journal = p5.TreeRoot;
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
                                var p7 = p6.TreeParent;
                                if (p7 is null)
                                {
                                    if (p6.TreeRoot is null)
                                    {
                                        journal = null;
                                        writer = default;
                                        return false;
                                    }
                                    else
                                    {
                                        journal = p6.TreeRoot;
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
                                else
                                {
                                    var p8 = p7.TreeParent;
                                    if (p8 is null)
                                    {
                                        if (p7.TreeRoot is null)
                                        {
                                            journal = null;
                                            writer = default;
                                            return false;
                                        }
                                        else
                                        {
                                            journal = p7.TreeRoot;
                                            journal.TryGetJournalWriter(JournalType.Record, out writer);
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
                                        var p9 = p8.TreeParent;
                                        if (p9 is null)
                                        {
                                            if (p8.TreeRoot is null)
                                            {
                                                journal = null;
                                                writer = default;
                                                return false;
                                            }
                                            else
                                            {
                                                journal = p8.TreeRoot;
                                                journal.TryGetJournalWriter(JournalType.Record, out writer);
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

        journal = null;
        writer = default;
        return false;
    }
}
