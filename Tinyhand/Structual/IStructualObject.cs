// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Tinyhand.IO;

#pragma warning disable SA1202 // Elements should be ordered by access

namespace Tinyhand;

public interface IStructualObject
{// TinyhandGenerator, ValueLinkGenerator
    IStructualRoot? StructualRoot { get; set; }

    IStructualObject? StructualParent { get; protected set; }

    int StructualKey { get; protected set; } // Plane or IntKey

    public void SetParent(IStructualObject? parent, int key = -1)
    {
        this.StructualRoot = parent?.StructualRoot;
        this.StructualParent = parent;
        this.StructualKey = key;
    }

    public void SetParentActual(IStructualObject? parent, int key = -1)
    {
        this.StructualRoot = parent?.StructualRoot;
        this.StructualParent = parent;
        this.StructualKey = key;
    }

    Task<bool> Save(UnloadMode unloadMode)
        => Task.FromResult(true);

    void Erase()
    {
    }

    bool ReadRecord(ref TinyhandReader reader)
        => false;

    public void WriteLocator(ref TinyhandWriter writer)
    {
    }

    public void AddJournalRecord(JournalRecord record, bool includeCurrent = true)
    {
        if (this.TryGetJournalWriter(out var root, out var writer, includeCurrent))
        {
            if (this is Tinyhand.ITinyhandCustomJournal custom)
            {
                custom.WriteCustomLocator(ref writer);
            }

            writer.Write(record);
            root.AddJournal(writer);
        }
    }

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
