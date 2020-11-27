// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Buffers;
using Tinyhand.IO;

#pragma warning disable SA1009 // Closing parenthesis should be spaced correctly

namespace Tinyhand.Formatters
{
    public sealed class IgnoreFormatter<T> : ITinyhandFormatter<T>
    {
        public void Serialize(ref TinyhandWriter writer, ref T? value, TinyhandSerializerOptions options)
        {
            writer.WriteNil();
        }

        public void Deserialize(ref TinyhandReader reader, ref T? value, TinyhandSerializerOptions options)
        {
            reader.Skip();
            value = default;
        }

        public T Reconstruct(TinyhandSerializerOptions options)
        {
            return default(T)!;
        }
    }
}
