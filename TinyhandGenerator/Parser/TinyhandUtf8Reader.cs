// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers.Text;
using System.Runtime.CompilerServices;
using System.Text;

#pragma warning disable SA1201 // Elements should appear in the correct order
#pragma warning disable SA1202 // Elements should be ordered by access
#pragma warning disable SA1204 // Static elements should appear before instance elements
#pragma warning disable SA1309
#pragma warning disable SA1513 // Closing brace should be followed by blank line
#pragma warning disable SA1602 // Enumeration items should be documented

namespace Tinyhand;

internal enum TinyhandAtomType
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

internal enum TinyhandModifierType
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

internal ref struct TinyhandUtf8Reader
{
    private const int InitialLinePosition = 1;
    private const int LineFeedFlag = 1 << 30; // 0x4000_0000
    private const int BytePositionMask = ~LineFeedFlag;
    private const int MinimumUnescapeBufferLength = 64;

    public bool End => this.Position >= this.Length;

    public int Length => this.buffer.Length;

    public int Remaining => this.buffer.Length - this.Position;

    public byte Current => this.buffer[this.Position];

    public TinyhandAtomType AtomType { get; private set; }

    public int AtomLineNumber { get; private set; }

    public int AtomBytePositionInLine { get; private set; }

    /// <summary>
    /// Gets the value of the current atom.<br/>
    /// The span is only valid until the next call to <see cref="Read"/>: it either points into the
    /// source buffer, or, for a string containing escape sequences, into a buffer reused by the reader.
    /// Copy it if it has to outlive the current atom.
    /// </summary>
    public ReadOnlySpan<byte> ValueSpan { get; private set; }

    public TinyhandModifierType ValueModifierType { get; private set; }

    /// <summary>
    /// A buffer reused for unescaping. Escape sequences never grow the text, so a buffer
    /// as large as the escaped string is always enough.
    /// </summary>
    private byte[]? unescapeBuffer;

    public long ValueLong { get; private set; }

    public ulong ValueULong { get; private set; }

    public double ValueDouble { get; private set; }

    public byte[]? ValueBinary { get; private set; }

    public int LineNumber => this.lineNumber;

    public int BytePositionInLine => this.bytePositionInLine & BytePositionMask;

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

    private TinyhandGroupStack groupStack;

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

    private string ExceptionMessage(string message) => string.Format($"Line: {this.lineNumber}, Byte Position: {this.BytePositionInLine}, {message}");

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
        while (this._position < localBuffer.Length)
        {
            // Derived from the position so that it can never get out of step with it.
            var remaining = localBuffer.Length - this._position;
            var val = localBuffer[this._position];

            if ((val <= 0x0D && val >= 0x09) || val == 0x20)
            { // U+0009 to U+000D, U+0020
                this.AddPosition(1);

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

                if (this.readContextualInformation)
                { // Separator
                    this.AtomType = TinyhandAtomType.Separator;
                    return true;
                }

                // Flag: leave the loop so that one separator produces one atom.
                separatorFlag = true;
                break;
            }

            if (val == 0xC2 && remaining >= 2 && localBuffer[this._position + 1] == 0xA0)
            { // U+00A0 (C2 A0)
                this.AddPosition(2);
                continue;
            }

            if (val == 0xE2 && remaining >= 3 && localBuffer[this._position + 1] == 0x80)
            {
                if (localBuffer[this._position + 2] >= 0x80 && localBuffer[this._position + 2] <= 0x8A)
                {// U+2000 to U+200A, E2 80 80 to E2 80 8A
                    this.AddPosition(3);
                    continue;
                }
                else if (localBuffer[this._position + 2] == 0xA8 || localBuffer[this._position + 2] == 0xA9)
                {// U+2028- U+2029, E2 80 A8 to E2 80 A9
                    this.AddPosition(3);
                    this.IncrementLineNumber();
                    if (this.readContextualInformation)
                    { // LineFeed
                        this.AtomType = TinyhandAtomType.LineFeed;
                        return true;
                    }

                    continue;
                }
            }

            if (val == 0xE3 && remaining >= 3 && localBuffer[this._position + 1] == 0x80 && localBuffer[this._position + 2] == 0x80)
            { // U+3000, E3 80 80
                this.AddPosition(3);
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
        {// Other
            return false;
        }
    }

    /// <summary>
    /// Read one tinyhand symbol at a time.
    /// </summary>
    /// <returns>True if the read is successful. False if no data is available (AtomType is set to None).</returns>
    public bool Read()
    {
        this.AtomType = this.groupStack.GetGroup();
        if (this.AtomType != TinyhandAtomType.None)
        {
            return true;
        }

        this.InitializeValue();

        if (this.SkipWhiteSpace())
        { // Separator, (Comment, LineFeed)
            return true;
        }

        if (this.Position >= this.Length)
        { // No data left.
            this.groupStack.TerminateIndent();
            this.AtomType = this.groupStack.GetGroup();
            return this.AtomType != TinyhandAtomType.None;
        }

        if ((this.bytePositionInLine & LineFeedFlag) == 0 &&
            this.Current != TinyhandConstants.Slash &&
            this.Current != TinyhandConstants.Sharp)
        {
            if (this.groupStack.TrySetIndent(this.bytePositionInLine - 1) is { } ex)
            {
                if (this.Current != TinyhandConstants.CloseBrace)
                {
                    this.ThrowException(ex);
                }
            }

            this.bytePositionInLine |= LineFeedFlag; // Set line feed flag.

            this.AtomType = this.groupStack.GetGroup();
            if (this.AtomType != TinyhandAtomType.None)
            {
                return true;
            }
        }

        var b = this.Current;
        this.AtomLineNumber = this.LineNumber;
        this.AtomBytePositionInLine = this.BytePositionInLine;
        switch (b)
        {
            case TinyhandConstants.OpenBrace: // {
                this.groupStack.AddOpenBracket();
                this.AtomType = this.groupStack.GetGroup();
                this.ValueSpan = this.buffer.Slice(this.Position, 1);
                this.AddPosition(1);
                return true;

            case TinyhandConstants.CloseBrace: // }
                this.groupStack.AddCloseBracket();
                this.AtomType = this.groupStack.GetGroup();
                this.ValueSpan = this.buffer.Slice(this.Position, 1);
                this.AddPosition(1);
                return true;

            case TinyhandConstants.Quote: // "string"
                return this.ReadQuote(TinyhandConstants.Quote);

            case TinyhandConstants.Quote2: // 'string'
                return this.ReadQuote(TinyhandConstants.Quote2);

            case TinyhandConstants.EqualsSign: // =
                this.AtomType = TinyhandAtomType.Assignment;
                this.ValueSpan = this.buffer.Slice(this.Position, 1);
                this.AddPosition(1);
                return true;

            case TinyhandConstants.Slash: // // or /*
                this.AtomType = TinyhandAtomType.Comment;
                this.ReadComment();
                return true;

            case TinyhandConstants.Sharp: // #
                this.AtomType = TinyhandAtomType.Comment;
                this.ReadComment2();
                return true;

            default: // Number, Binary, Modifier/Value, Identifier/Limited identifier
                if (b == (byte)'+' && this.Remaining >= 2 && this.buffer[this.Position + 1] == ' ')
                {// "+ "
                    this.groupStack.AddIndent();
                    this.AtomType = this.groupStack.GetGroup();
                    this.ValueSpan = this.buffer.Slice(this.Position, 2);
                    this.AddPosition(2);
                    return true;
                }
                else if (TinyhandHelper.IsDigit(b) || b == (byte)'+' || b == (byte)'-')
                { // Number
                    if (this.ReadNumber())
                    {
                        return true;
                    }
                }

                if (b == (byte)'b' && this.Remaining >= 2 &&
                    (this.buffer[this.Position + 1] == TinyhandConstants.Quote || this.buffer[this.Position + 1] == TinyhandConstants.Quote2))
                { // Binary: b"Base64" or b'Base64'
                    return this.ReadBinary(this.buffer[this.Position + 1]);
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

            // The comment is terminated by the end of the data.
            this.ValueSpan = localBuffer.Slice(startPosition, this.Position - startPosition);
        }
        else if (localBuffer[this.Position] == TinyhandConstants.Asterisk)
        { // Multi line comment.
            for (var remaining = localBuffer.Length - this.Position; remaining > 0;)
            {
                var val = localBuffer[this.Position];

                if (val == TinyhandConstants.LineFeed)
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

            // The comment is terminated by the end of the data.
            this.ValueSpan = localBuffer.Slice(startPosition, this.Position - startPosition);
        }
        else
        { // Unexpected character.
            this.ThrowUnexpectedCharacterException(localBuffer[this.Position]);
        }
    }

    private void ReadComment2()
    {
        var startPosition = this.Position;
        this.AddPosition(1); // Skip slash.
        if (this.Position == this.Length)
        { // No data left.
            return;
        }

        ReadOnlySpan<byte> localBuffer = this.buffer;
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

            // The comment is terminated by the end of the data.
            this.ValueSpan = localBuffer.Slice(startPosition, this.Position - startPosition);
        }
    }

    private void ReadRawString()
    {
        ReadOnlySpan<byte> localBuffer = this.buffer.Slice(this.Position);
        ReadOnlySpan<byte> table = TinyhandConstants.FirstByteTable;
        int position = 0;

        for (var remaining = localBuffer.Length; remaining > 0; remaining--, position++)
        {
            if (IsDelimiter(table, localBuffer, position, remaining))
            {
                break;
            }
        }

        this.ValueSpan = localBuffer.Slice(0, position);
    }

    /// <param name="hasEscape">Receives whether the string contains an escape sequence.
    /// When it does not, the string is a verbatim slice of the source and needs no unescaping.</param>
    private int GetQuotedStringLength(ReadOnlySpan<byte> utf8, byte q, out bool hasEscape)
    {
        int count;
        hasEscape = false;

        for (count = 0; count < utf8.Length; count++)
        {
            if (utf8[count] < 0x20)
            {
                this.ThrowException("\"Single-line literal\" cannot contain control characters. Use \"\"\"Multi-line literal\"\"\" instead.");
            }
            else if (utf8[count] == q)
            { // "
                return count;
            }
            else if (utf8[count] == TinyhandConstants.BackSlash)
            {
                hasEscape = true;
                if (count + 1 < utf8.Length)
                { // Skip \?
                    count++;
                }
            }
        }

        this.ThrowUnexpectedEndException();
        return count;
    }

    private int Get3QuotedStringLength(ReadOnlySpan<byte> utf8, byte q)
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
            else if (utf8[count] == q)
            { // "
                if ((count + 2 < utf8.Length) && utf8[count + 1] == q && utf8[count + 2] == q)
                { // """
                    return count;
                }
            }
        }

        this.ThrowUnexpectedEndException();
        return count;
    }

    private bool ReadQuote(byte q)
    {
        this.AddPosition(1); // Skip quote.

        if (this.Remaining >= 2 && this.buffer[this.Position] == q && this.buffer[this.Position + 1] == q)
        { // """Triple quoted string""". Multi-line literal.
            this.AddPosition(2); // Skip 2 quotes.
            var stringSpan = this.buffer.Slice(this.Position);
            var length = this.Get3QuotedStringLength(stringSpan, q);
            this.ValueSpan = stringSpan.Slice(0, length);

            this.AddPosition(length + 3); // String + 3 quotes.
            this.AtomType = TinyhandAtomType.Value_String;
            this.ValueLong = 1; // Triple quoted.
        }
        else
        { // "single line string" or 'string'
            var stringSpan = this.buffer.Slice(this.Position);
            var length = this.GetQuotedStringLength(stringSpan, q, out var hasEscape);

            // Without an escape sequence the string is a verbatim slice of the source,
            // so unescaping can be skipped entirely.
            this.ValueSpan = hasEscape ?
                this.Unescape(stringSpan.Slice(0, length)) :
                stringSpan.Slice(0, length);

            this.AddPosition(length + 1); // String + quote.
            this.AtomType = TinyhandAtomType.Value_String;
        }
        return true;
    }

    /// <summary>
    /// Unescapes a string into <see cref="unescapeBuffer"/>, which is grown as needed and reused
    /// by every subsequent string, so a document allocates at most one buffer.
    /// </summary>
    /// <param name="source">The escaped string.</param>
    /// <returns>The unescaped string.</returns>
    private ReadOnlySpan<byte> Unescape(ReadOnlySpan<byte> source)
    {
        // Unescaping never grows the text, so a buffer as large as the source is always enough.
        if (this.unescapeBuffer is null || this.unescapeBuffer.Length < source.Length)
        {
            this.unescapeBuffer = new byte[Math.Max(source.Length, MinimumUnescapeBufferLength)];
        }

        TinyhandHelper.Unescape(source, this.unescapeBuffer, out var written);
        return this.unescapeBuffer.AsSpan(0, written);
    }

    private bool ReadBinary(byte q)
    {
        this.AddPosition(2); // Skip b"

        // "single line string" or 'string'
        var stringSpan = this.buffer.Slice(this.Position);
        var length = this.GetQuotedStringLength(stringSpan, q, out _);
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

    /// <summary>
    /// Determines whether the byte at <paramref name="position"/> terminates a token.
    /// </summary>
    /// <param name="table">A local copy of <see cref="TinyhandConstants.FirstByteTable"/>, hoisted out of the caller's loop.</param>
    /// <param name="localBuffer">The buffer.</param>
    /// <param name="position">The position in the buffer.</param>
    /// <param name="remaining">The number of bytes left from <paramref name="position"/>.</param>
    /// <returns><see langword="true"/>; the byte is a white space or a delimiter.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsDelimiter(ReadOnlySpan<byte> table, ReadOnlySpan<byte> localBuffer, int position, int remaining)
    {
        // UTF-8 first byte table. 0:other, 1:may be white space, 2:white space, 3:delimiters
        var val = localBuffer[position];
        var tv = table[val];

        if (tv >= 2)
        { // White space or delimiters
            return true;
        }
        else if (tv == 0)
        { // Other characters.
            return false;
        }

        return IsMultiByteWhiteSpace(localBuffer, position, remaining);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool IsMultiByteWhiteSpace(ReadOnlySpan<byte> localBuffer, int position, int remaining)
    {
        var val = localBuffer[position];

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

    public static bool HasDelimiter(scoped ReadOnlySpan<byte> utf8)
    {
        ReadOnlySpan<byte> table = TinyhandConstants.FirstByteTable;
        for (var n = 0; n < utf8.Length; n++)
        {
            if (IsDelimiter(table, utf8, n, utf8.Length - n))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Reads a number. The token always starts with a digit, '+' or '-', so it can only be a number;
    /// a token that cannot be parsed as one is an error.
    /// </summary>
    /// <returns><see langword="true"/>; the number was read.</returns>
    private bool ReadNumber()
    {
        ReadOnlySpan<byte> localBuffer = this.buffer.Slice(this.Position);
        ReadOnlySpan<byte> table = TinyhandConstants.FirstByteTable;
        int position = 0;
        var isDouble = false;

        // Utf8Parser.TryParse("NaN"u8, out var dd, out _); // NaN, Infinity, +/-Infinity

        for (var remaining = localBuffer.Length; remaining > 0; remaining--, position++)
        {
            if (IsDelimiter(table, localBuffer, position, remaining))
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

        var span = localBuffer.Slice(0, position);

        // The whole token must be consumed; otherwise a value like "1.2.3" would silently become 1.2.
        if (isDouble)
        {
            if (Utf8Parser.TryParse(span, out double result, out var bytesConsumed) && bytesConsumed == span.Length)
            {
                this.AtomType = TinyhandAtomType.Value_Double;
                this.ValueDouble = result;
                this.ValueSpan = span;
                this.AddPosition(position);
                return true;
            }
        }
        else
        {
            if (Utf8Parser.TryParse(span, out long longResult, out var bytesConsumed) && bytesConsumed == span.Length)
            {// long
                this.AtomType = TinyhandAtomType.Value_Long;
                this.ValueLong = longResult;
                this.ValueSpan = span;
                this.AddPosition(position);
                return true;
            }

            if (Utf8Parser.TryParse(span, out ulong ulongResult, out bytesConsumed) && bytesConsumed == span.Length)
            {// Maybe ulong...
                this.AtomType = TinyhandAtomType.Value_ULong;
                this.ValueULong = ulongResult;
                this.ValueSpan = span;
                this.AddPosition(position);
                return true;
            }
        }

        this.ThrowException($"\"{Encoding.UTF8.GetString(span.ToArray())}\" is not a valid number.");
        return false;
    }
}
