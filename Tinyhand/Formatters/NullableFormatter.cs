// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Buffers;
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

    public T? Deserialize(ref TinyhandReader reader, TinyhandSerializerOptions options)
    {
        if (reader.IsNil)
        {
            reader.ReadNil();
            return null;
        }
        else
        {
            return options.Resolver.GetFormatter<T>().Deserialize(ref reader, options);
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

    public T? Deserialize(ref TinyhandReader reader, TinyhandSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
            return null;
        }
        else
        {
            return this.underlyingFormatter.Deserialize(ref reader, options);
        }
    }

    public T? Reconstruct(TinyhandSerializerOptions options)
    {
        return default(T);
    }

    public T? Clone(T? value, TinyhandSerializerOptions options) => value == null ? null : this.underlyingFormatter.Clone(value.Value, options);
}
