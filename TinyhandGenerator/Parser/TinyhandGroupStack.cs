// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Tinyhand;

[StructLayout(LayoutKind.Explicit, Size = 8)]
internal struct TinyhandGroupStack
{// Bracket stack 48bits, Bracket store (sbyte 8bits), Depth (byte 8bits)
    public const int MaxDepth = 48;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowIndentationDepthException()
    {
        throw new InvalidOperationException("The maximum indentation depth has been reached.");
    }

    [FieldOffset(0)]
    private byte depth;

    [FieldOffset(1)]
    private sbyte bracketStore;

    [FieldOffset(0)]
    private ulong stack16;

    // public byte Depth => this.depth;

    public bool IsCurrentIndented => (this.stack16 & this.CurrentStackMask) == 0;

    public bool IsPreviousIndented => (this.stack16 & this.PreviousStackMask) == 0;

    private ulong CurrentStackMask => 1UL << (16 + this.depth);

    private ulong PreviousStackMask => 1UL << (15 + this.depth);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TinyhandAtomType GetGroup()
    {
        if (this.bracketStore == 0)
        {
            return TinyhandAtomType.None;
        }
        else if (this.bracketStore > 0)
        {// {
            this.bracketStore--;
            return TinyhandAtomType.StartGroup;
        }
        else
        {// }
            this.bracketStore++;
            return TinyhandAtomType.EndGroup;
        }
    }

    public void AddIndent()
    {
        if (this.bracketStore != 0)
        {
            throw new InvalidOperationException("The bracket store is not empty.");
        }

        this.IncrementDepth(false);
        this.bracketStore = 1;
    }

    public void AddOpenBracket()
    {
        if (this.bracketStore != 0)
        {
            throw new InvalidOperationException("The bracket store is not empty.");
        }

        this.IncrementDepth(true);
        this.bracketStore = 1;
    }

    public void AddCloseBracket()
    {
        if (this.depth == 0)
        {
            throw new TinyhandException("The group stack is empty.");
        }

        if (this.bracketStore != 0)
        {
            throw new InvalidOperationException("The bracket store is not empty.");
        }

        this.bracketStore = -1;
        while (this.depth > 0)
        {
            this.depth--;
            if (this.IsCurrentIndented)
            {// Indent
                this.bracketStore--;
                continue;
            }
            else
            {// {}
                break;
            }
        }
    }

    public void TerminateIndent()
    {
        if (this.depth == 0)
        {
            return;
        }

        if (this.bracketStore != 0)
        {
            throw new InvalidOperationException("The bracket store is not empty.");
        }

        while (this.depth > 0)
        {
            this.depth--;
            if (this.IsCurrentIndented)
            {// Indent
                this.bracketStore--;
                continue;
            }
            else
            {
                break;
            }
        }
    }

    public string? TrySetIndent(int indent)
    {
        if ((indent & 1) != 0)
        {
            return "The indent must be even.";
        }

        var currentIndent = this.depth * 2;
        if (indent == currentIndent)
        {
            return default;
        }

        var dif = (indent - currentIndent) >> 1;
        if (dif > 0)
        {
            for (var i = 0; i < dif; i++)
            {
                this.IncrementDepth(false);
                this.bracketStore++;
            }
        }
        else
        {
            for (var i = 0; i < -dif && this.depth > 0; i++)
            {
                if (this.IsPreviousIndented)
                {// Indent
                    this.depth--;
                    this.bracketStore--;
                    continue;
                }
                else
                {
                    break;
                }
            }

            if (indent < (this.depth * 2))
            {
                return "The indent must be greater than the current depth.";
            }
        }

        return default;
    }

    public override string ToString()
        => $"Depth: {this.depth}, Store: {this.bracketStore}";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void IncrementDepth(bool bracket)
    {
        if (this.depth >= MaxDepth)
        {
            ThrowIndentationDepthException();
        }

        if (bracket)
        {
            this.stack16 |= this.CurrentStackMask;
        }
        else
        {
            this.stack16 &= ~this.CurrentStackMask;
        }

        this.depth++;
    }
}
