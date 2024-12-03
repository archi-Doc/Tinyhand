// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers.Text;
using System.Runtime.CompilerServices;
using System.Text;

#pragma warning disable SA1201 // Elements should appear in the correct order
#pragma warning disable SA1202 // Elements should be ordered by access
#pragma warning disable SA1309
#pragma warning disable SA1513 // Closing brace should be followed by blank line
#pragma warning disable SA1602 // Enumeration items should be documented

namespace Tinyhand;

public enum TinyhandAtomType
{
    None, // None
    Separator, // , ;
    LineFeed, // \n
    StartGroup, // {
    EndGroup, // }
    Identifier, // objectA
    SpecialIdentifier, // @mode
    Modifier, // &i32, &key(1), &required
    Assignment, // =
    Comment, // // comment
    Value_Base64, // b"Base64"
    Value_String, // "text"
    Value_Long, // -123(long)
    Value_ULong, // 123(ulong)
    Value_Double, // 1.23(double)
    Value_Null, // null
    Value_True, // true
    Value_False, // false
}

public enum TinyhandModifierType
{
    None,
    Bool,
    I32,
    I64,
    U32,
    U64,
    Single,
    Double,
    String,
    Key,
    Array,
    Map,
    Required,
    Optional,
}

public struct TinyhandUtf8LinePosition
{
    public int LineNumber;
    public int BytePositionInLine;
}

public ref struct TinyhandUtf8Reader
{
    private const int InitialLinePosition = 1;

    private ReadOnlySpan<byte> buffer;
    private bool readContextualInformation;
    private int lineNumber;
    private int bytePositionInLine;

    public TinyhandUtf8Reader(ReadOnlySpan<byte> utf8Data, bool readContextualInformation = false)
    {
        this.buffer = utf8Data;
        if (this.buffer.StartsWith(TinyhandConstants.Utf8Bom))
        { // Ignore UTF-8 BOM
            this.buffer = this.buffer.Slice(TinyhandConstants.Utf8Bom.Length);
        }
        this.readContextualInformation = readContextualInformation;

        this.lineNumber = InitialLinePosition;
        this.bytePositionInLine = InitialLinePosition;
        this._position = 0;
        this.AtomType = TinyhandAtomType.None;
        this.AtomLineNumber = InitialLinePosition;
        this.AtomBytePositionInLine = InitialLinePosition;
        this.ValueSpan = ReadOnlySpan<byte>.Empty;
        this.ValueModifierType = TinyhandModifierType.None;
        this.ValueLong = 0;
        this.ValueDouble = 0;
        this.ValueBinary = null;
    }

    private int _position;

    public int Position => this._position;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int AddPosition(int difference)
    {
        this.bytePositionInLine += difference;
        return this._position += difference;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void IncrementLineNumber()
    {
        this.lineNumber++;
        this.bytePositionInLine = InitialLinePosition;
    }

    public bool End => this.Position >= this.Length;

    public int Length => this.buffer.Length;

    public int Remaining => this.buffer.Length - this.Position;

    public byte Current => this.buffer[this.Position];

    public TinyhandAtomType AtomType { get; private set; }

    public int AtomLineNumber { get; private set; }

    public int AtomBytePositionInLine { get; private set; }

    public ReadOnlySpan<byte> ValueSpan { get; private set; }

    public TinyhandModifierType ValueModifierType { get; private set; }

    public long ValueLong { get; private set; }

    public ulong ValueULong { get; private set; }

    public double ValueDouble { get; private set; }

    public byte[]? ValueBinary { get; private set; }

    private string ExceptionMessage(string message) => string.Format($"Line: {this.lineNumber}, Byte Position: {this.bytePositionInLine}, {message}");

    internal void ThrowException(string message)
    {
        throw new TinyhandException(this.ExceptionMessage(message));
    }

    internal void ThrowException(string message, Exception innerException)
    {
        throw new TinyhandException(this.ExceptionMessage(message), innerException);
    }

    internal void ThrowUnexpectedCharacterException(byte b)
    {
        this.ThrowException($"Unexpected character \"{(char)b}\".");
    }

    internal void ThrowUnexpectedEndException()
    {
        this.ThrowException($"Tinyhand Reader reached the end of the data before the data is complete.");
    }

    private void InitializeValue()
    {
        this.AtomType = TinyhandAtomType.None;
        this.ValueSpan = ReadOnlySpan<byte>.Empty;
        this.ValueLong = 0;
        this.ValueDouble = 0;
        this.ValueBinary = null;
    }

    private bool SkipWhiteSpace()
    {
        var separatorFlag = false;

        // Create local copy to avoid bounds checks.
        ReadOnlySpan<byte> localBuffer = this.buffer;
        for (var remaining = localBuffer.Length - this.Position; remaining > 0;)
        {
            var val = localBuffer[this.Position];

            if ((val <= 0x0D && val >= 0x09) || val == 0x20)
            { // U+0009 to U+000D, U+0020
                this.AddPosition(1);
                remaining--;

                if (val == TinyhandConstants.LineFeed)
                {
                    this.IncrementLineNumber();
                    if (this.readContextualInformation)
                    { // LineFeed
                        this.AtomType = TinyhandAtomType.LineFeed;
                        return true;
                    }
                }

                continue;
            }
            else if (val == TinyhandConstants.Separator || val == TinyhandConstants.Separator2)
            { // Separator
                this.AddPosition(1);
                remaining--;

                if (this.readContextualInformation)
                { // Separator
                    this.AtomType = TinyhandAtomType.Separator;
                    return true;
                }
                else
                { // Flag
                    separatorFlag = true;
                }
            }

            if (val == 0xC2 && remaining >= 2 && localBuffer[this.Position + 1] == 0xA0)
            { // U+00A0 (C2 A0)
                this.AddPosition(2);
                remaining -= 2;
                continue;
            }

            if (val == 0xE2 && remaining >= 3 && localBuffer[this.Position + 1] == 0x80)
            {
                if (localBuffer[this.Position + 2] >= 0x80 && localBuffer[this.Position + 2] <= 0x8A)
                {// U+2000 to U+200A, E2 80 80 to E2 80 8A
                    this.AddPosition(3);
                    continue;
                }
                else if (localBuffer[this.Position + 2] == 0xA8 || localBuffer[this.Position + 2] == 0xA9)
                {// U+2028- U+2029, E2 80 A8 to E2 80 A9
                    this.AddPosition(3);
                    this.IncrementLineNumber();
                    if (this.readContextualInformation)
                    { // LineFeed
                        this.AtomType = TinyhandAtomType.LineFeed;
                        return true;
                    }
                }
            }

            if (val == 0xE3 && remaining >= 3 && localBuffer[this.Position + 1] == 0x80 && localBuffer[this.Position + 2] == 0x80)
            { // U+3000, E3 80 80
                this.AddPosition(3);
                remaining -= 3;
                continue;
            }

            // Not white space.
            break;
        }

        if (separatorFlag)
        {
            this.AtomType = TinyhandAtomType.Separator;
            return true;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// Read one tinyhand symbol at a time.
    /// </summary>
    /// <returns>True if the read is successful. False if no data is available (AtomType is set to None).</returns>
    public bool Read()
    {
        this.InitializeValue();

        if (this.SkipWhiteSpace())
        { // Separator, (Comment, LineFeed)
            return true;
        }
        if (this.Position >= this.Length)
        { // No data left.
            return false;
        }

        var b = this.Current;
        this.AtomLineNumber = this.lineNumber;
        this.AtomBytePositionInLine = this.bytePositionInLine;
        switch (b)
        {
            case TinyhandConstants.OpenBrace: // {
                this.AtomType = TinyhandAtomType.StartGroup;
                this.ValueSpan = this.buffer.Slice(this.Position, 1);
                this.AddPosition(1);
                return true;

            case TinyhandConstants.CloseBrace: // }
                this.AtomType = TinyhandAtomType.EndGroup;
                this.ValueSpan = this.buffer.Slice(this.Position, 1);
                this.AddPosition(1);
                return true;

            case TinyhandConstants.Quote: // "string"
                return this.ReadQuote();

            case TinyhandConstants.EqualsSign: // =
                this.AtomType = TinyhandAtomType.Assignment;
                this.ValueSpan = this.buffer.Slice(this.Position, 1);
                this.AddPosition(1);
                return true;

            case TinyhandConstants.Slash: // "//" or "/*"
                this.AtomType = TinyhandAtomType.Comment;
                this.ReadComment();
                return true;

            default: // Number, Binary, Modifier/Value, Identifier/Limited identifier
                if (TinyhandHelper.IsDigit(b) || b == (byte)'+' || b == (byte)'-')
                { // Number
                    if (this.ReadNumber())
                    {
                        return true;
                    }
                }

                if (b == (byte)'b' && this.Remaining >= 2 && this.buffer[this.Position + 1] == TinyhandConstants.Quote)
                { // Binary b"Base64"
                    return this.ReadBinary();
                }

                this.ReadRawString();
                if (this.ValueSpan.Length == 0)
                {
                    goto Unexpected_Symbol;
                }

                if (this.ProcessValue())
                {
                    this.AddPosition(this.ValueSpan.Length);
                    return true;
                }

                if (this.ValueSpan[0] == TinyhandConstants.ModifierPrefix)
                {// Modifier
                    this.ValueSpan = this.ValueSpan.Slice(1);
                    this.AddPosition(1);
                    if (this.ProcessModifier())
                    {
                        this.AddPosition(this.ValueSpan.Length);
                        return true;
                    }
                }
                else
                {// Other
                    if (this.ProcessIdentifier())
                    {
                        this.AddPosition(this.ValueSpan.Length);
                        return true;
                    }
                }

Unexpected_Symbol:
                this.ThrowUnexpectedCharacterException(b);
                break;
        }

        return false;
    }

    private bool ProcessIdentifier()
    {
        var type = TinyhandAtomType.Identifier;

        if (this.ValueSpan[0] == TinyhandConstants.IdentifierPrefix)
        { // @ Special Identifier
            type = TinyhandAtomType.SpecialIdentifier;
            this.ValueSpan = this.ValueSpan.Slice(1);
            this.AddPosition(1);

            if (this.ValueSpan.Length == 0)
            {
                return false;
            }

            var b = this.ValueSpan[0];
            if (TinyhandHelper.IsDigit(b) || b == (byte)'+' || b == (byte)'-')
            { // Number
                this.ThrowException("An identifier can not begin with a digit.");
            }
        }

        this.AtomType = type;

        return true;
    }

    private bool ProcessValue()
    { // null, true, false
        if (this.ValueSpan.Length == 4)
        {
            if (this.ValueSpan[0] == (byte)'n' && this.ValueSpan[1] == (byte)'u' && this.ValueSpan[2] == (byte)'l' && this.ValueSpan[3] == (byte)'l')
            { // null
                this.AtomType = TinyhandAtomType.Value_Null;
                return true;
            }
            else if (this.ValueSpan[0] == (byte)'t' && this.ValueSpan[1] == (byte)'r' && this.ValueSpan[2] == (byte)'u' && this.ValueSpan[3] == (byte)'e')
            { // true
                this.AtomType = TinyhandAtomType.Value_True;
                return true;
            }
        }
        else if (this.ValueSpan.Length == 5)
        {
            if (this.ValueSpan[0] == (byte)'f' && this.ValueSpan[1] == (byte)'a' && this.ValueSpan[2] == (byte)'l' && this.ValueSpan[3] == (byte)'s' && this.ValueSpan[4] == (byte)'e')
            { // false
                this.AtomType = TinyhandAtomType.Value_False;
                return true;
            }
        }
        else if (this.ValueSpan.Length == TinyhandConstants.DoubleNaNSpan.Length &&
            this.ValueSpan.SequenceEqual(TinyhandConstants.DoubleNaNSpan))
        {// double.NaN
            this.AtomType = TinyhandAtomType.Value_Double;
            this.ValueDouble = double.NaN;
            return true;
        }
        else if (this.ValueSpan.Length == TinyhandConstants.DoublePositiveInfinitySpan.Length)
        {
            if (this.ValueSpan.SequenceEqual(TinyhandConstants.DoublePositiveInfinitySpan))
            {// double.PositiveInfinity
                this.AtomType = TinyhandAtomType.Value_Double;
                this.ValueDouble = double.PositiveInfinity;
                return true;
            }
            else if (this.ValueSpan.SequenceEqual(TinyhandConstants.DoubleNegativeInfinitySpan))
            {// double.NegativeInfinity
                this.AtomType = TinyhandAtomType.Value_Double;
                this.ValueDouble = double.NegativeInfinity;
                return true;
            }
        }

        return false;
    }

    private bool ProcessModifier()
    {
        // Mofidier/Value
        if (TinyhandHelper.ModifierTable.TryGetValue(this.ValueSpan, out var modifier))
        {
            this.AtomType = TinyhandAtomType.Modifier;
            this.ValueModifierType = modifier;
            return true;
        }

        return false;
    }

    private void ReadComment()
    {
        var startPosition = this.Position;
        this.AddPosition(1); // Skip slash.
        if (this.Position == this.Length)
        { // No data left.
            return;
        }

        ReadOnlySpan<byte> localBuffer = this.buffer;
        if (localBuffer[this.Position] == TinyhandConstants.Slash)
        { // Single line comment.
            for (var remaining = localBuffer.Length - this.Position; remaining > 0;)
            {
                var val = localBuffer[this.Position];

                if (val == TinyhandConstants.LineFeed)
                { // \n
                    if (localBuffer[this.Position - 1] == TinyhandConstants.CarriageReturn)
                    {
                        this.ValueSpan = localBuffer.Slice(startPosition, this.Position - 1 - startPosition);
                    }
                    else
                    {
                        this.ValueSpan = localBuffer.Slice(startPosition, this.Position - startPosition);
                    }

                    if (!this.readContextualInformation)
                    {
                        this.AddPosition(1);
                        this.IncrementLineNumber();
                    }
                    return;
                }

                if (val == 0xE2 && remaining >= 3 && localBuffer[this.Position + 1] == 0x80)
                {
                    if (localBuffer[this.Position + 2] == 0xA8 || localBuffer[this.Position + 2] == 0xA9)
                    {// U+2028- U+2029, E2 80 A8 to E2 80 A9
                        this.ValueSpan = localBuffer.Slice(startPosition, this.Position - startPosition);
                        if (!this.readContextualInformation)
                        {
                            this.AddPosition(3);
                            this.IncrementLineNumber();
                        }

                        return;
                    }
                }

                // other
                remaining--;
                this.AddPosition(1);
            }
        }
        else if (localBuffer[this.Position] == TinyhandConstants.Asterisk)
        { // Multi line comment.
            for (var remaining = localBuffer.Length - this.Position; remaining > 0;)
            {
                var val = localBuffer[this.Position];

                if (val == 0x0D)
                { // \n
                    remaining--;
                    this.AddPosition(1);
                    this.IncrementLineNumber();
                    continue;
                }

                if (val == 0xE2 && remaining >= 3 && localBuffer[this.Position + 1] == 0x80)
                {
                    if (localBuffer[this.Position + 2] == 0xA8 || localBuffer[this.Position + 2] == 0xA9)
                    {// U+2028- U+2029, E2 80 A8 to E2 80 A9
                        remaining -= 3;
                        this.AddPosition(3);
                        this.IncrementLineNumber();
                        continue;
                    }
                }

                if (val == TinyhandConstants.Asterisk && remaining >= 2 && localBuffer[this.Position + 1] == TinyhandConstants.Slash)
                { // "*/" to exit.
                    this.AddPosition(2);
                    this.ValueSpan = localBuffer.Slice(startPosition, this.Position - startPosition);
                    return;
                }

                // other
                remaining--;
                this.AddPosition(1);
            }
        }
        else
        { // Unexpected character.
            this.ThrowUnexpectedCharacterException(localBuffer[this.Position]);
        }
    }

    private void ReadRawString()
    {
        ReadOnlySpan<byte> localBuffer = this.buffer.Slice(this.Position);
        int position = 0;

        for (var remaining = localBuffer.Length; remaining > 0; remaining--, position++)
        {
            if (this.IsDelimiter(localBuffer, position, remaining))
            {
                break;
            }
        }

        this.ValueSpan = localBuffer.Slice(0, position);
    }

    private int GetQuotedStringLength(ReadOnlySpan<byte> utf8)
    {
        int count;

        for (count = 0; count < utf8.Length; count++)
        {
            if (utf8[count] < 0x20)
            {
                this.ThrowException("\"Single-line literal\" cannot contain control characters. Use \"\"\"Multi-line literal\"\"\" instead.");
            }
            else if (utf8[count] == TinyhandConstants.Quote)
            { // "
                return count;
            }
            else if (utf8[count] == TinyhandConstants.BackSlash)
            {
                if (count + 1 < utf8.Length)
                { // Skip \?
                    count++;
                }
            }
        }

        this.ThrowUnexpectedEndException();
        return count;
    }

    private int Get3QuotedStringLength(ReadOnlySpan<byte> utf8)
    {
        int count;

        for (count = 0; count < utf8.Length; count++)
        {
            if (utf8[count] < 0x20)
            {
                if (utf8[count] < 0x09 || utf8[count] > 0x0D)
                {
                    this.ThrowException("A literal can not contain control characters except CR/LF.");
                }
            }
            else if (utf8[count] == TinyhandConstants.Quote)
            { // "
                if ((count + 2 < utf8.Length) && utf8[count + 1] == TinyhandConstants.Quote && utf8[count + 2] == TinyhandConstants.Quote)
                { // """
                    return count;
                }
            }
        }

        this.ThrowUnexpectedEndException();
        return count;
    }

    private bool ReadQuote()
    {
        this.AddPosition(1); // Skip quote.

        if (this.Remaining >= 2 && this.buffer[this.Position] == TinyhandConstants.Quote && this.buffer[this.Position + 1] == TinyhandConstants.Quote)
        { // """Triple quoted string""". Multi-line literal.
            this.AddPosition(2); // Skip 2 quotes.
            var stringSpan = this.buffer.Slice(this.Position);
            var length = this.Get3QuotedStringLength(stringSpan);
            this.ValueSpan = stringSpan.Slice(0, length);

            this.AddPosition(length + 3); // String + 3 quotes.
            this.AtomType = TinyhandAtomType.Value_String;
            this.ValueLong = 1; // Triple quoted.
        }
        else
        { // "single line string"
            var stringSpan = this.buffer.Slice(this.Position);
            var length = this.GetQuotedStringLength(stringSpan);
            this.ValueSpan = TinyhandHelper.GetUnescapedSpan(stringSpan.Slice(0, length));

            this.AddPosition(length + 1); // String + quote.
            this.AtomType = TinyhandAtomType.Value_String;
        }
        return true;
    }

    private bool ReadBinary()
    {
        this.AddPosition(2); // Skip b"

        // "single line string"
        var stringSpan = this.buffer.Slice(this.Position);
        var length = this.GetQuotedStringLength(stringSpan);
        this.ValueSpan = stringSpan.Slice(0, length);

        // this.ValueBinary = Base64.DecodeFromBase64Utf8(this.ValueSpan);
        this.ValueBinary = TinyhandHelper.FromBase64UrlToByteArray(this.ValueSpan.ToArray()); // Arc.Crypto.Base64.Url.FromUtf8ToByteArray(this.ValueSpan);
        if (this.ValueBinary == null)
        {
            this.ThrowException("Cannot decode Base64 string.");
        }

        this.AddPosition(length + 1); // String + quote.
        this.AtomType = TinyhandAtomType.Value_Base64;

        return true;
    }

    private bool IsDelimiter(ReadOnlySpan<byte> localBuffer, int position, int remaining)
    {
        var val = localBuffer[position];
        var tv = TinyhandConstants.FirstByteTable[val]; // UTF-8 first byte table. 0:other, 1:may be white space, 2:white space, 3:delimiters

        if (tv >= 2)
        { // White space or delimiters
            return true;
        }
        else if (tv == 0)
        { // Other characters.
            return false;
        }

        if (val == 0xC2 && remaining >= 2 && localBuffer[position + 1] == 0xA0)
        { // U+00A0 (C2 A0)
            return true;
        }

        if (val == 0xE2 && remaining >= 3 && localBuffer[position + 1] == 0x80)
        { // U+2000 to U+200A, E2 80 80 to E2 80 8A  U+2028- U+2029, E2 80 A8 to E2 80 A9
            if (localBuffer[position + 2] >= 0x80 && localBuffer[position + 2] <= 0x8A)
            {
                return true;
            }
            else if (localBuffer[position + 2] == 0xA8 || localBuffer[position + 2] == 0xA9)
            {
                return true;
            }
        }

        if (val == 0xE3 && remaining >= 3 && localBuffer[position + 1] == 0x80 && localBuffer[position + 2] == 0x80)
        { // U+3000, E3 80 80
            return true;
        }

        return false;
    }

    public static bool HasDelimiter(byte[] utf8)
    {
        for (var n = 0; n < utf8.Length; n++)
        {
            var val = utf8[n];
            var tv = TinyhandConstants.FirstByteTable[val]; // UTF-8 first byte table. 0:other, 1:may be white space, 2:white space, 3:delimiters

            if (tv >= 2)
            { // White space or delimiters
                return true;
            }
            else if (tv == 0)
            { // Other characters.
                continue;
            }

            if (val == 0xC2 && (utf8.Length - n) >= 2 && utf8[n + 1] == 0xA0)
            { // U+00A0 (C2 A0)
                return true;
            }

            if (val == 0xE2 && (utf8.Length - n) >= 3 && utf8[n + 1] == 0x80)
            { // U+2000 to U+200A, E2 80 80 to E2 80 8A  U+2028- U+2029, E2 80 A8 to E2 80 A9
                if (utf8[n + 2] >= 0x80 && utf8[n + 2] <= 0x8A)
                {
                    return true;
                }
                else if (utf8[n + 2] == 0xA8 || utf8[n + 2] == 0xA9)
                {
                    return true;
                }
            }

            if (val == 0xE3 && (utf8.Length - n) >= 3 && utf8[n + 1] == 0x80 && utf8[n + 2] == 0x80)
            { // U+3000, E3 80 80
                return true;
            }
        }

        return false;
    }

    private bool ReadNumber()
    {
        ReadOnlySpan<byte> localBuffer = this.buffer.Slice(this.Position);
        int position = 0;
        var isDouble = false;

        // Utf8Parser.TryParse("NaN"u8, out var dd, out _); // NaN, Infinity, +/-Infinity

        for (var remaining = localBuffer.Length; remaining > 0; remaining--, position++)
        {
            if (this.IsDelimiter(localBuffer, position, remaining))
            {
                break;
            }

            var val = localBuffer[position];
            if (val == '.' || val == 'e' || val == 'E')
            {
                isDouble = true;
            }
            else if (val == '+' || val == '-')
            {
            }
            else if (!TinyhandHelper.IsDigit(val))
            {// Not a number.
                return false;
            }
        }

        this.ValueSpan = localBuffer.Slice(0, position);
        this.AddPosition(position);

        if (this.ValueSpan.Length > 0)
        {
            var last = this.ValueSpan[this.ValueSpan.Length - 1];
            if (last == 'f' || last == 'F' || last == 'd' || last == 'D')
            {
                isDouble = true;
            }
        }

        if (isDouble)
        {
            this.AtomType = TinyhandAtomType.Value_Double;
            var ret = Utf8Parser.TryParse(this.ValueSpan, out double result, out int bytesConsumed);
            this.ValueDouble = result;
            return ret;
        }
        else
        {// long
            this.AtomType = TinyhandAtomType.Value_Long;
            var ret = Utf8Parser.TryParse(this.ValueSpan, out long result, out int bytesConsumed);
            if (ret)
            {
                this.ValueLong = result;
                return ret;
            }

            // Maybe ulong...
            this.AtomType = TinyhandAtomType.Value_ULong;
            ret = Utf8Parser.TryParse(this.ValueSpan, out ulong result2, out bytesConsumed);
            this.ValueULong = result2;
            return ret;
        }
    }
}
