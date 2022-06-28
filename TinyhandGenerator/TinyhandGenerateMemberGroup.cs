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

internal class TinyhandGenerateMemberGroup
{
    public TinyhandGenerateMemberGroup(string identifier)
    {
        this.Identifier = identifier;
    }

    public void Process(Element element, bool generateHash)
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
                            g = new TinyhandGenerateMemberGroup(identifier);
                            this.Groups.Add(identifier, g);
                        }

                        g.Process(subgroup, generateHash);
                    }
                    else // if (assignment.RightElement is Value_String valueString)
                    {
                        this.Items.Add(new(generateHash, identifier, assignment.RightElement));
                    }
                }
            }
        }
    }

    public void Generate(TinyhandGenerateMemberObject generateMemberObject, ScopingStringBuilder ssb, string groupName)
    {
        if (this.Groups.Count == 0 && this.Items.Count == 0)
        {
            return;
        }

        var name = string.IsNullOrEmpty(this.Identifier) ? generateMemberObject.LocalName : this.Identifier;
        var partial = string.IsNullOrEmpty(this.Identifier) ? "partial " : string.Empty;
        using (var cls = ssb.ScopeBrace($"{generateMemberObject.AccessibilityName} static {partial}{generateMemberObject.KindName} {name}"))
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

                if (x.GenerateHash)
                {// Define members and set hashes generated from identifiers.
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
                {// Define members and set values.
                    var y = this.ElementToTypeValue(x.Element);
                    if (y.Type != null && y.Value != null)
                    {
                        ssb.AppendLine($"public static {y.Type} {x.Identifier} => {y.Value};");
                    }
                }
            }

            foreach (var x in this.Groups.Values)
            {
                if (!firstFlag)
                {
                    ssb.AppendLine();
                }

                firstFlag = false;
                x.Generate(generateMemberObject, ssb, groupName);
            }
        }
    }

    public string Identifier { get; }

    public SortedDictionary<string, TinyhandGenerateMemberGroup> Groups { get; } = new(); // new(StringComparer.InvariantCultureIgnoreCase);

    public SortedSet<Item> Items { get; } = new(); // new(StringComparer.InvariantCultureIgnoreCase);

    internal struct Item : IComparable<Item>
    {
        public Item(bool generateHash, string identifier, Element? element)
        {
            this.GenerateHash = generateHash;
            this.Identifier = identifier;
            this.Element = element;
        }

        public bool GenerateHash;
        public string Identifier;
        public Element? Element;

        public int CompareTo(Item other) => this.Identifier.CompareTo(other.Identifier);
    }

    private (string? Type, string? Value) ElementToTypeValue(Element? element)
    {
        if (element is Value_Bool valueBool)
        {// bool
            return ("bool", valueBool.ValueBool ? "true" : "false");
        }
        else if (element is Value_String valueString)
        {// string
            return ("string", "\"" + valueString.ValueStringUtf16 + "\"");
        }
        else if (element is Value_Long valueLong)
        {// long
            // return ("long", "0x" + valueLong.ValueLong.ToString("x"));
            return ("long", valueLong.ValueLong.ToString());
        }
        else if (element is Value_Double valueDouble)
        {// long
            return ("double", valueDouble.ValueDouble.ToString() + "d");
        }

        return (null, null);
    }
}
