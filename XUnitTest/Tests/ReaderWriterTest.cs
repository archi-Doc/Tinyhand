// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using Tinyhand;
using Tinyhand.IO;
using Xunit;

namespace XUnitTest.Tests;

public class ReaderWriterTest
{
    private delegate void WriteAction(ref TinyhandWriter writer);

    private static byte[] WriteAndGet(WriteAction action)
    {
        var writer = TinyhandWriter.CreateFromBytePool();
        try
        {
            action(ref writer);
            return writer.FlushAndGetArray();
        }
        finally
        {
            writer.Dispose();
        }
    }

    [Fact]
    public void Int128RoundTrip()
    {
        Int128[] values =
        [
            0,
            1,
            -1,
            sbyte.MinValue,
            sbyte.MaxValue,
            short.MinValue,
            short.MaxValue,
            int.MinValue,
            int.MaxValue,
            long.MinValue,
            long.MaxValue,
            (Int128)long.MaxValue + 1, // Upper == 0, Lower >= 2^63
            (Int128)ulong.MaxValue,
            (Int128)long.MinValue - 1, // Upper == ~0, Lower < 2^63
            -(Int128)ulong.MaxValue,
            new Int128(0xFFFF_FFFF_FFFF_FFFF, 0), // Upper == ~0, Lower == 0
            new Int128(0, 0x8000_0000_0000_0000), // Upper == 0, Lower == 2^63
            Int128.MinValue,
            Int128.MaxValue,
        ];

        foreach (var value in values)
        {
            var v = value;
            var bin = WriteAndGet((ref TinyhandWriter w) => w.Write(v));
            var reader = new TinyhandReader(bin);
            reader.ReadInt128().Is(v);
        }
    }

    [Fact]
    public void UInt128RoundTrip()
    {
        UInt128[] values =
        [
            0,
            1,
            byte.MaxValue,
            ushort.MaxValue,
            uint.MaxValue,
            (UInt128)long.MaxValue,
            (UInt128)long.MaxValue + 1,
            ulong.MaxValue,
            (UInt128)ulong.MaxValue + 1,
            UInt128.MaxValue,
        ];

        foreach (var value in values)
        {
            var v = value;
            var bin = WriteAndGet((ref TinyhandWriter w) => w.Write(v));
            var reader = new TinyhandReader(bin);
            reader.ReadUInt128().Is(v);
        }
    }

    [Fact]
    public void Int128ToDouble()
    {
        ((Int128)0).ToDouble().Is(0d);
        ((Int128)(-1)).ToDouble().Is(-1d);
        ((Int128)long.MaxValue).ToDouble().Is((double)long.MaxValue);
        ((Int128)long.MinValue).ToDouble().Is((double)long.MinValue);
        (((Int128)long.MaxValue) + 1).ToDouble().Is((double)((Int128)long.MaxValue + 1));
        ((Int128)ulong.MaxValue).ToDouble().Is((double)(Int128)ulong.MaxValue);
        (-(Int128)ulong.MaxValue).ToDouble().Is((double)(-(Int128)ulong.MaxValue));
        new Int128(0xFFFF_FFFF_FFFF_FFFF, 0).ToDouble().Is((double)new Int128(0xFFFF_FFFF_FFFF_FFFF, 0));

        ((UInt128)0).ToDouble().Is(0d);
        ((UInt128)ulong.MaxValue).ToDouble().Is((double)ulong.MaxValue);
        (((UInt128)ulong.MaxValue) + 1).ToDouble().Is((double)((UInt128)ulong.MaxValue + 1));
    }

    [Fact]
    public void IdentifierRoundTrip()
    {
        string[] values = [string.Empty, "a", "identifier", "日本語の識別子", new string('x', 1000)];
        foreach (var value in values)
        {
            var v = value;
            var bin = WriteAndGet((ref TinyhandWriter w) => w.WriteIdentifier(v));
            var reader = new TinyhandReader(bin);
            reader.ReadIdentifierUtf16().Is(v);
        }
    }

    [Fact]
    public void SkipExtension()
    {
        // An extension whose length would be negative when cast to int must not corrupt the reader.
        var bin = new byte[] { MessagePackCode.Ext32, 0xFF, 0xFF, 0xFF, 0xFF, 0x01, };
        var reader = new TinyhandReader(bin);
        reader.TrySkip().IsFalse();
    }

    [Fact]
    public void SkipCorruptedMapDoesNotOverflow()
    {
        // Map32 with a huge count: count * 2 overflows int.
        var bin = new byte[] { MessagePackCode.Map32, 0x40, 0x00, 0x00, 0x00, };
        var reader = new TinyhandReader(bin);
        reader.TrySkip().IsFalse();

        Assert.ThrowsAny<Exception>(() => new TinyhandReader(bin).ReadMapHeader());
    }

    [Fact]
    public void ReadArrayHeaderRejectsNegativeCount()
    {
        // Array32 with a count larger than int.MaxValue.
        var bin = new byte[] { MessagePackCode.Array32, 0xFF, 0xFF, 0xFF, 0xFF, };
        Assert.ThrowsAny<Exception>(() => new TinyhandReader(bin).ReadArrayHeader());
    }

    [Fact]
    public void WriterLargeString()
    {
        // Exercises every string header size.
        int[] lengths = [0, 1, 31, 32, 255, 256, 65535, 65536];
        foreach (var length in lengths)
        {
            var value = new string('a', length);
            var bin = WriteAndGet((ref TinyhandWriter w) => w.Write(value));
            var reader = new TinyhandReader(bin);
            reader.ReadString().Is(value);
        }
    }

    [Fact]
    public void WriterBinary()
    {
        int[] lengths = [0, 1, 255, 256, 65535, 65536];
        foreach (var length in lengths)
        {
            var value = new byte[length];
            new Random(length).NextBytes(value);
            var bin = WriteAndGet((ref TinyhandWriter w) => w.Write(value));
            var reader = new TinyhandReader(bin);
            reader.ReadBytesToArray().SequenceEqual(value).IsTrue();
        }
    }

    [Fact]
    public void ReaderReverseAndFork()
    {
        var bin = WriteAndGet((ref TinyhandWriter w) =>
        {
            w.Write(1);
            w.Write("test");
            w.Write(true);
        });

        var reader = new TinyhandReader(bin);
        reader.ReadInt32().Is(1);

        var fork = reader.Fork();
        fork.ReadString().Is("test");
        fork.ReadBoolean().IsTrue();
        fork.End.IsTrue();

        // The original reader is unaffected.
        reader.ReadString().Is("test");
        reader.Reverse(reader.Consumed - 1);
        reader.Consumed.Is(1);
    }
}
