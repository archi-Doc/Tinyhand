﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using Arc.IO;
using Tinyhand.Tree;

#pragma warning disable SA1513 // Closing brace should be followed by blank line
#pragma warning disable SA1602 // Enumeration items should be documented

namespace Tinyhand;

public enum TinyhandComposeOption
{
    Standard,
    UseContextualInformation,
    Simple,
    Strict,
}

public static class TinyhandComposer
{
    private const int InitialBufferLength = 32 * 1024;

    [ThreadStatic]
    private static byte[]? initialBuffer;

    public static string ComposeToString(Element element, TinyhandComposeOption option = TinyhandComposeOption.Standard)
    {
        return TinyhandHelper.GetTextFromUtf8(Compose(element, option));
    }

    public static byte[] Compose(Element element, TinyhandComposeOption option = TinyhandComposeOption.Standard)
    {
        if (initialBuffer == null)
        {
            initialBuffer = new byte[InitialBufferLength];
        }

        var writer = new TinyhandRawWriter(initialBuffer);
        try
        {
            Compose(ref writer, element, option);
            return writer.FlushAndGetArray();
        }
        finally
        {
            writer.Dispose();
        }
    }

    public static void Compose(IBufferWriter<byte> bufferWriter, Element element, TinyhandComposeOption option = TinyhandComposeOption.Standard)
    {
        var writer = new TinyhandRawWriter(bufferWriter);
        try
        {
            Compose(ref writer, element, option);
            return;
        }
        finally
        {
            writer.Dispose();
        }
    }

    private static void Compose(ref TinyhandRawWriter writer, Element element, TinyhandComposeOption option)
    {
        var core = new ComposerCore(option);
        core.Compose(ref writer, element);
    }

    internal class ComposerCore
    {
        private TinyhandComposeOption option;

        private bool useContextualInformation;

        private int indent;
        private bool firstElement;
        private bool requireIndentation;
        // private bool requireDelimiter;

        public ComposerCore(TinyhandComposeOption option)
        {
            this.option = option;
            if (option == TinyhandComposeOption.UseContextualInformation)
            {
                this.useContextualInformation = true;
            }

            this.indent = 0;
            this.firstElement = true;
        }

        public void Compose(ref TinyhandRawWriter writer, Element element, int contextualIndex = 0)
        {
            switch (element.Type)
            {
                case ElementType.Value:
                    this.ComposeIndent(ref writer);
                    this.ComposeValue(ref writer, (Value)element);
                    break;

                case ElementType.Assignment:
                    this.ComposeAssignment(ref writer, (Assignment)element);
                    contextualIndex = 1; // Already composed in ComposeAssignment()
                    break;

                case ElementType.Group:
                    var g = (Group)element;
                    this.ComposeGroup(ref writer, g);
                    break;

                case ElementType.Modifier:
                    this.ComposeModifier(ref writer, (Modifier)element);
                    break;

                case ElementType.LineFeed:
                    if (this.useContextualInformation)
                    {
                        writer.WriteLF();
                        // this.requireDelimiter = false;
                    }
                    break;

                case ElementType.Comment:
                    if (this.useContextualInformation)
                    {
                        if (contextualIndex == 1 && !this.firstElement)
                        {
                            writer.WriteUInt8(TinyhandConstants.Space);
                        }
                        writer.WriteSpan(((Comment)element).CommentUtf8);
                        if (element.contextualChain?.Type != ElementType.LineFeed)
                        {
                            writer.WriteUInt8(TinyhandConstants.Space);
                        }
                    }
                    break;
            }

            this.firstElement = false;

            if (this.useContextualInformation && contextualIndex == 0)
            {
                this.ComposeContextualInformation(ref writer, element.contextualChain);
            }
        }

        private void ComposeContextualInformation(ref TinyhandRawWriter writer, Element? element)
        {
            var contextualNumber = 1;

            while (element != null)
            {
                this.Compose(ref writer, element, contextualNumber++);
                element = element.contextualChain;
            }
        }

        private void NewLine(ref TinyhandRawWriter writer, int indent = 0)
        {
            /*if (this.useContextualInformation && !this.requireDelimiter)
            {
                return;
            }*/

            if (!this.useContextualInformation)
            {
                writer.WriteLF();
            }

            this.requireIndentation = true;
            this.indent += indent;
        }

        private void ComposeIndent(ref TinyhandRawWriter writer)
        {
            if (this.requireIndentation)
            {
                this.requireIndentation = false;
                for (var i = 0; i < this.indent; i++)
                {
                    writer.WriteSpan(TinyhandConstants.IndentSpan);
                }
            }
        }

        private void ComposeModifier(ref TinyhandRawWriter writer, Modifier element)
        {
            writer.WriteUInt8(TinyhandConstants.ModifierPrefix);
            writer.WriteSpan(element.Utf8);
        }

        private void ComposeValue(ref TinyhandRawWriter writer, Value element)
        {
            switch (element.ValueType)
            {
                case ValueElementType.Identifier:
                case ValueElementType.SpecialIdentifier:
                    var i = (Value_Identifier)element;
                    if (i.IsSpecial)
                    {
                        writer.WriteUInt8(TinyhandConstants.IdentifierPrefix);
                    }
                    writer.WriteSpan(i.Utf8);
                    break;

                case ValueElementType.Value_Binary:
                    var binary = (Value_Binary)element;
                    writer.WriteUInt8((byte)'b');
                    writer.WriteUInt8(TinyhandConstants.Quote);
                    writer.WriteSpan(binary.ValueBinaryToBase64);
                    writer.WriteUInt8(TinyhandConstants.Quote);
                    break;

                case ValueElementType.Value_String:
                    var s = (Value_String)element;
                    if (!s.IsTripleQuoted || s.HasTripleQuote())
                    { // Escape.
                        writer.WriteUInt8(TinyhandConstants.Quote);
                        writer.WriteEscapedUtf8(s.Utf8);
                        writer.WriteUInt8(TinyhandConstants.Quote);
                    }
                    else
                    { // """string"""
                        writer.WriteSpan(TinyhandConstants.TripleQuotesSpan);
                        writer.WriteSpan(s.Utf8);
                        writer.WriteSpan(TinyhandConstants.TripleQuotesSpan);
                    }
                    break;

                case ValueElementType.Value_Long:
                    var l = (Value_Long)element;
                    writer.WriteStringInt64(l.ValueLong);
                    break;

                case ValueElementType.Value_ULong:
                    var ul = (Value_ULong)element;
                    writer.WriteStringUInt64(ul.ValueULong);
                    break;

                case ValueElementType.Value_Double:
                    var d = (Value_Double)element;
                    writer.WriteStringDouble(d.ValueDouble);
                    // writer.WriteUInt8(TinyhandConstants.DoubleSuffix);
                    break;

                case ValueElementType.Value_Null:
                    writer.WriteSpan(TinyhandConstants.NullSpan);
                    break;

                case ValueElementType.Value_Bool:
                    var b = (Value_Bool)element;
                    if (b.ValueBool)
                    {
                        writer.WriteSpan(TinyhandConstants.TrueSpan);
                    }
                    else
                    {
                        writer.WriteSpan(TinyhandConstants.FalseSpan);
                    }
                    break;
            }
        }

        private void ComposeAssignment(ref TinyhandRawWriter writer, Assignment element)
        {
            if (element.LeftElement != null)
            {
                this.Compose(ref writer, element.LeftElement);
            }

            // writer.WriteSpan(TinyhandConstants.AssignmentSpan);
            writer.WriteUInt8(TinyhandConstants.EqualsSign);

            // this.requireDelimiter = true;
            this.ComposeContextualInformation(ref writer, element.contextualChain);

            if (element.RightElement != null)
            {
                // this.requireDelimiter = true;
                this.Compose(ref writer, element.RightElement);
            }
        }

        private void ComposeGroup(ref TinyhandRawWriter writer, Group element)
        {
            var newLine = true;
            if (element.Parent == null ||
                this.option == TinyhandComposeOption.Simple)
            {
                newLine = false;
            }

            if (element.ElementList.Count == 0)
            {
                this.ComposeIndent(ref writer);
                writer.WriteUInt8(TinyhandConstants.OpenBrace);
                writer.WriteUInt8(TinyhandConstants.CloseBrace);
            }

            this.ComposeContextualInformation(ref writer, element.forwardContextual?.contextualChain);

            if (newLine)
            {
                this.NewLine(ref writer, 1);
            }

            var hasAssignment = false;
            if (element.ElementList.Count > 0 && element.ElementList[0] is Assignment)
            {
                hasAssignment = true;
            }

            for (var i = 0; i < element.ElementList.Count; i++)
            {
                this.Compose(ref writer, element.ElementList[i]);
                if (i == element.ElementList.Count - 1)
                {
                    // writer.WriteLF();
                    break;
                }

                if (!hasAssignment || this.option == TinyhandComposeOption.Simple)
                {
                    writer.WriteUInt16(0x2C20); // ", "
                }
                else
                {
                    this.NewLine(ref writer);
                }
            }

            if (newLine)
            {
                this.NewLine(ref writer, -1);
            }
        }

        /*private void ComposeGroup(ref TinyhandRawWriter writer, Group element)
        {
            var addBrace = true;
            if (element.Parent == null &&
                (this.option == TinyhandComposeOption.Simple ||
                this.option == TinyhandComposeOption.UseContextualInformation))
            {
                addBrace = false;
            }

            var newLine = true;
            if (this.option == TinyhandComposeOption.Simple ||
                element.ElementList.Count == 0)
            {
                newLine = false;
            }

            if (addBrace)
            {
                writer.WriteUInt8(TinyhandConstants.OpenBrace);
            }

            this.ComposeContextualInformation(ref writer, element.forwardContextual?.contextualChain);

            if (addBrace && newLine)
            {
                this.NewLine(ref writer, 1);
            }

            var hasAssignment = false;
            if (element.ElementList.Count > 0 && element.ElementList[0] is Assignment)
            {
                hasAssignment = true;
            }

            for (var i = 0; i < element.ElementList.Count; i++)
            {
                this.Compose(ref writer, element.ElementList[i]);
                if (i == element.ElementList.Count - 1)
                {
                    break;
                }

                if (!hasAssignment || this.option == TinyhandComposeOption.Simple)
                {
                    writer.WriteUInt16(0x2C20); // ", "
                }
                else
                {
                    this.NewLine(ref writer);
                }
            }

            if (addBrace)
            {
                if (newLine)
                {
                    this.NewLine(ref writer, -1);
                }

                writer.WriteUInt8(TinyhandConstants.CloseBrace);
            }
        }*/
    }
}
