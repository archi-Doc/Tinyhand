// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Tinyhand.IO;

public static class JournalHelper
{
    public static bool ReadJournal(IJournalObject journalObject, ReadOnlyMemory<byte> data)
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
            Span<byte> span = stackalloc byte[3];
            span[0] = reader.ReadUInt8();
            span[1] = reader.ReadUInt8();
            span[2] = reader.ReadUInt8();
            length = span[0] << 16 | span[1] << 8 | span[2];

            reader.TryRead(out byte code);
            journalType = (JournalType)code;
            // reader.TryReadBigEndian(out plane);
        }
        catch
        {
            length = 0;
            journalType = default;
            // plane = 0;
            return false;
        }

        return true;
    }

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
    public static void Write_Remove(ref this TinyhandWriter writer)
        => writer.Write((byte)JournalRecord.Remove);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write_Clear(ref this TinyhandWriter writer)
        => writer.Write((byte)JournalRecord.Clear);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JournalRecord Read_Record(ref this TinyhandReader reader)
       => (JournalRecord)reader.ReadUInt8();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Read_Locator(ref this TinyhandReader reader)
    {
        if (reader.Read_Record() != JournalRecord.Locator)
        {
            throw new InvalidDataException();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Read_Key(ref this TinyhandReader reader)
    {
        if (reader.Read_Record() != JournalRecord.Key)
        {
            throw new InvalidDataException();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Read_Value(ref this TinyhandReader reader)
    {
        if (reader.Read_Record() != JournalRecord.Value)
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
