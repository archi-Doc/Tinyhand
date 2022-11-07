// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Arc.IO;

#pragma warning disable SA1615 // Element return value should be documented

namespace Tinyhand.IO;

public ref partial struct TinyhandReaderB
{
    private ref byte b;
    private int remaining;

    public TinyhandReaderB(ReadOnlySpan<byte> data)
    {
        this.b = ref MemoryMarshal.GetReference(data);
        this.remaining = data.Length;
    }

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
    /// Reads nil if it is the next token.
    /// </summary>
    /// <returns><c>true</c> if the next token was nil; <c>false</c> otherwise.</returns>
    /// <exception cref="EndOfStreamException">Thrown if the end of the sequence provided to the constructor is reached before the expected end of the data.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryReadNil()
    {
        if (this.remaining > 0 && this.b == MessagePackCode.Nil)
        {
            this.remaining--;
            this.b = ref Unsafe.Add(ref this.b, 1);
            return true;
        }

        return false;
    }

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

    public void Skip() => ThrowInsufficientBufferUnless(this.TrySkip());

    public bool TrySkip()
    {
        if (this.remaining == 0)
        {
            return false;
        }

        var code = this.b;
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
            // return this.TrySkipNextMap();
            case MessagePackCode.Array16:
            case MessagePackCode.Array32:
            // return this.TrySkipNextArray();
            case MessagePackCode.Str8:
            case MessagePackCode.Str16:
            case MessagePackCode.Str32:
            // return this.TryGetStringLengthInBytes(out int length) && this.reader.TryAdvance(length);
            case MessagePackCode.Bin8:
            case MessagePackCode.Bin16:
            case MessagePackCode.Bin32:
            // return this.TryGetBytesLength(out length) && this.reader.TryAdvance(length);
            case MessagePackCode.FixExt1:
            case MessagePackCode.FixExt2:
            case MessagePackCode.FixExt4:
            case MessagePackCode.FixExt8:
            case MessagePackCode.FixExt16:
            case MessagePackCode.Ext8:
            case MessagePackCode.Ext16:
            case MessagePackCode.Ext32:
            // return this.TryReadExtensionFormatHeader(out ExtensionHeader header) && this.reader.TryAdvance(header.Length);
            default:
                if ((code >= MessagePackCode.MinNegativeFixInt && code <= MessagePackCode.MaxNegativeFixInt) ||
                    (code >= MessagePackCode.MinFixInt && code <= MessagePackCode.MaxFixInt))
                {
                    return this.TryAdvance(1);
                }

                if (code >= MessagePackCode.MinFixMap && code <= MessagePackCode.MaxFixMap)
                {
                    // return this.TrySkipNextMap();
                }

                if (code >= MessagePackCode.MinFixArray && code <= MessagePackCode.MaxFixArray)
                {
                    // return this.TrySkipNextArray();
                }

                if (code >= MessagePackCode.MinFixStr && code <= MessagePackCode.MaxFixStr)
                {
                    // return this.TryGetStringLengthInBytes(out length) && this.reader.TryAdvance(length);
                }

                // We don't actually expect to ever hit this point, since every code is supported.
                Debug.Fail("Missing handler for code: " + code);
                throw ThrowInvalidCode(code, MessagePackType.Unknown);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryReadCode(out byte code)
    {
        if (this.remaining > 0)
        {
            code = this.b;
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
    /// Gets the length of the next string.
    /// </summary>
    /// <returns>The length of the next string.</returns>
    private int GetStringLengthInBytes()
    {
        ThrowInsufficientBufferUnless(this.TryGetStringLengthInBytes(out int length));
        return length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe string? ReadString()
    {
        if (this.TryReadNil())
        {
            return null;
        }

        int byteLength = this.GetStringLengthInBytes();

        if (this.remaining >= byteLength)
        {
            // Fast path: all bytes to decode appear in the same span.
            // string value = Encoding.UTF8.GetString((byte*)this.b, byteLength);
            var value = string.Empty;
            this.TryAdvance(byteLength);
            return value;
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// Reads an <see cref="int"/> value from:
    /// Some value between <see cref="MessagePackCode.MinNegativeFixInt"/> and <see cref="MessagePackCode.MaxNegativeFixInt"/>,
    /// Some value between <see cref="MessagePackCode.MinFixInt"/> and <see cref="MessagePackCode.MaxFixInt"/>,
    /// or any of the other MsgPack integer types.
    /// </summary>
    /// <returns>The value.</returns>
    /// <exception cref="OverflowException">Thrown when the value exceeds what can be stored in the returned type.</exception>
    public int ReadInt32()
    {
        ThrowInsufficientBufferUnless(this.TryReadCode(out byte code));

        switch (code)
        {
            case MessagePackCode.UInt8:
                ThrowInsufficientBufferUnless(this.TryReadRaw(out byte byteResult));
                return checked((int)byteResult);
            case MessagePackCode.Int8:
                ThrowInsufficientBufferUnless(this.TryReadRaw(out sbyte sbyteResult));
                return checked((int)sbyteResult);
            case MessagePackCode.UInt16:
                ThrowInsufficientBufferUnless(this.TryReadRaw(out ushort ushortResult));
                return checked((int)ushortResult);
            case MessagePackCode.Int16:
                ThrowInsufficientBufferUnless(this.TryReadRaw(out short shortResult));
                return checked((int)shortResult);
            case MessagePackCode.UInt32:
                ThrowInsufficientBufferUnless(this.TryReadRaw(out uint uintResult));
                return checked((int)uintResult);
            case MessagePackCode.Int32:
                ThrowInsufficientBufferUnless(this.TryReadRaw(out int intResult));
                return checked((int)intResult);
            case MessagePackCode.UInt64:
                ThrowInsufficientBufferUnless(this.TryReadRaw(out ulong ulongResult));
                return checked((int)ulongResult);
            case MessagePackCode.Int64:
                ThrowInsufficientBufferUnless(this.TryReadRaw(out long longResult));
                return checked((int)longResult);
            default:
                if (code >= MessagePackCode.MinNegativeFixInt && code <= MessagePackCode.MaxNegativeFixInt)
                {
                    return checked((int)unchecked((sbyte)code));
                }

                if (code >= MessagePackCode.MinFixInt && code <= MessagePackCode.MaxFixInt)
                {
                    return (int)code;
                }

                throw ThrowInvalidCode(code, MessagePackType.Integer);
        }
    }

    public int ReadInt32B()
    {
        this.TryReadCode(out byte code);

        if (code != MessagePackCode.Int32)
        {
            throw ThrowInvalidCode(code, MessagePackType.Integer);
        }

        this.TryReadRaw(out int intResult);
        return intResult;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryReadArrayHeader(out int count)
    {
        count = -1;
        if (!this.TryReadCode(out var code))
        {
            return false;
        }

        switch (code)
        {
            case MessagePackCode.Array16:
                if (!this.TryReadRaw(out short shortValue))
                {
                    return false;
                }

                count = unchecked((ushort)shortValue);
                break;

            case MessagePackCode.Array32:
                if (!this.TryReadRaw(out int intValue))
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TryGetStringLengthInBytesSlow(byte code, out int length)
    {
        switch (code)
        {
            case MessagePackCode.Str8:
                if (this.TryReadRaw(out byte byteValue))
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
                if (this.TryReadRaw(out short shortValue))
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
                if (this.TryReadRaw(out int intValue))
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
    private unsafe bool TryReadRaw<T>(out T value)
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
}

/* public ref partial struct TinyhandReaderB
{
    private ReadOnlySpan<byte> span;

    public TinyhandReaderB(ReadOnlySpan<byte> data)
    {
        this.span = data;
    }

    public int ReadArrayHeader()
    {
        ThrowInsufficientBufferUnless(this.TryReadArrayHeader(out int count));

        // Protect against corrupted or mischievious data that may lead to allocating way too much memory.
        // We allow for each primitive to be the minimal 1 byte in size.
        // Formatters that know each element is larger can optionally add a stronger check.
        ThrowInsufficientBufferUnless(this.Remaining >= count);

        return count;
    }

    /// <summary>
    /// Reads nil if it is the next token.
    /// </summary>
    /// <returns><c>true</c> if the next token was nil; <c>false</c> otherwise.</returns>
    /// <exception cref="EndOfStreamException">Thrown if the end of the sequence provided to the constructor is reached before the expected end of the data.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryReadNil()
    {
        if (this.span.Length > 0 && this.span[0] == MessagePackCode.Nil)
        {
            this.span = this.span.Slice(1);
            return true;
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAdvance(int count)
    {
        if (this.span.Length >= count)
        {
            this.span = this.span.Slice(count);
            return true;
        }
        else
        {
            return false;
        }
    }

    public void Skip() => ThrowInsufficientBufferUnless(this.TrySkip());

    public bool TrySkip()
    {
        if (this.span.Length == 0)
        {
            return false;
        }

        var code = this.span[0];
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
            // return this.TrySkipNextMap();
            case MessagePackCode.Array16:
            case MessagePackCode.Array32:
            // return this.TrySkipNextArray();
            case MessagePackCode.Str8:
            case MessagePackCode.Str16:
            case MessagePackCode.Str32:
            // return this.TryGetStringLengthInBytes(out int length) && this.reader.TryAdvance(length);
            case MessagePackCode.Bin8:
            case MessagePackCode.Bin16:
            case MessagePackCode.Bin32:
            // return this.TryGetBytesLength(out length) && this.reader.TryAdvance(length);
            case MessagePackCode.FixExt1:
            case MessagePackCode.FixExt2:
            case MessagePackCode.FixExt4:
            case MessagePackCode.FixExt8:
            case MessagePackCode.FixExt16:
            case MessagePackCode.Ext8:
            case MessagePackCode.Ext16:
            case MessagePackCode.Ext32:
            // return this.TryReadExtensionFormatHeader(out ExtensionHeader header) && this.reader.TryAdvance(header.Length);
            default:
                if ((code >= MessagePackCode.MinNegativeFixInt && code <= MessagePackCode.MaxNegativeFixInt) ||
                    (code >= MessagePackCode.MinFixInt && code <= MessagePackCode.MaxFixInt))
                {
                    return this.TryAdvance(1);
                }

                if (code >= MessagePackCode.MinFixMap && code <= MessagePackCode.MaxFixMap)
                {
                    // return this.TrySkipNextMap();
                }

                if (code >= MessagePackCode.MinFixArray && code <= MessagePackCode.MaxFixArray)
                {
                    // return this.TrySkipNextArray();
                }

                if (code >= MessagePackCode.MinFixStr && code <= MessagePackCode.MaxFixStr)
                {
                    // return this.TryGetStringLengthInBytes(out length) && this.reader.TryAdvance(length);
                }

                // We don't actually expect to ever hit this point, since every code is supported.
                Debug.Fail("Missing handler for code: " + code);
                throw ThrowInvalidCode(code, MessagePackType.Unknown);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryReadCode(out byte code)
    {
        if (this.span.Length > 0)
        {
            code = this.span[0];
            this.span = this.span.Slice(1);
            return true;
        }
        else
        {
            code = 0;
            return false;
        }
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
    public string? ReadString()
    {
        if (this.TryReadNil())
        {
            return null;
        }

        int byteLength = this.GetStringLengthInBytes();

        if (this.span.Length >= byteLength)
        {
            // Fast path: all bytes to decode appear in the same span.
            string value = Encoding.UTF8.GetString(this.span.Slice(0, byteLength));
            this.TryAdvance(byteLength);
            return value;
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// Reads an <see cref="int"/> value from:
    /// Some value between <see cref="MessagePackCode.MinNegativeFixInt"/> and <see cref="MessagePackCode.MaxNegativeFixInt"/>,
    /// Some value between <see cref="MessagePackCode.MinFixInt"/> and <see cref="MessagePackCode.MaxFixInt"/>,
    /// or any of the other MsgPack integer types.
    /// </summary>
    /// <returns>The value.</returns>
    /// <exception cref="OverflowException">Thrown when the value exceeds what can be stored in the returned type.</exception>
    public int ReadInt32()
    {
        ThrowInsufficientBufferUnless(this.TryReadCode(out byte code));

        switch (code)
        {
            case MessagePackCode.UInt8:
                ThrowInsufficientBufferUnless(this.TryReadRaw(out byte byteResult));
                return checked((int)byteResult);
            case MessagePackCode.Int8:
                ThrowInsufficientBufferUnless(this.TryReadRaw(out sbyte sbyteResult));
                return checked((int)sbyteResult);
            case MessagePackCode.UInt16:
                ThrowInsufficientBufferUnless(this.TryReadRaw(out ushort ushortResult));
                return checked((int)ushortResult);
            case MessagePackCode.Int16:
                ThrowInsufficientBufferUnless(this.TryReadRaw(out short shortResult));
                return checked((int)shortResult);
            case MessagePackCode.UInt32:
                ThrowInsufficientBufferUnless(this.TryReadRaw(out uint uintResult));
                return checked((int)uintResult);
            case MessagePackCode.Int32:
                ThrowInsufficientBufferUnless(this.TryReadRaw(out int intResult));
                return checked((int)intResult);
            case MessagePackCode.UInt64:
                ThrowInsufficientBufferUnless(this.TryReadRaw(out ulong ulongResult));
                return checked((int)ulongResult);
            case MessagePackCode.Int64:
                ThrowInsufficientBufferUnless(this.TryReadRaw(out long longResult));
                return checked((int)longResult);
            default:
                if (code >= MessagePackCode.MinNegativeFixInt && code <= MessagePackCode.MaxNegativeFixInt)
                {
                    return checked((int)unchecked((sbyte)code));
                }

                if (code >= MessagePackCode.MinFixInt && code <= MessagePackCode.MaxFixInt)
                {
                    return (int)code;
                }

                throw ThrowInvalidCode(code, MessagePackType.Integer);
        }
    }

    public int ReadInt32B()
    {
        this.TryReadCode(out byte code);

        if (code != MessagePackCode.Int32)
        {
            throw ThrowInvalidCode(code, MessagePackType.Integer);
        }

        this.TryReadRaw(out int intResult);
        return intResult;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryReadArrayHeader(out int count)
    {
        count = -1;
        if (!this.TryReadCode(out var code))
        {
            return false;
        }

        switch (code)
        {
            case MessagePackCode.Array16:
                if (!this.TryReadRaw(out short shortValue))
                {
                    return false;
                }

                count = unchecked((ushort)shortValue);
                break;

            case MessagePackCode.Array32:
                if (!this.TryReadRaw(out int intValue))
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

    public readonly long Remaining => this.span.Length;

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TryGetStringLengthInBytesSlow(byte code, out int length)
    {
        switch (code)
        {
            case MessagePackCode.Str8:
                if (this.TryReadRaw(out byte byteValue))
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
                if (this.TryReadRaw(out short shortValue))
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
                if (this.TryReadRaw(out int intValue))
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
    private unsafe bool TryReadRaw<T>(out T value)
        where T : unmanaged
    {
        if (this.span.Length >= sizeof(T))
        {
            value = Unsafe.ReadUnaligned<T>(ref MemoryMarshal.GetReference(this.span));
            this.span = this.span.Slice(sizeof(T));
            return true;
        }
        else
        {
            value = default;
            return false;
        }
    }
}
*/
