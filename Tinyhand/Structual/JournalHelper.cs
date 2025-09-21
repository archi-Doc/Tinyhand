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
            if (!reader.TryReadJournal(out var length, out var journalType))
            {
                return false;
            }

            var fork = reader.Fork();
            try
            {
                if (journalType == JournalType.Record)
                {
                    if (journalObject.ProcessJournalRecord(ref reader))
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
    public static bool TryReadJournal(this ref TinyhandReader reader, out int length, out JournalType journalType)
    {
        try
        {
            reader.TryRead(out byte b0);
            reader.TryRead(out byte b1);
            reader.TryRead(out byte b2);
            reader.TryRead(out byte code);
            length = b0 << 16 | b1 << 8 | b2;
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
    public static bool TryReadJournalRecord(ref this TinyhandReader reader, out JournalRecord journalRecord)
    {
        var result = reader.TryRead(out byte b);
        journalRecord = (JournalRecord)b;
        return result;
    }

    /* /// <summary>
    /// Attempts to peek at the next journal record in the reader and checks if it is either a <see cref="JournalRecord.Key"/> or <see cref="JournalRecord.Locator"/>.<br/>
    /// If the next record is <see cref="JournalRecord.Key"/> or <see cref="JournalRecord.Locator"/>, sets <paramref name="journalRecord"/> and returns <c>true</c>.<br/>
    /// Otherwise, advances the reader by one byte, sets <paramref name="journalRecord"/>, and returns <c>false</c>.<br/>
    /// If there are no remaining bytes, sets <paramref name="journalRecord"/> to <see cref="JournalRecord.Invalid"/> and returns <c>false</c>.
    /// </summary>
    /// <param name="reader">The <see cref="TinyhandReader"/> to read from.</param>
    /// <param name="journalRecord">When this method returns, contains the journal record that was peeked or <see cref="JournalRecord.Invalid"/> if none was found.</param>
    /// <returns>
    /// <c>true</c> if the next journal record is <see cref="JournalRecord.Key"/> or <see cref="JournalRecord.Locator"/>; otherwise, <c>false</c>.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryReadJournalRecord_PeekIfKeyOrLocator(ref this TinyhandReader reader, out JournalRecord journalRecord)
    {
        if (reader.Remaining > 0)
        {
            journalRecord = (JournalRecord)reader.NextCode;
            if (journalRecord == JournalRecord.Key ||
                journalRecord == JournalRecord.Locator)
            {
                return true;
            }
            else
            {
                reader.Advance(1);
                return false;
            }
        }

        journalRecord = JournalRecord.Invalid;
        return false;
    }*/

    /// <summary>
    /// Attempts to peek at the next journal record in the reader and determines if it should be processed by this object or delegated to descendant objects.<br/>
    /// If the next record is <see cref="JournalRecord.Value"/> or <see cref="JournalRecord.Delete"/>, advances the reader by one byte, sets <paramref name="journalRecord"/>, and returns <c>false</c>.<br/>
    /// Otherwise, sets <paramref name="journalRecord"/> and returns <c>true</c>.<br/>
    /// If there are no remaining bytes, sets <paramref name="journalRecord"/> to <see cref="JournalRecord.Invalid"/> and returns <c>false</c>.
    /// </summary>
    /// <param name="reader">The <see cref="TinyhandReader"/> to read from.</param>
    /// <param name="journalRecord">When this method returns, contains the journal record that was peeked or <see cref="JournalRecord.Invalid"/> if none was found.</param>
    /// <returns>
    /// <c>true</c> if the next journal record is intended to be processed by descendant objects; <c>false</c> if it is intended to be processed by this object or if no record is found.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryReadJournalRecord_PeekIDelegated(ref this TinyhandReader reader, out JournalRecord journalRecord)
    {
        if (reader.Remaining > 0)
        {
            journalRecord = (JournalRecord)reader.NextCode;
            if (journalRecord == JournalRecord.Value ||
                journalRecord == JournalRecord.Delete)
            {// Journal is intended to be processed by this object.
                reader.Advance(1);
                return false;
            }
            else
            {// Journal is intended to be processed by descendant objects.
                return true;
            }
        }

        journalRecord = JournalRecord.Invalid;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryPeekJournalRecord(ref this TinyhandReader reader, out JournalRecord journalRecord)
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
        if (!reader.TryReadJournalRecord(out JournalRecord record) || record != JournalRecord.Locator)
        {
            throw new InvalidDataException();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Read_Key(ref this TinyhandReader reader)
    {
        if (!reader.TryReadJournalRecord(out JournalRecord record) || record != JournalRecord.Key)
        {
            throw new InvalidDataException();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Read_Value(ref this TinyhandReader reader)
    {
        if (!reader.TryReadJournalRecord(out JournalRecord record) || record != JournalRecord.Value)
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
