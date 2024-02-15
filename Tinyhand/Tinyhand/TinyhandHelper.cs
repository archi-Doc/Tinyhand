// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Arc.Crypto;

#pragma warning disable SA1201 // Elements should appear in the correct order
#pragma warning disable SA1405 // Debug.Assert should provide message text
#pragma warning disable SA1611 // Element parameters should be documented
#pragma warning disable SA1615 // Element return value should be documented

namespace Tinyhand;

public static class TinyhandHelper
{
    static TinyhandHelper()
    {
        ReservedTable.TryAdd(TinyhandConstants.NullSpan, 1);
        ReservedTable.TryAdd(TinyhandConstants.TrueSpan, 1);
        ReservedTable.TryAdd(TinyhandConstants.FalseSpan, 1);

        foreach (var x in Enum.GetValues(typeof(TinyhandModifierType)).Cast<TinyhandModifierType>())
        {
            if (x == TinyhandModifierType.None)
            {
                continue;
            }

            var name = Enum.GetName(typeof(TinyhandModifierType), x);
            if (name != null)
            {
                var s = Encoding.UTF8.GetBytes(name.ToLower());
                ModifierTable.TryAdd(s, x);
                ReservedTable.TryAdd(s, 2);
            }
        }
    }

    public static Utf8Hashtable<int> ReservedTable { get; } = new();

    public static Utf8Hashtable<TinyhandModifierType> ModifierTable { get; } = new();

    public static ReadOnlySpan<byte> GetUnescapedSpan(ReadOnlySpan<byte> utf8Source)
    {
        // The escaped name is always >= than the unescaped, so it is safe to use escaped name for the buffer length.
        int length = utf8Source.Length;
        byte[]? pooledName = null;

        Span<byte> utf8Unescaped = length <= TinyhandConstants.StackallocThreshold ?
            stackalloc byte[length] : (pooledName = ArrayPool<byte>.Shared.Rent(length));

        Unescape(utf8Source, utf8Unescaped, out var written);

        ReadOnlySpan<byte> result = utf8Unescaped.Slice(0, written).ToArray();

        if (pooledName != null)
        {
            ArrayPool<byte>.Shared.Return(pooledName);
        }

        return result;
    }

    internal static void Unescape(ReadOnlySpan<byte> source, Span<byte> destination, out int written)
    {
        Debug.Assert(destination.Length >= source.Length);

        written = 0;
        for (var i = 0; i < source.Length; i++)
        {
            var b = source[i];

            if (b < 0x20)
            { // Control characters.
                if (b >= 0x09 && b <= 0x0D)
                {
                    destination[written++] = b;
                }

                // Ignore other.
                continue;
            }
            else if (b == TinyhandConstants.BackSlash)
            { // Escape.
                i++;
                b = source[i];
                if (i >= source.Length)
                {
                    return;
                }

                switch (b)
                {
                    case TinyhandConstants.Quote:
                    case TinyhandConstants.Slash:
                    case TinyhandConstants.BackSlash:
                        destination[written++] = b;
                        break;

                    case (byte)'b':
                        destination[written++] = TinyhandConstants.BackSpace;
                        break;
                    case (byte)'f':
                        destination[written++] = TinyhandConstants.FormFeed;
                        break;
                    case (byte)'n':
                        destination[written++] = TinyhandConstants.LineFeed;
                        break;
                    case (byte)'r':
                        destination[written++] = TinyhandConstants.CarriageReturn;
                        break;
                    case (byte)'t':
                        destination[written++] = TinyhandConstants.Tab;
                        break;

                    case (byte)'u':
                        i--;
                        var ret = UnescapeUnicodeCharacter(source.Slice(i), out var consumed, destination, ref written);
                        if (ret != true)
                        {
                            return;
                        }

                        i += consumed;
                        i--; // i++ in for loop.
                        break;
                }
            }
            else
            {
                destination[written++] = b;
            }
        }
    }

    internal static bool UnescapeUnicodeCharacter(ReadOnlySpan<byte> source, out int consumed, Span<byte> destination, ref int written)
    {
        consumed = 0;

        if (source.Length < 6 || source[0] != TinyhandConstants.BackSlash || source[1] != (byte)'u')
        {
            throw new TinyhandException("Invalid UTF-8 text");
        }

        source = source.Slice(2);
        consumed += 2;

        bool result = Utf8Parser.TryParse(source, out int scalar, out var bytesConsumed, 'x');
        if (result != true)
        {
            throw new TinyhandException("Invalid UTF-8 text");
        }

        source = source.Slice(bytesConsumed);
        consumed += bytesConsumed;

        if (TinyhandHelper.IsInRangeInclusive((uint)scalar, TinyhandConstants.HighSurrogateStartValue, TinyhandConstants.LowSurrogateEndValue))
        {
            // The first hex value cannot be a low surrogate.
            if (scalar >= TinyhandConstants.LowSurrogateStartValue)
            {
                throw new TinyhandException("Invalid UTF-8 text");
            }

            if (source.Length < 6 || source[0] != TinyhandConstants.BackSlash || source[1] != (byte)'u')
            {
                throw new TinyhandException("Invalid UTF-8 text");
            }

            source = source.Slice(2);
            consumed += 2;

            result = Utf8Parser.TryParse(source, out int lowSurrogate, out bytesConsumed, 'x');
            if (result != true)
            {
                throw new TinyhandException("Invalid UTF-8 text");
            }

            consumed += bytesConsumed;

            // If the first hex value is a high surrogate, the next one must be a low surrogate.
            if (!TinyhandHelper.IsInRangeInclusive((uint)lowSurrogate, TinyhandConstants.LowSurrogateStartValue, TinyhandConstants.LowSurrogateEndValue))
            {
                throw new TinyhandException("Invalid UTF-8 text");
            }

            // To find the unicode scalar:
            // (0x400 * (High surrogate - 0xD800)) + Low surrogate - 0xDC00 + 0x10000
            scalar = (TinyhandConstants.BitShiftBy10 * (scalar - TinyhandConstants.HighSurrogateStartValue))
                + (lowSurrogate - TinyhandConstants.LowSurrogateStartValue)
                + TinyhandConstants.UnicodePlane01StartValue;
        }

        EncodeToUtf8Bytes((uint)scalar, destination.Slice(written), out int bytesWritten);
        written += bytesWritten;

        return true;
    }

    /// <summary>
    /// Copies the UTF-8 code unit representation of this scalar to an output buffer.
    /// The buffer must be large enough to hold the required number of <see cref="byte"/>s.
    /// </summary>
    private static void EncodeToUtf8Bytes(uint scalar, Span<byte> utf8Destination, out int bytesWritten)
    {
        Debug.Assert(IsValidUnicodeScalar(scalar));
        Debug.Assert(utf8Destination.Length >= 4);

        if (scalar < 0x80U)
        {
            // Single UTF-8 code unit
            utf8Destination[0] = (byte)scalar;
            bytesWritten = 1;
        }
        else if (scalar < 0x800U)
        {
            // Two UTF-8 code units
            utf8Destination[0] = (byte)(0xC0U | (scalar >> 6));
            utf8Destination[1] = (byte)(0x80U | (scalar & 0x3FU));
            bytesWritten = 2;
        }
        else if (scalar < 0x10000U)
        {
            // Three UTF-8 code units
            utf8Destination[0] = (byte)(0xE0U | (scalar >> 12));
            utf8Destination[1] = (byte)(0x80U | ((scalar >> 6) & 0x3FU));
            utf8Destination[2] = (byte)(0x80U | (scalar & 0x3FU));
            bytesWritten = 3;
        }
        else
        {
            // Four UTF-8 code units
            utf8Destination[0] = (byte)(0xF0U | (scalar >> 18));
            utf8Destination[1] = (byte)(0x80U | ((scalar >> 12) & 0x3FU));
            utf8Destination[2] = (byte)(0x80U | ((scalar >> 6) & 0x3FU));
            utf8Destination[3] = (byte)(0x80U | (scalar & 0x3FU));
            bytesWritten = 4;
        }
    }

    // Reject any invalid UTF-8 data rather than silently replacing.
    public static readonly UTF8Encoding Utf8Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="value"/> is between
    /// <paramref name="lowerBound"/> and <paramref name="upperBound"/>, inclusive.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsInRangeInclusive(uint value, uint lowerBound, uint upperBound)
        => (value - lowerBound) <= (upperBound - lowerBound);

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="value"/> is between
    /// <paramref name="lowerBound"/> and <paramref name="upperBound"/>, inclusive.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsInRangeInclusive(int value, int lowerBound, int upperBound)
        => (uint)(value - lowerBound) <= (uint)(upperBound - lowerBound);

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="value"/> is between
    /// <paramref name="lowerBound"/> and <paramref name="upperBound"/>, inclusive.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsInRangeInclusive(long value, long lowerBound, long upperBound)
        => (ulong)(value - lowerBound) <= (ulong)(upperBound - lowerBound);

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="value"/> is a valid Unicode scalar
    /// value, i.e., is in [ U+0000..U+D7FF ], inclusive; or [ U+E000..U+10FFFF ], inclusive.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsValidUnicodeScalar(uint value)
    {
        // By XORing the incoming value with 0xD800, surrogate code points
        // are moved to the range [ U+0000..U+07FF ], and all valid scalar
        // values are clustered into the single range [ U+0800..U+10FFFF ],
        // which allows performing a single fast range check.

        return IsInRangeInclusive(value ^ 0xD800U, 0x800U, 0x10FFFFU);
    }

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="value"/> is in the range [0..9].
    /// Otherwise, returns <see langword="false"/>.
    /// </summary>
    /// <returns>True if the value is in the range [0..9].</returns>
    public static bool IsDigit(byte value) => (uint)(value - '0') <= '9' - '0';

    public static int GetUtf8ByteCount(ReadOnlySpan<char> text)
    {
        try
        {
#if BUILDING_INBOX_LIBRARY
            return s_utf8Encoding.GetByteCount(text);
#else
            if (text.IsEmpty)
            {
                return 0;
            }

            unsafe
            {
                fixed (char* charPtr = text)
                {
                    return Utf8Encoding.GetByteCount(charPtr, text.Length);
                }
            }
#endif
        }
        catch (EncoderFallbackException ex)
        {
            // We want to be consistent with the exception being thrown
            // so the user only has to catch a single exception.
            // Since we already throw ArgumentException when validating other arguments,
            // using that exception for failure to encode invalid UTF-16 chars as well.
            // Therefore, wrapping the EncoderFallbackException around an ArgumentException.
            throw new TinyhandException("Cannot transcode invalid UTF-16 string.", ex);
        }
    }

    public static int GetUtf8FromText(ReadOnlySpan<char> text, Span<byte> dest)
    {
        try
        {
#if BUILDING_INBOX_LIBRARY
            return s_utf8Encoding.GetBytes(text, dest);
#else
            if (text.IsEmpty)
            {
                return 0;
            }

            unsafe
            {
                fixed (char* charPtr = text)
                {
                    fixed (byte* destPtr = dest)
                {
                    return Utf8Encoding.GetBytes(charPtr, text.Length, destPtr, dest.Length);
                }
                }
            }
#endif
        }
        catch (EncoderFallbackException ex)
        {
            // We want to be consistent with the exception being thrown
            // so the user only has to catch a single exception.
            // Since we already throw ArgumentException when validating other arguments,
            // using that exception for failure to encode invalid UTF-16 chars as well.
            // Therefore, wrapping the EncoderFallbackException around an ArgumentException.
            throw new TinyhandException("Cannot transcode invalid UTF-16 string.", ex);
        }
    }

    public static string GetTextFromUtf8(ReadOnlySpan<byte> utf8Text)
    {
        try
        {
            if (utf8Text.IsEmpty)
            {
                return string.Empty;
            }

            unsafe
            {
                fixed (byte* bytePtr = utf8Text)
                {
                    return Utf8Encoding.GetString(bytePtr, utf8Text.Length);
                }
            }
        }
        catch (DecoderFallbackException ex)
        {
            throw new TinyhandException("Invalid UTF-8 text", ex);
        }
    }

    public static uint GetFullNameId<T>() => IdCache<T>.Id;

    private static class IdCache<T>
    {
        public static readonly uint Id;

        static IdCache()
        {
            Id = (uint)Arc.Crypto.FarmHash.Hash64(typeof(T).FullName ?? string.Empty);
        }
    }
}
