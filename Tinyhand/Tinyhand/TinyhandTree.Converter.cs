// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Arc;
using Arc.Collections;
using Arc.IO;
using Tinyhand.IO;
using Tinyhand.Tree;

#pragma warning disable SA1011 // Closing square brackets should be spaced correctly
#pragma warning disable SA1201 // Elements should appear in the correct order
#pragma warning disable SA1202
#pragma warning disable SA1401 // Fields should be private
#pragma warning disable SA1611 // Element parameters should be documented
#pragma warning disable SA1615 // Element return value should be documented

namespace Tinyhand;

public static partial class TinyhandTreeConverter
{
    private const int InitialBufferSize = 32 * 1024;
    private const int InitialStackDepth = 64;
    private const int OutputSpanHint = 256;
    private const int MaxFormattedNumberLength = 32;

    /// <summary>
    /// A thread-local, recyclable array that may be used for short bursts of code.
    /// </summary>
    [ThreadStatic]
    private static byte[]? initialBuffer;

    /// <summary>
    /// The bytes that must be escaped inside a quoted string.
    /// </summary>
#pragma warning disable SA1214 // Readonly fields should appear before non-readonly fields
    private static readonly SearchValues<byte> EscapeSearchValues = SearchValues.Create("\"\\\b\f\n\r\t"u8);
#pragma warning restore SA1214 // Readonly fields should appear before non-readonly fields

    /// <summary>
    /// Maps a byte to the character that follows the backslash in its escape sequence, or 0 when the byte needs no escaping.
    /// </summary>
    private static readonly byte[] EscapeTable = CreateEscapeTable();

    private static byte[] CreateEscapeTable()
    {
        var table = new byte[256];
        table[TinyhandConstants.Quote] = TinyhandConstants.Quote;
        table[TinyhandConstants.BackSlash] = TinyhandConstants.BackSlash;
        table[TinyhandConstants.BackSpace] = (byte)'b';
        table[TinyhandConstants.FormFeed] = (byte)'f';
        table[TinyhandConstants.LineFeed] = (byte)'n';
        table[TinyhandConstants.CarriageReturn] = (byte)'r';
        table[TinyhandConstants.Tab] = (byte)'t';
        return table;
    }

    #region BinaryToUtf8

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

        // The decompression buffer is only created when the data can actually be an LZ4 block array.
        if (MayBeCompressed(span))
        {
            var reader = new TinyhandReader(span);
            var byteSequence = new ByteSequence();
            try
            {
                if (TinyhandSerializer.TryDecompress(ref reader, byteSequence))
                {
                    ConvertAllToUtf8(byteSequence.ToReadOnlySpan(), ref writer, options.Compose, omitTopLevelBracket);
                    return;
                }
            }
            finally
            {
                byteSequence.Dispose();
            }
        }

        ConvertAllToUtf8(span, ref writer, options.Compose, omitTopLevelBracket);
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
        var fork = reader.Fork();
        var source = fork.ReadRaw(reader.Remaining);
        var destination = writer.GetSpan(OutputSpanHint);
        var destinationPosition = 0;
        var sourcePosition = 0;
        var isPrimitive = ConvertElementToUtf8(source, ref sourcePosition, ref writer, ref destination, ref destinationPosition, ref groupWriter, omitTopLevelBracket, convertToIdentifier);
        writer.Advance(destinationPosition);
        reader.Advance(sourcePosition);
        return isPrimitive;
    }

    /// <summary>
    /// Checks whether the data can be an LZ4 block array ([Ext, bin, bin...]) without allocating anything.
    /// </summary>
    private static bool MayBeCompressed(ReadOnlySpan<byte> span)
    {
        if (span.Length < 2)
        {
            return false;
        }

        int headerLength;
        var code = span[0];
        if (code >= MessagePackCode.MinFixArray && code <= MessagePackCode.MaxFixArray)
        {
            headerLength = 1;
        }
        else if (code == MessagePackCode.Array16)
        {
            headerLength = 3;
        }
        else if (code == MessagePackCode.Array32)
        {
            headerLength = 5;
        }
        else
        {
            return false;
        }

        if (span.Length <= headerLength)
        {
            return false;
        }

        code = span[headerLength];
        return (code >= MessagePackCode.Ext8 && code <= MessagePackCode.Ext32) ||
            (code >= MessagePackCode.FixExt1 && code <= MessagePackCode.FixExt16);
    }

    private static void ConvertAllToUtf8(ReadOnlySpan<byte> source, ref TinyhandRawWriter writer, TinyhandComposeOption compose, bool omitTopLevelBracket)
    {
        var groupWriter = new TinyhandGroupWriter(compose);
        var destination = writer.GetSpan(OutputSpanHint);
        var destinationPosition = 0;
        var sourcePosition = 0;
        while (sourcePosition < source.Length)
        {
            ConvertElementToUtf8(source, ref sourcePosition, ref writer, ref destination, ref destinationPosition, ref groupWriter, omitTopLevelBracket, false);
            FlushGroup(ref writer, ref destination, ref destinationPosition, ref groupWriter);
        }

        writer.Advance(destinationPosition);
    }

    /// <summary>
    /// Converts one MessagePack element (including the content of arrays and maps) to text.<br/>
    /// The output is written to <paramref name="destination"/> from <paramref name="destinationPosition"/>; when the span is full,
    /// the written part is committed to <paramref name="writer"/> and a new span is acquired.<br/>
    /// Nested containers are tracked with an explicit stack instead of recursion.
    /// </summary>
    /// <returns><see langword="true"/> if the element is a primitive; otherwise, <see langword="false"/>.</returns>
    private static bool ConvertElementToUtf8(ReadOnlySpan<byte> source, ref int sourcePosition, ref TinyhandRawWriter writer, ref Span<byte> destination, ref int destinationPosition, scoped ref TinyhandGroupWriter groupWriter, bool omitTopLevelBracket, bool convertToIdentifier)
    {
        ref byte src = ref MemoryMarshal.GetReference(source);
        var length = source.Length;
        var p = sourcePosition;

        var compose = groupWriter.ComposeOption;
        var indent = groupWriter.EnableIndent;
        var keyToIdentifier = compose != TinyhandComposeOption.Simple;
        var commaBetweenPairs = compose == TinyhandComposeOption.Simple || compose == TinyhandComposeOption.Strict;

        // Stack entry: (remaining items << 2) | (is map ? 2 : 0) | (bracket omitted ? 1 : 0).
        Span<long> stack = stackalloc long[InitialStackDepth];
        long[]? rentedStack = null;
        var depth = 0;
        var toIdentifier = convertToIdentifier;
        var isPrimitive = false;
        long count;
        int n;

        try
        {
Next:
            if (p >= length)
            {
                ThrowEndOfStream();
            }

            var code = Unsafe.Add(ref src, p++);
            if (code <= MessagePackCode.MaxFixInt)
            {// Positive fixint
                FlushGroup(ref writer, ref destination, ref destinationPosition, ref groupWriter);
                Ensure(ref writer, ref destination, ref destinationPosition, 3);
                WriteSmallNumber(destination, ref destinationPosition, code);
                isPrimitive = true;
                goto AfterElement;
            }
            else if (code >= MessagePackCode.MinNegativeFixInt)
            {// Negative fixint (-32 to -1)
                FlushGroup(ref writer, ref destination, ref destinationPosition, ref groupWriter);
                Ensure(ref writer, ref destination, ref destinationPosition, 3);
                destination[destinationPosition++] = TinyhandConstants.Hyphen;
                WriteSmallNumber(destination, ref destinationPosition, (uint)(-(int)(sbyte)code));
                isPrimitive = true;
                goto AfterElement;
            }
            else if (code < MessagePackCode.Nil)
            {// 0x80 - 0xbf
                if (code <= MessagePackCode.MaxFixMap)
                {
                    count = code & 0x0F;
                    goto Map;
                }
                else if (code <= MessagePackCode.MaxFixArray)
                {
                    count = code & 0x0F;
                    goto Array;
                }
                else
                {
                    n = code & 0x1F;
                    goto Str;
                }
            }
            else
            {// 0xc0 - 0xdf: dense switch -> jump table.
                switch (code)
                {
                    case MessagePackCode.Nil:
                        FlushGroup(ref writer, ref destination, ref destinationPosition, ref groupWriter);
                        Ensure(ref writer, ref destination, ref destinationPosition, 4);
                        TinyhandConstants.NullSpan.CopyTo(destination.Slice(destinationPosition));
                        destinationPosition += 4;
                        isPrimitive = true;
                        goto AfterElement;

                    case MessagePackCode.False:
                        FlushGroup(ref writer, ref destination, ref destinationPosition, ref groupWriter);
                        Ensure(ref writer, ref destination, ref destinationPosition, 5);
                        TinyhandConstants.FalseSpan.CopyTo(destination.Slice(destinationPosition));
                        destinationPosition += 5;
                        isPrimitive = true;
                        goto AfterElement;

                    case MessagePackCode.True:
                        FlushGroup(ref writer, ref destination, ref destinationPosition, ref groupWriter);
                        Ensure(ref writer, ref destination, ref destinationPosition, 4);
                        TinyhandConstants.TrueSpan.CopyTo(destination.Slice(destinationPosition));
                        destinationPosition += 4;
                        isPrimitive = true;
                        goto AfterElement;

                    case MessagePackCode.Bin8:
                        n = ReadUInt8(ref src, ref p, length);
                        goto Bin;

                    case MessagePackCode.Bin16:
                        n = ReadUInt16(ref src, ref p, length);
                        goto Bin;

                    case MessagePackCode.Bin32:
                        n = ReadLength32(ref src, ref p, length);
                        goto Bin;

                    case MessagePackCode.Ext8:
                    case MessagePackCode.Ext16:
                    case MessagePackCode.Ext32:
                    case MessagePackCode.FixExt1:
                    case MessagePackCode.FixExt2:
                    case MessagePackCode.FixExt4:
                    case MessagePackCode.FixExt8:
                    case MessagePackCode.FixExt16:
                        goto Ext;

                    case MessagePackCode.Float32:
                        FlushGroup(ref writer, ref destination, ref destinationPosition, ref groupWriter);
                        WriteSingle(ref writer, ref destination, ref destinationPosition, BitConverter.UInt32BitsToSingle(ReadUInt32(ref src, ref p, length)));
                        isPrimitive = true;
                        goto AfterElement;

                    case MessagePackCode.Float64:
                        FlushGroup(ref writer, ref destination, ref destinationPosition, ref groupWriter);
                        WriteDouble(ref writer, ref destination, ref destinationPosition, BitConverter.UInt64BitsToDouble(ReadUInt64(ref src, ref p, length)));
                        isPrimitive = true;
                        goto AfterElement;

                    case MessagePackCode.UInt8:
                        FlushGroup(ref writer, ref destination, ref destinationPosition, ref groupWriter);
                        Ensure(ref writer, ref destination, ref destinationPosition, 3);
                        WriteSmallNumber(destination, ref destinationPosition, ReadUInt8(ref src, ref p, length));
                        isPrimitive = true;
                        goto AfterElement;

                    case MessagePackCode.UInt16:
                        FlushGroup(ref writer, ref destination, ref destinationPosition, ref groupWriter);
                        WriteUInt64(ref writer, ref destination, ref destinationPosition, ReadUInt16(ref src, ref p, length));
                        isPrimitive = true;
                        goto AfterElement;

                    case MessagePackCode.UInt32:
                        FlushGroup(ref writer, ref destination, ref destinationPosition, ref groupWriter);
                        WriteUInt64(ref writer, ref destination, ref destinationPosition, ReadUInt32(ref src, ref p, length));
                        isPrimitive = true;
                        goto AfterElement;

                    case MessagePackCode.UInt64:
                        FlushGroup(ref writer, ref destination, ref destinationPosition, ref groupWriter);
                        WriteUInt64(ref writer, ref destination, ref destinationPosition, ReadUInt64(ref src, ref p, length));
                        isPrimitive = true;
                        goto AfterElement;

                    case MessagePackCode.Int8:
                        FlushGroup(ref writer, ref destination, ref destinationPosition, ref groupWriter);
                        WriteInt64(ref writer, ref destination, ref destinationPosition, (sbyte)ReadUInt8(ref src, ref p, length));
                        isPrimitive = true;
                        goto AfterElement;

                    case MessagePackCode.Int16:
                        FlushGroup(ref writer, ref destination, ref destinationPosition, ref groupWriter);
                        WriteInt64(ref writer, ref destination, ref destinationPosition, (short)ReadUInt16(ref src, ref p, length));
                        isPrimitive = true;
                        goto AfterElement;

                    case MessagePackCode.Int32:
                        FlushGroup(ref writer, ref destination, ref destinationPosition, ref groupWriter);
                        WriteInt64(ref writer, ref destination, ref destinationPosition, (int)ReadUInt32(ref src, ref p, length));
                        isPrimitive = true;
                        goto AfterElement;

                    case MessagePackCode.Int64:
                        FlushGroup(ref writer, ref destination, ref destinationPosition, ref groupWriter);
                        WriteInt64(ref writer, ref destination, ref destinationPosition, (long)ReadUInt64(ref src, ref p, length));
                        isPrimitive = true;
                        goto AfterElement;

                    case MessagePackCode.Str8:
                        n = ReadUInt8(ref src, ref p, length);
                        goto Str;

                    case MessagePackCode.Str16:
                        n = ReadUInt16(ref src, ref p, length);
                        goto Str;

                    case MessagePackCode.Str32:
                        n = ReadLength32(ref src, ref p, length);
                        goto Str;

                    case MessagePackCode.Array16:
                        count = ReadUInt16(ref src, ref p, length);
                        goto Array;

                    case MessagePackCode.Array32:
                        count = ReadUInt32(ref src, ref p, length);
                        goto Array;

                    case MessagePackCode.Map16:
                        count = ReadUInt16(ref src, ref p, length);
                        goto Map;

                    case MessagePackCode.Map32:
                        count = ReadUInt32(ref src, ref p, length);
                        goto Map;

                    default: // MessagePackCode.NeverUsed
                        throw new TinyhandException($"code is invalid. code: {code} format: {MessagePackCode.ToFormatName(code)}");
                }
            }

Str:
            {
                if (length - p < n)
                {
                    ThrowEndOfStream();
                }

                var utf8 = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.Add(ref src, p), n);
                p += n;
                FlushGroup(ref writer, ref destination, ref destinationPosition, ref groupWriter);
                if (toIdentifier && IsValidIdentifier(utf8))
                {
                    Ensure(ref writer, ref destination, ref destinationPosition, n);
                    utf8.CopyTo(destination.Slice(destinationPosition));
                    destinationPosition += n;
                }
                else
                {
                    WriteQuotedString(ref writer, ref destination, ref destinationPosition, utf8);
                }

                isPrimitive = true;
                goto AfterElement;
            }

Bin:
            {
                if (length - p < n)
                {
                    ThrowEndOfStream();
                }

                var binary = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.Add(ref src, p), n);
                p += n;
                FlushGroup(ref writer, ref destination, ref destinationPosition, ref groupWriter);

                var encodedLength = Arc.Crypto.Base64Url.GetEncodedLength(n);
                Ensure(ref writer, ref destination, ref destinationPosition, encodedLength + 3);
                destination[destinationPosition] = (byte)'b';
                destination[destinationPosition + 1] = TinyhandConstants.Quote;
                Arc.Crypto.Base64Url.Encode(binary, destination.Slice(destinationPosition + 2, encodedLength));
                destinationPosition += encodedLength + 2;
                destination[destinationPosition++] = TinyhandConstants.Quote;

                isPrimitive = true;
                goto AfterElement;
            }

Ext:
            {
                FlushGroup(ref writer, ref destination, ref destinationPosition, ref groupWriter);

                // Extensions are rare; TinyhandReader is used for the header and the timestamp decoding.
                var extReader = new TinyhandReader(source.Slice(p - 1));
                var extHeader = extReader.ReadExtensionFormatHeader();
                if (extHeader.TypeCode == MessagePackExtensionCodes.DateTime)
                {// DateTime
                    var dt = extReader.ReadDateTime(extHeader);
                    if (dt.Kind != DateTimeKind.Utc)
                    {
                        dt = dt.ToUniversalTime();
                    }

                    Span<byte> formatted = stackalloc byte[64];
                    dt.TryFormat(formatted, out var formattedLength, "o", CultureInfo.InvariantCulture);
                    WriteQuotedString(ref writer, ref destination, ref destinationPosition, formatted.Slice(0, formattedLength));
                }
                else if (extHeader.TypeCode == MessagePackExtensionCodes.Identifier)
                {// Identifier
                    var identifier = extReader.ReadRaw((int)extHeader.Length);
                    Ensure(ref writer, ref destination, ref destinationPosition, identifier.Length);
                    identifier.CopyTo(destination.Slice(destinationPosition));
                    destinationPosition += identifier.Length;
                }
                else
                {// "[TypeCode,\"Base64\"]"
                    var data = extReader.ReadRaw((int)extHeader.Length);
                    WriteExtensionAsString(ref writer, ref destination, ref destinationPosition, extHeader.TypeCode, data);
                }

                p = p - 1 + extReader.Consumed;
                isPrimitive = true;
                goto AfterElement;
            }

Array:
            {
                // Each element needs at least one byte.
                if ((ulong)count > (ulong)(length - p))
                {
                    ThrowEndOfStream();
                }

                long entry = count << 2;
                if (depth == 0 && omitTopLevelBracket)
                {
                    if (count == 0)
                    {
                        isPrimitive = false;
                        goto AfterElement;
                    }

                    entry |= 1;
                }
                else
                {
                    if (count == 0)
                    {// {}
                        FlushGroup(ref writer, ref destination, ref destinationPosition, ref groupWriter);
                        Ensure(ref writer, ref destination, ref destinationPosition, 2);
                        destination[destinationPosition] = TinyhandConstants.OpenBrace;
                        destination[destinationPosition + 1] = TinyhandConstants.CloseBrace;
                        destinationPosition += 2;
                        isPrimitive = false;
                        goto AfterElement;
                    }

                    if (indent)
                    {
                        groupWriter.AddOpen();
                    }
                    else
                    {
                        Ensure(ref writer, ref destination, ref destinationPosition, 1);
                        destination[destinationPosition++] = TinyhandConstants.OpenBrace;
                    }
                }

                if (depth == stack.Length)
                {
                    GrowStack(ref stack, ref rentedStack);
                }

                stack[depth++] = entry;
                toIdentifier = false;
                goto Next;
            }

Map:
            {
                // Each key/value pair needs at least two bytes.
                if ((ulong)count * 2 > (ulong)(length - p))
                {
                    ThrowEndOfStream();
                }

                long entry = (count << 3) | 2; // (count * 2) << 2 | IsMap
                if (depth == 0 && omitTopLevelBracket)
                {
                    if (count == 0)
                    {
                        isPrimitive = false;
                        goto AfterElement;
                    }

                    entry |= 1;
                }
                else
                {
                    if (count == 0)
                    {// {}
                        FlushGroup(ref writer, ref destination, ref destinationPosition, ref groupWriter);
                        Ensure(ref writer, ref destination, ref destinationPosition, 2);
                        destination[destinationPosition] = TinyhandConstants.OpenBrace;
                        destination[destinationPosition + 1] = TinyhandConstants.CloseBrace;
                        destinationPosition += 2;
                        isPrimitive = false;
                        goto AfterElement;
                    }

                    if (indent)
                    {
                        groupWriter.AddOpen();
                    }
                    else
                    {
                        Ensure(ref writer, ref destination, ref destinationPosition, 1);
                        destination[destinationPosition++] = TinyhandConstants.OpenBrace;
                    }
                }

                if (depth == stack.Length)
                {
                    GrowStack(ref stack, ref rentedStack);
                }

                stack[depth++] = entry;
                toIdentifier = keyToIdentifier;
                goto Next;
            }

AfterElement:
            while (true)
            {
                if (depth == 0)
                {
                    sourcePosition = p;
                    return isPrimitive;
                }

                ref var top = ref stack[depth - 1];
                var entry = top - 4; // One item consumed.
                top = entry;
                var remaining = entry >> 2;
                if (remaining > 0)
                {
                    if ((entry & 2) != 0)
                    {// Map
                        if ((remaining & 1) != 0)
                        {// A key has been written -> "="
                            Ensure(ref writer, ref destination, ref destinationPosition, 1);
                            destination[destinationPosition++] = TinyhandConstants.EqualsSign;
                            toIdentifier = false;
                        }
                        else
                        {// A value has been written -> ", " or next line
                            if (commaBetweenPairs)
                            {
                                Ensure(ref writer, ref destination, ref destinationPosition, 2);
                                destination[destinationPosition] = TinyhandConstants.Separator;
                                destination[destinationPosition + 1] = TinyhandConstants.Space;
                                destinationPosition += 2;
                            }
                            else
                            {
                                groupWriter.AddLF();
                            }

                            toIdentifier = keyToIdentifier;
                        }
                    }
                    else
                    {// Array
                        if (!indent || isPrimitive)
                        {
                            Ensure(ref writer, ref destination, ref destinationPosition, 2);
                            destination[destinationPosition] = TinyhandConstants.Separator;
                            destination[destinationPosition + 1] = TinyhandConstants.Space;
                            destinationPosition += 2;
                        }
                        else
                        {
                            groupWriter.AddLF();
                        }

                        toIdentifier = false;
                    }

                    goto Next;
                }

                // The container is complete.
                depth--;
                if ((entry & 1) == 0)
                {
                    if (indent)
                    {
                        groupWriter.AddClose();
                    }
                    else
                    {
                        Ensure(ref writer, ref destination, ref destinationPosition, 1);
                        destination[destinationPosition++] = TinyhandConstants.CloseBrace;
                    }
                }

                isPrimitive = false;
            }
        }
        finally
        {
            if (rentedStack is not null)
            {
                ArrayPool<long>.Shared.Return(rentedStack);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void FlushGroup(ref TinyhandRawWriter writer, ref Span<byte> destination, ref int destinationPosition, scoped ref TinyhandGroupWriter groupWriter)
    {
        if (groupWriter.HasPending)
        {
            Ensure(ref writer, ref destination, ref destinationPosition, TinyhandGroupWriter.MaxFlushLength);
            destinationPosition += groupWriter.FlushCore(destination.Slice(destinationPosition));
        }
    }

    /// <summary>
    /// Makes sure that <paramref name="size"/> bytes can be written to <paramref name="destination"/> at <paramref name="destinationPosition"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Ensure(ref TinyhandRawWriter writer, ref Span<byte> destination, ref int destinationPosition, int size)
    {
        if (destination.Length - destinationPosition < size)
        {
            Refill(ref writer, ref destination, ref destinationPosition, size);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Refill(ref TinyhandRawWriter writer, ref Span<byte> destination, ref int destinationPosition, int size)
    {
        writer.Advance(destinationPosition);
        destination = writer.GetSpan(Math.Max(size, OutputSpanHint));
        destinationPosition = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte ReadUInt8(ref byte src, ref int p, int length)
    {
        if (length - p < 1)
        {
            ThrowEndOfStream();
        }

        var value = Unsafe.Add(ref src, p);
        p += 1;
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ushort ReadUInt16(ref byte src, ref int p, int length)
    {
        if (length - p < 2)
        {
            ThrowEndOfStream();
        }

        var value = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<ushort>(ref Unsafe.Add(ref src, p)));
        p += 2;
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint ReadUInt32(ref byte src, ref int p, int length)
    {
        if (length - p < 4)
        {
            ThrowEndOfStream();
        }

        var value = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref src, p)));
        p += 4;
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong ReadUInt64(ref byte src, ref int p, int length)
    {
        if (length - p < 8)
        {
            ThrowEndOfStream();
        }

        var value = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref src, p)));
        p += 8;
        return value;
    }

    /// <summary>
    /// Reads a 32-bit length that must fit in the remaining data.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int ReadLength32(ref byte src, ref int p, int length)
    {
        var value = ReadUInt32(ref src, ref p, length);
        if (value > (uint)(length - p))
        {
            ThrowEndOfStream();
        }

        return (int)value;
    }

    /// <summary>
    /// Writes a number from 0 to 999 (3 bytes must be available).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteSmallNumber(Span<byte> destination, ref int destinationPosition, uint value)
    {
        if (value < 10)
        {
            destination[destinationPosition] = (byte)('0' + value);
            destinationPosition += 1;
        }
        else if (value < 100)
        {
            var tens = value / 10;
            destination[destinationPosition] = (byte)('0' + tens);
            destination[destinationPosition + 1] = (byte)('0' + (value - (tens * 10)));
            destinationPosition += 2;
        }
        else
        {
            var hundreds = value / 100;
            var rest = value - (hundreds * 100);
            var tens = rest / 10;
            destination[destinationPosition] = (byte)('0' + hundreds);
            destination[destinationPosition + 1] = (byte)('0' + tens);
            destination[destinationPosition + 2] = (byte)('0' + (rest - (tens * 10)));
            destinationPosition += 3;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteUInt64(ref TinyhandRawWriter writer, ref Span<byte> destination, ref int destinationPosition, ulong value)
    {
        Ensure(ref writer, ref destination, ref destinationPosition, TinyhandConstants.MaximumFormatUInt64Length);
        Utf8Formatter.TryFormat(value, destination.Slice(destinationPosition), out var written);
        destinationPosition += written;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteInt64(ref TinyhandRawWriter writer, ref Span<byte> destination, ref int destinationPosition, long value)
    {
        Ensure(ref writer, ref destination, ref destinationPosition, TinyhandConstants.MaximumFormatInt64Length);
        Utf8Formatter.TryFormat(value, destination.Slice(destinationPosition), out var written);
        destinationPosition += written;
    }

    private static void WriteSingle(ref TinyhandRawWriter writer, ref Span<byte> destination, ref int destinationPosition, float value)
    {
        Ensure(ref writer, ref destination, ref destinationPosition, MaxFormattedNumberLength);
        if (float.IsFinite(value))
        {
            Utf8Formatter.TryFormat(value, destination.Slice(destinationPosition), out var written);
            destinationPosition += written;
        }
        else
        {
            WriteNonFinite(destination, ref destinationPosition, float.IsNaN(value), float.IsPositiveInfinity(value));
        }
    }

    private static void WriteDouble(ref TinyhandRawWriter writer, ref Span<byte> destination, ref int destinationPosition, double value)
    {
        Ensure(ref writer, ref destination, ref destinationPosition, MaxFormattedNumberLength);
        if (double.IsFinite(value))
        {
            Utf8Formatter.TryFormat(value, destination.Slice(destinationPosition), out var written);
            destinationPosition += written;
        }
        else
        {
            WriteNonFinite(destination, ref destinationPosition, double.IsNaN(value), double.IsPositiveInfinity(value));
        }
    }

    private static void WriteNonFinite(Span<byte> destination, ref int destinationPosition, bool isNaN, bool isPositiveInfinity)
    {
        var span = isNaN ? TinyhandConstants.DoubleNaNSpan :
            isPositiveInfinity ? TinyhandConstants.DoublePositiveInfinitySpan : TinyhandConstants.DoubleNegativeInfinitySpan;
        span.CopyTo(destination.Slice(destinationPosition));
        destinationPosition += span.Length;
    }

    /// <summary>
    /// Writes a string surrounded by quotes, escaping the characters that need it.
    /// </summary>
    private static void WriteQuotedString(ref TinyhandRawWriter writer, ref Span<byte> destination, ref int destinationPosition, scoped ReadOnlySpan<byte> utf8)
    {
        var index = utf8.IndexOfAny(EscapeSearchValues);
        if (index < 0)
        {// Nothing to escape: a single copy.
            Ensure(ref writer, ref destination, ref destinationPosition, utf8.Length + 2);
            destination[destinationPosition++] = TinyhandConstants.Quote;
            utf8.CopyTo(destination.Slice(destinationPosition));
            destinationPosition += utf8.Length;
            destination[destinationPosition++] = TinyhandConstants.Quote;
            return;
        }

        WriteEscapedString(ref writer, ref destination, ref destinationPosition, utf8, index);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WriteEscapedString(ref TinyhandRawWriter writer, ref Span<byte> destination, ref int destinationPosition, scoped ReadOnlySpan<byte> utf8, int firstEscape)
    {
        ReadOnlySpan<byte> table = EscapeTable;

        // Count the escapes so that the whole string fits in one span.
        var extra = 0;
        for (var i = firstEscape; i < utf8.Length; i++)
        {
            if (table[utf8[i]] != 0)
            {
                extra++;
            }
        }

        Ensure(ref writer, ref destination, ref destinationPosition, utf8.Length + extra + 2);
        var span = destination;
        var position = destinationPosition;
        span[position++] = TinyhandConstants.Quote;

        var from = 0;
        var index = firstEscape;
        while (true)
        {
            utf8.Slice(from, index - from).CopyTo(span.Slice(position));
            position += index - from;
            span[position] = TinyhandConstants.BackSlash;
            span[position + 1] = table[utf8[index]];
            position += 2;
            from = index + 1;

            var next = utf8.Slice(from).IndexOfAny(EscapeSearchValues);
            if (next < 0)
            {
                break;
            }

            index = from + next;
        }

        utf8.Slice(from).CopyTo(span.Slice(position));
        position += utf8.Length - from;
        span[position++] = TinyhandConstants.Quote;
        destinationPosition = position;
    }

    private static void WriteExtensionAsString(ref TinyhandRawWriter writer, ref Span<byte> destination, ref int destinationPosition, byte typeCode, scoped ReadOnlySpan<byte> data)
    {// "[TypeCode,\"Base64\"]"
        var maxLength = Base64.GetMaxEncodedToUtf8Length(data.Length) + 16;
        Ensure(ref writer, ref destination, ref destinationPosition, maxLength);
        var span = destination;
        var position = destinationPosition;
        span[position++] = TinyhandConstants.Quote;
        span[position++] = TinyhandConstants.OpenBracket;
        Utf8Formatter.TryFormat((int)typeCode, span.Slice(position), out var written);
        position += written;
        span[position] = TinyhandConstants.Separator;
        span[position + 1] = TinyhandConstants.BackSlash;
        span[position + 2] = TinyhandConstants.Quote;
        position += 3;
        Base64.EncodeToUtf8(data, span.Slice(position), out _, out written);
        position += written;
        span[position] = TinyhandConstants.BackSlash;
        span[position + 1] = TinyhandConstants.Quote;
        span[position + 2] = TinyhandConstants.CloseBracket;
        span[position + 3] = TinyhandConstants.Quote;
        destinationPosition = position + 4;
    }

    private static void GrowStack(scoped ref Span<long> stack, ref long[]? rentedStack)
    {
        var newStack = ArrayPool<long>.Shared.Rent(stack.Length * 2);
        stack.CopyTo(newStack);
        if (rentedStack is not null)
        {
            ArrayPool<long>.Shared.Return(rentedStack);
        }

        rentedStack = newStack;
        stack = newStack;
    }

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowEndOfStream()
    {
        throw new EndOfStreamException();
    }

    private static bool IsValidIdentifier(ReadOnlySpan<byte> s)
    {
        if (s.Length == 0)
        {// Empty
            return false;
        }

        if (TinyhandHelper.IsDigit(s[0]))
        {// Number
            return false;
        }

        if (TinyhandUtf8Reader.HasDelimiter(s))
        {// Has delimiter
            return false;
        }

        // Every reserved word (null, true, false and the modifier names) is 3 to 8 bytes long, so other lengths skip the lookup.
        if ((uint)(s.Length - 3) <= 5 && TinyhandHelper.ReservedTable.TryGetValue(s, out _))
        {// Reserved
            return false;
        }

        return true;
    }

    #endregion

    #region Utf8ToBinary

    /// <summary>
    /// A growable buffer for the binary produced from UTF-8 text.<br/>
    /// It starts as a thread-static array (or a pooled one when the thread-static array is already in use) and moves to a larger pooled array on demand.
    /// </summary>
    internal ref struct BinaryBuffer
    {
        [ThreadStatic]
        private static byte[]? threadStaticArray;

        [ThreadStatic]
        private static bool threadStaticArrayInUse;

        private byte[] array;
        private byte[]? rentedArray;
        private bool ownsThreadStaticArray;

        public static BinaryBuffer Acquire()
        {
            var buffer = default(BinaryBuffer);
            if (!threadStaticArrayInUse)
            {
                threadStaticArrayInUse = true;
                buffer.ownsThreadStaticArray = true;
                buffer.array = threadStaticArray ??= new byte[InitialBufferSize];
            }
            else
            {
                buffer.rentedArray = ArrayPool<byte>.Shared.Rent(InitialBufferSize);
                buffer.array = buffer.rentedArray;
            }

            return buffer;
        }

        public byte[] Array => this.array;

        public int Length { get; set; }

        public ReadOnlySpan<byte> Span => this.array.AsSpan(0, this.Length);

        public void Release()
        {
            if (this.rentedArray is not null)
            {
                ArrayPool<byte>.Shared.Return(this.rentedArray);
                this.rentedArray = null;
            }

            if (this.ownsThreadStaticArray)
            {
                threadStaticArrayInUse = false;
                this.ownsThreadStaticArray = false;
            }

            this.array = System.Array.Empty<byte>();
        }

        /// <summary>
        /// Grows the buffer so that <paramref name="size"/> more bytes fit after <paramref name="length"/> bytes.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public byte[] Grow(int length, int size)
        {
            var newSize = Math.Max(this.array.Length * 2, length + size);
            var newArray = ArrayPool<byte>.Shared.Rent(newSize);
            this.array.AsSpan(0, length).CopyTo(newArray);
            if (this.rentedArray is not null)
            {
                ArrayPool<byte>.Shared.Return(this.rentedArray);
            }

            this.rentedArray = newArray;
            this.array = newArray;
            return newArray;
        }
    }

    /// <summary>
    /// The binary position of an atom and its position in the text (used to report errors).
    /// </summary>
    internal struct AtomPosition
    {
        public int Position;
        public int LineNumber;
        public int BytePositionInLine;

        public AtomPosition(int position, int lineNumber, int bytePositionInLine)
        {
            this.Position = position;
            this.LineNumber = lineNumber;
            this.BytePositionInLine = bytePositionInLine;
        }
    }

    /// <summary>
    /// Get the Line/BytePosition from binary position.
    /// </summary>
    /// <param name="utf8">UTF-8 text.</param>
    /// <param name="position">The byte position.</param>
    /// <param name="omitTopLevelBracket"><see langword="true"/> if the binary was created with the top level bracket omitted.</param>
    /// <returns>Returns a <see cref="TinyhandUtf8LinePosition"/> representing the line and byte position in the UTF-8 text of the atom that starts at or before <paramref name="position"/>.</returns>
    public static TinyhandUtf8LinePosition GetTextPositionFromBinaryPosition(ReadOnlySpan<byte> utf8, long position, bool omitTopLevelBracket = false)
    {
        var positions = new List<AtomPosition>();
        var buffer = BinaryBuffer.Acquire();
        try
        {
            FromUtf8ToBinaryWithReader(utf8, omitTopLevelBracket, ref buffer, positions);
        }
        finally
        {
            buffer.Release();
        }

        // The positions are sorted, so the last atom that starts at or before the position is wanted.
        var span = CollectionsMarshal.AsSpan(positions);
        var result = default(TinyhandUtf8LinePosition);
        for (var i = 0; i < span.Length; i++)
        {
            if (span[i].Position > position)
            {
                break;
            }

            result.LineNumber = span[i].LineNumber;
            result.BytePositionInLine = span[i].BytePositionInLine;
        }

        return result;
    }

    /// <summary>
    /// Converts UTF-8 text to a sequence of byte.
    /// </summary>
    /// <param name="utf8">UTF-8 text.</param>
    /// <param name="writer">TinyhandRawWriter.</param>
    /// <param name="omitTopLevelBracket"><see langword="true"/> to omit the top level bracket.</param>
    public static void FromUtf8ToBinary(ReadOnlySpan<byte> utf8, ref TinyhandWriter writer, bool omitTopLevelBracket = false)
    {
        var buffer = BinaryBuffer.Acquire();
        try
        {
            FromUtf8ToBinaryFast(utf8, omitTopLevelBracket, ref buffer);
            writer.WriteSpan(buffer.Span);
        }
        finally
        {
            buffer.Release();
        }
    }

    /// <summary>
    /// Converts UTF-8 text to a sequence of byte stored in <paramref name="buffer"/> (which must be acquired and released by the caller).
    /// </summary>
    /// <param name="utf8">UTF-8 text.</param>
    /// <param name="omitTopLevelBracket"><see langword="true"/> to omit the top level bracket.</param>
    /// <param name="buffer">The buffer that receives the binary.</param>
    internal static void FromUtf8ToBinary(ReadOnlySpan<byte> utf8, bool omitTopLevelBracket, ref BinaryBuffer buffer)
        => FromUtf8ToBinaryFast(utf8, omitTopLevelBracket, ref buffer);

    /// <summary>
    /// The text to binary conversion built on <see cref="TinyhandUtf8Reader"/>.<br/>
    /// It produces exactly the same binary as the specialized lexer and additionally records the text position of every atom,
    /// which is what error reporting needs. The normal path uses the faster specialized lexer.<br/>
    /// A group header is reserved as a single byte when the group starts and patched when the group ends;
    /// when the group turns out to have more than 15 items, the content is shifted to make room for a larger header.
    /// This keeps the whole output in one contiguous buffer without scratch writers.
    /// </summary>
    internal static void FromUtf8ToBinaryWithReader(ReadOnlySpan<byte> utf8, bool omitTopLevelBracket, ref BinaryBuffer buffer, List<AtomPosition>? positions)
    {
        var reader = new TinyhandUtf8Reader(utf8, false, true); // Separators are not needed.
        var array = buffer.Array;
        var position = 0;

        // Stack entry: (header position << 32) | (item count << 1) | (assignment found ? 1 : 0).
        Span<long> stack = stackalloc long[InitialStackDepth];
        long[]? rentedStack = null;
        var depth = 0;
        var stopDepth = 0;

        try
        {
            if (omitTopLevelBracket)
            {// A virtual top level group.
                array = EnsureCapacity(ref buffer, array, position, 1);
                position = 1;
                stack[depth++] = 0;
                stopDepth = 1;
            }

            while (reader.Read())
            {
                switch (reader.AtomType)
                {
                    case TinyhandAtomType.StartGroup: // {
                        positions?.Add(new(position, reader.AtomLineNumber, reader.AtomBytePositionInLine));
                        array = EnsureCapacity(ref buffer, array, position, 1);
                        if (depth == stack.Length)
                        {
                            GrowStack(ref stack, ref rentedStack);
                        }

                        stack[depth++] = (long)position << 32;
                        position++;
                        break;

                    case TinyhandAtomType.EndGroup: // }
                        if (depth == stopDepth)
                        {// The end of the top level group: the rest is ignored.
                            goto Done;
                        }

                        depth--;
                        array = FinalizeGroup(stack[depth], ref buffer, array, ref position, positions);
                        if (depth > 0)
                        {
                            stack[depth - 1] += 2;
                        }

                        break;

                    case TinyhandAtomType.Identifier: // objectA
                    case TinyhandAtomType.Value_String: // "text"
                        {
                            positions?.Add(new(position, reader.AtomLineNumber, reader.AtomBytePositionInLine));
                            var value = reader.ValueSpan;
                            array = EnsureCapacity(ref buffer, array, position, value.Length + 5);
                            position = WriteStringHeader(array, position, value.Length);
                            value.CopyTo(array.AsSpan(position));
                            position += value.Length;
                            if (depth > 0)
                            {
                                stack[depth - 1] += 2;
                            }

                            break;
                        }

                    case TinyhandAtomType.SpecialIdentifier: // @mode
                        {
                            positions?.Add(new(position, reader.AtomLineNumber, reader.AtomBytePositionInLine));
                            var value = reader.ValueSpan;
                            array = EnsureCapacity(ref buffer, array, position, value.Length + 6);
                            position = WriteStringHeader(array, position, value.Length + 1);
                            array[position++] = TinyhandConstants.IdentifierPrefix;
                            value.CopyTo(array.AsSpan(position));
                            position += value.Length;
                            if (depth > 0)
                            {
                                stack[depth - 1] += 2;
                            }

                            break;
                        }

                    case TinyhandAtomType.Assignment: // =
                        if (depth > 0)
                        {
                            stack[depth - 1] |= 1;
                        }

                        break;

                    case TinyhandAtomType.Value_Base64: // b"Base64"
                        {
                            positions?.Add(new(position, reader.AtomLineNumber, reader.AtomBytePositionInLine));
                            var base64 = reader.ValueSpan;
                            var decodedLength = Arc.Crypto.Base64Url.GetDecodedLength(base64);
                            array = EnsureCapacity(ref buffer, array, position, decodedLength + 5);
                            position = WriteBinHeader(array, position, decodedLength);
                            if (!Arc.Crypto.Base64Url.TryDecode(base64, array.AsSpan(position, decodedLength), out var written) ||
                                written != decodedLength)
                            {
                                reader.ThrowBase64Exception();
                            }

                            position += decodedLength;
                            if (depth > 0)
                            {
                                stack[depth - 1] += 2;
                            }

                            break;
                        }

                    case TinyhandAtomType.Value_Long: // -123(long)
                        positions?.Add(new(position, reader.AtomLineNumber, reader.AtomBytePositionInLine));
                        array = EnsureCapacity(ref buffer, array, position, 9);
                        position = WriteInt64(array, position, reader.ValueLong);
                        if (depth > 0)
                        {
                            stack[depth - 1] += 2;
                        }

                        break;

                    case TinyhandAtomType.Value_ULong: // 123(ulong)
                        positions?.Add(new(position, reader.AtomLineNumber, reader.AtomBytePositionInLine));
                        array = EnsureCapacity(ref buffer, array, position, 9);
                        position = WriteUInt64(array, position, reader.ValueULong);
                        if (depth > 0)
                        {
                            stack[depth - 1] += 2;
                        }

                        break;

                    case TinyhandAtomType.Value_Double: // 1.23(double)
                        positions?.Add(new(position, reader.AtomLineNumber, reader.AtomBytePositionInLine));
                        array = EnsureCapacity(ref buffer, array, position, 9);
                        array[position] = MessagePackCode.Float64;
                        BinaryPrimitives.WriteDoubleBigEndian(array.AsSpan(position + 1), reader.ValueDouble);
                        position += 9;
                        if (depth > 0)
                        {
                            stack[depth - 1] += 2;
                        }

                        break;

                    case TinyhandAtomType.Value_Null: // null
                        positions?.Add(new(position, reader.AtomLineNumber, reader.AtomBytePositionInLine));
                        array = EnsureCapacity(ref buffer, array, position, 1);
                        array[position++] = MessagePackCode.Nil;
                        if (depth > 0)
                        {
                            stack[depth - 1] += 2;
                        }

                        break;

                    case TinyhandAtomType.Value_True: // true
                        positions?.Add(new(position, reader.AtomLineNumber, reader.AtomBytePositionInLine));
                        array = EnsureCapacity(ref buffer, array, position, 1);
                        array[position++] = MessagePackCode.True;
                        if (depth > 0)
                        {
                            stack[depth - 1] += 2;
                        }

                        break;

                    case TinyhandAtomType.Value_False: // false
                        positions?.Add(new(position, reader.AtomLineNumber, reader.AtomBytePositionInLine));
                        array = EnsureCapacity(ref buffer, array, position, 1);
                        array[position++] = MessagePackCode.False;
                        if (depth > 0)
                        {
                            stack[depth - 1] += 2;
                        }

                        break;

                    default: // Separator, LineFeed, Modifier, Comment
                        break;
                }
            }

Done:
            // Unclosed groups (including the virtual top level group) are closed at the end of the data.
            while (depth > 0)
            {
                depth--;
                array = FinalizeGroup(stack[depth], ref buffer, array, ref position, positions);
                if (depth > 0)
                {
                    stack[depth - 1] += 2;
                }
            }

            buffer.Length = position;
        }
        finally
        {
            if (rentedStack is not null)
            {
                ArrayPool<long>.Shared.Return(rentedStack);
            }
        }
    }

    /// <summary>
    /// Writes the header of a completed group. The header was reserved as one byte; a larger header shifts the content of the group.
    /// </summary>
    private static byte[] FinalizeGroup(long entry, ref BinaryBuffer buffer, byte[] array, ref int position, List<AtomPosition>? positions)
    {
        var headerPosition = (int)(entry >> 32);
        var count = (int)((entry >> 1) & 0x7FFF_FFFF);
        var isMap = (entry & 1) != 0;
        var n = isMap ? count >> 1 : count;

        if (n <= MessagePackRange.MaxFixMapCount)
        {// MaxFixMapCount == MaxFixArrayCount
            array[headerPosition] = (byte)((isMap ? MessagePackCode.MinFixMap : MessagePackCode.MinFixArray) | n);
            return array;
        }

        var shift = n <= ushort.MaxValue ? 2 : 4;
        array = EnsureCapacity(ref buffer, array, position, shift);
        var contentPosition = headerPosition + 1;
        System.Array.Copy(array, contentPosition, array, contentPosition + shift, position - contentPosition);
        position += shift;

        if (shift == 2)
        {
            array[headerPosition] = isMap ? MessagePackCode.Map16 : MessagePackCode.Array16;
            BinaryPrimitives.WriteUInt16BigEndian(array.AsSpan(headerPosition + 1), (ushort)n);
        }
        else
        {
            array[headerPosition] = isMap ? MessagePackCode.Map32 : MessagePackCode.Array32;
            BinaryPrimitives.WriteUInt32BigEndian(array.AsSpan(headerPosition + 1), (uint)n);
        }

        if (positions is not null)
        {
            var span = CollectionsMarshal.AsSpan(positions);
            for (var i = span.Length - 1; i >= 0 && span[i].Position > headerPosition; i--)
            {
                span[i].Position += shift;
            }
        }

        return array;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte[] EnsureCapacity(ref BinaryBuffer buffer, byte[] array, int position, int size)
    {
        if (array.Length - position < size)
        {
            return buffer.Grow(position, size);
        }

        return array;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int WriteStringHeader(byte[] array, int position, int byteCount)
    {
        if (byteCount <= MessagePackRange.MaxFixStringLength)
        {
            array[position] = (byte)(MessagePackCode.MinFixStr | byteCount);
            return position + 1;
        }
        else if (byteCount <= byte.MaxValue)
        {
            array[position] = MessagePackCode.Str8;
            array[position + 1] = unchecked((byte)byteCount);
            return position + 2;
        }
        else if (byteCount <= ushort.MaxValue)
        {
            array[position] = MessagePackCode.Str16;
            BinaryPrimitives.WriteUInt16BigEndian(array.AsSpan(position + 1), (ushort)byteCount);
            return position + 3;
        }
        else
        {
            array[position] = MessagePackCode.Str32;
            BinaryPrimitives.WriteUInt32BigEndian(array.AsSpan(position + 1), (uint)byteCount);
            return position + 5;
        }
    }

    private static int WriteBinHeader(byte[] array, int position, int length)
    {
        if (length <= byte.MaxValue)
        {
            array[position] = MessagePackCode.Bin8;
            array[position + 1] = unchecked((byte)length);
            return position + 2;
        }
        else if (length <= ushort.MaxValue)
        {
            array[position] = MessagePackCode.Bin16;
            BinaryPrimitives.WriteUInt16BigEndian(array.AsSpan(position + 1), (ushort)length);
            return position + 3;
        }
        else
        {
            array[position] = MessagePackCode.Bin32;
            BinaryPrimitives.WriteUInt32BigEndian(array.AsSpan(position + 1), (uint)length);
            return position + 5;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int WriteUInt64(byte[] array, int position, ulong value)
    {
        if (value <= MessagePackRange.MaxFixPositiveInt)
        {
            array[position] = unchecked((byte)value);
            return position + 1;
        }
        else if (value <= byte.MaxValue)
        {
            array[position] = MessagePackCode.UInt8;
            array[position + 1] = unchecked((byte)value);
            return position + 2;
        }
        else if (value <= ushort.MaxValue)
        {
            array[position] = MessagePackCode.UInt16;
            BinaryPrimitives.WriteUInt16BigEndian(array.AsSpan(position + 1), (ushort)value);
            return position + 3;
        }
        else if (value <= uint.MaxValue)
        {
            array[position] = MessagePackCode.UInt32;
            BinaryPrimitives.WriteUInt32BigEndian(array.AsSpan(position + 1), (uint)value);
            return position + 5;
        }
        else
        {
            array[position] = MessagePackCode.UInt64;
            BinaryPrimitives.WriteUInt64BigEndian(array.AsSpan(position + 1), value);
            return position + 9;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int WriteInt64(byte[] array, int position, long value)
    {
        if (value >= 0)
        {
            return WriteUInt64(array, position, (ulong)value);
        }
        else if (value >= MessagePackRange.MinFixNegativeInt)
        {
            array[position] = unchecked((byte)value);
            return position + 1;
        }
        else if (value >= sbyte.MinValue)
        {
            array[position] = MessagePackCode.Int8;
            array[position + 1] = unchecked((byte)value);
            return position + 2;
        }
        else if (value >= short.MinValue)
        {
            array[position] = MessagePackCode.Int16;
            BinaryPrimitives.WriteInt16BigEndian(array.AsSpan(position + 1), (short)value);
            return position + 3;
        }
        else if (value >= int.MinValue)
        {
            array[position] = MessagePackCode.Int32;
            BinaryPrimitives.WriteInt32BigEndian(array.AsSpan(position + 1), (int)value);
            return position + 5;
        }
        else
        {
            array[position] = MessagePackCode.Int64;
            BinaryPrimitives.WriteInt64BigEndian(array.AsSpan(position + 1), value);
            return position + 9;
        }
    }

    #endregion

    #region Element

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
                if (extHeader.TypeCode == MessagePackExtensionCodes.DateTime)
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

    #endregion
}
