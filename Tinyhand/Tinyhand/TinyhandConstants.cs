// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;

#pragma warning disable SA1201 // Elements should appear in the correct order
#pragma warning disable SA1401 // Fields should be private
#pragma warning disable SA1516 // Elements should be separated by blank line

namespace Tinyhand;

public static class TinyhandConstants
{
    public const byte OpenBrace = (byte)'{';
    public const byte CloseBrace = (byte)'}';
    public const byte OpenBracket = (byte)'[';
    public const byte CloseBracket = (byte)']';
    public const byte Space = (byte)' ';
    public const byte CarriageReturn = (byte)'\r';
    public const byte LineFeed = (byte)'\n';
    public const byte Tab = (byte)'\t';
    public const byte Separator = (byte)',';
    public const byte Separator2 = (byte)';';
    public const byte Quote = (byte)'"';
    public const byte Quote2 = (byte)'\'';
    public const byte BackSlash = (byte)'\\';
    public const byte Slash = (byte)'/';
    public const byte Sharp = (byte)'#';
    public const byte BackSpace = (byte)'\b';
    public const byte FormFeed = (byte)'\f';
    public const byte Asterisk = (byte)'*';
    public const byte Colon = (byte)':';
    public const byte Period = (byte)'.';
    public const byte Plus = (byte)'+';
    public const byte Hyphen = (byte)'-';
    public const byte EqualsSign = (byte)'=';
    public const byte LeftParenthesis = (byte)'(';
    public const byte RightParenthesis = (byte)')';
    public const byte IdentifierPrefix = (byte)'@';
    public const byte ModifierPrefix = (byte)'&';
    public const byte DoubleSuffix = (byte)'d';
    public const ushort StartGroup = 0x2B20; // '+ '

    public static ReadOnlySpan<byte> Utf8Bom => new byte[] { 0xEF, 0xBB, 0xBF };
    public static ReadOnlySpan<byte> AssignmentSpan => new byte[] { Space, EqualsSign, Space };
    public static ReadOnlySpan<byte> TrueSpan => "true"u8; // new byte[] { (byte)'t', (byte)'r', (byte)'u', (byte)'e' };
    public static ReadOnlySpan<byte> FalseSpan => "false"u8; // new byte[] { (byte)'f', (byte)'a', (byte)'l', (byte)'s', (byte)'e' };
    public static ReadOnlySpan<byte> NullSpan => "null"u8; // new byte[] { (byte)'n', (byte)'u', (byte)'l', (byte)'l' };
    public static ReadOnlySpan<byte> IndentSpan => new byte[] { Space, Space, };
    public static ReadOnlySpan<byte> TripleQuotesSpan => new byte[] { Quote, Quote, Quote, };
    public static ReadOnlySpan<byte> ColonSpan => new byte[] { Colon, };

    public static ReadOnlySpan<byte> DoubleNaNSpan => "double.NaN"u8;

    public static ReadOnlySpan<byte> DoublePositiveInfinitySpan => "double.PositiveInfinity"u8;

    public static ReadOnlySpan<byte> DoubleNegativeInfinitySpan => "double.NegativeInfinity"u8;

    /*public static ReadOnlySpan<byte> FloatNaNSpan => "float.NaN"u8;

    public static ReadOnlySpan<byte> FloatPositiveInfinitySpan => "float.PositiveInfinity"u8;

    public static ReadOnlySpan<byte> FloatNegativeInfinitySpan => "float.NegativeInfinity"u8;*/

    public const int StackallocThreshold = 1024;

    public const int MaxHintSize = 1024 * 1024;

    // In the worst case, an ASCII character represented as a single utf-8 byte could expand 6x when escaped.
    // For example: '+' becomes '\u0043'
    // Escaping surrogate pairs (represented by 3 or 4 utf-8 bytes) would expand to 12 bytes (which is still <= 6x).
    // The same factor applies to utf-16 characters.
    public const int MaxExpansionFactorWhileEscaping = 6;

    // In the worst case, a single UTF-16 character could be expanded to 3 UTF-8 bytes.
    // Only surrogate pairs expand to 4 UTF-8 bytes but that is a transformation of 2 UTF-16 characters goign to 4 UTF-8 bytes (factor of 2).
    // All other UTF-16 characters can be represented by either 1 or 2 UTF-8 bytes.
    public const int MaxExpansionFactorWhileTranscoding = 3;

    public const int MaximumFormatInt64Length = 20;   // 19 + sign (i.e. -9223372036854775808)
    public const int MaximumFormatUInt64Length = 20;  // i.e. 18446744073709551615
    public const int MaximumFormatDoubleLength = 32;
    public const int MaximumFormatSingleLength = 32;
    public const int MaximumFormatDecimalLength = 31; // default (i.e. 'G')
    public const int MaximumFormatGuidLength = 36;    // default (i.e. 'D'), 8 + 4 + 4 + 4 + 12 + 4 for the hyphens (e.g. 094ffa0a-0442-494d-b452-04003fa755cc)
    public const int MaximumEscapedGuidLength = MaxExpansionFactorWhileEscaping * MaximumFormatGuidLength;

    public const int UnicodePlane01StartValue = 0x10000;
    public const int HighSurrogateStartValue = 0xD800;
    public const int HighSurrogateEndValue = 0xDBFF;
    public const int LowSurrogateStartValue = 0xDC00;
    public const int LowSurrogateEndValue = 0xDFFF;
    public const int BitShiftBy10 = 0x400;

    public static byte[] FirstByteTable = new byte[256];

    static TinyhandConstants()
    {
        // UTF-8 first byte table. 0:other, 1:may be white space, 2:white space, 3:delimiters
        FirstByteTable[0x09] = 2;
        FirstByteTable[0x0A] = 2;
        FirstByteTable[0x0B] = 2;
        FirstByteTable[0x0C] = 2;
        FirstByteTable[0x0D] = 2;
        FirstByteTable[TinyhandConstants.Space] = 2;
        FirstByteTable[0xC2] = 1;
        FirstByteTable[0xE2] = 1;
        FirstByteTable[0xE3] = 1;

        // Delimiters
        FirstByteTable[TinyhandConstants.Quote] = 3;
        FirstByteTable[TinyhandConstants.LeftParenthesis] = 3;
        FirstByteTable[TinyhandConstants.RightParenthesis] = 3;
        FirstByteTable[TinyhandConstants.Separator] = 3;
        FirstByteTable[TinyhandConstants.Separator2] = 3;
        // FirstByteTable[TinyhandConstants.OpenBracket] = 3;
        // FirstByteTable[TinyhandConstants.CloseBracket] = 3;
        FirstByteTable[TinyhandConstants.OpenBrace] = 3;
        FirstByteTable[TinyhandConstants.CloseBrace] = 3;
        FirstByteTable[TinyhandConstants.EqualsSign] = 3;
        FirstByteTable[TinyhandConstants.Slash] = 3;
        FirstByteTable[TinyhandConstants.Sharp] = 3;
    }
}
