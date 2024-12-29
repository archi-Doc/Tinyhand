// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Tinyhand.IO;

namespace Tinyhand.Formatters;

public class NilFormatter : ITinyhandFormatter<Nil>
{
    public static readonly ITinyhandFormatter<Nil> Instance = new NilFormatter();

    private NilFormatter()
    {
    }

    public void Serialize(ref TinyhandWriter writer, Nil value, TinyhandSerializerOptions options)
    {
        writer.WriteNil();
    }

    public void Deserialize(ref TinyhandReader reader, ref Nil value, TinyhandSerializerOptions options)
    {
        value = reader.ReadNil();
    }

    public Nil Reconstruct(TinyhandSerializerOptions options)
    {
        return Nil.Default;
    }

    public Nil Clone(Nil value, TinyhandSerializerOptions options) => value;
}

// NullableNil is same as Nil.
public class NullableNilFormatter : ITinyhandFormatter<Nil?>
{
    public static readonly ITinyhandFormatter<Nil?> Instance = new NullableNilFormatter();

    private NullableNilFormatter()
    {
    }

    public void Serialize(ref TinyhandWriter writer, Nil? value, TinyhandSerializerOptions options)
    {
        writer.WriteNil();
    }

    public void Deserialize(ref TinyhandReader reader, ref Nil? value, TinyhandSerializerOptions options)
    {
        value = reader.ReadNil();
    }

    public Nil? Reconstruct(TinyhandSerializerOptions options)
    {
        return Nil.Default;
    }

    public Nil? Clone(Nil? value, TinyhandSerializerOptions options) => value;
}
