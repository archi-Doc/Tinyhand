// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.IO;
using System.Runtime.CompilerServices;
using Tinyhand.IO;

namespace Tinyhand;

public class JournalTester : IStructualRoot
{
    private const int MaxJournalLength = 1024 * 1024 * 1; // 1 MB
    private const int MaxRecordLength = 1024 * 16; // 16 KB

    [ThreadStatic]
    private static byte[]? initialBuffer;

    public JournalTester(int journalLength = MaxJournalLength)
    {
        this.JournalLength = journalLength;
        this.buffer = new byte[this.JournalLength];
    }

    public byte[] GetJournal()
    {
        lock (this.syncObject)
        {
            return this.buffer.AsSpan(0, this.position).ToArray();
        }
    }

    public bool TryGetJournalWriter(JournalType recordType, out TinyhandWriter writer)
    {
        if (initialBuffer == null)
        {
            initialBuffer = new byte[MaxRecordLength];
        }

        writer = new(initialBuffer);
        writer.Advance(3); // Size(0-16MB): byte[3]
        writer.RawWriteUInt8(Unsafe.As<JournalType, byte>(ref recordType)); // JournalRecordType: byte
        // writer.RawWriteUInt32(plane); // Plane: byte[4]

        return true;
    }

    public ulong AddJournal(in TinyhandWriter writer)
    {
        writer.FlushAndGetMemory(out var memory, out _);
        writer.Dispose();

        if (memory.Length > MaxRecordLength)
        {
            throw new InvalidDataException($"The maximum length per record is {MaxRecordLength} bytes.");
        }

        // Size (0-16MB)
        var span = memory.Span;
        var length = memory.Length - 4;
        span[2] = (byte)length;
        span[1] = (byte)(length >> 8);
        span[0] = (byte)(length >> 16);

        lock (this.syncObject)
        {
            if ((this.buffer.Length - this.position) >= span.Length)
            {
                span.CopyTo(this.buffer.AsSpan(this.position));
                this.position += span.Length;
                return (ulong)this.position;
            }
            else
            {
                return 0;
            }
        }
    }

    public bool TryAddToSaveQueue()
        => false;

    public int JournalLength { get; init; }

    private object syncObject = new();
    private byte[] buffer;
    private int position;
}
