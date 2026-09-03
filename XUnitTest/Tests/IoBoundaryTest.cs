// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers.Binary;
using System.IO;
using System.Numerics;
using System.Text;
using Arc;
using Tinyhand;
using Tinyhand.IO;
using Xunit;

namespace XUnitTest.Tests;

public class IoBoundaryTest
{
    private static readonly (byte Code, int Size, bool Signed)[] Encodings =
    [
        (MessagePackCode.UInt8, 1, false), (MessagePackCode.UInt16, 2, false),
        (MessagePackCode.UInt32, 4, false), (MessagePackCode.UInt64, 8, false),
        (MessagePackCode.Int8, 1, true), (MessagePackCode.Int16, 2, true),
        (MessagePackCode.Int32, 4, true), (MessagePackCode.Int64, 8, true),
        (MessagePackExtensionCodes.Int128, 16, true), (MessagePackExtensionCodes.UInt128, 16, false),
    ];

    [Fact]
    public void IntegerConversionsCheckEveryWidthAndBoundary()
    {
        foreach (var source in Encodings)
        {
            var (sourceMin, sourceMax) = Range(source.Size, source.Signed);
            BigInteger[] values =
            [
                sourceMin, sourceMax, 0, 1, -1, -33, -32, 127, 128, 255, 256,
                short.MinValue, short.MaxValue, (BigInteger)short.MaxValue + 1,
                ushort.MaxValue, (BigInteger)ushort.MaxValue + 1,
                int.MinValue, int.MaxValue, (BigInteger)int.MaxValue + 1,
                uint.MaxValue, (BigInteger)uint.MaxValue + 1,
                long.MinValue, long.MaxValue, (BigInteger)long.MaxValue + 1,
                ulong.MaxValue, (BigInteger)ulong.MaxValue + 1,
            ];

            foreach (var value in values)
            {
                if (value < sourceMin || value > sourceMax)
                {
                    continue;
                }

                var bytes = Encode(source.Code, source.Size, value);
                for (var target = 0; target < Encodings.Length; target++)
                {
                    // Only the 128-bit readers accept the extension encodings.
                    if (source.Size == 16 && target < 8)
                    {
                        continue;
                    }

                    var (min, max) = Range(Encodings[target].Size, Encodings[target].Signed);
                    if (value < min || value > max)
                    {
                        Assert.Throws<OverflowException>(() => Decode(bytes, target));
                    }
                    else
                    {
                        Assert.Equal(value, Decode(bytes, target));
                    }
                }

                for (var length = 0; length < bytes.Length; length++)
                {
                    var truncated = bytes.AsSpan(0, length).ToArray();
                    Assert.Throws<EndOfStreamException>(() => Decode(truncated, source.Size == 16 ? 9 : 3));
                }
            }
        }
    }

    [Fact]
    public void FixintsAndInvalidCodes()
    {
        for (var code = 0; code <= byte.MaxValue; code++)
        {
            var bytes = new byte[] { (byte)code };
            for (var target = 0; target < Encodings.Length; target++)
            {
                if (code <= 127 || code >= 224)
                {
                    var value = (BigInteger)unchecked((sbyte)code);
                    if (value < 0 && !Encodings[target].Signed)
                    {
                        Assert.Throws<OverflowException>(() => Decode(bytes, target));
                    }
                    else
                    {
                        Assert.Equal(value, Decode(bytes, target));
                    }
                }
                else if (code is >= MessagePackCode.UInt8 and <= MessagePackCode.Int64 ||
                    (code == MessagePackCode.FixExt16 && target >= 8))
                {
                    Assert.Throws<EndOfStreamException>(() => Decode(bytes, target));
                }
                else
                {
                    Assert.Throws<TinyhandUnexpectedCodeException>(() => Decode(bytes, target));
                }
            }
        }
    }

    [Fact]
    public void TryReadUnsignedRejectsNegativeValuesAndPreservesPosition()
    {
        foreach (var source in Encodings.AsSpan(0, 8))
        {
            var bytes = Encode(source.Code, source.Size, source.Signed ? -1 : Range(source.Size, false).Max);
            var reader = new TinyhandReader(bytes);
            Assert.Equal(!source.Signed, reader.TryReadUInt64(out var value));
            Assert.Equal(source.Signed ? 0UL : (ulong)Range(source.Size, false).Max, value);
            Assert.Equal(source.Signed ? 0 : bytes.Length, reader.Consumed);

            for (var length = 0; length < bytes.Length; length++)
            {
                reader = new TinyhandReader(bytes.AsSpan(0, length));
                Assert.False(reader.TryReadUInt64(out value));
                Assert.Equal(0UL, value);
                Assert.Equal(0, reader.Consumed);
            }

            bytes = Encode(source.Code, source.Size, 1);
            reader = new TinyhandReader(bytes);
            Assert.True(reader.TryReadUInt64(out value));
            Assert.Equal(1UL, value);
            Assert.True(reader.End);
        }

        foreach (byte code in new byte[] { 0xff, 0xe0, MessagePackCode.Nil, MessagePackCode.True })
        {
            var reader = new TinyhandReader(new byte[] { code });
            Assert.False(reader.TryReadUInt64(out var value));
            Assert.Equal(0UL, value);
            Assert.Equal(0, reader.Consumed);
        }
    }

    [Fact]
    public void IdentifiersRejectEveryTruncatedPrefix()
    {
        using var writer = TinyhandWriter.CreateFromBytePool();
        writer.WriteIdentifier("identifier");
        var bytes = writer.FlushAndGetArray();
        for (var length = 0; length < bytes.Length; length++)
        {
            var truncated = bytes.AsSpan(0, length).ToArray();
            Assert.Throws<EndOfStreamException>(() => new TinyhandReader(truncated).ReadIdentifierUtf8());
            Assert.Throws<EndOfStreamException>(() => new TinyhandReader(truncated).ReadIdentifierUtf16());
        }

        bytes[5] = MessagePackExtensionCodes.Int128;
        Assert.Throws<TinyhandUnexpectedCodeException>(() => new TinyhandReader(bytes).ReadIdentifierUtf8());
        Assert.Throws<TinyhandUnexpectedCodeException>(() => new TinyhandReader(bytes).ReadIdentifierUtf16());
    }

    [Fact]
    public void PooledBinaryHasExactLength()
    {
        foreach (var length in new[] { 0, 1, 3, 17, 257, 1025 })
        {
            var data = new byte[length];
            new Random(length).NextBytes(data);
            using var writer = TinyhandWriter.CreateFromBytePool();
            writer.Write(data);
            var reader = new TinyhandReader(writer);
            var memory = reader.ReadBytesToRentMemory();
            try
            {
                Assert.Equal(length, memory.Length);
                Assert.True(data.AsSpan().SequenceEqual(memory.Span));
                Assert.True(reader.End);
            }
            finally
            {
                memory.Return();
            }
        }
    }

    [Fact]
    public void SkipDeepContainersWithoutRecursion()
    {
        var bytes = new byte[100_002];
        bytes.AsSpan(0, 100_000).Fill(0x91);
        bytes[100_000] = MessagePackCode.Nil;
        bytes[100_001] = MessagePackCode.True;
        var reader = new TinyhandReader(bytes);
        Assert.True(reader.TrySkip());
        Assert.Equal(100_001, reader.Consumed);
        Assert.True(reader.ReadBoolean());

        reader = new TinyhandReader(bytes.AsSpan(0, 100_000));
        Assert.False(reader.TrySkip());
    }

    [Fact]
    public void SkipMixedContainersAndTruncatedPayloads()
    {
        using var writer = TinyhandWriter.CreateFromBytePool();
        writer.WriteArrayHeader(6);
        writer.WriteMapHeader(1);
        writer.Write("key");
        writer.WriteArrayHeader(0);
        writer.Write(new string('x', 300));
        writer.Write(new byte[300]);
        writer.WriteUInt64(ulong.MaxValue);
        writer.WriteExtensionFormatHeader(new ExtensionHeader(42, 4));
        writer.WriteRawInt32(123);
        writer.Write(false);
        var bytes = writer.FlushAndGetArray();
        var reader = new TinyhandReader(bytes);
        Assert.True(reader.TrySkip());
        Assert.True(reader.End);
        for (var length = 0; length < bytes.Length; length++)
        {
            reader = new TinyhandReader(bytes.AsSpan(0, length));
            Assert.False(reader.TrySkip());
        }
    }

    [Fact]
    public void StringSpanHandlesEmptyAndMultibyteBoundaries()
    {
        foreach (var length in new[] { 0, 1, 10, 11, 31, 32, 85, 86, 255, 256, 21845, 21846, 65535, 65536 })
        {
            foreach (var character in new[] { 'a', '漢', '\ud800' })
            {
                var value = new string(character, length);
                var expected = Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(value));
                using var writer = TinyhandWriter.CreateFromBytePool();
                writer.Write(value.AsSpan());
                var reader = new TinyhandReader(writer);
                Assert.Equal(expected, reader.ReadString());
                Assert.True(reader.End);
            }
        }
    }

    [Fact]
    public void HeadersRejectNegativeCountsAndOverflow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ExtensionHeader(0, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => WriteInvalidHeader(0, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => WriteInvalidHeader(1, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => WriteInvalidHeader(2, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => WriteInvalidHeader(3, -1));
        Assert.Throws<OverflowException>(() => WriteInvalidHeader(2, int.MaxValue));
        Assert.Throws<OverflowException>(() => WriteInvalidHeader(3, int.MaxValue));

        foreach (byte code in new[] { MessagePackCode.Array32, MessagePackCode.Map32 })
        {
            var reader = new TinyhandReader(new byte[] { code, 0xff, 0xff, 0xff, 0xff });
            Assert.False(reader.TrySkip());
        }

        using var writer = TinyhandWriter.CreateFromBytePool();
        writer.WriteExtensionFormatHeader(new ExtensionHeader(123, uint.MaxValue));
        var headerReader = new TinyhandReader(writer);
        Assert.True(headerReader.TryReadExtensionFormatHeader(out var header));
        Assert.Equal(uint.MaxValue, header.Length);
        Assert.Equal((byte)123, header.TypeCode);
        Assert.True(headerReader.End);
    }

    [Fact]
    public void BooleanCodesAreValidated()
    {
        for (var code = 0; code <= byte.MaxValue; code++)
        {
            var bytes = new byte[] { (byte)code };
            if (code == MessagePackCode.False || code == MessagePackCode.True)
            {
                Assert.Equal(code == MessagePackCode.True, new TinyhandReader(bytes).ReadBoolean());
            }
            else
            {
                Assert.Throws<TinyhandUnexpectedCodeException>(() => new TinyhandReader(bytes).ReadBoolean());
            }
        }
    }

    [Fact]
    public void FixedWidthWritersMatchWireBytes()
    {
        foreach (var encoding in Encodings)
        {
            var (min, max) = Range(encoding.Size, encoding.Signed);
            foreach (var value in new[] { min, BigInteger.Zero, BigInteger.One, max })
            {
                using var writer = TinyhandWriter.CreateFromBytePool();
                switch (encoding.Code)
                {
                    case MessagePackCode.UInt8: writer.WriteUInt8((byte)value); break;
                    case MessagePackCode.UInt16: writer.WriteUInt16((ushort)value); break;
                    case MessagePackCode.UInt32: writer.WriteUInt32((uint)value); break;
                    case MessagePackCode.UInt64: writer.WriteUInt64((ulong)value); break;
                    case MessagePackCode.Int8: writer.WriteInt8((sbyte)value); break;
                    case MessagePackCode.Int16: writer.WriteInt16((short)value); break;
                    case MessagePackCode.Int32: writer.WriteInt32((int)value); break;
                    case MessagePackCode.Int64: writer.WriteInt64((long)value); break;
                    case MessagePackExtensionCodes.Int128: writer.WriteInt128((Int128)value); break;
                    case MessagePackExtensionCodes.UInt128: writer.WriteUInt128((UInt128)value); break;
                }

                Assert.Equal(Encode(encoding.Code, encoding.Size, value), writer.FlushAndGetArray());
            }
        }
    }

    [Fact]
    public void FloatingPointWritersPreserveBits()
    {
        foreach (var bits in new[] { 0L, long.MinValue, 1L, long.MaxValue, 0x7ff0000000000000, 0x7ff8000000000042 })
        {
            using var writer = TinyhandWriter.CreateFromBytePool();
            writer.Write(BitConverter.Int64BitsToDouble(bits));
            var bytes = writer.FlushAndGetArray();
            Assert.Equal(MessagePackCode.Float64, bytes[0]);
            Assert.Equal(bits, BinaryPrimitives.ReadInt64BigEndian(bytes.AsSpan(1)));
            Assert.Equal(bits, BitConverter.DoubleToInt64Bits(new TinyhandReader(bytes).ReadDouble()));
        }

        foreach (var bits in new[] { 0, int.MinValue, 1, int.MaxValue, 0x7f800000, 0x7fc00042 })
        {
            using var writer = TinyhandWriter.CreateFromBytePool();
            writer.Write(BitConverter.Int32BitsToSingle(bits));
            var bytes = writer.FlushAndGetArray();
            Assert.Equal(MessagePackCode.Float32, bytes[0]);
            Assert.Equal(bits, BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(1)));
            Assert.Equal(bits, BitConverter.SingleToInt32Bits(new TinyhandReader(bytes).ReadSingle()));
        }
    }

    [Fact]
    public void CodeLookupCoversEveryByte()
    {
        for (var code = 0; code <= byte.MaxValue; code++)
        {
            var expected = code switch
            {
                <= 0x7f or >= 0xe0 or (>= 0xcc and <= 0xd3) => MessagePackType.Integer,
                <= 0x8f or 0xde or 0xdf => MessagePackType.Map,
                <= 0x9f or 0xdc or 0xdd => MessagePackType.Array,
                <= 0xbf or 0xd9 or 0xda or 0xdb => MessagePackType.String,
                0xc0 => MessagePackType.Nil,
                0xc1 => MessagePackType.Unknown,
                0xc2 or 0xc3 => MessagePackType.Boolean,
                0xc4 or 0xc5 or 0xc6 => MessagePackType.Binary,
                0xca or 0xcb => MessagePackType.Float,
                _ => MessagePackType.Extension,
            };
            Assert.Equal(expected, MessagePackCode.ToMessagePackType((byte)code));
            Assert.False(string.IsNullOrEmpty(MessagePackCode.ToFormatName((byte)code)));
            Assert.Equal(code is >= 0xe0 or (>= 0xd0 and <= 0xd3), MessagePackCode.IsSignedInteger((byte)code));
        }
    }

    [Fact]
    public void StringConvertibleUsesCompleteDecodedText()
    {
        foreach (var text in new[] { string.Empty, "test", "日本語", new string('x', 1024), new string('漢', 1025), "\ud800" })
        {
            foreach (var identifier in new[] { false, true })
            {
                using var writer = TinyhandWriter.CreateFromBytePool();
                if (identifier)
                {
                    writer.WriteIdentifier(text);
                }
                else
                {
                    writer.Write(text);
                }

                var reader = new TinyhandReader(writer);
                CapturedText? parsed = null;
                reader.TryReadStringConvertible(ref parsed);
                Assert.Equal(Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(text)), parsed!.Text);
                Assert.True(reader.End);
            }
        }

        var nilReader = new TinyhandReader(new byte[] { MessagePackCode.Nil });
        CapturedText? original = new() { Text = "unchanged" };
        var instance = original;
        nilReader.TryReadStringConvertible(ref instance);
        Assert.Same(original, instance);
        Assert.True(nilReader.End);

        var invalidUtf8Reader = new TinyhandReader(new byte[] { 0xa2, 0xff, (byte)'a' });
        invalidUtf8Reader.TryReadStringConvertible(ref instance);
        Assert.Equal("\ufffda", instance!.Text);
        Assert.True(invalidUtf8Reader.End);
    }

    private sealed class CapturedText : IStringConvertible<CapturedText>
    {
        public string Text { get; init; } = string.Empty;

        public static int MaxStringLength => -1;

        public static bool TryParse(ReadOnlySpan<char> source, out CapturedText? instance, out int read, IConversionOptions? conversionOptions = default)
        {
            instance = new CapturedText { Text = source.ToString() };
            read = source.Length;
            return true;
        }

        public int GetStringLength() => this.Text.Length;

        public bool TryFormat(Span<char> destination, out int written, IConversionOptions? conversionOptions = default)
        {
            var success = this.Text.AsSpan().TryCopyTo(destination);
            written = success ? this.Text.Length : 0;
            return success;
        }
    }

    private static (BigInteger Min, BigInteger Max) Range(int size, bool signed)
        => signed ? (-(BigInteger.One << ((size * 8) - 1)), (BigInteger.One << ((size * 8) - 1)) - 1)
            : (BigInteger.Zero, (BigInteger.One << (size * 8)) - 1);

    private static byte[] Encode(byte code, int size, BigInteger value)
    {
        var prefix = size == 16 ? 2 : 1;
        var result = new byte[prefix + size];
        result[0] = size == 16 ? MessagePackCode.FixExt16 : code;
        if (size == 16)
        {
            result[1] = code;
        }

        var payload = result.AsSpan(prefix);
        payload.Fill(value.Sign < 0 ? (byte)0xff : (byte)0);
        var bytes = value.ToByteArray(isUnsigned: value.Sign >= 0, isBigEndian: true);
        bytes.CopyTo(payload.Slice(size - bytes.Length));
        return result;
    }

    private static BigInteger Decode(byte[] bytes, int target)
    {
        var reader = new TinyhandReader(bytes);
        BigInteger result = target switch
        {
            0 => reader.ReadUInt8(), 1 => reader.ReadUInt16(),
            2 => reader.ReadUInt32(), 3 => reader.ReadUInt64(),
            4 => reader.ReadInt8(), 5 => reader.ReadInt16(),
            6 => reader.ReadInt32(), 7 => reader.ReadInt64(),
            8 => (BigInteger)reader.ReadInt128(), 9 => (BigInteger)reader.ReadUInt128(),
            _ => throw new ArgumentOutOfRangeException(nameof(target)),
        };
        Assert.True(reader.End);
        return result;
    }

    private static void WriteInvalidHeader(int kind, int length)
    {
        using var writer = TinyhandWriter.CreateFromBytePool();
        switch (kind)
        {
            case 0: writer.WriteArrayHeader(length); break;
            case 1: writer.WriteMapHeader(length); break;
            case 2: writer.WriteBinHeader(length); break;
            case 3: writer.WriteStringHeader(length); break;
        }
    }
}
