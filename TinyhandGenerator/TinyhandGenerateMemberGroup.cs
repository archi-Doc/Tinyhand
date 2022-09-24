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
    public const int MaxSummaryLength = 128;

    public TinyhandGenerateMemberGroup(string identifier)
    {
        this.Identifier = identifier;
    }

    public void Process(TinyhandGenerateMemberBody body, Location location, Element element, bool generateHash)
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
                {// Get the left element [Identifier]
                    var identifier = i.IdentifierUtf16;
                    if (!VisceralHelper.IsValidIdentifier(identifier))
                    {// Invalid identifier
                        body.AddDiagnostic(TinyhandBody.Warning_InvalidIdentifier2, location, identifier, i.GetLinePositionString());
                        continue;
                    }

                    if (assignment.RightElement is Group subgroup)
                    {// Group
                        if (!this.Groups.TryGetValue(identifier, out var g))
                        {
                            g = new TinyhandGenerateMemberGroup(identifier);
                            this.Groups.Add(identifier, g);
                        }

                        g.Process(body, location, subgroup, generateHash);
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

                    // Comment
                    var y = this.ElementToTypeValue(x.Element);
                    if (y.Value != null)
                    {
                        ssb.AppendLine($"/// <summary>{this.GetCommentSafeString(y.Value)}</summary>");
                    }

                    // Property
                    var hash = FarmHash.Hash64(identifier);
                    ssb.AppendLine($"public static ulong {x.Identifier} => 0x{hash.ToString("x")}ul;");
                }
                else
                {// Define members and set values.
                    var y = this.ElementToTypeValue(x.Element);
                    if (y.Type != null && y.Value != null)
                    {
                        if (y.Type == "string")
                        {
                            ssb.AppendLine($"public static {y.Type} {x.Identifier} => \"{this.GetValueSafeString(y.Value)}\";");
                        }
                        else
                        {
                            ssb.AppendLine($"public static {y.Type} {x.Identifier} => {y.Value};");
                        }
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

    private unsafe string GetCommentSafeString(string source)
    {
        var length = source.Length <= MaxSummaryLength ? source.Length : MaxSummaryLength;
        var span = source.AsSpan().Slice(0, length);

        // Count
        var count = 0;
        var change = false;
        foreach (var x in span)
        {
            switch (x)
            {
                case '\b':
                case '\f':
                case '\n':
                case '\r':
                case '\t':
                    change = true;
                    count += 2;
                    continue;

                case '<': // &lt;
                case '>': // &gt;
                    change = true;
                    count += 4;
                    continue;

                default:
                    count++;
                    continue;
            }
        }

        if (!change)
        {// No change
            return source;
        }

        // Get safe string
        char* chars = stackalloc char[count];
        count = 0;
        foreach (var x in span)
        {
            switch (x)
            {
                case '\b':
                    chars[count++] = '\\';
                    chars[count++] = 'b';
                    continue;

                case '\f':
                    chars[count++] = '\\';
                    chars[count++] = 'f';
                    continue;

                case '\n':
                    chars[count++] = '\\';
                    chars[count++] = 'n';
                    continue;

                case '\r':
                    chars[count++] = '\\';
                    chars[count++] = 'r';
                    continue;

                case '\t':
                    chars[count++] = '\\';
                    chars[count++] = 't';
                    continue;

                case '<': // &lt;
                    chars[count++] = '&';
                    chars[count++] = 'l';
                    chars[count++] = 't';
                    chars[count++] = ';';
                    continue;

                case '>': // &gt;
                    chars[count++] = '&';
                    chars[count++] = 'g';
                    chars[count++] = 't';
                    chars[count++] = ';';
                    continue;

                default:
                    chars[count++] = x;
                    continue;
            }
        }

        var dest = new string(chars);
        return dest;
    }

    private unsafe string GetValueSafeString(string source)
    {
        var span = source.AsSpan();

        // Count
        var count = 0;
        var change = false;
        foreach (var x in span)
        {
            switch (x)
            {
                case '\\':
                case '\"':
                case '\b':
                case '\f':
                case '\n':
                case '\r':
                case '\t':
                    change = true;
                    count += 2;
                    continue;

                default:
                    count++;
                    continue;
            }
        }

        if (!change)
        {// No change
            return source;
        }

        // Get safe string
        Span<char> buffer = count <= 1024 ? stackalloc char[count] : new char[count];
        fixed (char* chars = buffer)
        {
            count = 0;
            foreach (var x in span)
            {
                switch (x)
                {
                    case '\\':
                        chars[count++] = '\\';
                        chars[count++] = '\\';
                        continue;

                    case '\"':
                        chars[count++] = '\\';
                        chars[count++] = '\"';
                        continue;

                    case '\b':
                        chars[count++] = '\\';
                        chars[count++] = 'b';
                        continue;

                    case '\f':
                        chars[count++] = '\\';
                        chars[count++] = 'f';
                        continue;

                    case '\n':
                        chars[count++] = '\\';
                        chars[count++] = 'n';
                        continue;

                    case '\r':
                        chars[count++] = '\\';
                        chars[count++] = 'r';
                        continue;

                    case '\t':
                        chars[count++] = '\\';
                        chars[count++] = 't';
                        continue;

                    default:
                        chars[count++] = x;
                        continue;
                }
            }

            var dest = new string(chars);
            return dest;
        }
    }

    private (string? Type, string? Value) ElementToTypeValue(Element? element)
    {
        if (element is Value_Bool valueBool)
        {// bool
            return ("bool", valueBool.ValueBool ? "true" : "false");
        }
        else if (element is Value_String valueString)
        {// string
            return ("string", valueString.ValueStringUtf16);
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
