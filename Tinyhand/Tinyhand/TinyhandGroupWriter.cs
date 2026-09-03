// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Arc.IO;

#pragma warning disable SA1011 // Closing square brackets should be spaced correctly
#pragma warning disable SA1201 // Elements should appear in the correct order
#pragma warning disable SA1202
#pragma warning disable SA1401 // Fields should be private

namespace Tinyhand;

/// <summary>
/// Decides how group brackets are laid out when a binary is converted to text.<br/>
/// In the indented modes (<see cref="TinyhandComposeOption.Standard"/> and <see cref="TinyhandComposeOption.UseContextualInformation"/>)
/// the brackets are not written immediately: a run of closing brackets followed by a run of opening brackets is
/// accumulated and rendered as a line feed, an indent and "+ " markers when the next value is written.<br/>
/// A run of opening brackets can never be followed by a closing bracket without a value in between
/// (an empty group is written as "{}" directly), so the pending state is fully described by
/// the number of pending closes, the number of pending opens that follow them, and a pending line feed.
/// </summary>
public ref struct TinyhandGroupWriter
{
    /// <summary>
    /// The maximum number of bytes <see cref="FlushCore"/> can write: a line feed, an indent, "+ " markers, and another line feed with an indent.
    /// </summary>
    internal const int MaxFlushLength = 1 + (MaxIndent * 2) + (MaxIndent * 2) + 1 + (MaxIndent * 2);

    private const int MaxIndent = TinyhandGroupStack.MaxDepth;

    public readonly TinyhandComposeOption ComposeOption;

    private readonly bool enableIndent;
    private int indents;
    private int closes;
    private int opens;
    private int lfCount;

    public TinyhandGroupWriter(TinyhandComposeOption composeOption)
    {
        this.ComposeOption = composeOption;
        this.enableIndent = composeOption == TinyhandComposeOption.Standard || composeOption == TinyhandComposeOption.UseContextualInformation;
    }

    public bool EnableIndent => this.enableIndent;

    public int Indents => this.indents;

    /// <summary>
    /// Gets a value indicating whether <see cref="Flush(ref TinyhandRawWriter)"/> has something to write.
    /// </summary>
    internal bool HasPending => (this.closes | this.opens | this.lfCount) != 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddLF()
    {
        this.lfCount++;
    }

    /// <summary>
    /// Adds a pending opening bracket (indented modes only).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void AddOpen()
    {
        this.opens++;
    }

    /// <summary>
    /// Adds a pending closing bracket (indented modes only).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void AddClose()
    {
        if (this.opens == 0)
        {
            this.closes++;
        }
        else
        {// An opening bracket directly followed by a closing bracket: an empty group, which the converter never produces.
            this.opens--;
            this.lfCount++;
        }
    }

    public void ProcessStartGroup(ref TinyhandRawWriter writer)
    {
        if (!this.enableIndent)
        {
            writer.WriteUInt8(TinyhandConstants.OpenBrace);
            return;
        }

        this.opens++;
    }

    public void ProcessEndGroup(ref TinyhandRawWriter writer)
    {
        if (!this.enableIndent)
        {
            writer.WriteUInt8(TinyhandConstants.CloseBrace);
            return;
        }

        if (this.opens == 0)
        {
            this.closes++;
        }
        else
        {// An opening bracket directly followed by a closing bracket: an empty group, which the converter never produces.
            this.opens--;
            this.Flush(ref writer);
            writer.WriteUInt16(TinyhandConstants.OpenCloseBrace);
        }
    }

    /// <summary>
    /// Writes the pending brackets and line feed.
    /// </summary>
    /// <param name="writer">The writer.</param>
    public void Flush(ref TinyhandRawWriter writer)
    {
        if (!this.HasPending)
        {
            return;
        }

        var span = writer.GetSpan(MaxFlushLength);
        writer.Advance(this.FlushCore(span));
    }

    /// <summary>
    /// Writes the pending brackets and line feed to <paramref name="span"/>, which must be at least <see cref="MaxFlushLength"/> bytes long.
    /// </summary>
    /// <param name="span">The destination.</param>
    /// <returns>The number of bytes written.</returns>
    internal int FlushCore(Span<byte> span)
    {
        var position = 0;
        var opens = this.opens;
        var closes = this.closes;

        if ((opens | closes) != 0)
        {
            span[position++] = TinyhandConstants.LineFeed;
            this.lfCount--;

            if (closes == 0)
            {// {, {{, {{{ -> LF + indent, or LF + indent + "+ "
                this.indents += opens;
                if (opens > 1)
                {
                    position = WriteIndent(span, position, this.indents - 1);
                    span[position++] = TinyhandConstants.Plus;
                    span[position++] = TinyhandConstants.Space;
                }
                else
                {
                    position = WriteIndent(span, position, this.indents);
                }
            }
            else
            {// }}} -> LF + indent, or }}}{{ -> LF + indent + "+ + "
                this.indents -= closes;
                position = WriteIndent(span, position, this.indents);
                if (opens > 0)
                {
                    if (this.indents + opens > MaxIndent)
                    {
                        TinyhandGroupStack.ThrowIndentationDepthException();
                    }

                    for (var i = 0; i < opens; i++)
                    {
                        span[position++] = TinyhandConstants.Plus;
                        span[position++] = TinyhandConstants.Space;
                    }

                    this.indents += opens;
                }
            }

            this.opens = 0;
            this.closes = 0;
        }

        if (this.lfCount > 0)
        {
            span[position++] = TinyhandConstants.LineFeed;
            position = WriteIndent(span, position, this.indents);
        }

        this.lfCount = 0;
        return position;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int WriteIndent(Span<byte> span, int position, int indents)
    {
        if ((uint)indents > MaxIndent)
        {
            TinyhandGroupStack.ThrowIndentationDepthException();
        }

        var length = indents * 2;
        span.Slice(position, length).Fill(TinyhandConstants.Space);
        return position + length;
    }
}
