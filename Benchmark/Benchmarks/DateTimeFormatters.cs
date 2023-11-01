// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using Tinyhand.IO;

namespace Tinyhand.Formatters;

/// <summary>
/// Serialize by .NET native DateTime binary format.
/// </summary>
public sealed class NativeDateTimeFormatter : ITinyhandFormatter<DateTime>
{
    public static readonly NativeDateTimeFormatter Instance = new NativeDateTimeFormatter();

    public void Serialize(ref TinyhandWriter writer, DateTime value, TinyhandSerializerOptions options)
    {
        var dateData = value.ToBinary();
        writer.Write(dateData);
    }

    public DateTime Deserialize(ref TinyhandReader reader, TinyhandSerializerOptions options)
    {
        var dateData = reader.ReadInt64();
        return DateTime.FromBinary(dateData);
    }

    public DateTime Reconstruct(TinyhandSerializerOptions options)
    {
        return default;
    }

    public DateTime Clone(DateTime value, TinyhandSerializerOptions options) => value;
}

public sealed class NativeDateTimeArrayFormatter : ITinyhandFormatter<DateTime[]>
{
    public static readonly NativeDateTimeArrayFormatter Instance = new NativeDateTimeArrayFormatter();

    public void Serialize(ref TinyhandWriter writer, DateTime[]? value, TinyhandSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNil();
        }
        else
        {
            writer.WriteArrayHeader(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                writer.Write(value[i].ToBinary());
            }
        }
    }

    public DateTime[]? Deserialize(ref TinyhandReader reader, TinyhandSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
            return null;
        }

        var len = reader.ReadArrayHeader();
        if (len == 0)
        {
            return Array.Empty<DateTime>();
        }

        var array = new DateTime[len];
        for (int i = 0; i < array.Length; i++)
        {
            var dateData = reader.ReadInt64();
            array[i] = DateTime.FromBinary(dateData);
        }

        return array;
    }

    public DateTime[] Reconstruct(TinyhandSerializerOptions options)
    {
        return new DateTime[0];
    }

    public DateTime[]? Clone(DateTime[]? value, TinyhandSerializerOptions options)
    {
        if (value == null)
        {
            return null;
        }
        else
        {
            var array = new DateTime[value.Length];
            Array.Copy(value, array, value.Length);
            return array;
        }
    }
}
