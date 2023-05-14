// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.IO;
using System.Runtime.CompilerServices;

namespace Tinyhand.IO;

public static class JournalHelper
{
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
}
