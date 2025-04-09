// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;

namespace Tinyhand;

internal static class TinyhandGroupStack
{// Bracket stack 40bits, Bracket store (sbyte 8bits), Depth (byte 8bits), Current indent (byte 8bits)
    public const int MaxDepth = 40;
    public const byte InvalidIndent = 0xFF;

    public static TinyhandAtomType GetGroup(ref ulong groupStack)
    {
        var store = GetBracketStore(groupStack);
        if (store == 0)
        {
            return TinyhandAtomType.None;
        }
        else if (store > 0)
        {// {
            groupStack = (groupStack & ~0xFFFF_FFFF_FF00_FFFFUL) | ((ulong)(store - 1) << 16);
            return TinyhandAtomType.StartGroup;
        }
        else
        {// }
            groupStack = (groupStack & ~0xFFFF_FFFF_FF00_FFFFUL) | ((ulong)(store + 1) << 16);
            return TinyhandAtomType.EndGroup;
        }
    }

    public static void AddOpenBracket(ref ulong groupStack)
    {
        var depth = GetDepth(groupStack);
        if (depth >= MaxDepth)
        {
            throw new TinyhandException("The maximum depth of the group stack has been reached.");
        }

        var store = GetBracketStore(groupStack);
        if (store != 0)
        {
            throw new TinyhandException("The bracket store is not empty.");
        }

        var stackMask = BracketStackMask(depth);
        var mask = ~(0xFF_FFFFUL | stackMask);
        groupStack = (groupStack & mask) | BracketStackMask(depth) | (1 << 16) | ((ulong)(depth + 1) << 8) | InvalidIndent;
    }

    public static void AddCloseBracket(ref ulong groupStack)
    {
        var depth = GetDepth(groupStack);
        if (depth == 0)
        {
            throw new TinyhandException("The group stack is empty.");
        }

        var store = GetBracketStore(groupStack);
        if (store != 0)
        {
            throw new TinyhandException("The bracket store is not empty.");
        }

        store = -1;
        while (depth-- > 0)
        {
            var stackMask = BracketStackMask(depth);
            if ((groupStack & stackMask) == 0)
            {// Indent
                store--;
                continue;
            }
            else
            {// {}
                break;
            }
        }

        groupStack = (groupStack & ~0xFF_FFFFUL) | ((ulong)store << 16) | ((ulong)depth << 8) | InvalidIndent;
    }

    public static void SetIndent(ref ulong groupStack, int indent)
    {
        if ((indent & 1) != 0)
        {
            throw new TinyhandException("The indent must be even.");
        }

        var currentIndent = GetCurrentIndent(groupStack);
        if (currentIndent == InvalidIndent)
        {// Set new indent
            SetCurrentIndent(ref groupStack, (byte)indent);
            return;
        }

        if (indent == currentIndent)
        {
            return;
        }

        var depth = GetDepth(groupStack);
        var dif = (indent - currentIndent) >> 1;
        if (dif > 0)
        {
            for (var i = 0; i < dif; i++)
            {
                groupStack &= ~BracketStackMask(depth);
                depth++;
                if (depth >= MaxDepth)
                {
                    throw new TinyhandException("The maximum depth of the group stack has been reached.");
                }
            }
        }
        else
        {
            for (var i = 0; i < -dif && depth > 0; i++)
            {
                if ((groupStack & BracketStackMask(depth)) == 0)
                {// Indent
                    depth--;
                    continue;
                }
                else
                {
                    break;
                }
            }
        }

        groupStack = (groupStack & ~0xFF_FFFFUL) | ((ulong)dif << 16) | (ulong)depth << 8 | ((uint)indent);
    }

    public static string ToString(ulong groupStack)
    {
        var depth = GetDepth(groupStack);
        var store = GetBracketStore(groupStack);
        var currentIndent = GetCurrentIndent(groupStack);
        // var stack = groupStack >> 24;
        return $"CurrentIndent: {currentIndent}, Depth: {depth}, Store: {store}";
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SetCurrentIndent(ref ulong groupStack, byte currentIndent)
    {
        groupStack = (groupStack & ~0xFFUL) | currentIndent;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte GetCurrentIndent(ulong groupStack)
        => (byte)(groupStack & 0xFF);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte GetDepth(ulong groupStack)
        => (byte)(groupStack >> 8 & 0xFF);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static sbyte GetBracketStore(ulong groupStack)
        => (sbyte)(groupStack >> 16);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong BracketStackMask(byte depth)
        => 1UL << (depth + 24);
}
