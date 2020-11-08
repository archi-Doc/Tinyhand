// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Arc.Crypto;
using Tinyhand.Tree;

#pragma warning disable SA1011 // Closing square brackets should be spaced correctly
#pragma warning disable SA1201 // Elements should appear in the correct order
#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
#pragma warning disable SA1401 // Fields should be private
#pragma warning disable SA1602 // Enumeration items should be documented
#pragma warning disable SA1649 // File name should match first type name

namespace Tinyhand
{
    public static class TinyhandParser
    {
        internal class ParserCore
        {
            public ParserCore()
            {
                this.ElementStack.Push(this.Root);
            }

            public Stack<Group> ElementStack { get; set; } = new Stack<Group>();

            public Stack<Element?> PreviousElementStack { get; set; } = new Stack<Element?>();

            public Group Root { get; set; } = new Group();

            public Element Parse(ref TinyhandUtf8Reader reader, bool parseContextualInformation = false)
            {
                Element? currentElement = null;
                Element? previousElement = null;
                var contextualElement = this.Root.forwardContextual; // {

                while (true)
                {
                    currentElement = this.ReadElement(ref reader, parseContextualInformation);
                    if (parseContextualInformation)
                    { // Parse Comment/LineFeed.
                        if (currentElement != null)
                        {
                            if (currentElement.Type == ElementType.Comment || currentElement.Type == ElementType.LineFeed)
                            { // Comment or LineFeed
                              // Add to the previous element's contextual chain.
                                contextualElement.contextualChain = currentElement;
                                contextualElement = currentElement;
                                currentElement = null;
                            }
                            else
                            { // Other
                                contextualElement = currentElement;
                            }
                        }
                    }

                    if (reader.AtomType == TinyhandAtomType.None)
                    { // No data is left.
                        break;
                    }
                    else if (reader.AtomType == TinyhandAtomType.EndGroup)
                    { // }
                        // Previous Element
                        ProcessPreviousElement();

                        if (this.PreviousElementStack.Count == 0)
                        {
                            reader.ThrowException("Close brace without a matching open brace.");
                        }

                        previousElement = this.PreviousElementStack.Pop();
                        currentElement = this.ElementStack.Pop();
                        contextualElement = currentElement;

                        // Previous Element
                        ProcessPreviousElement();

                        continue;
                    }
                    else if (currentElement == null)
                    { // Separator, Comment, LineFeed
                        if (reader.AtomType == TinyhandAtomType.Separator)
                        {
                            // Previous Element
                            ProcessPreviousElement();
                        }

                        continue;
                    }

                    currentElement.LineNumber = reader.AtomLineNumber;
                    currentElement.BytePositionInLine = reader.AtomBytePositionInLine;

                    if (reader.AtomType == TinyhandAtomType.StartGroup && currentElement is Group currentGroup)
                    { // {
                        this.PreviousElementStack.Push(previousElement);

                        previousElement = null;
                        currentElement = null;
                        contextualElement = currentGroup.forwardContextual;
                        this.ElementStack.Push(currentGroup);

                        continue;
                    }

                    if (reader.AtomType == TinyhandAtomType.Assignment && currentElement is Assignment currentAssignment)
                    { // =, If the current element is TinyhandAssignment, set LeftElement.
                        if (previousElement != null)
                        {
                            currentAssignment.LeftElement = previousElement;
                        }
                    }

                    // Previous Element
                    ProcessPreviousElement();
                }

                // Previous Element
                ProcessPreviousElement();

                if (this.ElementStack.Count != 1)
                {
                    reader.ThrowException("Group depth should be 0 at the end of the data.");
                }

                var root = this.ElementStack.Peek();
                if (root.ElementList.Count == 1 && root.ElementList[0].Type == ElementType.Group)
                {
                    root = (Group)root.ElementList[0];
                }

                return root;

                void ProcessPreviousElement()
                {
                    var previousAssignment = previousElement as Assignment;

                    if (previousAssignment != null)
                    { // If the previous element is TinyhandAssignment, set RightElement.
                        if (currentElement is Assignment)
                        {
                            if (previousAssignment.RightElement == null)
                            { // Double equal (= =) is not supported.
                                throw new Exception("Invalid Tinyhand syntax: double equals (==).");
                            }
                            else
                            {
                                previousElement = currentElement;
                                return;
                            }
                        }

                        if (previousAssignment.RightElement == null)
                        {
                            if (currentElement != null)
                            {
                                previousAssignment.RightElement = currentElement;
                                return;
                            }
                        }
                        else
                        { // RightElement is already set.
                        }
                    }

                    if (previousElement != null && previousElement.Parent == null)
                    { // Add the previous element to the upper group.
                        var group = this.ElementStack.Peek();
                        group.Add(previousElement);
                    }

                    // Update previous element.
                    previousElement = currentElement;
                }
            }

            internal Element? ReadElement(ref TinyhandUtf8Reader reader, bool parseContextualInformation)
            {
                List<Modifier>? modifierList = null;

ReadElementLoop:
                reader.Read();
                switch (reader.AtomType)
                {
                    case TinyhandAtomType.None: // No atom
                    case TinyhandAtomType.Separator: // Separator
                        return null;

                    case TinyhandAtomType.LineFeed:
                        return parseContextualInformation ? new LineFeed() : null;

                    case TinyhandAtomType.StartGroup: // {
                        return new Group();
                    case TinyhandAtomType.EndGroup: // }
                        return null;

                    case TinyhandAtomType.Identifier: // objectA
                    case TinyhandAtomType.SpecialIdentifier: // @mode
                        var identifier = new Value_Identifier(reader.AtomType == TinyhandAtomType.SpecialIdentifier, reader.ValueSpan.ToArray());
                        return identifier;

                    case TinyhandAtomType.Modifier: // i32: key(1): required
                        if (modifierList == null)
                        {
                            modifierList = new List<Modifier>();
                        }

                        modifierList.Add(new Modifier());
                        goto ReadElementLoop;

                    case TinyhandAtomType.Assignment: // =
                        var assignment = new Assignment();
                        return assignment;

                    case TinyhandAtomType.Comment: // // comment
                        return parseContextualInformation ? new Comment(reader.ValueSpan.ToArray()) : null;

                    case TinyhandAtomType.Value_Base64: // b"Base64"
                        var valueBinary = new Value_Binary(reader.ValueBinary);
                        return valueBinary;

                    case TinyhandAtomType.Value_String: // "text"
                        var valueString = new Value_String(reader.ValueSpan.ToArray());
                        valueString.IsTripleQuoted = reader.ValueLong != 0;
                        return valueString;

                    case TinyhandAtomType.Value_Long: // 123(long)
                        var valueLong = new Value_Long();
                        valueLong.ValueLong = reader.ValueLong;
                        return valueLong;

                    case TinyhandAtomType.Value_Double: // 1.23(double)
                        var valueDouble = new Value_Double();
                        valueDouble.ValueDouble = reader.ValueDouble;
                        return valueDouble;

                    case TinyhandAtomType.Value_Null: // null
                        return new Value_Null();

                    case TinyhandAtomType.Value_True: // true
                        var valueTrue = new Value_Bool();
                        valueTrue.ValueBool = true;
                        return valueTrue;

                    case TinyhandAtomType.Value_False: // false
                        var valueFalse = new Value_Bool();
                        valueFalse.ValueBool = false;
                        return valueFalse;

                    default:
                        return null;
                }
            }
        }

        public static Element ParseFile(string fileName, bool parseContextualInformation = false)
        {
            using var fs = new FileStream(fileName, FileMode.Open);
            var length = fs.Length;
            var buffer = new byte[length];
            fs.Read(buffer.AsSpan());

            return Parse(buffer, parseContextualInformation);
        }

        public static async Task<Element> ParseFileAsync(string fileName, bool parseContextualInformation = false)
        {
            using var fs = new FileStream(fileName, FileMode.Open);
            var length = fs.Length;
            var buffer = new byte[length];
            await fs.ReadAsync(buffer.AsMemory());

            return Parse(buffer, parseContextualInformation);
        }

        public static Element Parse(ReadOnlySpan<byte> utf8, bool parseContextualInformation = false)
        {
            var reader = new TinyhandUtf8Reader(utf8, true);
            var core = new ParserCore();

            return core.Parse(ref reader, parseContextualInformation);
        }

        public static Element Parse(Stream stream, bool parseContextualInformation = false)
        {
            Element result;

            if (stream is MemoryStream ms && ms.TryGetBuffer(out ArraySegment<byte> streamBuffer))
            {// MemoryStream
                var span = streamBuffer.AsSpan(checked((int)ms.Position));
                ms.Seek(span.Length, SeekOrigin.Current);
                result = Parse(span, parseContextualInformation);
            }
            else
            {// Other
                var buffer = ArrayPool<byte>.Shared.Rent(checked((int)(stream.Length - stream.Position)));
                var span = buffer.AsSpan();
                var readBytes = stream.Read(span);
                result = Parse(span.Slice(0, readBytes), parseContextualInformation);
                ArrayPool<byte>.Shared.Return(buffer);
            }

            return result;
        }

        public static Element Parse(string text, bool parseContextualInformation = false)
        {
            const long ArrayPoolMaxSizeBeforeUsingNormalAlloc = 1024 * 1024;

            byte[]? tempArray = null;

            // For performance, avoid obtaining actual byte count unless memory usage is higher than the threshold.
            Span<byte> utf8 = text.Length <= (ArrayPoolMaxSizeBeforeUsingNormalAlloc / TinyhandConstants.MaxExpansionFactorWhileTranscoding) ?
                // Use a pooled alloc.
                tempArray = ArrayPool<byte>.Shared.Rent(text.Length * TinyhandConstants.MaxExpansionFactorWhileTranscoding) :
                // Use a normal alloc since the pool would create a normal alloc anyway based on the threshold (per current implementation)
                // and by using a normal alloc we can avoid the Clear().
                new byte[TinyhandHelper.GetUtf8ByteCount(text.AsSpan())];

            try
            {
                int actualByteCount = TinyhandHelper.GetUtf8FromText(text.AsSpan(), utf8);
                utf8 = utf8.Slice(0, actualByteCount);

                return Parse(utf8, parseContextualInformation);
            }
            finally
            {
                if (tempArray != null)
                {
                    utf8.Clear();
                    ArrayPool<byte>.Shared.Return(tempArray);
                }
            }
        }
    }
}
