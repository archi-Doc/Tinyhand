// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Arc.Visceral;
using Microsoft.CodeAnalysis;
using Tinyhand.Tree;

#pragma warning disable SA1108

namespace Tinyhand.Generator;

internal class TinyhandHashedStringGroup
{
    public TinyhandHashedStringGroup(string identifier)
    {
        this.Identifier = identifier;
    }

    public void Process(Element element, bool hashedString)
    {
        if (element is not Group group)
        {
            return;
        }

        foreach (var x in group)
        {
            if (x is Assignment assignment)
            {
                if (assignment.LeftElement is Value_Identifier i)
                {// Get left element [Identifier]
                    var identifier = i.IdentifierUtf16;
                    if (assignment.RightElement is Group subgroup)
                    {// Group
                        if (!this.Groups.TryGetValue(identifier, out var g))
                        {
                            g = new TinyhandHashedStringGroup(identifier);
                            this.Groups.Add(identifier, g);
                        }

                        g.Process(subgroup, hashedString);
                    }
                    else // if (assignment.RightElement is Value_String valueString)
                    {
                        this.Items.Add(new(hashedString, identifier, assignment.RightElement));
                    }
                }
            }
        }
    }

    public void Generate(TinyhandHashedStringObject tinyhandHashedStringObject, ScopingStringBuilder ssb, string groupName)
    {
        if (this.Groups.Count == 0 && this.Items.Count == 0)
        {
            return;
        }

        var name = string.IsNullOrEmpty(this.Identifier) ? tinyhandHashedStringObject.LocalName : this.Identifier;
        var partial = string.IsNullOrEmpty(this.Identifier) ? "partial " : string.Empty;
        using (var cls = ssb.ScopeBrace($"{tinyhandHashedStringObject.AccessibilityName} static {partial}{tinyhandHashedStringObject.KindName} {name}"))
        {
            if (string.IsNullOrEmpty(groupName))
            {// "group"
                groupName = this.Identifier;
            }
            else
            {// "group.group2"
                groupName += "." + this.Identifier;
            }

            var firstFlag = true;
            foreach (var x in this.Items)
            {
                firstFlag = false;

                if (x.HashedString)
                {// Get a hash of an identifier.
                    string identifier;
                    if (string.IsNullOrEmpty(groupName))
                    {
                        identifier = x.Identifier;
                    }
                    else
                    {
                        identifier = groupName + "." + x.Identifier;
                    }

                    var hash = FarmHash.Hash64(identifier);
                    ssb.AppendLine($"public static ulong {x.Identifier} => 0x{hash.ToString("x")}ul;");
                }
                else
                {// Define a member and set value.
                }
            }

            foreach (var x in this.Groups.Values)
            {
                if (!firstFlag)
                {
                    ssb.AppendLine();
                }

                firstFlag = false;
                x.Generate(tinyhandHashedStringObject, ssb, groupName);
            }
        }
    }

    public string Identifier { get; }

    public SortedDictionary<string, TinyhandHashedStringGroup> Groups { get; } = new(); // new(StringComparer.InvariantCultureIgnoreCase);

    public SortedSet<Item> Items { get; } = new(); // new(StringComparer.InvariantCultureIgnoreCase);

    internal struct Item : IComparable<Item>
    {
        public Item(bool hashedString, string identifier, Element? element)
        {
            this.HashedString = hashedString;
            this.Identifier = identifier;
            this.Element = element;
        }

        public bool HashedString;
        public string Identifier;
        public Element? Element;

        public int CompareTo(Item other) => this.Identifier.CompareTo(other.Identifier);
    }
}
