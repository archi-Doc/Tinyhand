// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Tinyhand.IO;

namespace Tinyhand;

public class JournalRoot
{
    public JournalRoot()
    {
    }

    public ITinyhandJournal? Journal { get; set; }
}

public interface IJournalObject
{// TinyhandGenerator, ValueLinkGenerator
    JournalRoot Root { get; protected set; }

    IJournalObject? Parent { get; protected set; }

    int Key { get; protected set; } // Plane or IntKey

    public void SetParent(IJournalObject parent, int key = -1)
    {
        var p = parent;
        JournalRoot? root = default;
        while (true)
        {
            if (p.Root is not null)
            {// Root found
                root = p.Root;
                break;
            }
            else if (p.Parent is null)
            {// New root
                root = new();

                p = parent;
                while (p is not null)
                {
                    p.Root = root;
                    p = p.Parent;
                }

                break;
            }
            else
            {// Search parents
                p = p.Parent;
            }
        }

        this.Root = root;
        this.Parent = parent;
        this.Key = key;
    }

    public bool TryGetJournalWriter(out TinyhandWriter writer)
    {
        if (this.Root.Journal is null)
        {
            writer = default;
            return false;
        }

        var p = this.Parent;
        this.Root.Journal.TryGetJournalWriter(JournalType.Record, out writer);
        if (p == null)
        {
            return true;
        }
        else
        {
            var p2 = p.Parent;
            if (p2 is null)
            {
                writer.Write_Key();
                if (this.Key >= 0)
                {
                    writer.Write(this.Key);
                }
                else
                {
                    this.WriteLocator(ref writer);
                }

                return true;
            }
            else
            {
                var p3 = p2.Parent;
                if (p3 is null)
                {
                    writer.Write_Key();
                    if (p.Key >= 0)
                    {
                        writer.Write(p.Key);
                    }
                    else
                    {
                        p.WriteLocator(ref writer);
                    }

                    writer.Write_Key();
                    if (this.Key >= 0)
                    {
                        writer.Write(this.Key);
                    }
                    else
                    {
                        this.WriteLocator(ref writer);
                    }

                    return true;
                }
                else
                {
                    var p4 = p3.Parent;
                    if (p4 is null)
                    {
                        writer.Write_Key();
                        if (p2.Key >= 0)
                        {
                            writer.Write(p2.Key);
                        }
                        else
                        {
                            p2.WriteLocator(ref writer);
                        }

                        writer.Write_Key();
                        if (p.Key >= 0)
                        {
                            writer.Write(p.Key);
                        }
                        else
                        {
                            p.WriteLocator(ref writer);
                        }

                        writer.Write_Key();
                        if (this.Key >= 0)
                        {
                            writer.Write(this.Key);
                        }
                        else
                        {
                            this.WriteLocator(ref writer);
                        }

                        return true;
                    }
                    else
                    {
                        var p5 = p4.Parent;
                        if (p5 is null)
                        {
                            writer.Write_Key();
                            if (p3.Key >= 0)
                            {
                                writer.Write(p3.Key);
                            }
                            else
                            {
                                p3.WriteLocator(ref writer);
                            }

                            writer.Write_Key();
                            if (p2.Key >= 0)
                            {
                                writer.Write(p2.Key);
                            }
                            else
                            {
                                p2.WriteLocator(ref writer);
                            }

                            writer.Write_Key();
                            if (p.Key >= 0)
                            {
                                writer.Write(p.Key);
                            }
                            else
                            {
                                p.WriteLocator(ref writer);
                            }

                            writer.Write_Key();
                            if (this.Key >= 0)
                            {
                                writer.Write(this.Key);
                            }
                            else
                            {
                                this.WriteLocator(ref writer);
                            }

                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }

    bool ReadRecord(ref TinyhandReader reader)
        => false;

    public void WriteLocator(ref TinyhandWriter writer)
    {
    }
}
