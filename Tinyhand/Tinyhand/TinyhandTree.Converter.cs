﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Arc.IO;
using Tinyhand.IO;
using Tinyhand.Tree;

#pragma warning disable SA1011 // Closing square brackets should be spaced correctly
#pragma warning disable SA1201 // Elements should appear in the correct order
#pragma warning disable SA1202
#pragma warning disable SA1401 // Fields should be private

namespace Tinyhand;

public static class TinyhandTreeConverter
{
    private const int InitialBufferSize = 32 * 1024;
    private const int MaxIndentBuffer = TinyhandGroupStack.MaxDepth;

    /// <summary>
    /// A thread-local, recyclable array that may be used for short bursts of code.
    /// </summary>
    [ThreadStatic]
    private static byte[]? initialBuffer;

    private static byte[] indentSpaces;

    static TinyhandTreeConverter()
    {
        indentSpaces = Enumerable.Repeat<byte>(TinyhandConstants.Space, MaxIndentBuffer * 2).ToArray();
    }

    /// <summary>
    /// Converts a sequence of byte to UTF-8 text.
    /// </summary>
    /// <param name="span">A byte span to convert.</param>
    /// <param name="writer">TinyhandRawWriter.</param>
    /// <param name="options">The options. Use <c>null</c> to use default options.</param>
    /// <param name="omitTopLevelBracket"><see langword="true"/> to omit the top level bracket.</param>
    public static void FromBinaryToUtf8(ReadOnlySpan<byte> span, ref TinyhandRawWriter writer, TinyhandSerializerOptions? options, bool omitTopLevelBracket = false)
    {
        options ??= TinyhandSerializerOptions.ConvertToString;

        var reader = new TinyhandReader(span);
        var byteSequence = new ByteSequence();
        try
        {
            var groupWriter = new TinyhandGroupWriter(options.Compose);
            if (TinyhandSerializer.TryDecompress(ref reader, byteSequence))
            {
                var r = reader.Clone(byteSequence.ToReadOnlySpan());
                while (!r.End)
                {
                    FromReaderToUtf8(ref r, ref writer, ref groupWriter, omitTopLevelBracket); // r.ConvertToUtf8(ref writer);
                    groupWriter.Flush(ref writer);
                }
            }
            else
            {
                while (!reader.End)
                {
                    FromReaderToUtf8(ref reader, ref writer, ref groupWriter, omitTopLevelBracket); // reader.ConvertToUtf8(ref writer);
                    groupWriter.Flush(ref writer);
                }
            }
        }
        finally
        {
            byteSequence.Dispose();
        }
    }

    /// <summary>
    /// Converts a sequence of byte to an Element using TinyhandReader.
    /// </summary>
    /// <param name="reader">TinyhandReader which has a sequence of byte.</param>
    /// <param name="writer">TinyhandRawWriter.</param>
    /// <param name="groupWriter">TinyhandGroupWriter.</param>
    /// <param name="omitTopLevelBracket"><see langword="true"/> to omit the top level bracket.</param>
    /// <param name="convertToIdentifier">Convert a string to an identifier if possible.</param>
    /// <returns>Returns <see langword="true"/> if the content is primitive type; otherwise, <see langword="false"/>.</returns>
    public static bool FromReaderToUtf8(scoped ref TinyhandReader reader, ref TinyhandRawWriter writer, scoped ref TinyhandGroupWriter groupWriter, bool omitTopLevelBracket = false, bool convertToIdentifier = false)
    {
        var type = reader.NextMessagePackType;
        switch (type)
        {
            case MessagePackType.Integer:
                groupWriter.Flush(ref writer);
                if (MessagePackCode.IsSignedInteger(reader.NextCode))
                {
                    writer.WriteStringInt64(reader.ReadInt64());
                }
                else
                {
                    writer.WriteStringUInt64(reader.ReadUInt64());
                }

                return true;

            case MessagePackType.Boolean:
                groupWriter.Flush(ref writer);
                if (reader.ReadBoolean())
                {
                    writer.WriteSpan(TinyhandConstants.TrueSpan);
                }
                else
                {
                    writer.WriteSpan(TinyhandConstants.FalseSpan);
                }

                return true;

            case MessagePackType.Float:
                groupWriter.Flush(ref writer);
                if (reader.NextCode == MessagePackCode.Float32)
                {
                    writer.WriteStringSingle(reader.ReadSingle());
                }
                else
                {
                    writer.WriteStringDouble(reader.ReadDouble());
                }

                return true;

            case MessagePackType.String:
                groupWriter.Flush(ref writer);
                var span = reader.ReadStringSpan();
                var utf8 = span.ToArray();

                if (convertToIdentifier && IsValidIdentifier(utf8))
                {
                    writer.WriteSpan(utf8);
                }
                else
                {
                    writer.WriteUInt8(TinyhandConstants.Quote);
                    writer.WriteEscapedUtf8(utf8);
                    writer.WriteUInt8(TinyhandConstants.Quote);
                }

                return true;

            case MessagePackType.Binary:
                groupWriter.Flush(ref writer);
                writer.WriteUInt8((byte)'b');
                writer.WriteUInt8(TinyhandConstants.Quote);
                // writer.WriteSpan(Base64.EncodeToBase64Utf8(bytes.Value.ToArray()));
                writer.WriteSpan(Arc.Crypto.Base64.Url.FromByteArrayToUtf8(reader.ReadBytesToArray()));
                writer.WriteUInt8(TinyhandConstants.Quote);

                return true;

            case MessagePackType.Array:
                {
                    int length = reader.ReadArrayHeader();
                    if (!omitTopLevelBracket)
                    {
                        if (length == 0)
                        { // {}
                            groupWriter.Flush(ref writer);
                            writer.WriteUInt16(TinyhandConstants.OpenCloseBrace);
                            // groupWriter.AddLF();
                            return false;
                        }

                        groupWriter.ProcessStartGroup(ref writer);
                    }

                    for (int i = 0; i < length; i++)
                    {
                        var isPrimitive = FromReaderToUtf8(ref reader, ref writer, ref groupWriter, false);
                        if (i != (length - 1))
                        {
                            if (!groupWriter.EnableIndent || isPrimitive)
                            {
                                writer.WriteUInt16(0x2C20); // ", "
                            }
                            else
                            {
                                // writer.WriteUInt16(0x2C20); // ", "
                                groupWriter.AddLF();
                            }
                        }
                    }

                    if (!omitTopLevelBracket)
                    {
                        groupWriter.ProcessEndGroup(ref writer);
                    }
                }

                return false;

            case MessagePackType.Map:
                {
                    int length = reader.ReadMapHeader();

                    if (!omitTopLevelBracket)
                    {
                        if (length == 0)
                        { // {}
                            groupWriter.Flush(ref writer);
                            writer.WriteUInt16(TinyhandConstants.OpenCloseBrace);
                            // groupWriter.AddLF();
                            return false;
                        }

                        groupWriter.ProcessStartGroup(ref writer);
                    }

                    for (int i = 0; i < length; i++)
                    {
                        FromReaderToUtf8(ref reader, ref writer, ref groupWriter, false, groupWriter.ComposeOption != TinyhandComposeOption.Simple);

                        /*if (groupWriter.ComposeOption != TinyhandComposeOption.Simple)
                        {
                            writer.WriteSpan(TinyhandConstants.AssignmentSpan);
                        }
                        else*/
                        {
                            writer.WriteUInt8(TinyhandConstants.EqualsSign);
                        }

                        FromReaderToUtf8(ref reader, ref writer, ref groupWriter, false);

                        if (i != (length - 1))
                        {
                            if (groupWriter.ComposeOption == TinyhandComposeOption.Simple)
                            {
                                writer.WriteUInt16(0x2C20); // ", "
                            }
                            else
                            {// Next line + indent
                                groupWriter.AddLF();
                                // writer.WriteLF();
                                // writer.WriteSpan(GetIndentSpan(groupWriter.Indents));
                            }
                        }
                    }

                    if (!omitTopLevelBracket)
                    {
                        groupWriter.ProcessEndGroup(ref writer);
                    }
                }

                return false;

            case MessagePackType.Extension:
                groupWriter.Flush(ref writer);
                var extHeader = reader.ReadExtensionFormatHeader();
                string st;
                if (extHeader.TypeCode == ReservedMessagePackExtensionTypeCode.DateTime)
                {// DateTime
                    var dt = reader.ReadDateTime(extHeader);
                    if (dt.Kind != DateTimeKind.Utc)
                    {
                        dt = dt.ToUniversalTime();
                    }

                    st = dt.ToString("o", CultureInfo.InvariantCulture);
                }
                else if (extHeader.TypeCode == MessagePackExtensionCodes.Identifier)
                {// Identifier
                    var identifier = reader.ReadRaw((int)extHeader.Length);
                    writer.WriteSpan(identifier);
                    return true;
                }
                else
                {
                    var data = reader.ReadRaw((int)extHeader.Length);
                    st = "[" + extHeader.TypeCode + ",\"" + Convert.ToBase64String(data.ToArray()) + "\"]";
                }

                writer.WriteUInt8(TinyhandConstants.Quote);
                writer.WriteEscapedUtf8(Encoding.UTF8.GetBytes(st));
                writer.WriteUInt8(TinyhandConstants.Quote);

                return true;

            case MessagePackType.Nil:
                groupWriter.Flush(ref writer);
                reader.Skip();
                writer.WriteSpan(TinyhandConstants.NullSpan);
                return true;

            default:
                throw new TinyhandException($"code is invalid. code: {reader.NextCode} format: {MessagePackCode.ToFormatName(reader.NextCode)}");
        }
    }

    internal class FromReaderToBinary_State
    {
        public FromReaderToBinary_State(long positionToSearch)
        {
            this.PositionToSearch = positionToSearch;
        }

        public long PositionToSearch { get; }

        public TinyhandUtf8LinePosition Previous;

        public TinyhandUtf8LinePosition Found;
    }

    /// <summary>
    /// Get the Line/BytePosition from binary position.
    /// </summary>
    /// <param name="utf8">UTF-8 text.</param>
    /// <param name="position">The byte position.</param>
    /// <returns>Returns a <see cref="TinyhandUtf8LinePosition"/> representing the line and byte position in the UTF-8 text.</returns>
    public static TinyhandUtf8LinePosition GetTextPositionFromBinaryPosition(ReadOnlySpan<byte> utf8, long position)
    {
        var reader = new TinyhandUtf8Reader(utf8, true);
        var writer = default(TinyhandWriter);
        var state = new FromReaderToBinary_State(position);
        FromReaderToBinary(ref reader, ref writer, out _, state);
        return state.Found;
    }

    /// <summary>
    /// Converts UTF-8 text to a sequence of byte.
    /// </summary>
    /// <param name="utf8">UTF-8 text.</param>
    /// <param name="writer">TinyhandRawWriter.</param>
    /// <param name="omitTopLevelBracket"><see langword="true"/> to omit the top level bracket.</param>
    public static void FromUtf8ToBinary(ReadOnlySpan<byte> utf8, ref TinyhandWriter writer, bool omitTopLevelBracket = false)
    {
        var reader = new TinyhandUtf8Reader(utf8, true);
        var state = new FromReaderToBinary_State(-1);

        if (omitTopLevelBracket)
        {
            var scratchWriter = default(TinyhandWriter);
            try
            {
                var numberOfItems = FromReaderToBinary(ref reader, ref scratchWriter, out var assigned, state);

                if (assigned)
                {
                    writer.WriteMapHeader(numberOfItems / 2);
                    writer.WriteSequence(scratchWriter.FlushAndGetReadOnlySequence());
                }
                else
                {
                    writer.WriteArrayHeader(numberOfItems);
                    writer.WriteSequence(scratchWriter.FlushAndGetReadOnlySequence());
                }
            }
            finally
            {
                scratchWriter.Dispose();
            }
        }
        else
        {
            FromReaderToBinary(ref reader, ref writer, out _, state);
        }
    }

    /// <summary>
    /// Converts TinyhandUtf8Reader (UTF-8 text) to a sequence of byte.
    /// </summary>
    /// <param name="reader">TinyhandUtf8Reader.</param>
    /// <param name="writer">TinyhandRawWriter.</param>
    /// <param name="assignedFlag">Assignment element processed.</param>
    /// <param name="state">State.</param>
    /// <returns>Returns the number of the processed items.</returns>
    internal static uint FromReaderToBinary(ref TinyhandUtf8Reader reader, ref TinyhandWriter writer, out bool assignedFlag, FromReaderToBinary_State state)
    {
        uint assignedCount = 0;
        uint count = 0;

        assignedFlag = false;
        while (reader.Read())
        {
            if (state.PositionToSearch >= 0)
            {
                if (state.Found.LineNumber != 0)
                {// Found
                    return count;
                }

                var position = writer.Written;
                if (position < state.PositionToSearch)
                {
                    state.Previous.LineNumber = reader.AtomLineNumber;
                    state.Previous.BytePositionInLine = reader.AtomBytePositionInLine;
                }
                else
                {
                    state.Found = state.Previous;
                    return count;
                }
            }

            switch (reader.AtomType)
            {
                case TinyhandAtomType.StartGroup: // {
                    var scratchWriter = default(TinyhandWriter);
                    try
                    {
                        var numberOfItems = FromReaderToBinary(ref reader, ref scratchWriter, out var assigned, state);

                        if (assigned)
                        {
                            writer.WriteMapHeader(numberOfItems / 2);
                            writer.WriteSequence(scratchWriter.FlushAndGetReadOnlySequence());
                        }
                        else
                        {
                            writer.WriteArrayHeader(numberOfItems);
                            writer.WriteSequence(scratchWriter.FlushAndGetReadOnlySequence());
                        }
                    }
                    finally
                    {
                        scratchWriter.Dispose();
                    }

                    count++;
                    break;

                case TinyhandAtomType.EndGroup: // }
                    return count;

                case TinyhandAtomType.Identifier: // objectA
                    if (assignedCount <= 1)
                    {
                        writer.WriteString(reader.ValueSpan);
                        count++;
                    }

                    break;

                case TinyhandAtomType.SpecialIdentifier: // @mode
                    if (assignedCount <= 1)
                    {
                        var utf8 = reader.ValueSpan;
                        writer.WriteStringHeader(utf8.Length + 1);
                        writer.WriteRawUInt8(TinyhandConstants.IdentifierPrefix);
                        writer.WriteSpan(utf8);
                        count++;
                    }

                    break;

                case TinyhandAtomType.Modifier: // &i32, &key(1), &required
                    break;

                case TinyhandAtomType.Assignment: // =
                    assignedFlag = true;
                    assignedCount++;
                    break;

                case TinyhandAtomType.Value_Base64: // b"Base64"
                    if (assignedCount <= 1)
                    {
                        writer.Write(reader.ValueBinary);
                        count++;
                    }

                    break;

                case TinyhandAtomType.Value_String: // "text"
                    if (assignedCount <= 1)
                    {
                        writer.WriteString(reader.ValueSpan);
                        count++;
                    }

                    break;

                case TinyhandAtomType.Value_Long: // -123(long)
                    if (assignedCount <= 1)
                    {
                        writer.Write(reader.ValueLong);
                        count++;
                    }

                    break;

                case TinyhandAtomType.Value_ULong: // 123(ulong)
                    if (assignedCount <= 1)
                    {
                        writer.Write(reader.ValueULong);
                        count++;
                    }

                    break;

                case TinyhandAtomType.Value_Double: // 1.23(double)
                    if (assignedCount <= 1)
                    {
                        writer.Write(reader.ValueDouble);
                        count++;
                    }

                    break;

                case TinyhandAtomType.Value_Null: // null
                    if (assignedCount <= 1)
                    {
                        writer.WriteNil();
                        count++;
                    }

                    break;

                case TinyhandAtomType.Value_True: // true
                    if (assignedCount <= 1)
                    {
                        writer.Write(true);
                        count++;
                    }

                    break;

                case TinyhandAtomType.Value_False: // false
                    if (assignedCount <= 1)
                    {
                        writer.Write(false);
                        count++;
                    }

                    break;

                default:
                    assignedCount = 0;
                    break;
            }
        }

        return count;
    }

    /// <summary>
    /// Converts an Element to a sequence of byte.
    /// </summary>
    /// <param name="element">Element to convert.</param>
    /// <param name="byteArray">A byte array converted from an element.</param>
    /// <param name="options">The serialization options.</param>
    public static void FromElementToBinary(Element element, out byte[] byteArray, TinyhandSerializerOptions options)
    {
        if (initialBuffer == null)
        {
            initialBuffer = new byte[InitialBufferSize];
        }

        var w = new TinyhandWriter(initialBuffer);
        try
        {
            var state = new ToBinaryCoreState(options, -1);
            FromElementToBinary_Core(element, ref w, state);
            byteArray = w.FlushAndGetArray();
        }
        finally
        {
            w.Dispose();
        }
    }

    /// <summary>
    /// Get the Element from binary position.
    /// </summary>
    /// <param name="element">Element to search.</param>
    /// <param name="position">The byte position.</param>
    /// <param name="options">The serialization options.</param>
    /// <returns>Element found at position in byte array.</returns>
    public static Element? GetElementFromPosition(Element element, long position, TinyhandSerializerOptions options)
    {
        if (initialBuffer == null)
        {
            initialBuffer = new byte[InitialBufferSize];
        }

        var w = new TinyhandWriter(initialBuffer);
        try
        {
            var state = new ToBinaryCoreState(options, position);
            FromElementToBinary_Core(element, ref w, state);
            return state.ElementFound;
        }
        finally
        {
            w.Dispose();
        }
    }

    internal class ToBinaryCoreState
    {
        public ToBinaryCoreState(TinyhandSerializerOptions options, long positionToSearch)
        {
            this.Options = options;
            this.PositionToSearch = positionToSearch;
        }

        public TinyhandSerializerOptions Options { get; }

        public long PositionToSearch { get; }

        public Element? PreviousElement { get; set; }

        public Element? ElementFound { get; set; }
    }

    private static void FromElementToBinary_Core(Element element, ref TinyhandWriter writer, ToBinaryCoreState state)
    {
        if (state.PositionToSearch >= 0)
        {
            if (state.ElementFound != null)
            {// Found
                return;
            }

            var position = writer.Written;
            if (position < state.PositionToSearch)
            {
                state.PreviousElement = element;
            }
            else
            {
                state.ElementFound = state.PreviousElement;
                return;
            }
        }

        if (element.Type == ElementType.Value)
        {
            Value v = (Value)element;
            switch (v.ValueType)
            {
                case ValueElementType.Value_Binary:
                    writer.Write(((Value_Binary)v).ValueBinary);
                    break;

                case ValueElementType.Value_Bool:
                    writer.Write(((Value_Bool)v).ValueBool);
                    break;

                case ValueElementType.Value_Double:
                    writer.Write(((Value_Double)v).ValueDouble);
                    break;

                case ValueElementType.Value_Long:
                    writer.Write(((Value_Long)v).ValueLong);
                    break;

                case ValueElementType.Value_ULong:
                    writer.Write(((Value_ULong)v).ValueULong);
                    break;

                case ValueElementType.Value_Null:
                    writer.WriteNil();
                    break;

                case ValueElementType.Value_String:
                    writer.WriteString(((Value_String)v).Utf8);
                    break;

                case ValueElementType.Identifier:
                    writer.WriteString(((Value_Identifier)v).Utf8);
                    break;

                case ValueElementType.SpecialIdentifier:
                    var utf8 = ((Value_Identifier)v).Utf8;
                    writer.WriteStringHeader(utf8.Length + 1);
                    writer.WriteRawUInt8(TinyhandConstants.IdentifierPrefix);
                    writer.WriteSpan(utf8);
                    break;
            }
        }
        else if (element.Type == ElementType.Assignment)
        {
            var assignment = (Assignment)element;
            if (assignment.LeftElement == null)
            {
                writer.WriteNil();
            }
            else
            {
                FromElementToBinary_Core(assignment.LeftElement, ref writer, state);
            }

            if (assignment.RightElement == null)
            {
                writer.WriteNil();
            }
            else
            {
                FromElementToBinary_Core(assignment.RightElement, ref writer, state);
            }
        }
        else if (element.Type == ElementType.Group)
        {
            var group = (Group)element;
            var isMap = false;
            for (var i = 0; i < group.ElementList.Count; i++)
            {
                if (group.ElementList[i].Type == ElementType.Assignment)
                {
                    isMap = true;
                    break;
                }
            }

            if (isMap)
            {
                writer.WriteMapHeader(group.ElementList.Count);
                for (var i = 0; i < group.ElementList.Count; i++)
                {
                    if (group.ElementList[i] is Assignment assignment)
                    {
                        FromElementToBinary_Core(assignment, ref writer, state);
                    }
                    else
                    {
                        writer.WriteNil();
                        FromElementToBinary_Core(group.ElementList[i], ref writer, state);
                    }
                }
            }
            else
            {
                writer.WriteArrayHeader(group.ElementList.Count);
                for (var i = 0; i < group.ElementList.Count; i++)
                {
                    FromElementToBinary_Core(group.ElementList[i], ref writer, state);
                }
            }
        }
    }

    private static bool IsValidIdentifier(byte[] s)
    {
        if (s.Length == 0)
        {// Empty
            return false;
        }

        if (TinyhandUtf8Reader.HasDelimiter(s))
        {// Has delimiter
            return false;
        }

        if (s[0] >= '0' && s[0] <= '9')
        {// Number
            return false;
        }

        if (TinyhandHelper.ReservedTable.TryGetValue(s, out _))
        {// Reserved
            return false;
        }

        return true;
    }

    /// <summary>
    /// Converts a sequence of byte to an Element.
    /// </summary>
    /// <param name="byteArray">A byte array to convert.</param>
    /// <param name="element">Element converted from a byte array.</param>
    /// <param name="options">The serialization options.</param>
    public static void FromBinaryToElement(byte[] byteArray, out Element element, TinyhandSerializerOptions options)
    {
        var reader = new TinyhandReader(byteArray);
        var byteSequence = new ByteSequence();
        try
        {
            if (TinyhandSerializer.TryDecompress(ref reader, byteSequence))
            {
                var r = reader.Clone(byteSequence.ToReadOnlySpan());
                FromReaderToElement(ref r, out element, options);
            }
            else
            {
                FromReaderToElement(ref reader, out element, options);
            }
        }
        finally
        {
            byteSequence.Dispose();
        }
    }

    /// <summary>
    /// Converts a sequence of byte to an Element using TinyhandReader.
    /// </summary>
    /// <param name="reader">TinyhandReader which has a sequence of byte.</param>
    /// <param name="element">Output element.</param>
    /// <param name="options">The serialization options.</param>
    public static void FromReaderToElement(ref TinyhandReader reader, out Element element, TinyhandSerializerOptions options)
    {
        element = FromReaderToElement_Core(ref reader, options);
    }

    private static Element FromReaderToElement_Core(ref TinyhandReader reader, TinyhandSerializerOptions options, bool identifierFlag = false)
    {
        var type = reader.NextMessagePackType;
        switch (type)
        {
            case MessagePackType.Integer:
                if (MessagePackCode.IsSignedInteger(reader.NextCode))
                {
                    return new Value_Long(reader.ReadInt64());
                }
                else
                {
                    return new Value_Long((long)reader.ReadUInt64());
                }

            case MessagePackType.Boolean:
                return new Value_Bool(reader.ReadBoolean());

            case MessagePackType.Float:
                if (reader.NextCode == MessagePackCode.Float32)
                {
                    return new Value_Double(reader.ReadSingle());
                }
                else
                {
                    return new Value_Double(reader.ReadDouble());
                }

            case MessagePackType.String:
                var span = reader.ReadStringSpan();
                var utf8 = span.ToArray();
                if (identifierFlag)
                {
                    if (IsValidIdentifier(utf8))
                    {
                        return new Value_Identifier(false, utf8);
                    }
                    else
                    {
                        return new Value_String(utf8);
                    }
                }
                else
                {
                    return new Value_String(utf8);
                }

            case MessagePackType.Binary:
                return new Value_Binary(reader.ReadBytesToArray());

            case MessagePackType.Array:
                {
                    Group group;
                    int length = reader.ReadArrayHeader();
                    options.Security.DepthStep(ref reader);
                    try
                    {
                        group = new Group(length);
                        for (int i = 0; i < length; i++)
                        {
                            group.Add(FromReaderToElement_Core(ref reader, options));
                        }
                    }
                    finally
                    {
                        reader.Depth--;
                    }

                    return group;
                }

            case MessagePackType.Map:
                {
                    Group group;
                    int length = reader.ReadMapHeader();
                    options.Security.DepthStep(ref reader);
                    try
                    {
                        group = new Group(length);
                        for (int i = 0; i < length; i++)
                        {
                            var left = FromReaderToElement_Core(ref reader, options, true);
                            var right = FromReaderToElement_Core(ref reader, options);
                            group.Add(new Assignment(left, right));
                        }
                    }
                    finally
                    {
                        reader.Depth--;
                    }

                    return group;
                }

            case MessagePackType.Extension:
                ExtensionHeader extHeader = reader.ReadExtensionFormatHeader();
                if (extHeader.TypeCode == ReservedMessagePackExtensionTypeCode.DateTime)
                {// DateTime
                    var dt = reader.ReadDateTime(extHeader);
                    return new Value_String(dt.ToString("o", CultureInfo.InvariantCulture));
                }
                else if (extHeader.TypeCode == MessagePackExtensionCodes.Identifier)
                {// Identifier
                    var identifier = reader.ReadRaw((int)extHeader.Length);
                    return new Value_Identifier(false, identifier.ToArray());
                }
                else
                {
                    var data = reader.ReadRaw((int)extHeader.Length);
                    return new Value_String("[" + extHeader.TypeCode + ",\"" + Convert.ToBase64String(data.ToArray()) + "\"]");
                }

            case MessagePackType.Nil:
                reader.Skip();
                return new Value_Null();

            default:
                throw new TinyhandException($"code is invalid. code: {reader.NextCode} format: {MessagePackCode.ToFormatName(reader.NextCode)}");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ReadOnlySpan<byte> GetIndentSpan(int indents)
    {
        if (indents > MaxIndentBuffer)
        {
            TinyhandGroupStack.ThrowIndentationDepthException();
        }

        return indentSpaces.AsSpan(0, indents * 2);
    }
}
