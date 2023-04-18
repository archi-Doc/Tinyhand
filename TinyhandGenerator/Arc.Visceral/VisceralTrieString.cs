// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#pragma warning disable SA1602 // Enumeration items should be documented

namespace Arc.Visceral;

internal enum VisceralTrieAddNodeResult
{
    Success,
    KeyCollision,
    NullKey,
}

internal class VisceralTrieString<TObject, TMember>
{
    internal class VisceralTrieContext
    {
        public VisceralTrieContext(Action<TObject, TMember, VisceralTrieContext> generateMethod, ScopingStringBuilder ssb, object? extraInfo)
        {
            this.GenerateMethod = generateMethod;
            this.Ssb = ssb;
            this.ExtraInfo = extraInfo;
        }

        public Action<TObject, TMember, VisceralTrieContext> GenerateMethod { get; }

        public ScopingStringBuilder Ssb { get; }

        public object? ExtraInfo { get; }

        public bool AddContinueStatement { get; set; } = true;
    }

    public const int MaxStringKeySizeInBytes = 512;

    public VisceralTrieString(TObject obj)
    {
        this.Object = obj;
        this.root = new Node(this, 0);
    }

    public TObject Object { get; }

    public (Node? node, VisceralTrieAddNodeResult result, bool keyResized) AddNode(string name, TMember member)
    {
        var keyResized = false;

        var utf8 = Encoding.UTF8.GetBytes(name);
        if (utf8.Length > MaxStringKeySizeInBytes)
        {// String key size limit.
            keyResized = true;
            Array.Resize(ref utf8, MaxStringKeySizeInBytes);
        }

        if (this.NameToNode.TryGetValue(utf8, out var node))
        {// Key collision
            return (node, VisceralTrieAddNodeResult.KeyCollision, keyResized);
        }

        if (utf8.Length == 0 || utf8.Any(x => x == 0))
        {// Null key
            return (null, VisceralTrieAddNodeResult.NullKey, keyResized);
        }

        node = this.root;
        ReadOnlySpan<byte> span = utf8;
        while (span.Length > 0)
        {
            var key = VisceralTrieHelper.ReadKey(ref span);

            if (key == 0)
            {
            }
            else if (span.Length == 0)
            {// Leaf node
                node = node.Add(key, this.NodeList.Count, member, utf8);
                this.NodeList.Add(node);
            }
            else
            {// Branch node
                node = node.Add(key);
            }
        }

        this.NameToNode[utf8] = node;

        return (node, VisceralTrieAddNodeResult.Success, keyResized);
    }

    public void Generate(VisceralTrieContext context)
    {
        context.Ssb.AppendLine("ulong key;");
        context.Ssb.AppendLine("var utf8 = reader.ReadStringSpan();");
        using (var c = context.Ssb.ScopeBrace("if (utf8.Length == 0)"))
        {
            context.Ssb.AppendLine("goto SkipLabel;");
        }

        context.Ssb.AppendLine("key = global::Arc.Visceral.VisceralTrieHelper.ReadKey(ref utf8);");

        this.GenerateCore(context, this.root);

        if (context.AddContinueStatement)
        {
            context.Ssb.AppendLine("continue;");
        }

        context.Ssb.AppendLine("SkipLabel:", false);
        context.Ssb.AppendLine("reader.Skip();");
    }

    private void GenerateCore(VisceralTrieContext context, Node node)
    {
        if (node.Nexts == null)
        {
            context.Ssb.AppendLine("goto SkipLabel;");
            return;
        }

        this.GenerateNode(context, node.Nexts.Values.ToArray());
    }

    private void GenerateNode(VisceralTrieContext context, Node[] nexts)
    {// ReadOnlySpan<byte> utf8, ulong key (assgined)
        if (nexts.Length < 4)
        {// linear-search
            var valueNexts = nexts.Where(x => x.HasValue).ToArray();
            var childrenNexts = nexts.Where(x => x.HasChildren).ToArray();

            if (valueNexts.Length == 0)
            {
                if (childrenNexts.Length == 0)
                {
                    context.Ssb.AppendLine("goto SkipLabel;");
                }
                else
                {// valueNexts = 0, childrenNexts > 0
                    context.Ssb.AppendLine("if (utf8.Length == 0) goto SkipLabel;");
                    this.GenerateChildrenNexts(context, childrenNexts);
                }
            }
            else
            {
                if (childrenNexts.Length == 0)
                {// valueNexts > 0, childrenNexts = 0
                    context.Ssb.AppendLine("if (utf8.Length != 0) goto SkipLabel;");
                    this.GenerateValueNexts(context, valueNexts);
                }
                else
                {// valueNexts > 0, childrenNexts > 0
                    using (var scopeLeaf = context.Ssb.ScopeBrace("if (utf8.Length == 0)"))
                    {// Should be leaf node.
                        this.GenerateValueNexts(context, valueNexts);
                    }

                    using (var scopeBranch = context.Ssb.ScopeBrace("else"))
                    {// Should be branch node.
                        this.GenerateChildrenNexts(context, childrenNexts);
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

            using (var scopeLeft = context.Ssb.ScopeBrace($"if (key < 0x{mid:X})"))
            {// left
                this.GenerateNode(context, left);
            }

            using (var scopeRight = context.Ssb.ScopeBrace("else"))
            {// right
                this.GenerateNode(context, right);
            }
        }
    }

    private void GenerateChildrenNexts(VisceralTrieContext context, Node[] childrenNexts)
    {// childrenNexts.Length > 0
        if (childrenNexts.Length == 1)
        {
            var x = childrenNexts[0];
            context.Ssb.AppendLine($"if (key != 0x{x.Key:X}) goto SkipLabel;");
            context.Ssb.AppendLine("key = global::Arc.Visceral.VisceralTrieHelper.ReadKey(ref utf8);");
            this.GenerateCore(context, x);
            return;
        }

        var firstFlag = true;
        foreach (var x in childrenNexts)
        {
            var condition = firstFlag ? string.Format("if (key == 0x{0:X})", x.Key) : string.Format("else if (key == 0x{0:X})", x.Key);
            firstFlag = false;
            using (var c = context.Ssb.ScopeBrace(condition))
            {
                context.Ssb.AppendLine("key = global::Arc.Visceral.VisceralTrieHelper.ReadKey(ref utf8);");
                this.GenerateCore(context, x);
            }
        }

        using (var ifElse = context.Ssb.ScopeBrace("else"))
        {
            context.Ssb.AppendLine("goto SkipLabel;");
        }

        // ssb.GotoSkipLabel();
    }

    private void GenerateValueNexts(VisceralTrieContext context, Node[] valueNexts)
    {// valueNexts.Length > 0
        if (valueNexts.Length == 1)
        {
            var x = valueNexts[0];
            context.Ssb.AppendLine($"if (key != 0x{x.Key:X}) goto SkipLabel;");
            context.GenerateMethod(this.Object, x.Member!, context);
            return;
        }

        var firstFlag = true;
        foreach (var x in valueNexts)
        {
            var condition = firstFlag ? string.Format("if (key == 0x{0:X})", x.Key) : string.Format("else if (key == 0x{0:X})", x.Key);
            firstFlag = false;
            using (var c = context.Ssb.ScopeBrace(condition))
            {
                context.GenerateMethod(this.Object, x.Member!, context);
            }
        }

        using (var ifElse = context.Ssb.ScopeBrace("else"))
        {
            context.Ssb.AppendLine("goto SkipLabel;");
        }
    }

    public List<Node> NodeList { get; } = new();

    public Dictionary<byte[], Node> NameToNode { get; } = new(new ByteArrayComparer());

    private readonly Node root;

    internal class Node
    {
        public Node(VisceralTrieString<TObject, TMember> visceralTrie, ulong key)
        {
            this.VisceralTrie = visceralTrie;
            this.Key = key;
        }

        public VisceralTrieString<TObject, TMember> VisceralTrie { get; }

        public ulong Key { get; }

        public int Index { get; private set; } = -1;

        public TMember? Member { get; private set; }

        public byte[]? Utf8Name { get; private set; }

        public SortedDictionary<ulong, Node>? Nexts { get; private set; }

        public bool HasValue => this.Index != -1;

        public bool HasChildren => this.Nexts != null;

        public Node Add(ulong key)
        {// Branch node
            if (this.Nexts != null && this.Nexts.TryGetValue(key, out var node))
            {// Found
                return node;
            }
            else
            {// Not found
                node = new Node(this.VisceralTrie, key);
                if (this.Nexts == null)
                {
                    this.Nexts = new();
                }

                this.Nexts.Add(key, node);
                return node;
            }
        }

        public Node Add(ulong key, int index, TMember member, byte[] utf8)
        {// Leaf node
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

internal class ByteArrayComparer : EqualityComparer<byte[]>
{
    public override bool Equals(byte[] first, byte[] second)
    {
        if (first == null || second == null)
        {
            return first == second;
        }
        else if (ReferenceEquals(first, second))
        {
            return true;
        }
        else if (first.Length != second.Length)
        {
            return false;
        }

        return first.AsSpan().SequenceEqual(second);
    }

    public override int GetHashCode(byte[] obj)
    {
        if (obj == null)
        {
            throw new ArgumentNullException(nameof(obj));
        }

        return obj.Length;
    }
}
