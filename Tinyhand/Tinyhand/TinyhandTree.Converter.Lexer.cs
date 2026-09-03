// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Buffers.Text;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Tinyhand.IO;

#pragma warning disable SA1202

namespace Tinyhand;

/// <summary>
/// Provides the specialized lexer for converting UTF-8 text to binary data.
/// </summary>
public static partial class TinyhandTreeConverter
{
    /// <summary>
    /// The class of the first byte of a token, used to dispatch with a single jump table.
    /// </summary>
    private static class CharClass
    {
        public const byte Other = 0; // Part of an identifier or a number.
        public const byte Space = 1;
        public const byte LineFeed = 2;
        public const byte WhiteSpace = 3; // U+0009, U+000B, U+000C, U+000D
        public const byte Separator = 4; // , ;
        public const byte OpenBrace = 5;
        public const byte CloseBrace = 6;
        public const byte Quote = 7;
        public const byte Quote2 = 8;
        public const byte Equals2 = 9;
        public const byte Slash = 10;
        public const byte Sharp = 11;
        public const byte Digit = 12;
        public const byte Plus = 13;
        public const byte Hyphen = 14;
        public const byte BinaryPrefix = 15; // b
        public const byte Modifier = 16; // &
        public const byte At = 17; // @
        public const byte MultiByte = 18; // 0xC2, 0xE2, 0xE3: may be a multi-byte white space.
        public const byte Delimiter = 19; // ( )
    }

    private static readonly byte[] CharClassTable = CreateCharClassTable();

    private static byte[] CreateCharClassTable()
    {
        var table = new byte[256];
        table[TinyhandConstants.Space] = CharClass.Space;
        table[TinyhandConstants.LineFeed] = CharClass.LineFeed;
        table[0x09] = CharClass.WhiteSpace;
        table[0x0B] = CharClass.WhiteSpace;
        table[0x0C] = CharClass.WhiteSpace;
        table[0x0D] = CharClass.WhiteSpace;
        table[TinyhandConstants.Separator] = CharClass.Separator;
        table[TinyhandConstants.Separator2] = CharClass.Separator;
        table[TinyhandConstants.OpenBrace] = CharClass.OpenBrace;
        table[TinyhandConstants.CloseBrace] = CharClass.CloseBrace;
        table[TinyhandConstants.Quote] = CharClass.Quote;
        table[TinyhandConstants.Quote2] = CharClass.Quote2;
        table[TinyhandConstants.EqualsSign] = CharClass.Equals2;
        table[TinyhandConstants.Slash] = CharClass.Slash;
        table[TinyhandConstants.Sharp] = CharClass.Sharp;
        for (var i = '0'; i <= '9'; i++)
        {
            table[i] = CharClass.Digit;
        }

        table[TinyhandConstants.Plus] = CharClass.Plus;
        table[TinyhandConstants.Hyphen] = CharClass.Hyphen;
        table[(byte)'b'] = CharClass.BinaryPrefix;
        table[TinyhandConstants.ModifierPrefix] = CharClass.Modifier;
        table[TinyhandConstants.IdentifierPrefix] = CharClass.At;
        table[0xC2] = CharClass.MultiByte;
        table[0xE2] = CharClass.MultiByte;
        table[0xE3] = CharClass.MultiByte;
        table[TinyhandConstants.LeftParenthesis] = CharClass.Delimiter;
        table[TinyhandConstants.RightParenthesis] = CharClass.Delimiter;
        return table;
    }

    /// <summary>
    /// Converts UTF-8 text to a sequence of byte with a lexer specialized for this conversion.<br/>
    /// It recognizes exactly the same syntax as <see cref="TinyhandUtf8Reader"/> (and throws the same exceptions),
    /// but keeps all of its state in local variables, does not materialize atoms it does not need
    /// (separators, comments, modifiers) and writes the values straight into the output buffer.<br/>
    /// The line and byte position are only needed for error messages, so they are tracked with a single store per line feed.
    /// </summary>
    private static void FromUtf8ToBinaryFast(ReadOnlySpan<byte> utf8, bool omitTopLevelBracket, ref BinaryBuffer buffer)
    {
        if (utf8.StartsWith(TinyhandConstants.Utf8Bom))
        { // Ignore UTF-8 BOM
            utf8 = utf8.Slice(TinyhandConstants.Utf8Bom.Length);
        }

        ref byte src = ref MemoryMarshal.GetReference(utf8);
        var length = utf8.Length;
        var p = 0;
        var lineNumber = 1;
        var lineStart = 0;
        var atLineStart = true;
        var groupStack = default(TinyhandGroupStack);
        ReadOnlySpan<byte> scanTable = TinyhandConstants.FirstByteTable;
        ref byte classTable = ref MemoryMarshal.GetArrayDataReference(CharClassTable);

        var array = buffer.Array;
        var position = 0;

        // Stack entry: (header position << 32) | (item count << 1) | (assignment found ? 1 : 0).
        Span<long> stack = stackalloc long[InitialStackDepth];
        long[]? rentedStack = null;
        var depth = 0;
        var stopDepth = 0;
        TinyhandAtomType group;
        byte b;
        byte cls;

        try
        {
            if (omitTopLevelBracket)
            {// A virtual top level group.
                array = EnsureCapacity(ref buffer, array, position, 1);
                position = 1;
                stack[depth++] = 0;
                stopDepth = 1;
            }

Loop:
// 1. Brackets stored in the group stack.
            group = groupStack.GetGroup();
            if (group != TinyhandAtomType.None)
            {
                goto HandleGroup;
            }

            if (p >= length)
            { // No data left.
                groupStack.TerminateIndent();
                group = groupStack.GetGroup();
                if (group == TinyhandAtomType.None)
                {
                    goto Done;
                }

                goto HandleGroup;
            }

            b = Unsafe.Add(ref src, p);
            cls = Unsafe.Add(ref classTable, b);

Dispatch:
            switch (cls)
            {
                case CharClass.Space:
                    { // A run of spaces (indentation) is skipped in a tight loop.
                        do
                        {
                            p++;
                        }
                        while (p < length && Unsafe.Add(ref src, p) == TinyhandConstants.Space);

                        goto Loop;
                    }

                case CharClass.LineFeed:
                    p++;
                    lineNumber++;
                    lineStart = p;
                    atLineStart = true;
                    goto Loop;

                case CharClass.WhiteSpace:
                case CharClass.Separator:
                    p++;
                    goto Loop;

                case CharClass.MultiByte:
                    {
                        var remaining = length - p;
                        if (b == 0xC2)
                        {
                            if (remaining >= 2 && Unsafe.Add(ref src, p + 1) == 0xA0)
                            { // U+00A0 (C2 A0)
                                p += 2;
                                goto Loop;
                            }
                        }
                        else if (b == 0xE2)
                        {
                            if (remaining >= 3 && Unsafe.Add(ref src, p + 1) == 0x80)
                            {
                                var third = Unsafe.Add(ref src, p + 2);
                                if (third >= 0x80 && third <= 0x8A)
                                {// U+2000 to U+200A, E2 80 80 to E2 80 8A
                                    p += 3;
                                    goto Loop;
                                }
                                else if (third == 0xA8 || third == 0xA9)
                                {// U+2028- U+2029, E2 80 A8 to E2 80 A9
                                    p += 3;
                                    lineNumber++;
                                    lineStart = p;
                                    atLineStart = true;
                                    goto Loop;
                                }
                            }
                        }
                        else
                        {// 0xE3
                            if (remaining >= 3 && Unsafe.Add(ref src, p + 1) == 0x80 && Unsafe.Add(ref src, p + 2) == 0x80)
                            { // U+3000, E3 80 80
                                p += 3;
                                goto Loop;
                            }
                        }

                        // Otherwise the byte starts an identifier.
                        if (atLineStart)
                        {
                            goto IndentCheck;
                        }

                        goto RawToken;
                    }

                case CharClass.OpenBrace: // {
                    if (atLineStart)
                    {
                        goto IndentCheck;
                    }

                    groupStack.AddOpenBracket();
                    p++;
                    goto Loop;

                case CharClass.CloseBrace: // }
                    if (atLineStart)
                    {
                        goto IndentCheck;
                    }

                    groupStack.AddCloseBracket();
                    p++;
                    goto Loop;

                case CharClass.Quote: // "string"
                case CharClass.Quote2: // 'string'
                    {
                        if (atLineStart)
                        {
                            goto IndentCheck;
                        }

                        var quote = b;
                        p++; // Skip quote.
                        if (length - p >= 2 && Unsafe.Add(ref src, p) == quote && Unsafe.Add(ref src, p + 1) == quote)
                        { // """Triple quoted string""". Multi-line literal.
                            p += 2; // Skip 2 quotes.
                            var start = p;
                            var i = start;
                            for (; i < length; i++)
                            {
                                var v = Unsafe.Add(ref src, i);
                                if (v < 0x20)
                                {
                                    if (v < 0x09 || v > 0x0D)
                                    {
                                        ThrowAt("A literal can not contain control characters except CR/LF.", lineNumber, start - lineStart);
                                    }
                                }
                                else if (v == quote && (i + 2 < length) && Unsafe.Add(ref src, i + 1) == quote && Unsafe.Add(ref src, i + 2) == quote)
                                { // """
                                    break;
                                }
                            }

                            if (i >= length)
                            {
                                ThrowUnexpectedEnd(lineNumber, start - lineStart);
                            }

                            var n = i - start;
                            array = EnsureCapacity(ref buffer, array, position, n + 5);
                            position = WriteStringHeader(array, position, n);
                            utf8.Slice(start, n).CopyTo(array.AsSpan(position));
                            position += n;
                            p = i + 3; // String + 3 quotes.
                        }
                        else
                        { // "single line string" or 'string'
                            var start = p;
                            var i = start;
                            var hasEscape = false;
                            for (; i < length; i++)
                            {
                                var v = Unsafe.Add(ref src, i);
                                if (v < 0x20)
                                {
                                    ThrowAt("\"Single-line literal\" cannot contain control characters. Use \"\"\"Multi-line literal\"\"\" instead.", lineNumber, start - lineStart);
                                }
                                else if (v == quote)
                                {
                                    break;
                                }
                                else if (v == TinyhandConstants.BackSlash)
                                {
                                    hasEscape = true;
                                    if (i + 1 < length)
                                    { // Skip \?
                                        i++;
                                    }
                                }
                            }

                            if (i >= length)
                            {
                                ThrowUnexpectedEnd(lineNumber, start - lineStart);
                            }

                            var n = i - start;
                            array = EnsureCapacity(ref buffer, array, position, n + 5);
                            if (!hasEscape)
                            {// Verbatim.
                                position = WriteStringHeader(array, position, n);
                                CopySmall(ref Unsafe.Add(ref src, start), n, array, position);
                                position += n;
                            }
                            else
                            {// Unescape straight into the buffer; the unescaped string is never longer than the escaped one.
                                var headerLength = GetStringHeaderLength(n);
                                TinyhandHelper.Unescape(utf8.Slice(start, n), array.AsSpan(position + headerLength, n), out var written);
                                var actualHeaderLength = GetStringHeaderLength(written);
                                if (actualHeaderLength != headerLength)
                                {
                                    Array.Copy(array, position + headerLength, array, position + actualHeaderLength, written);
                                }

                                position = WriteStringHeader(array, position, written) + written;
                            }

                            p = i + 1; // String + quote.
                        }

                        if (depth > 0)
                        {
                            Top(stack, depth) += 2;
                        }

                        goto Loop;
                    }

                case CharClass.Equals2: // =
                    if (atLineStart)
                    {
                        goto IndentCheck;
                    }

                    if (depth > 0)
                    {
                        Top(stack, depth) |= 1;
                    }

                    p++;
                    goto Loop;

                case CharClass.Slash: // // or /*
                    {
                        p++; // Skip slash.
                        if (p == length)
                        { // No data left.
                            goto Loop;
                        }

                        var c = Unsafe.Add(ref src, p);
                        if (c == TinyhandConstants.Slash)
                        { // Single line comment.
                            goto SingleLineComment;
                        }
                        else if (c == TinyhandConstants.Asterisk)
                        { // Multi line comment.
                            var i = p;
                            while (i < length)
                            {
                                var v = Unsafe.Add(ref src, i);
                                if (v == TinyhandConstants.LineFeed)
                                { // \n
                                    i++;
                                    lineNumber++;
                                    lineStart = i;
                                    atLineStart = true;
                                    continue;
                                }
                                else if (v == 0xE2 && length - i >= 3 && Unsafe.Add(ref src, i + 1) == 0x80 && (Unsafe.Add(ref src, i + 2) == 0xA8 || Unsafe.Add(ref src, i + 2) == 0xA9))
                                {// U+2028- U+2029, E2 80 A8 to E2 80 A9
                                    i += 3;
                                    lineNumber++;
                                    lineStart = i;
                                    atLineStart = true;
                                    continue;
                                }
                                else if (v == TinyhandConstants.Asterisk && length - i >= 2 && Unsafe.Add(ref src, i + 1) == TinyhandConstants.Slash)
                                { // "*/" to exit.
                                    p = i + 2;
                                    goto Loop;
                                }

                                i++;
                            }

                            // The comment is terminated by the end of the data.
                            p = length;
                            goto Loop;
                        }
                        else
                        { // Unexpected character.
                            ThrowUnexpectedCharacter(c, lineNumber, p - lineStart);
                        }

                        goto Loop;
                    }

                case CharClass.Sharp: // #
                    p++; // Skip sharp.
                    if (p == length)
                    { // No data left.
                        goto Loop;
                    }

                    goto SingleLineComment;

                case CharClass.Digit:
                    {
                        if (atLineStart)
                        {
                            goto IndentCheck;
                        }

                        // A token made of digits only (at most 18) is scanned and parsed in one pass.
                        ulong value = (uint)(b - '0');
                        var q = p + 1;
                        while (q < length)
                        {
                            var d = (uint)(Unsafe.Add(ref src, q) - '0');
                            if (d > 9)
                            {
                                break;
                            }

                            value = (value * 10) + d;
                            q++;
                        }

                        if ((q - p) <= 18 && (q == length || IsDelimiter(scanTable, ref src, q, length - q)))
                        {
                            array = EnsureCapacity(ref buffer, array, position, 9);
                            position = WriteUInt64(array, position, value);
                            if (depth > 0)
                            {
                                Top(stack, depth) += 2;
                            }

                            p = q;
                            goto Loop;
                        }

                        goto Number;
                    }

                case CharClass.Plus:
                    if (atLineStart)
                    {
                        goto IndentCheck;
                    }

                    if (length - p >= 2 && Unsafe.Add(ref src, p + 1) == TinyhandConstants.Space)
                    {// "+ ": an indented group.
                        groupStack.AddIndent();
                        p += 2;
                        goto Loop;
                    }

                    goto Number;

                case CharClass.Hyphen:
                    if (atLineStart)
                    {
                        goto IndentCheck;
                    }

                    goto Number;

                case CharClass.BinaryPrefix: // b"Base64" or b'Base64'
                    {
                        if (atLineStart)
                        {
                            goto IndentCheck;
                        }

                        if (length - p < 2)
                        {
                            goto RawToken;
                        }

                        var quote = Unsafe.Add(ref src, p + 1);
                        if (quote != TinyhandConstants.Quote && quote != TinyhandConstants.Quote2)
                        {
                            goto RawToken;
                        }

                        p += 2; // Skip b"
                        var start = p;
                        var i = start;
                        for (; i < length; i++)
                        {
                            var v = Unsafe.Add(ref src, i);
                            if (v < 0x20)
                            {
                                ThrowAt("\"Single-line literal\" cannot contain control characters. Use \"\"\"Multi-line literal\"\"\" instead.", lineNumber, start - lineStart);
                            }
                            else if (v == quote)
                            {
                                break;
                            }
                            else if (v == TinyhandConstants.BackSlash)
                            {
                                if (i + 1 < length)
                                { // Skip \?
                                    i++;
                                }
                            }
                        }

                        if (i >= length)
                        {
                            ThrowUnexpectedEnd(lineNumber, start - lineStart);
                        }

                        var base64 = utf8.Slice(start, i - start);
                        var decodedLength = Arc.Crypto.Base64Url.GetDecodedLength(base64);
                        array = EnsureCapacity(ref buffer, array, position, decodedLength + 5);
                        var contentPosition = WriteBinHeader(array, position, decodedLength);
                        if (!Arc.Crypto.Base64Url.TryDecode(base64, array.AsSpan(contentPosition, decodedLength), out var written) ||
                            written != decodedLength)
                        {
                            ThrowAt("Cannot decode Base64 string.", lineNumber, start - lineStart);
                        }

                        position = contentPosition + decodedLength;
                        if (depth > 0)
                        {
                            Top(stack, depth) += 2;
                        }

                        p = i + 1; // String + quote.
                        goto Loop;
                    }

                case CharClass.Modifier: // &i32, &key, &required
                    {
                        if (atLineStart)
                        {
                            goto IndentCheck;
                        }

                        var q = ScanToken(scanTable, ref src, p, length);
                        if (TinyhandHelper.ModifierTable.TryGetValue(utf8.Slice(p + 1, q - p - 1), out _))
                        {
                            p = q;
                            goto Loop;
                        }

                        ThrowUnexpectedCharacter(b, lineNumber, p + 1 - lineStart);
                        goto Loop;
                    }

                case CharClass.At: // @ Special Identifier
                    {
                        if (atLineStart)
                        {
                            goto IndentCheck;
                        }

                        var q = ScanToken(scanTable, ref src, p, length);
                        var identifier = utf8.Slice(p + 1, q - p - 1);
                        if (identifier.Length == 0)
                        {
                            ThrowUnexpectedCharacter(b, lineNumber, p + 1 - lineStart);
                        }

                        var first = identifier[0];
                        if (TinyhandHelper.IsDigit(first) || first == TinyhandConstants.Plus || first == TinyhandConstants.Hyphen)
                        { // Number
                            ThrowAt("An identifier can not begin with a digit.", lineNumber, p + 1 - lineStart);
                        }

                        array = EnsureCapacity(ref buffer, array, position, identifier.Length + 6);
                        position = WriteStringHeader(array, position, identifier.Length + 1);
                        array[position++] = TinyhandConstants.IdentifierPrefix;
                        identifier.CopyTo(array.AsSpan(position));
                        position += identifier.Length;
                        if (depth > 0)
                        {
                            Top(stack, depth) += 2;
                        }

                        p = q;
                        goto Loop;
                    }

                case CharClass.Delimiter: // ( )
                    if (atLineStart)
                    {
                        goto IndentCheck;
                    }

                    ThrowUnexpectedCharacter(b, lineNumber, p - lineStart);
                    goto Loop;

                default: // CharClass.Other: identifier, null, true, false, double.NaN...
                    if (atLineStart)
                    {
                        goto IndentCheck;
                    }

                    goto RawToken;
            }

IndentCheck:
            {// The first token of a line: the indentation opens or closes indented groups.
                atLineStart = false;
                var indent = p - lineStart;
                if (indent != groupStack.Depth * 2)
                {
                    if (groupStack.TrySetIndent(indent) is { } indentError && b != TinyhandConstants.CloseBrace)
                    {
                        ThrowAt(indentError, lineNumber, indent);
                    }

                    group = groupStack.GetGroup();
                    if (group != TinyhandAtomType.None)
                    {
                        goto HandleGroup;
                    }
                }

                goto Dispatch;
            }

Number:
            {// General number: sign, fraction, exponent, long digit runs.
                var q = p;
                var isDouble = false;
                for (; q < length; q++)
                {
                    var v = Unsafe.Add(ref src, q);
                    if (TinyhandHelper.IsDigit(v))
                    {// Most of the bytes are digits.
                        continue;
                    }

                    if (IsDelimiter(scanTable, ref src, q, length - q))
                    {
                        break;
                    }

                    if (v == '.' || v == 'e' || v == 'E')
                    {
                        isDouble = true;
                    }
                    else if (v == '+' || v == '-')
                    {
                    }
                    else
                    {// Not a number: an identifier such as "123abc".
                        goto RawToken;
                    }
                }

                var number = utf8.Slice(p, q - p);
                array = EnsureCapacity(ref buffer, array, position, 9);

                // The whole token must be consumed; otherwise a value like "1.2.3" would silently become 1.2.
                if (isDouble)
                {
                    if (Utf8Parser.TryParse(number, out double doubleResult, out var bytesConsumed) && bytesConsumed == number.Length)
                    {
                        array[position] = MessagePackCode.Float64;
                        BinaryPrimitives.WriteDoubleBigEndian(array.AsSpan(position + 1), doubleResult);
                        position += 9;
                        goto NumberWritten;
                    }
                }
                else
                {
                    if (TryParseInt64Fast(number, out var longResult) ||
                        (Utf8Parser.TryParse(number, out longResult, out var bytesConsumed) && bytesConsumed == number.Length))
                    {// long
                        position = WriteInt64(array, position, longResult);
                        goto NumberWritten;
                    }

                    if (Utf8Parser.TryParse(number, out ulong ulongResult, out bytesConsumed) && bytesConsumed == number.Length)
                    {// Maybe ulong...
                        position = WriteUInt64(array, position, ulongResult);
                        goto NumberWritten;
                    }
                }

                ThrowAt($"\"{Encoding.UTF8.GetString(number)}\" is not a valid number.", lineNumber, p - lineStart);

NumberWritten:
                if (depth > 0)
                {
                    Top(stack, depth) += 2;
                }

                p = q;
                goto Loop;
            }

RawToken:
            {// identifier, null, true, false, double.NaN, double.PositiveInfinity, double.NegativeInfinity
                var q = ScanToken(scanTable, ref src, p, length);
                var n = q - p;
                if (n == 0)
                {
                    ThrowUnexpectedCharacter(b, lineNumber, p - lineStart);
                }

                if (n == 4)
                {
                    var v = Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref src, p));
                    if (!BitConverter.IsLittleEndian)
                    {
                        v = BinaryPrimitives.ReverseEndianness(v);
                    }

                    if (v == 0x6C6C756E)
                    { // "null"
                        array = EnsureCapacity(ref buffer, array, position, 1);
                        array[position++] = MessagePackCode.Nil;
                        goto RawTokenWritten;
                    }
                    else if (v == 0x65757274)
                    { // "true"
                        array = EnsureCapacity(ref buffer, array, position, 1);
                        array[position++] = MessagePackCode.True;
                        goto RawTokenWritten;
                    }
                }
                else if (n == 5)
                {
                    var v = Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref src, p));
                    if (!BitConverter.IsLittleEndian)
                    {
                        v = BinaryPrimitives.ReverseEndianness(v);
                    }

                    if (v == 0x736C6166 && Unsafe.Add(ref src, p + 4) == (byte)'e')
                    { // "false"
                        array = EnsureCapacity(ref buffer, array, position, 1);
                        array[position++] = MessagePackCode.False;
                        goto RawTokenWritten;
                    }
                }
                else if (n >= TinyhandConstants.DoubleNaNSpan.Length && b == (byte)'d')
                {
                    var raw = utf8.Slice(p, n);
                    double doubleValue;
                    if (raw.SequenceEqual(TinyhandConstants.DoubleNaNSpan))
                    {
                        doubleValue = double.NaN;
                    }
                    else if (raw.SequenceEqual(TinyhandConstants.DoublePositiveInfinitySpan))
                    {
                        doubleValue = double.PositiveInfinity;
                    }
                    else if (raw.SequenceEqual(TinyhandConstants.DoubleNegativeInfinitySpan))
                    {
                        doubleValue = double.NegativeInfinity;
                    }
                    else
                    {
                        goto Identifier;
                    }

                    array = EnsureCapacity(ref buffer, array, position, 9);
                    array[position] = MessagePackCode.Float64;
                    BinaryPrimitives.WriteDoubleBigEndian(array.AsSpan(position + 1), doubleValue);
                    position += 9;
                    goto RawTokenWritten;
                }

Identifier:
                array = EnsureCapacity(ref buffer, array, position, n + 5);
                position = WriteStringHeader(array, position, n);
                CopySmall(ref Unsafe.Add(ref src, p), n, array, position);
                position += n;

RawTokenWritten:
                if (depth > 0)
                {
                    Top(stack, depth) += 2;
                }

                p = q;
                goto Loop;
            }

SingleLineComment:
            {// p points to the second character of the comment marker.
                var i = p;
                for (; i < length; i++)
                {
                    var v = Unsafe.Add(ref src, i);
                    if (v == TinyhandConstants.LineFeed)
                    { // \n
                        p = i + 1;
                        lineNumber++;
                        lineStart = p;
                        atLineStart = true;
                        goto Loop;
                    }
                    else if (v == 0xE2 && length - i >= 3 && Unsafe.Add(ref src, i + 1) == 0x80 && (Unsafe.Add(ref src, i + 2) == 0xA8 || Unsafe.Add(ref src, i + 2) == 0xA9))
                    {// U+2028- U+2029, E2 80 A8 to E2 80 A9
                        p = i + 3;
                        lineNumber++;
                        lineStart = p;
                        atLineStart = true;
                        goto Loop;
                    }
                }

                // The comment is terminated by the end of the data.
                p = length;
                goto Loop;
            }

HandleGroup:
            if (group == TinyhandAtomType.StartGroup)
            {
                array = EnsureCapacity(ref buffer, array, position, 1);
                if (depth == stack.Length)
                {
                    GrowStack(ref stack, ref rentedStack);
                }

                stack[depth++] = (long)position << 32;
                position++;
            }
            else
            {// EndGroup
                if (depth == stopDepth)
                {// The end of the top level group: the rest is ignored.
                    goto Done;
                }

                depth--;
                array = FinalizeGroup(stack[depth], ref buffer, array, ref position, null);
                if (depth > 0)
                {
                    Top(stack, depth) += 2;
                }
            }

            goto Loop;

Done:
// Unclosed groups (including the virtual top level group) are closed at the end of the data.
            while (depth > 0)
            {
                depth--;
                array = FinalizeGroup(stack[depth], ref buffer, array, ref position, null);
                if (depth > 0)
                {
                    Top(stack, depth) += 2;
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
    /// Returns the position of the first white space or delimiter at or after <paramref name="position"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int ScanToken(ReadOnlySpan<byte> scanTable, ref byte src, int position, int length)
    {
        for (; position < length; position++)
        {
            // UTF-8 first byte table. 0:other, 1:may be white space, 2:white space, 3:delimiters
            var tv = scanTable[Unsafe.Add(ref src, position)];
            if (tv == 0)
            {
                continue;
            }
            else if (tv >= 2 || IsMultiByteWhiteSpace(ref src, position, length - position))
            {
                break;
            }
        }

        return position;
    }

    /// <summary>
    /// Determines whether the byte at <paramref name="position"/> terminates a token (white space or delimiter).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsDelimiter(ReadOnlySpan<byte> scanTable, ref byte src, int position, int remaining)
    {
        // UTF-8 first byte table. 0:other, 1:may be white space, 2:white space, 3:delimiters
        var tv = scanTable[Unsafe.Add(ref src, position)];
        if (tv >= 2)
        { // White space or delimiters
            return true;
        }
        else if (tv == 0)
        { // Other characters.
            return false;
        }

        return IsMultiByteWhiteSpace(ref src, position, remaining);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool IsMultiByteWhiteSpace(ref byte src, int position, int remaining)
    {
        var val = Unsafe.Add(ref src, position);
        if (val == 0xC2 && remaining >= 2 && Unsafe.Add(ref src, position + 1) == 0xA0)
        { // U+00A0 (C2 A0)
            return true;
        }

        if (val == 0xE2 && remaining >= 3 && Unsafe.Add(ref src, position + 1) == 0x80)
        { // U+2000 to U+200A, E2 80 80 to E2 80 8A  U+2028- U+2029, E2 80 A8 to E2 80 A9
            var third = Unsafe.Add(ref src, position + 2);
            if (third >= 0x80 && third <= 0x8A)
            {
                return true;
            }
            else if (third == 0xA8 || third == 0xA9)
            {
                return true;
            }
        }

        if (val == 0xE3 && remaining >= 3 && Unsafe.Add(ref src, position + 1) == 0x80 && Unsafe.Add(ref src, position + 2) == 0x80)
        { // U+3000, E3 80 80
            return true;
        }

        return false;
    }

    /// <summary>
    /// Parses "[+|-]digits" with at most 18 digits, which always fits in a <see cref="long"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryParseInt64Fast(ReadOnlySpan<byte> span, out long result)
    {
        var i = 0;
        var negative = false;
        if (span.Length > 0)
        {
            if (span[0] == (byte)'-')
            {
                negative = true;
                i = 1;
            }
            else if (span[0] == (byte)'+')
            {
                i = 1;
            }
        }

        var digits = span.Length - i;
        if ((uint)(digits - 1) >= 18)
        {// No digits, or too many for the fast path.
            result = 0;
            return false;
        }

        ulong value = 0;
        for (; i < span.Length; i++)
        {
            var d = (uint)(span[i] - '0');
            if (d > 9)
            {
                result = 0;
                return false;
            }

            value = (value * 10) + d;
        }

        result = negative ? -(long)value : (long)value;
        return true;
    }

    /// <summary>
    /// Gets the entry of the innermost group.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ref long Top(Span<long> stack, int depth)
        => ref Unsafe.Add(ref MemoryMarshal.GetReference(stack), depth - 1);

    /// <summary>
    /// Copies bytes that are usually few (an identifier or a key) without the overhead of a memmove call.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CopySmall(ref byte source, int length, byte[] array, int position)
    {
        ref byte destination = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(array), position);
        if (length <= 16)
        {
            for (var i = 0; i < length; i++)
            {
                Unsafe.Add(ref destination, i) = Unsafe.Add(ref source, i);
            }
        }
        else
        {
            Unsafe.CopyBlockUnaligned(ref destination, ref source, (uint)length);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetStringHeaderLength(int byteCount)
        => byteCount <= MessagePackRange.MaxFixStringLength ? 1 : byteCount <= byte.MaxValue ? 2 : byteCount <= ushort.MaxValue ? 3 : 5;

    /// <summary>
    /// Throws a <see cref="TinyhandException"/> with the same message format as <see cref="TinyhandUtf8Reader"/>.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="lineNumber">The line number (1-based).</param>
    /// <param name="bytePositionInLine">The byte position in the line (0-based).</param>
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowAt(string message, int lineNumber, int bytePositionInLine)
    {
        throw new TinyhandException($"Line: {lineNumber}, Byte Position: {bytePositionInLine + 1}, {message}");
    }

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowUnexpectedCharacter(byte b, int lineNumber, int bytePositionInLine)
        => ThrowAt($"Unexpected character \"{(char)b}\".", lineNumber, bytePositionInLine);

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowUnexpectedEnd(int lineNumber, int bytePositionInLine)
        => ThrowAt("Tinyhand Reader reached the end of the data before the data is complete.", lineNumber, bytePositionInLine);
}
