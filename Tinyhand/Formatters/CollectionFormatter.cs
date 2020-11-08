// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Tinyhand.IO;

namespace Tinyhand.Formatters
{
    public sealed class ArrayFormatter<T> : ITinyhandFormatter<T[]>
    {
        public void Serialize(ref TinyhandWriter writer, T[]? value, TinyhandSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                ITinyhandFormatter<T> formatter = options.Resolver.GetFormatter<T>();

                writer.WriteArrayHeader(value.Length);

                for (int i = 0; i < value.Length; i++)
                {
                    writer.CancellationToken.ThrowIfCancellationRequested();
                    formatter.Serialize(ref writer, value[i], options);
                }
            }
        }

        public T[]? Deserialize(ref TinyhandReader reader, TinyhandSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return default;
            }
            else
            {
                ITinyhandFormatter<T> formatter = options.Resolver.GetFormatter<T>();

                var len = reader.ReadArrayHeader();
                var array = new T[len];
                options.Security.DepthStep(ref reader);
                try
                {
                    for (int i = 0; i < array.Length; i++)
                    {
                        reader.CancellationToken.ThrowIfCancellationRequested();
                        array[i] = formatter.Deserialize(ref reader, options) ?? formatter.Reconstruct(options);
                    }
                }
                finally
                {
                    reader.Depth--;
                }

                return array;
            }
        }

        public T[] Reconstruct(TinyhandSerializerOptions options)
        {
            return new T[0];
        }
    }
}
