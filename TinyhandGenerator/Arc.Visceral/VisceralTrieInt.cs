// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Arc.Visceral;

internal class VisceralTrieInt<TObject, TMember>
{
    public VisceralTrieInt(TObject obj, Action<TObject, VisceralTrieContext, TMember> generateMethod)
    {
        this.Object = obj;
        this.GenerateMethod = generateMethod;
        this.root = new Node(this, 0);
    }

    public TObject Object { get; }

    public Action<TObject, VisceralTrieContext, TMember> GenerateMethod { get; }

    public (Node? node, VisceralTrieAddNodeResult result) AddNode(int key, TMember member)
    {
        if (this.root.Nexts?.TryGetValue(key, out var node) == true)
        {// Key collision
            return (node, VisceralTrieAddNodeResult.KeyCollision);
        }

        node = this.root.Add(key, this.NodeList.Count, member);
        this.NodeList.Add(node);

        return (node, VisceralTrieAddNodeResult.Success);
    }

    public void Generate(VisceralTrieContext context)
    {
        context.Ssb.AppendLine("var key = reader.ReadInt32();");

        if (this.root.Nexts == null)
        {
            context.Ssb.AppendLine("goto SkipLabel;");
        }
        else
        {
            this.GenerateNode(context, this.root.Nexts.Values.ToArray());

            if (context.AddContinueStatement)
            {
                context.Ssb.AppendLine("continue;");
            }
        }

        context.Ssb.AppendLine("SkipLabel:", false);
        context.Ssb.AppendLine("reader.Skip();");
    }

    private void GenerateNode(VisceralTrieContext context, Node[] nexts)
    {// ReadOnlySpan<byte> utf8, ulong key (assgined)
        if (nexts.Length < 4)
        {// linear-search
            this.GenerateValueNexts(context, nexts.ToArray());
        }
        else
        {// binary-search
            var midline = nexts.Length / 2;
            var mid = nexts[midline].Key;
            var left = nexts.Take(midline).ToArray();
            var right = nexts.Skip(midline).ToArray();

            using (var scopeLeft = context.Ssb.ScopeBrace($"if (key < {mid})"))
            {// left
                this.GenerateNode(context, left);
            }

            using (var scopeRight = context.Ssb.ScopeBrace("else"))
            {// right
                this.GenerateNode(context, right);
            }
        }
    }

    private void GenerateValueNexts(VisceralTrieContext context, Node[] valueNexts)
    {// valueNexts.Length > 0
        if (valueNexts.Length == 1)
        {
            var x = valueNexts[0];
            context.Ssb.AppendLine($"if (key != {x.Key}) goto SkipLabel;");
            this.GenerateMethod(this.Object, context, x.Member!);
            return;
        }

        var firstFlag = true;
        foreach (var x in valueNexts)
        {
            var condition = firstFlag ? string.Format("if (key == 0x{0})", x.Key) : string.Format("else if (key == 0x{0})", x.Key);
            firstFlag = false;
            using (var c = context.Ssb.ScopeBrace(condition))
            {
                this.GenerateMethod(this.Object, context, x.Member!);
            }
        }

        using (var ifElse = context.Ssb.ScopeBrace("else"))
        {
            context.Ssb.AppendLine("goto SkipLabel;");
        }
    }

    public List<Node> NodeList { get; } = new();

    private readonly Node root;

    internal class Node
    {
        public Node(VisceralTrieInt<TObject, TMember> visceralTrie, int key)
        {
            this.VisceralTrie = visceralTrie;
            this.Key = key;
        }

        public VisceralTrieInt<TObject, TMember> VisceralTrie { get; }

        public int Key { get; }

        public int Index { get; private set; } = -1;

        public TMember? Member { get; private set; }

        public SortedDictionary<int, Node>? Nexts { get; private set; }

        public bool HasChildren => this.Nexts != null;

        public Node Add(int key, int index, TMember member)
        {// Leaf node
            var node = new Node(this.VisceralTrie, key);
            node.Index = index;
            node.Member = member;

            this.Nexts ??= new();
            this.Nexts.Add(key, node);

            return node;
        }
    }
}
