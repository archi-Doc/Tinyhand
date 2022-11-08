// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Unicode;
using System.Threading;

#pragma warning disable SA1615 // Element return value should be documented

namespace Tinyhand.IO;

public ref partial struct TinyhandReader
{
    private ref byte b;
    private int remaining;
    private int length;

    public TinyhandReader(ReadOnlySpan<byte> span)
        : this()
    {
        this.b = ref MemoryMarshal.GetReference(span);
        this.remaining = span.Length;
        this.length = span.Length;
    }

    /// <summary>
    /// Gets or sets the cancellation token for this deserialization operation.
    /// </summary>
    public CancellationToken CancellationToken { get; set; }

    /// <summary>
    /// Gets or sets the present depth of the object graph being deserialized.
    /// </summary>
    public int Depth { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TinyhandReader"/> struct,
    /// with the same settings as this one, but with its own buffer to read from.
    /// </summary>
    /// <param name="span">Span.</param>
    /// <returns>The new reader.</returns>
    public TinyhandReader Clone(in ReadOnlySpan<byte> span) => new TinyhandReader(span)
    {
        CancellationToken = this.CancellationToken,
        Depth = this.Depth,
    };

    /// <summary>
    /// Creates a new <see cref="TinyhandReader"/> at this reader's current position.
    /// </summary>
    /// <returns>A new <see cref="TinyhandReader"/>.</returns>
    public TinyhandReader Fork() => this;

    /// <summary>
    /// Gets a value indicating whether the reader position is pointing at a nil value.
    /// </summary>
    /// <exception cref="EndOfStreamException">Thrown if the end of the sequence provided to the constructor is reached before the expected end of the data.</exception>
    public bool IsNil => this.NextCode == MessagePackCode.Nil;

    /// <summary>
    /// Gets the next message pack type to be read.
    /// </summary>
    public MessagePackType NextMessagePackType => MessagePackCode.ToMessagePackType(this.NextCode);

    /// <summary>
    /// Gets the type of the next MessagePack block.
    /// </summary>
    /// <exception cref="EndOfStreamException">Thrown if the end of the sequence provided to the constructor is reached before the expected end of the data.</exception>
    /// <remarks>
    /// See <see cref="MessagePackCode"/> for valid message pack codes and ranges.
    /// </remarks>
    public byte NextCode
    {
        get
        {
            ThrowInsufficientBufferUnless(this.remaining > 0);
            return this.b;
        }
    }

    /// <summary>
    /// Gets the number of bytes consumed by the reader.
    /// </summary>
    public int Consumed => this.length - this.remaining;

    /// <summary>
    /// Gets a value indicating whether the reader is at the end of the sequence.
    /// </summary>
    public bool End => this.remaining == 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAdvance(int count)
    {
        if (this.remaining >= count)
        {
            this.remaining -= count;
            this.b = ref Unsafe.Add(ref this.b, count);
            return true;
        }
        else
        {
            return false;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Advance(int count)
    {
        ThrowInsufficientBufferUnless(this.remaining >= count);
        this.remaining -= count;
        this.b = ref Unsafe.Add(ref this.b, count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryReadCode(out byte code)
    {
        if (this.remaining > 0)
        {
            code = this.b;
            this.remaining--;
            this.b = ref Unsafe.Add(ref this.b, 1);
            return true;
        }
        else
        {
            code = 0;
            return false;
        }
    }

    /// <summary>
    /// Advances the reader to the next MessagePack primitive to be read.
    /// </summary>
    /// <remarks>
    /// The entire primitive is skipped, including content of maps or arrays, or any other type with payloads.
    /// To get the raw MessagePack sequence that was skipped, use <see cref="ReadRaw()"/> instead.
    /// </remarks>
    public void Skip() => ThrowInsufficientBufferUnless(this.TrySkip());

    /// <summary>
    /// Advances the reader to the next MessagePack primitive to be read.
    /// </summary>
    /// <returns><c>true</c> if the entire structure beginning at the current position is found in the span; <c>false</c> otherwise.</returns>
    /// <remarks>
    /// The entire primitive is skipped, including content of maps or arrays, or any other type with payloads.
    /// To get the raw MessagePack sequence that was skipped, use <see cref="ReadRaw()"/> instead.
    /// WARNING: when false is returned, the position of the reader is undefined.
    /// </remarks>
    public bool TrySkip()
    {
        if (this.remaining == 0)
        {
            return false;
        }

        byte code = this.NextCode;
        switch (code)
        {
            case MessagePackCode.Nil:
            case MessagePackCode.True:
            case MessagePackCode.False:
                return this.TryAdvance(1);
            case MessagePackCode.Int8:
            case MessagePackCode.UInt8:
                return this.TryAdvance(2);
            case MessagePackCode.Int16:
            case MessagePackCode.UInt16:
                return this.TryAdvance(3);
            case MessagePackCode.Int32:
            case MessagePackCode.UInt32:
            case MessagePackCode.Float32:
                return this.TryAdvance(5);
            case MessagePackCode.Int64:
            case MessagePackCode.UInt64:
            case MessagePackCode.Float64:
                return this.TryAdvance(9);
            case MessagePackCode.Map16:
            case MessagePackCode.Map32:
                return this.TrySkipNextMap();
            case MessagePackCode.Array16:
            case MessagePackCode.Array32:
                return this.TrySkipNextArray();
            case MessagePackCode.Str8:
            case MessagePackCode.Str16:
            case MessagePackCode.Str32:
                return this.TryGetStringLengthInBytes(out int length) && this.TryAdvance(length);
            case MessagePackCode.Bin8:
            case MessagePackCode.Bin16:
            case MessagePackCode.Bin32:
                return this.TryGetBytesLength(out length) && this.TryAdvance(length);
            case MessagePackCode.FixExt1:
            case MessagePackCode.FixExt2:
            case MessagePackCode.FixExt4:
            case MessagePackCode.FixExt8:
            case MessagePackCode.FixExt16:
            case MessagePackCode.Ext8:
            case MessagePackCode.Ext16:
            case MessagePackCode.Ext32:
                return this.TryReadExtensionFormatHeader(out ExtensionHeader header) && this.TryAdvance((int)header.Length);
            default:
                if ((code >= MessagePackCode.MinNegativeFixInt && code <= MessagePackCode.MaxNegativeFixInt) ||
                    (code >= MessagePackCode.MinFixInt && code <= MessagePackCode.MaxFixInt))
                {
                    return this.TryAdvance(1);
                }

                if (code >= MessagePackCode.MinFixMap && code <= MessagePackCode.MaxFixMap)
                {
                    return this.TrySkipNextMap();
                }

                if (code >= MessagePackCode.MinFixArray && code <= MessagePackCode.MaxFixArray)
                {
                    return this.TrySkipNextArray();
                }

                if (code >= MessagePackCode.MinFixStr && code <= MessagePackCode.MaxFixStr)
                {
                    return this.TryGetStringLengthInBytes(out length) && this.TryAdvance(length);
                }

                // We don't actually expect to ever hit this point, since every code is supported.
                Debug.Fail("Missing handler for code: " + code);
                throw ThrowInvalidCode(code, MessagePackType.Unknown);
        }
    }

    /// <summary>
    /// Reads a <see cref="MessagePackCode.Nil"/> value.
    /// </summary>
    /// <returns>A nil value.</returns>
    public Nil ReadNil()
    {
        ThrowInsufficientBufferUnless(this.TryReadCode(out byte code));

        return code == MessagePackCode.Nil
            ? Nil.Default
            : throw ThrowInvalidCode(code, MessagePackType.Nil);
    }

    /// <summary>
    /// Reads nil if it is the next token.
    /// </summary>
    /// <returns><c>true</c> if the next token was nil; <c>false</c> otherwise.</returns>
    /// <exception cref="EndOfStreamException">Thrown if the end of the sequence provided to the constructor is reached before the expected end of the data.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryReadNil()
    {
        if (this.NextCode == MessagePackCode.Nil)
        {
            this.Advance(1);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Reads a sequence of bytes without any decoding.
    /// </summary>
    /// <param name="length">The number of bytes to read.</param>
    /// <returns>The sequence of bytes read.</returns>
    public ReadOnlySpan<byte> ReadRaw(int length)
    {
        if (this.remaining < length)
        {
            throw ThrowNotEnoughBytesException();
        }

        var span = MemoryMarshal.CreateReadOnlySpan(ref this.b, length);
        this.Advance(length);
        return span;
    }

    /// <summary>
    /// Reads the next MessagePack primitive.
    /// </summary>
    /// <returns>The raw MessagePack sequence.</returns>
    /// <remarks>
    /// The entire primitive is read, including content of maps or arrays, or any other type with payloads.
    /// </remarks>
    public ReadOnlySpan<byte> ReadRaw()
    {
        ref byte b = ref this.b;
        var r = this.remaining;
        this.Skip();
        return MemoryMarshal.CreateReadOnlySpan(ref b, r - this.remaining);
    }

    /// <summary>
    /// Read an array header from
    /// <see cref="MessagePackCode.Array16"/>,
    /// <see cref="MessagePackCode.Array32"/>, or
    /// some built-in code between <see cref="MessagePackCode.MinFixArray"/> and <see cref="MessagePackCode.MaxFixArray"/>.
    /// </summary>
    /// <exception cref="EndOfStreamException">
    /// Thrown if the header cannot be read in the bytes left in the span
    /// or if it is clear that there are insufficient bytes remaining after the header to include all the elements the header claims to be there.
    /// </exception>
    /// <exception cref="TinyhandException">Thrown if a code other than an array header is encountered.</exception>
    public int ReadArrayHeader()
    {
        ThrowInsufficientBufferUnless(this.TryReadArrayHeader(out int count));

        // Protect against corrupted or mischievious data that may lead to allocating way too much memory.
        // We allow for each primitive to be the minimal 1 byte in size.
        // Formatters that know each element is larger can optionally add a stronger check.
        ThrowInsufficientBufferUnless(this.remaining >= count);

        return count;
    }

    /// <summary>
    /// Reads an array header from
    /// <see cref="MessagePackCode.Array16"/>,
    /// <see cref="MessagePackCode.Array32"/>, or
    /// some built-in code between <see cref="MessagePackCode.MinFixArray"/> and <see cref="MessagePackCode.MaxFixArray"/>
    /// if there is sufficient buffer to read it.
    /// </summary>
    /// <param name="count">Receives the number of elements in the array if the entire array header could be read.</param>
    /// <returns><c>true</c> if there was sufficient buffer and an array header was found; <c>false</c> if the buffer incompletely describes an array header.</returns>
    /// <exception cref="TinyhandException">Thrown if a code other than an array header is encountered.</exception>
    /// <remarks>
    /// When this method returns <c>false</c> the position of the reader is left in an undefined position.
    /// The caller is expected to recreate the reader (presumably with a longer sequence to read from) before continuing.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryReadArrayHeader(out int count)
    {
        count = -1;
        if (!this.TryReadCode(out byte code))
        {
            return false;
        }

        switch (code)
        {
            case MessagePackCode.Array16:
                if (!this.TryReadUnmanaged(out short shortValue))
                {
                    return false;
                }

                count = unchecked((ushort)shortValue);
                break;
            case MessagePackCode.Array32:
                if (!this.TryReadUnmanaged(out int intValue))
                {
                    return false;
                }

                count = intValue;
                break;
            default:
                if (code >= MessagePackCode.MinFixArray && code <= MessagePackCode.MaxFixArray)
                {
                    count = code & 0xF;
                    break;
                }

                throw ThrowInvalidCode(code, MessagePackType.Array);
        }

        return true;
    }

    /// <summary>
    /// Read a map header from
    /// <see cref="MessagePackCode.Map16"/>,
    /// <see cref="MessagePackCode.Map32"/>, or
    /// some built-in code between <see cref="MessagePackCode.MinFixMap"/> and <see cref="MessagePackCode.MaxFixMap"/>.
    /// </summary>
    /// <returns>The number of key=value pairs in the map.</returns>
    public int ReadMapHeader()
    {
        ThrowInsufficientBufferUnless(this.TryReadMapHeader(out int count));

        // Protect against corrupted or mischievious data that may lead to allocating way too much memory.
        // We allow for each primitive to be the minimal 1 byte in size, and we have a key=value map, so that's 2 bytes.
        // Formatters that know each element is larger can optionally add a stronger check.
        ThrowInsufficientBufferUnless(this.remaining >= count * 2);

        return count;
    }

    /// <summary>
    /// Read a map header from
    /// <see cref="MessagePackCode.Map16"/>,
    /// <see cref="MessagePackCode.Map32"/>, or
    /// some built-in code between <see cref="MessagePackCode.MinFixMap"/> and <see cref="MessagePackCode.MaxFixMap"/>.
    /// </summary>
    /// <returns>The number of key=value pairs in the map.</returns>
    /// <exception cref="EndOfStreamException">
    /// Thrown if the header cannot be read in the bytes left in the span
    /// or if it is clear that there are insufficient bytes remaining after the header to include all the elements the header claims to be there.
    /// </exception>
    /// <exception cref="TinyhandException">Thrown if a code other than an map header is encountered.</exception>
    /// <remarks>Returns 0 if the next code is an empty array.</remarks>
    public int ReadMapHeader2()
    {
        ThrowInsufficientBufferUnless(this.TryReadMapHeader2(out int count));

        // Protect against corrupted or mischievious data that may lead to allocating way too much memory.
        // We allow for each primitive to be the minimal 1 byte in size, and we have a key=value map, so that's 2 bytes.
        // Formatters that know each element is larger can optionally add a stronger check.
        ThrowInsufficientBufferUnless(this.remaining >= count * 2);

        return count;
    }

    /// <summary>
    /// Reads a map header from
    /// <see cref="MessagePackCode.Map16"/>,
    /// <see cref="MessagePackCode.Map32"/>, or
    /// some built-in code between <see cref="MessagePackCode.MinFixMap"/> and <see cref="MessagePackCode.MaxFixMap"/>
    /// if there is sufficient buffer to read it.
    /// </summary>
    /// <param name="count">Receives the number of key=value pairs in the map if the entire map header can be read.</param>
    /// <returns><c>true</c> if there was sufficient buffer and a map header was found; <c>false</c> if the buffer incompletely describes an map header.</returns>
    /// <exception cref="TinyhandException">Thrown if a code other than an map header is encountered.</exception>
    /// <remarks>
    /// When this method returns <c>false</c> the position of the reader is left in an undefined position.
    /// The caller is expected to recreate the reader (presumably with a longer sequence to read from) before continuing.
    /// </remarks>
    public bool TryReadMapHeader(out int count)
    {
        count = -1;
        if (!this.TryReadCode(out byte code))
        {
            return false;
        }

        switch (code)
        {
            case MessagePackCode.Map16:
                if (!this.TryReadUnmanaged(out short shortValue))
                {
                    return false;
                }

                count = unchecked((ushort)shortValue);
                break;
            case MessagePackCode.Map32:
                if (!this.TryReadUnmanaged(out int intValue))
                {
                    return false;
                }

                count = intValue;
                break;
            default:
                if (code >= MessagePackCode.MinFixMap && code <= MessagePackCode.MaxFixMap)
                {
                    count = (byte)(code & 0xF);
                    break;
                }

                throw ThrowInvalidCode(code, MessagePackType.Map);
        }

        return true;
    }

    /// <summary>
    /// Reads a map header from
    /// <see cref="MessagePackCode.Map16"/>,
    /// <see cref="MessagePackCode.Map32"/>, or
    /// some built-in code between <see cref="MessagePackCode.MinFixMap"/> and <see cref="MessagePackCode.MaxFixMap"/>
    /// if there is sufficient buffer to read it. This method can read an array with zero element.
    /// </summary>
    /// <param name="count">Receives the number of key=value pairs in the map if the entire map header can be read.</param>
    /// <returns><c>true</c> if there was sufficient buffer and a map header was found; <c>false</c> if the buffer incompletely describes an map header.</returns>
    /// <exception cref="TinyhandException">Thrown if a code other than an map header is encountered.</exception>
    /// <remarks>
    /// When this method returns <c>false</c> the position of the reader is left in an undefined position.
    /// The caller is expected to recreate the reader (presumably with a longer sequence to read from) before continuing.
    /// </remarks>
    public bool TryReadMapHeader2(out int count)
    {
        count = -1;
        if (!this.TryReadCode(out byte code))
        {
            return false;
        }

        short shortValue;
        int intValue;
        switch (code)
        {
            case MessagePackCode.Map16:
                if (!this.TryReadUnmanaged(out shortValue))
                {
                    return false;
                }

                count = unchecked((ushort)shortValue);
                break;

            case MessagePackCode.Map32:
                if (!this.TryReadUnmanaged(out intValue))
                {
                    return false;
                }

                count = intValue;
                break;

            case MessagePackCode.Array16:
                if (!this.TryReadUnmanaged(out shortValue))
                {
                    return false;
                }

                count = unchecked((ushort)shortValue);
                if (count != 0)
                {// Map expected
                    throw ThrowInvalidCode(code, MessagePackType.Map);
                }

                break;

            case MessagePackCode.Array32:
                if (!this.TryReadUnmanaged(out intValue))
                {
                    return false;
                }

                count = intValue;
                if (count != 0)
                {// Map expected
                    throw ThrowInvalidCode(code, MessagePackType.Map);
                }

                break;

            default:
                if (code >= MessagePackCode.MinFixMap && code <= MessagePackCode.MaxFixMap)
                {
                    count = code & 0xF;
                    break;
                }
                else if (code == MessagePackCode.MinFixArray)
                {
                    count = 0;
                    break;
                }

                throw ThrowInvalidCode(code, MessagePackType.Map);
        }

        return true;
    }

    /// <summary>
    /// Reads a boolean value from either a <see cref="MessagePackCode.False"/> or <see cref="MessagePackCode.True"/>.
    /// </summary>
    /// <returns>The value.</returns>
    public bool ReadBoolean()
    {
        ThrowInsufficientBufferUnless(this.TryReadCode(out byte code));
        switch (code)
        {
            case MessagePackCode.True:
                return true;
            case MessagePackCode.False:
                return false;
            default:
                throw ThrowInvalidCode(code, MessagePackType.Boolean);
        }
    }

    /// <summary>
    /// Reads a <see cref="char"/> from any of:
    /// <see cref="MessagePackCode.UInt8"/>,
    /// <see cref="MessagePackCode.UInt16"/>,
    /// or anything between <see cref="MessagePackCode.MinFixInt"/> and <see cref="MessagePackCode.MaxFixInt"/>.
    /// </summary>
    /// <returns>A character.</returns>
    public char ReadChar() => (char)this.ReadUInt16();

    /// <summary>
    /// Reads an <see cref="float"/> value from any value encoded with:
    /// <see cref="MessagePackCode.Float32"/>,
    /// <see cref="MessagePackCode.Int8"/>,
    /// <see cref="MessagePackCode.Int16"/>,
    /// <see cref="MessagePackCode.Int32"/>,
    /// <see cref="MessagePackCode.Int64"/>,
    /// <see cref="MessagePackCode.UInt8"/>,
    /// <see cref="MessagePackCode.UInt16"/>,
    /// <see cref="MessagePackCode.UInt32"/>,
    /// <see cref="MessagePackCode.UInt64"/>,
    /// or some value between <see cref="MessagePackCode.MinNegativeFixInt"/> and <see cref="MessagePackCode.MaxNegativeFixInt"/>,
    /// or some value between <see cref="MessagePackCode.MinFixInt"/> and <see cref="MessagePackCode.MaxFixInt"/>.
    /// </summary>
    /// <returns>The value.</returns>
    public unsafe float ReadSingle()
    {
        ThrowInsufficientBufferUnless(this.TryReadCode(out byte code));

        switch (code)
        {
            case MessagePackCode.Float32:
                ThrowInsufficientBufferUnless(this.TryReadUnmanaged(out float floatValue));
                return floatValue;
            case MessagePackCode.Float64:
                ThrowInsufficientBufferUnless(this.TryReadUnmanaged(out double doubleValue));
                return (float)doubleValue;
            case MessagePackCode.Int8:
                ThrowInsufficientBufferUnless(this.TryReadUnmanaged(out sbyte sbyteValue));
                return sbyteValue;
            case MessagePackCode.Int16:
                ThrowInsufficientBufferUnless(this.TryReadUnmanaged(out short shortValue));
                return shortValue;
            case MessagePackCode.Int32:
                ThrowInsufficientBufferUnless(this.TryReadUnmanaged(out int intValue));
                return intValue;
            case MessagePackCode.Int64:
                ThrowInsufficientBufferUnless(this.TryReadUnmanaged(out long longValue));
                return longValue;
            case MessagePackCode.UInt8:
                ThrowInsufficientBufferUnless(this.TryReadCode(out byte byteValue));
                return byteValue;
            case MessagePackCode.UInt16:
                ThrowInsufficientBufferUnless(this.TryReadUnmanaged(out ushort ushortValue));
                return ushortValue;
            case MessagePackCode.UInt32:
                ThrowInsufficientBufferUnless(this.TryReadUnmanaged(out uint uintValue));
                return uintValue;
            case MessagePackCode.UInt64:
                ThrowInsufficientBufferUnless(this.TryReadUnmanaged(out ulong ulongValue));
                return ulongValue;
            default:
                if (code >= MessagePackCode.MinNegativeFixInt && code <= MessagePackCode.MaxNegativeFixInt)
                {
                    return unchecked((sbyte)code);
                }
                else if (code >= MessagePackCode.MinFixInt && code <= MessagePackCode.MaxFixInt)
                {
                    return code;
                }

                throw ThrowInvalidCode(code, MessagePackType.Float);
        }
    }

    /// <summary>
    /// Reads an <see cref="double"/> value from any value encoded with:
    /// <see cref="MessagePackCode.Float64"/>,
    /// <see cref="MessagePackCode.Float32"/>,
    /// <see cref="MessagePackCode.Int8"/>,
    /// <see cref="MessagePackCode.Int16"/>,
    /// <see cref="MessagePackCode.Int32"/>,
    /// <see cref="MessagePackCode.Int64"/>,
    /// <see cref="MessagePackCode.UInt8"/>,
    /// <see cref="MessagePackCode.UInt16"/>,
    /// <see cref="MessagePackCode.UInt32"/>,
    /// <see cref="MessagePackCode.UInt64"/>,
    /// or some value between <see cref="MessagePackCode.MinNegativeFixInt"/> and <see cref="MessagePackCode.MaxNegativeFixInt"/>,
    /// or some value between <see cref="MessagePackCode.MinFixInt"/> and <see cref="MessagePackCode.MaxFixInt"/>.
    /// </summary>
    /// <returns>The value.</returns>
    public unsafe double ReadDouble()
    {
        ThrowInsufficientBufferUnless(this.TryReadCode(out byte code));

        switch (code)
        {
            case MessagePackCode.Float64:
                ThrowInsufficientBufferUnless(this.TryReadUnmanaged(out double doubleValue));
                return doubleValue;
            case MessagePackCode.Float32:
                ThrowInsufficientBufferUnless(this.TryReadUnmanaged(out float floatValue));
                return floatValue;
            case MessagePackCode.Int8:
                ThrowInsufficientBufferUnless(this.TryReadCode(out byte byteValue));
                return unchecked((sbyte)byteValue);
            case MessagePackCode.Int16:
                ThrowInsufficientBufferUnless(this.TryReadUnmanaged(out short shortValue));
                return shortValue;
            case MessagePackCode.Int32:
                ThrowInsufficientBufferUnless(this.TryReadUnmanaged(out int intValue));
                return intValue;
            case MessagePackCode.Int64:
                ThrowInsufficientBufferUnless(this.TryReadUnmanaged(out long longValue));
                return longValue;
            case MessagePackCode.UInt8:
                ThrowInsufficientBufferUnless(this.TryReadCode(out byteValue));
                return byteValue;
            case MessagePackCode.UInt16:
                ThrowInsufficientBufferUnless(this.TryReadUnmanaged(out shortValue));
                return unchecked((ushort)shortValue);
            case MessagePackCode.UInt32:
                ThrowInsufficientBufferUnless(this.TryReadUnmanaged(out intValue));
                return unchecked((uint)intValue);
            case MessagePackCode.UInt64:
                ThrowInsufficientBufferUnless(this.TryReadUnmanaged(out longValue));
                return unchecked((ulong)longValue);
            default:
                if (code >= MessagePackCode.MinNegativeFixInt && code <= MessagePackCode.MaxNegativeFixInt)
                {
                    return unchecked((sbyte)code);
                }
                else if (code >= MessagePackCode.MinFixInt && code <= MessagePackCode.MaxFixInt)
                {
                    return code;
                }

                /*this.reader.Rewind(1);
                var span = this.ReadStringSpan();
                if (span.Length == 3)
                {// 3: NaN
                    if ((span[0] == (byte)'N' || span[0] == (byte)'n') && span[1] == (byte)'a' && span[2] == (byte)'N')
                    {
                        return double.NaN;
                    }
                }
                else if (span.Length == 8)
                {// 8: Infinity
                    if ((span[0] == (byte)'I' || span[0] == (byte)'i') && span[1] == (byte)'n' && span[2] == (byte)'f' && span[3] == (byte)'i' &&
                        span[4] == (byte)'n' && span[5] == (byte)'i' && span[6] == (byte)'t' && span[7] == (byte)'y')
                    {
                        return double.PositiveInfinity;
                    }
                }
                else if (span.Length == 9)
                {// 9: +Infinity, -Infinity
                    if (span[0] == (byte)'+' && (span[1] == (byte)'I' || span[1] == (byte)'i') && span[2] == (byte)'n' && span[3] == (byte)'f' &&
                        span[4] == (byte)'i' && span[5] == (byte)'n' && span[6] == (byte)'i' && span[7] == (byte)'t' && span[8] == (byte)'y')
                    {
                        return double.PositiveInfinity;
                    }
                    else if (span[0] == (byte)'-' && (span[1] == (byte)'I' || span[1] == (byte)'i') && span[2] == (byte)'n' && span[3] == (byte)'f' &&
                        span[4] == (byte)'i' && span[5] == (byte)'n' && span[6] == (byte)'i' && span[7] == (byte)'t' && span[8] == (byte)'y')
                    {
                        return double.NegativeInfinity;
                    }
                }*/

                throw ThrowInvalidCode(code, MessagePackType.Float);
        }
    }

    /// <summary>
    /// Reads a <see cref="DateTime"/> from a value encoded with
    /// <see cref="MessagePackCode.FixExt4"/>,
    /// <see cref="MessagePackCode.FixExt8"/>, or
    /// <see cref="MessagePackCode.Ext8"/>.
    /// Expects extension type code <see cref="ReservedMessagePackExtensionTypeCode.DateTime"/>.
    /// </summary>
    /// <returns>The value.</returns>
    public DateTime ReadDateTime()
    {
        if (this.NextMessagePackType == MessagePackType.String)
        {
            return DateTime.Parse(this.ReadString() ?? string.Empty, CultureInfo.InvariantCulture).ToUniversalTime();
        }

        return this.ReadDateTime(this.ReadExtensionFormatHeader());
    }

    /// <summary>
    /// Reads a <see cref="DateTime"/> from a value encoded with
    /// <see cref="MessagePackCode.FixExt4"/>,
    /// <see cref="MessagePackCode.FixExt8"/>,
    /// <see cref="MessagePackCode.Ext8"/>.
    /// Expects extension type code <see cref="ReservedMessagePackExtensionTypeCode.DateTime"/>.
    /// </summary>
    /// <param name="header">The extension header that was already read.</param>
    /// <returns>The value.</returns>
    public DateTime ReadDateTime(ExtensionHeader header)
    {
        if (header.TypeCode != ReservedMessagePackExtensionTypeCode.DateTime)
        {
            throw new TinyhandException(string.Format("Extension TypeCode is invalid. typeCode: {0}", header.TypeCode));
        }

        switch (header.Length)
        {
            case 4:
                ThrowInsufficientBufferUnless(this.TryReadUnmanaged(out int intValue));
                return DateTimeConstants.UnixEpoch.AddSeconds(unchecked((uint)intValue));
            case 8:
                ThrowInsufficientBufferUnless(this.TryReadUnmanaged(out long longValue));
                ulong ulongValue = unchecked((ulong)longValue);
                long nanoseconds = (long)(ulongValue >> 34);
                ulong seconds = ulongValue & 0x00000003ffffffffL;
                return DateTimeConstants.UnixEpoch.AddSeconds(seconds).AddTicks(nanoseconds / DateTimeConstants.NanosecondsPerTick);
            case 12:
                ThrowInsufficientBufferUnless(this.TryReadUnmanaged(out intValue));
                nanoseconds = unchecked((uint)intValue);
                ThrowInsufficientBufferUnless(this.TryReadUnmanaged(out longValue));
                return DateTimeConstants.UnixEpoch.AddSeconds(longValue).AddTicks(nanoseconds / DateTimeConstants.NanosecondsPerTick);
            default:
                throw new TinyhandException($"Length of extension was {header.Length}. Either 4 or 8 were expected.");
        }
    }

    /// <summary>
    /// Reads a span of bytes, whose length is determined by a header of one of these types:
    /// <see cref="MessagePackCode.Bin8"/>,
    /// <see cref="MessagePackCode.Bin16"/>,
    /// <see cref="MessagePackCode.Bin32"/>,
    /// or to support OldSpec compatibility:
    /// <see cref="MessagePackCode.Str16"/>,
    /// <see cref="MessagePackCode.Str32"/>,
    /// or something between <see cref="MessagePackCode.MinFixStr"/> and <see cref="MessagePackCode.MaxFixStr"/>.
    /// </summary>
    /// <returns>
    /// A sequence of bytes, or <c>null</c> if the read token is <see cref="MessagePackCode.Nil"/>.
    /// The data is a slice from the original sequence passed to this reader's constructor.
    /// </returns>
    public ReadOnlySpan<byte> ReadBytes()
    {
        if (this.TryReadNil())
        {
            return null;
        }

        int length = this.GetBytesLength();
        ThrowInsufficientBufferUnless(this.remaining >= length);
        return this.ReadRaw(length);
    }

    public byte[] ReadBytesToArray()
    {
        if (this.TryReadNil())
        {
            return Array.Empty<byte>();
        }

        int length = this.GetBytesLength();
        ThrowInsufficientBufferUnless(this.remaining >= length);
        var span = this.ReadRaw(length);
        return span.ToArray();
    }

    /// <summary>
    /// Reads a string of bytes, whose length is determined by a header of one of these types:
    /// <see cref="MessagePackCode.Str8"/>,
    /// <see cref="MessagePackCode.Str16"/>,
    /// <see cref="MessagePackCode.Str32"/>,
    /// or a code between <see cref="MessagePackCode.MinFixStr"/> and <see cref="MessagePackCode.MaxFixStr"/>.
    /// </summary>
    /// <param name="span">Receives the span to the string.</param>
    /// <returns>
    /// <c>true</c> if the string is contiguous in memory such that it could be set as a single span.
    /// <c>false</c> if the read token is <see cref="MessagePackCode.Nil"/> or the string is not in a contiguous span.
    /// </returns>
    public bool TryReadStringSpan(out ReadOnlySpan<byte> span)
    {
        if (this.IsNil)
        {
            span = default;
            return false;
        }

        int length = this.GetStringLengthInBytes();
        ThrowInsufficientBufferUnless(this.remaining >= length);

        span = this.ReadRaw(length);
        return true;
    }

    /// <summary>
    /// Reads a string, whose length is determined by a header of one of these types:
    /// <see cref="MessagePackCode.Str8"/>,
    /// <see cref="MessagePackCode.Str16"/>,
    /// <see cref="MessagePackCode.Str32"/>,
    /// or a code between <see cref="MessagePackCode.MinFixStr"/> and <see cref="MessagePackCode.MaxFixStr"/>.
    /// </summary>
    /// <returns>A string, or <c>null</c> if the current msgpack token is <see cref="MessagePackCode.Nil"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe string? ReadString()
    {
        if (this.TryReadNil())
        {
            return null;
        }

        var byteLength = this.GetStringLengthInBytes();
        var span = this.ReadRaw(byteLength);
        var value = Encoding.UTF8.GetString(span);
        return value;
    }

    /// <summary>
    /// Reads an extension format header, based on one of these codes:
    /// <see cref="MessagePackCode.FixExt1"/>,
    /// <see cref="MessagePackCode.FixExt2"/>,
    /// <see cref="MessagePackCode.FixExt4"/>,
    /// <see cref="MessagePackCode.FixExt8"/>,
    /// <see cref="MessagePackCode.FixExt16"/>,
    /// <see cref="MessagePackCode.Ext8"/>,
    /// <see cref="MessagePackCode.Ext16"/>, or
    /// <see cref="MessagePackCode.Ext32"/>.
    /// </summary>
    /// <returns>The extension header.</returns>
    /// <exception cref="TinyhandException">Thrown if a code other than an extension format header is encountered.</exception>
    public ExtensionHeader ReadExtensionFormatHeader()
    {
        ThrowInsufficientBufferUnless(this.TryReadExtensionFormatHeader(out ExtensionHeader header));

        // Protect against corrupted or mischievious data that may lead to allocating way too much memory.
        ThrowInsufficientBufferUnless(this.remaining >= header.Length);

        return header;
    }

    /// <summary>
    /// Reads an extension format header, based on one of these codes:
    /// <see cref="MessagePackCode.FixExt1"/>,
    /// <see cref="MessagePackCode.FixExt2"/>,
    /// <see cref="MessagePackCode.FixExt4"/>,
    /// <see cref="MessagePackCode.FixExt8"/>,
    /// <see cref="MessagePackCode.FixExt16"/>,
    /// <see cref="MessagePackCode.Ext8"/>,
    /// <see cref="MessagePackCode.Ext16"/>, or
    /// <see cref="MessagePackCode.Ext32"/>
    /// if there is sufficient buffer to read it.
    /// </summary>
    /// <param name="extensionHeader">Receives the extension header if the remaining bytes in the span fully describe the header.</param>
    /// <returns>The number of key=value pairs in the map.</returns>
    /// <exception cref="TinyhandException">Thrown if a code other than an extension format header is encountered.</exception>
    /// <remarks>
    /// When this method returns <c>false</c> the position of the reader is left in an undefined position.
    /// The caller is expected to recreate the reader (presumably with a longer sequence to read from) before continuing.
    /// </remarks>
    public bool TryReadExtensionFormatHeader(out ExtensionHeader extensionHeader)
    {
        extensionHeader = default;
        if (!this.TryReadCode(out byte code))
        {
            return false;
        }

        uint length;
        switch (code)
        {
            case MessagePackCode.FixExt1:
                length = 1;
                break;
            case MessagePackCode.FixExt2:
                length = 2;
                break;
            case MessagePackCode.FixExt4:
                length = 4;
                break;
            case MessagePackCode.FixExt8:
                length = 8;
                break;
            case MessagePackCode.FixExt16:
                length = 16;
                break;
            case MessagePackCode.Ext8:
                if (!this.TryReadCode(out byte byteLength))
                {
                    return false;
                }

                length = byteLength;
                break;
            case MessagePackCode.Ext16:
                if (!this.TryReadUnmanaged(out short shortLength))
                {
                    return false;
                }

                length = unchecked((ushort)shortLength);
                break;
            case MessagePackCode.Ext32:
                if (!this.TryReadUnmanaged(out int intLength))
                {
                    return false;
                }

                length = unchecked((uint)intLength);
                break;
            default:
                throw ThrowInvalidCode(code, MessagePackType.Extension);
        }

        if (!this.TryReadCode(out byte typeCode))
        {
            return false;
        }

        extensionHeader = new ExtensionHeader(unchecked((sbyte)typeCode), length);
        return true;
    }

    /*/// <summary>
    /// Reads an extension format header and data, based on one of these codes:
    /// <see cref="MessagePackCode.FixExt1"/>,
    /// <see cref="MessagePackCode.FixExt2"/>,
    /// <see cref="MessagePackCode.FixExt4"/>,
    /// <see cref="MessagePackCode.FixExt8"/>,
    /// <see cref="MessagePackCode.FixExt16"/>,
    /// <see cref="MessagePackCode.Ext8"/>,
    /// <see cref="MessagePackCode.Ext16"/>, or
    /// <see cref="MessagePackCode.Ext32"/>.
    /// </summary>
    /// <returns>
    /// The extension format.
    /// The data is a slice from the original sequence passed to this reader's constructor.
    /// </returns>
    public ExtensionResult ReadExtensionFormat()
    {
        ExtensionHeader header = this.ReadExtensionFormatHeader();
        try
        {
            var span = this.ReadRaw((int)header.Length);
            MemoryMarshal.CreateSpan(ref this.b, (int)header.Length);
            return new ExtensionResult(header.TypeCode, span);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            throw ThrowNotEnoughBytesException(ex);
        }
    }*/

    public ReadOnlySpan<byte> GetSpanFromSequence(scoped in ReadOnlySequence<byte> sequence)
    {
        if (sequence.IsSingleSegment)
        {
            return sequence.First.Span;
        }

        return sequence.ToArray();
    }

    public ReadOnlySpan<byte> ReadStringSpan()
    {
        if (!this.TryReadStringSpan(out ReadOnlySpan<byte> result))
        {
            return default;
        }

        return result;
    }

    /// <summary>
    /// Throws an exception indicating that there aren't enough bytes remaining in the buffer to store
    /// the promised data.
    /// </summary>
    private static EndOfStreamException ThrowNotEnoughBytesException(Exception innerException) => throw new EndOfStreamException(new EndOfStreamException().Message, innerException);

    /// <summary>
    /// Throws an exception indicating that there aren't enough bytes remaining in the buffer to store
    /// the promised data.
    /// </summary>
    private static EndOfStreamException ThrowNotEnoughBytesException() => throw new EndOfStreamException(new EndOfStreamException().Message);

    private static void ThrowInsufficientBufferUnless(bool condition)
    {
        if (!condition)
        {
            throw new EndOfStreamException();
        }
    }

    /// <summary>
    /// Throws an <see cref="TinyhandException"/> explaining an unexpected code was encountered.
    /// </summary>
    /// <param name="code">The code that was encountered.</param>
    /// <returns>Nothing. This method always throws.</returns>
    private static Exception ThrowInvalidCode(byte code, MessagePackType expected)
    {
        throw new TinyhandUnexpectedCodeException(
            string.Format("Unexpected msgpack code {0} ({1}) encountered.", code, MessagePackCode.ToFormatName(code)),
            MessagePackCode.ToMessagePackType(code),
            expected);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe bool TryReadUnmanaged<T>(out T value)
        where T : unmanaged
    {
        if (this.remaining >= sizeof(T))
        {
            value = Unsafe.ReadUnaligned<T>(ref this.b);
            this.remaining -= sizeof(T);
            this.b = ref Unsafe.Add(ref this.b, sizeof(T));
            return true;
        }
        else
        {
            value = default;
            return false;
        }
    }

    private int GetBytesLength()
    {
        ThrowInsufficientBufferUnless(this.TryGetBytesLength(out int length));
        return length;
    }

    private bool TryGetBytesLength(out int length)
    {
        if (!this.TryReadCode(out byte code))
        {
            length = 0;
            return false;
        }

        // In OldSpec mode, Bin didn't exist, so Str was used. Str8 didn't exist either.
        switch (code)
        {
            case MessagePackCode.Bin8:
                if (this.TryReadCode(out byte byteLength))
                {
                    length = byteLength;
                    return true;
                }

                break;
            case MessagePackCode.Bin16:
            case MessagePackCode.Str16: // OldSpec compatibility
                if (this.TryReadUnmanaged(out short shortLength))
                {
                    length = unchecked((ushort)shortLength);
                    return true;
                }

                break;
            case MessagePackCode.Bin32:
            case MessagePackCode.Str32: // OldSpec compatibility
                if (this.TryReadUnmanaged(out length))
                {
                    return true;
                }

                break;
            default:
                // OldSpec compatibility
                if (code >= MessagePackCode.MinFixStr && code <= MessagePackCode.MaxFixStr)
                {
                    length = code & 0x1F;
                    return true;
                }

                throw ThrowInvalidCode(code, MessagePackType.Binary);
        }

        length = 0;
        return false;
    }

    /// <summary>
    /// Gets the length of the next string.
    /// </summary>
    /// <param name="length">Receives the length of the next string, if there were enough bytes to read it.</param>
    /// <returns><c>true</c> if there were enough bytes to read the length of the next string; <c>false</c> otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TryGetStringLengthInBytes(out int length)
    {
        if (!this.TryReadCode(out byte code))
        {
            length = 0;
            return false;
        }

        if (code >= MessagePackCode.MinFixStr && code <= MessagePackCode.MaxFixStr)
        {
            length = code & 0x1F;
            return true;
        }

        return this.TryGetStringLengthInBytesSlow(code, out length);
    }

    /// <summary>
    /// Gets the length of the next string.
    /// </summary>
    /// <returns>The length of the next string.</returns>
    private int GetStringLengthInBytes()
    {
        ThrowInsufficientBufferUnless(this.TryGetStringLengthInBytes(out int length));
        return length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TryGetStringLengthInBytesSlow(byte code, out int length)
    {
        switch (code)
        {
            case MessagePackCode.Str8:
                if (this.TryReadCode(out byte byteValue))
                {
                    length = byteValue;
                    return true;
                }
                else
                {
                    length = 0;
                    return false;
                }

            case MessagePackCode.Str16:
                if (this.TryReadUnmanaged(out short shortValue))
                {
                    length = unchecked((ushort)shortValue);
                    return true;
                }
                else
                {
                    length = 0;
                    return false;
                }

            case MessagePackCode.Str32:
                if (this.TryReadUnmanaged(out int intValue))
                {
                    length = intValue;
                    return true;
                }
                else
                {
                    length = 0;
                    return false;
                }

            default:
                if (code >= MessagePackCode.MinFixStr && code <= MessagePackCode.MaxFixStr)
                {
                    length = code & 0x1F;
                    return true;
                }
                else
                {
                    length = 0;
                    return false;
                }
        }
    }

    private bool TrySkipNextArray() => this.TryReadArrayHeader(out int count) && this.TrySkip(count);

    private bool TrySkipNextMap() => this.TryReadMapHeader(out int count) && this.TrySkip(count * 2);

    private bool TrySkip(int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (!this.TrySkip())
            {
                return false;
            }
        }

        return true;
    }
}
