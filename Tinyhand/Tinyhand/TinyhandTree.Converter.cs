// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using Arc.Crypto;
using Arc.IO;
using Tinyhand.IO;
using Tinyhand.Tree;

#pragma warning disable SA1011 // Closing square brackets should be spaced correctly
#pragma warning disable SA1201 // Elements should appear in the correct order
#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
#pragma warning disable SA1401 // Fields should be private
#pragma warning disable SA1602 // Enumeration items should be documented
#pragma warning disable SA1649 // File name should match first type name

namespace Tinyhand
{
    public static class TinyhandTreeConverter
    {
        private const int InitialBufferSize = 32 * 1024;

        /// <summary>
        /// A thread-local, recyclable array that may be used for short bursts of code.
        /// </summary>
        [ThreadStatic]
        private static byte[]? initialBuffer;

        /// <summary>
        /// Converts a sequence of byte to an Element.
        /// </summary>
        /// <param name="byteArray">A byte array to convert.</param>
        /// <param name="element">Element converted from a byte array.</param>
        /// <param name="options">The serialization options.</param>
        public static void FromBinary(byte[] byteArray, out Element element, TinyhandSerializerOptions options)
        {
            var reader = new TinyhandReader(byteArray);
            var byteSequence = new ByteSequence();
            try
            {
                if (TinyhandSerializer.TryDecompress(ref reader, byteSequence))
                {
                    var r = reader.Clone(byteSequence.GetReadOnlySequence());
                    FromReader(ref r, out element, options);
                }
                else
                {
                    FromReader(ref reader, out element, options);
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
        public static void FromReader(ref TinyhandReader reader, out Element element, TinyhandSerializerOptions options)
        {
            element = FromReaderCore(ref reader, options);
        }

        /// <summary>
        /// Converts an Element to a sequence of byte.
        /// </summary>
        /// <param name="element">Element to convert.</param>
        /// <param name="byteArray">A byte array converted from an element.</param>
        /// <param name="options">The serialization options.</param>
        public static void ToBinary(Element element, out byte[] byteArray, TinyhandSerializerOptions options)
        {
            if (initialBuffer == null)
            {
                initialBuffer = new byte[InitialBufferSize];
            }

            var w = new TinyhandWriter(initialBuffer);
            try
            {
                var state = new ToBinaryCoreState(options, -1);
                ToBinaryCore(element, ref w, state);
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
                ToBinaryCore(element, ref w, state);
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

        private static void ToBinaryCore(Element element, ref TinyhandWriter writer, ToBinaryCoreState state)
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

                    case ValueElementType.Value_Null:
                        writer.WriteNil();
                        break;

                    case ValueElementType.Value_String:
                        writer.WriteString(((Value_String)v).ValueStringUtf8);
                        break;

                    case ValueElementType.Identifier:
                        writer.WriteString(((Value_Identifier)v).IdentifierUtf8);
                        break;

                    case ValueElementType.SpecialIdentifier:
                        var utf8 = ((Value_Identifier)v).IdentifierUtf8;
                        writer.WriteStringHeader(utf8.Length + 1);
                        writer.RawWriteUInt8(TinyhandConstants.AtSign);
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
                    ToBinaryCore(assignment.LeftElement, ref writer, state);
                }

                if (assignment.RightElement == null)
                {
                    writer.WriteNil();
                }
                else
                {
                    ToBinaryCore(assignment.RightElement, ref writer, state);
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
                            ToBinaryCore(assignment, ref writer, state);
                        }
                        else
                        {
                            writer.WriteNil();
                            ToBinaryCore(group.ElementList[i], ref writer, state);
                        }
                    }
                }
                else
                {
                    writer.WriteArrayHeader(group.ElementList.Count);
                    for (var i = 0; i < group.ElementList.Count; i++)
                    {
                        ToBinaryCore(group.ElementList[i], ref writer, state);
                    }
                }
            }
        }

        private static bool IsValidIdentifier(byte[] s)
        {
            if (s.Length == 0)
            {
                return false;
            }

            for (var n = 0; n < s.Length; n++)
            {
                if (s[n] == '{' || s[n] == '}' || s[n] == '"' || s[n] == '=' ||
                    s[n] == '/' || s[n] == ' ' || s[n] == '\r' || s[n] == '\n')
                {// Invalid character
                    return false;
                }
            }

            return true;
        }

        private static Element FromReaderCore(ref TinyhandReader reader, TinyhandSerializerOptions options, bool identifierFlag = false)
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
                    var seq = reader.ReadStringSequence();
                    if (seq == null)
                    {
                        return new Value_Null();
                    }

                    var utf8 = seq.Value.ToArray();
                    if (identifierFlag)
                    {
                        if (utf8.Length > 0 && !TinyhandUtf8Reader.HasDelimiter(utf8))
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
                    var bytes = reader.ReadBytes();
                    if (bytes == null)
                    {
                        return new Value_Null();
                    }

                    return new Value_Binary(bytes.Value.ToArray());

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
                                group.Add(FromReaderCore(ref reader, options));
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
                                var left = FromReaderCore(ref reader, options, true);
                                var right = FromReaderCore(ref reader, options);
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
                    {
                        var dt = reader.ReadDateTime(extHeader);
                        return new Value_String(dt.ToString("o", CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        var data = reader.ReadRaw((long)extHeader.Length);
                        return new Value_String("[" + extHeader.TypeCode + ",\"" + Convert.ToBase64String(data.ToArray()) + "\"]");
                    }

                case MessagePackType.Nil:
                    reader.Skip();
                    return new Value_Null();

                default:
                    throw new TinyhandException($"code is invalid. code: {reader.NextCode} format: {MessagePackCode.ToFormatName(reader.NextCode)}");
            }
        }
    }
}
