// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Arc.Visceral;

namespace Tinyhand.Generator;

internal class Automata
{
    public Automata(TinyhandObject obj)
    {
        this.Object = obj;
        this.root = new Node(this, 0);
    }

    public TinyhandObject Object { get; }

    public int ReconstructCount { get; set; }

    public void GenerateReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
    {
        if (this.ReconstructCount <= 0)
        {
            return;
        }

        ssb.AppendLine();
        foreach (var x in this.NodeList)
        {
            if (x.ReconstructIndex < 0 || x.Member == null)
            {
                continue;
            }

            this.Object.GenerateReconstructCore2(ssb, info, x.Member, x.ReconstructIndex);
        }
    }

    public Node AddNode(string name, TinyhandObject member)
    {
        var utf8 = Encoding.UTF8.GetBytes(name);
        if (utf8.Length > TinyhandBody.MaxStringKeySizeInBytes)
        {// String key size limit.
            var location = member.KeyVisceralAttribute?.Location ?? member.Location;
            this.Object.Body.AddDiagnostic(TinyhandBody.Warning_StringKeySizeLimit, location, TinyhandBody.MaxStringKeySizeInBytes);
            Array.Resize(ref utf8, TinyhandBody.MaxStringKeySizeInBytes);
        }

        if (this.NameToNode.TryGetValue(utf8, out var node))
        {// String key collision.
            var location = node.Member?.KeyVisceralAttribute?.Location ?? node.Member?.Location;
            this.Object.Body.AddDiagnostic(TinyhandBody.Error_StringKeyConflict, location);
            location = member.KeyVisceralAttribute?.Location ?? member.Location;
            this.Object.Body.AddDiagnostic(TinyhandBody.Error_StringKeyConflict, location);
            return node;
        }

        if (utf8.Any(x => x == 0))
        {
            var location = member.KeyVisceralAttribute?.Location ?? member.Location;
            this.Object.Body.AddDiagnostic(TinyhandBody.Error_StringKeyNull, location);
        }

        node = this.root;
        ReadOnlySpan<byte> bytes = utf8;
        while (bytes.Length > 0)
        {
            var key = AutomataKeyMock.GetKey(ref bytes);

            if (key == 0)
            {
            }
            else if (bytes.Length == 0)
            {// leaf node
                node = node.Add(key, this.NodeList.Count, member, utf8);
                this.NodeList.Add(node);
            }
            else
            {// branch node
                node = node.Add(key);
            }
        }

        this.NameToNode[utf8] = node;

        return node;
    }

    public void GenerateDeserialize(ScopingStringBuilder ssb, GeneratorInformation info, Node? node = null)
    {
        if (node == null)
        {
            node = this.root;
        }

        if (node.Nexts == null)
        {
            ssb.GotoSkipLabel();
            return;
        }

        this.GenerateDeserializeNode(ssb, info, node.Nexts.Values.ToArray());
    }

    public void GenerateDeserializeChildrenNexts(ScopingStringBuilder ssb, GeneratorInformation info, Node[] childrenNexts)
    {// childrenNexts.Length > 0
        if (childrenNexts.Length == 1)
        {
            var x = childrenNexts[0];
            ssb.AppendLine($"if (key != 0x{x.Key:X}) goto SkipLabel;");
            ssb.AppendLine("key = global::Tinyhand.Generator.AutomataKey.GetKey(ref utf8);");
            this.GenerateDeserialize(ssb, info, x);
            return;
        }

        var firstFlag = true;
        foreach (var x in childrenNexts)
        {
            var condition = firstFlag ? string.Format("if (key == 0x{0:X})", x.Key) : string.Format("else if (key == 0x{0:X})", x.Key);
            firstFlag = false;
            using (var c = ssb.ScopeBrace(condition))
            {
                ssb.AppendLine("key = global::Tinyhand.Generator.AutomataKey.GetKey(ref utf8);");
                this.GenerateDeserialize(ssb, info, x);
            }
        }

        using (var ifElse = ssb.ScopeBrace("else"))
        {
            ssb.AppendLine("goto SkipLabel;");
        }

        // ssb.GotoSkipLabel();
    }

    public void GenerateDeserializeValueNexts(ScopingStringBuilder ssb, GeneratorInformation info, Node[] valueNexts)
    {// valueNexts.Length > 0
        if (valueNexts.Length == 1)
        {
            var x = valueNexts[0];
            ssb.AppendLine($"if (key != 0x{x.Key:X}) goto SkipLabel;");
            if (x.ReconstructIndex >= 0)
            {
                ssb.AppendLine($"deserializedFlag[{x.ReconstructIndex}] = true;");
            }

            this.Object.GenerateDeserializeCore2(ssb, info, x.Member);
            return;
        }

        var firstFlag = true;
        foreach (var x in valueNexts)
        {
            var condition = firstFlag ? string.Format("if (key == 0x{0:X})", x.Key) : string.Format("else if (key == 0x{0:X})", x.Key);
            firstFlag = false;
            using (var c = ssb.ScopeBrace(condition))
            {
                if (x.ReconstructIndex >= 0)
                {
                    ssb.AppendLine($"deserializedFlag[{x.ReconstructIndex}] = true;");
                }

                this.Object.GenerateDeserializeCore2(ssb, info, x.Member);
            }
        }

        using (var ifElse = ssb.ScopeBrace("else"))
        {
            ssb.AppendLine("goto SkipLabel;");
        }

        // ssb.GotoSkipLabel();
    }

    public void GenerateDeserializeNode(ScopingStringBuilder ssb, GeneratorInformation info, Node[] nexts)
    {// ReadOnlySpan<byte> utf8, ulong key (assgined)
        if (nexts.Length < 4)
        {// linear-search
            var valueNexts = nexts.Where(x => x.HasValue).ToArray();
            var childrenNexts = nexts.Where(x => x.HasChildren).ToArray();

            if (valueNexts.Length == 0)
            {
                if (childrenNexts.Length == 0)
                {
                    ssb.GotoSkipLabel();
                }
                else
                {// valueNexts = 0, childrenNexts > 0
                    ssb.AppendLine("if (utf8.Length == 0) goto SkipLabel;");
                    this.GenerateDeserializeChildrenNexts(ssb, info, childrenNexts);
                }
            }
            else
            {
                if (childrenNexts.Length == 0)
                {// valueNexts > 0, childrenNexts = 0
                    ssb.AppendLine("if (utf8.Length != 0) goto SkipLabel;");
                    this.GenerateDeserializeValueNexts(ssb, info, valueNexts);
                }
                else
                {// valueNexts > 0, childrenNexts > 0
                    using (var scopeLeaf = ssb.ScopeBrace("if (utf8.Length == 0)"))
                    {// Should be leaf node.
                        this.GenerateDeserializeValueNexts(ssb, info, valueNexts);
                    }

                    using (var scopeBranch = ssb.ScopeBrace("else"))
                    {// Should be branch node.
                        this.GenerateDeserializeChildrenNexts(ssb, info, childrenNexts);
                    }
                }
            }
        }
        else
        {// binary-search
            var midline = nexts.Length / 2;
            var mid = nexts[midline].Key;
            var left = nexts.Take(midline).ToArray();
            var right = nexts.Skip(midline).ToArray();

            using (var scopeLeft = ssb.ScopeBrace($"if (key < 0x{mid:X})"))
            {// left
                this.GenerateDeserializeNode(ssb, info, left);
            }

            using (var scopeRight = ssb.ScopeBrace("else"))
            {// right
                this.GenerateDeserializeNode(ssb, info, right);
            }
        }
    }

    public List<Node> NodeList { get; } = new();

    public Dictionary<byte[], Node> NameToNode { get; } = new(new ByteArrayComparer());

    private readonly Node root;

    internal class Node
    {
        public Node(Automata automata, ulong key)
        {
            this.Automata = automata;
            this.Key = key;
        }

        public Automata Automata { get; }

        public ulong Key { get; }

        public int Index { get; private set; } = -1;

        public TinyhandObject? Member { get; private set; }

        public byte[]? Utf8Name { get; private set; }

        public string? Identifier { get; set; }

        public SortedDictionary<ulong, Node>? Nexts { get; private set; }

        public bool HasValue => this.Index != -1;

        public bool HasChildren => this.Nexts != null;

        public int ReconstructIndex { get; set; }

        public Node Add(ulong key)
        {
            if (this.Nexts != null && this.Nexts.TryGetValue(key, out var node))
            {// Found
                return node;
            }
            else
            {// Not found
                node = new Node(this.Automata, key);
                if (this.Nexts == null)
                {
                    this.Nexts = new();
                }

                this.Nexts.Add(key, node);
                return node;
            }
        }

        public Node Add(ulong key, int index, TinyhandObject member, byte[] utf8)
        {
            var node = this.Add(key);
            node.Index = index;
            node.Member = member;
            node.Utf8Name = utf8;

            return node;
        }

        public override string ToString()
        {
            return Encoding.UTF8.GetString(this.Utf8Name);
        }
    }
}
