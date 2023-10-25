// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Tinyhand.IO;

public static class JournalHelper
{
    public static bool ReadJournal(IStructualObject journalObject, ReadOnlyMemory<byte> data)
    {
        var reader = new TinyhandReader(data.Span);
        var success = true;

        while (reader.Consumed < data.Length)
        {
            if (!reader.TryReadRecord(out var length, out var journalType))
            {
                return false;
            }

            var fork = reader.Fork();
            try
            {
                if (journalType == JournalType.Record)
                {
                    if (journalObject.ReadRecord(ref reader))
                    {// Success
                    }
                    else
                    {// Failure
                        success = false;
                    }
                }
                else
                {
                }
            }
            catch
            {
                success = false;
            }
            finally
            {
                reader = fork;
                reader.Advance(length);
            }
        }

        return success;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryReadRecord(this ref TinyhandReader reader, out int length, out JournalType journalType)
    {
        try
        {
            var b0 = reader.ReadUInt8();
            var b1 = reader.ReadUInt8();
            var b2 = reader.ReadUInt8();
            length = b0 << 16 | b1 << 8 | b2;

            reader.TryRead(out byte code);
            journalType = (JournalType)code;
        }
        catch
        {
            length = 0;
            journalType = default;
            return false;
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(ref this TinyhandWriter writer, JournalRecord journalRecord)
        => writer.Write((byte)journalRecord);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write_Locator(ref this TinyhandWriter writer)
        => writer.Write((byte)JournalRecord.Locator);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write_Key(ref this TinyhandWriter writer)
        => writer.Write((byte)JournalRecord.Key);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write_Value(ref this TinyhandWriter writer)
        => writer.Write((byte)JournalRecord.Value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write_Add(ref this TinyhandWriter writer)
        => writer.Write((byte)JournalRecord.Add);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryRead(ref this TinyhandReader reader, out JournalRecord journalRecord)
    {
        var result = reader.TryRead(out byte b);
        journalRecord = (JournalRecord)b;
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryPeek(ref this TinyhandReader reader, out JournalRecord journalRecord)
    {
        if (reader.Remaining > 0)
        {
            journalRecord = (JournalRecord)reader.NextCode;
            return true;
        }
        else
        {
            journalRecord = JournalRecord.Locator;
            return false;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Read_Locator(ref this TinyhandReader reader)
    {
        if (!reader.TryRead(out JournalRecord record) || record != JournalRecord.Locator)
        {
            throw new InvalidDataException();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Read_Key(ref this TinyhandReader reader)
    {
        if (!reader.TryRead(out JournalRecord record) || record != JournalRecord.Key)
        {
            throw new InvalidDataException();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Read_Value(ref this TinyhandReader reader)
    {
        if (!reader.TryRead(out JournalRecord record) || record != JournalRecord.Value)
        {
            throw new InvalidDataException();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNext_Key(ref this TinyhandReader reader)
    {
        return reader.Remaining > 0 && reader.NextCode == (byte)JournalRecord.Key;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNext_NonValue(ref this TinyhandReader reader)
    {
        return reader.Remaining > 0 && reader.NextCode != (byte)JournalRecord.Value;
    }
}
