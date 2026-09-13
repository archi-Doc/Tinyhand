// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Tinyhand.IO;

/// <summary>
/// Reads and writes journal headers, record markers, and structural updates.
/// </summary>
public static class JournalHelper
{
    public static bool ReplayJournal(IStructuralObject target, ReadOnlyMemory<byte> data)
    {
        var reader = new TinyhandReader(data.Span);
        var success = true;

        while (reader.Consumed < data.Length)
        {
            if (!reader.TryReadJournalHeader(out var length, out var journalType))
            {
                return false;
            }

            if (length > reader.Remaining)
            {// The journal is truncated. Do not apply a partial record.
                return false;
            }

            var recordReader = reader.CreateSubReader(reader.ReadRaw(length));
            try
            {
                if (journalType == JournalType.Record)
                {
                    if (!target.ProcessJournalRecord(ref recordReader))
                    {// Failure
                        success = false;
                    }
                }
            }
            catch
            {
                success = false;
            }
        }

        return success;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryReadJournalHeader(this ref TinyhandReader reader, out int length, out JournalType journalType)
    {
        if (reader.Remaining < 4)
        {
            length = 0;
            journalType = default;
            return false;
        }

        reader.TryRead(out byte b0);
        reader.TryRead(out byte b1);
        reader.TryRead(out byte b2);
        reader.TryRead(out byte code);
        length = (b0 << 16) | (b1 << 8) | b2;
        journalType = (JournalType)code;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(ref this TinyhandWriter writer, JournalRecordType journalRecord)
        => writer.Write((byte)journalRecord);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteLocatorRecord(ref this TinyhandWriter writer)
        => writer.Write((byte)JournalRecordType.Locator);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteKeyRecord(ref this TinyhandWriter writer)
        => writer.Write((byte)JournalRecordType.Key);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteValueRecord(ref this TinyhandWriter writer)
        => writer.Write((byte)JournalRecordType.Value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryReadJournalRecord(ref this TinyhandReader reader, out JournalRecordType journalRecord)
    {
        var result = reader.TryRead(out byte b);
        journalRecord = (JournalRecordType)b;
        return result;
    }

    /*/// <summary>
    /// Attempts to peek at the next journal record in the reader and determines if it should be processed by this object or delegated to descendant objects.<br/>
    /// If the next record is <see cref="JournalRecordType.Value"/> or <see cref="JournalRecordType.Delete"/>, advances the reader by one byte, sets <paramref name="journalRecord"/>, and returns <c>false</c>.<br/>
    /// Otherwise, sets <paramref name="journalRecord"/> and returns <c>true</c>.<br/>
    /// If there are no remaining bytes, sets <paramref name="journalRecord"/> to <see cref="JournalRecordType.Invalid"/> and returns <c>false</c>.
    /// </summary>
    /// <param name="reader">The <see cref="TinyhandReader"/> to read from.</param>
    /// <param name="journalRecord">When this method returns, contains the journal record that was peeked or <see cref="JournalRecordType.Invalid"/> if none was found.</param>
    /// <returns>
    /// <c>true</c> if the next journal record is intended to be processed by descendant objects; <c>false</c> if it is intended to be processed by this object or if no record is found.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryReadJournalRecord_PeekIDelegated(ref this TinyhandReader reader, out JournalRecordType journalRecord)
    {
        if (reader.Remaining > 0)
        {
            journalRecord = (JournalRecordType)reader.NextCode;
            if (journalRecord == JournalRecordType.Value ||
                journalRecord == JournalRecordType.Delete)
            {// Journal is intended to be processed by this object.
                reader.Advance(1);
                return false;
            }
            else
            {// Journal is intended to be processed by descendant objects.
                return true;
            }
        }

        journalRecord = JournalRecordType.Invalid;
        return false;
    }*/

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryPeekJournalRecord(ref this TinyhandReader reader, out JournalRecordType journalRecord)
    {
        if (reader.Remaining > 0)
        {
            journalRecord = (JournalRecordType)reader.NextCode;
            return true;
        }
        else
        {
            journalRecord = JournalRecordType.Invalid;
            return false;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadLocatorRecord(ref this TinyhandReader reader)
    {
        if (!reader.TryReadJournalRecord(out JournalRecordType record) || record != JournalRecordType.Locator)
        {
            throw new InvalidDataException();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadKeyRecord(ref this TinyhandReader reader)
    {
        if (!reader.TryReadJournalRecord(out JournalRecordType record) || record != JournalRecordType.Key)
        {
            throw new InvalidDataException();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadValueRecord(ref this TinyhandReader reader)
    {
        if (!reader.TryReadJournalRecord(out JournalRecordType record) || record != JournalRecordType.Value)
        {
            throw new InvalidDataException();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNextKeyRecord(ref this TinyhandReader reader)
    {
        return reader.Remaining > 0 && reader.NextCode == (byte)JournalRecordType.Key;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNextNonValueRecord(ref this TinyhandReader reader)
    {
        return reader.Remaining > 0 && reader.NextCode != (byte)JournalRecordType.Value;
    }
}
