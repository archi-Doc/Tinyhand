// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Buffers;
using Tinyhand.IO;

#pragma warning disable SA1649 // File name should match first type name

namespace Tinyhand.Formatters
{
    public sealed class NullableFormatter<T> : ITinyhandFormatter<T?>
        where T : struct
    {
        public void Serialize(ref TinyhandWriter writer, ref T? value, TinyhandSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                var rv = value.Value;
                options.Resolver.GetFormatter<T>().Serialize(ref writer, ref rv, options);
            }
        }

        public void Deserialize(ref TinyhandReader reader, ref T? value, TinyhandSerializerOptions options)
        {
            if (reader.IsNil)
            {
                reader.ReadNil();
                value = null;
            }
            else
            {
                T rv = default;
                options.Resolver.GetFormatter<T>().Deserialize(ref reader, ref rv, options);
                value = rv;
            }
        }

        public T? Reconstruct(TinyhandSerializerOptions options)
        {
            return default(T);
        }
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
    }
}
