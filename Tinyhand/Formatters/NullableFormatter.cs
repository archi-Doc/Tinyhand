// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Tinyhand.IO;

namespace Tinyhand.Formatters;

public sealed class NullableFormatter<T> : ITinyhandFormatter<T?>
    where T : struct
{
    public void Serialize(ref TinyhandWriter writer, T? value, TinyhandSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNil();
        }
        else
        {
            options.Resolver.GetFormatter<T>().Serialize(ref writer, value.Value, options);
        }
    }

    public void Deserialize(ref TinyhandReader reader, ref T? value, TinyhandSerializerOptions options)
    {
        if (reader.IsNil)
        {
            reader.ReadNil();
        }
        else
        {
            value = options.Resolver.GetFormatter<T>().Deserialize(ref reader, options);
        }
    }

    public T? Reconstruct(TinyhandSerializerOptions options)
    {
        return default(T);
    }

    public T? Clone(T? value, TinyhandSerializerOptions options) => value == null ? null : options.Resolver.GetFormatter<T>().Clone(value.Value, options);
}

public sealed class StaticNullableFormatter<T> : ITinyhandFormatter<T?>
    where T : struct
{
    private readonly ITinyhandFormatter<T> underlyingFormatter;

    public StaticNullableFormatter(ITinyhandFormatter<T> underlyingFormatter)
    {
        this.underlyingFormatter = underlyingFormatter;
    }

    public void Serialize(ref TinyhandWriter writer, T? value, TinyhandSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNil();
        }
        else
        {
            this.underlyingFormatter.Serialize(ref writer, value.Value, options);
        }
    }

    public void Deserialize(ref TinyhandReader reader, ref T? value, TinyhandSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
        }
        else
        {
            value = this.underlyingFormatter.Deserialize(ref reader, options);
        }
    }

    public T? Reconstruct(TinyhandSerializerOptions options)
    {
        return default(T);
    }

    public T? Clone(T? value, TinyhandSerializerOptions options) => value == null ? null : this.underlyingFormatter.Clone(value.Value, options);
}
