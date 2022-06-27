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

    public void Process(Element element)
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

                        g.Process(subgroup);
                    }
                    else // if (assignment.RightElement is Value_String valueString)
                    {
                        this.Strings.Add(identifier);
                    }
                }
            }
        }
    }

    public void Generate(TinyhandHashedStringObject tinyhandHashedStringObject, ScopingStringBuilder ssb, string groupName)
    {
        if (this.Groups.Count == 0 && this.Strings.Count == 0)
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
            foreach (var x in this.Strings)
            {
                firstFlag = false;

                string identifier;
                if (string.IsNullOrEmpty(groupName))
                {
                    identifier = x;
                }
                else
                {
                    identifier = groupName + "." + x;
                }

                var hash = FarmHash.Hash64(identifier);
                ssb.AppendLine($"public static ulong {x} => 0x{hash.ToString("x")}ul;");
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

    public SortedSet<string> Strings { get; } = new(); // new(StringComparer.InvariantCultureIgnoreCase);
}
