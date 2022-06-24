// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Buffers;
using Tinyhand.IO;

#pragma warning disable SA1009 // Closing parenthesis should be spaced correctly

namespace Tinyhand.Formatters;

public sealed class IgnoreFormatter<T> : ITinyhandFormatter<T>
{
    public void Serialize(ref TinyhandWriter writer, T? value, TinyhandSerializerOptions options)
    {
        writer.WriteNil();
    }

    public T? Deserialize(ref TinyhandReader reader, TinyhandSerializerOptions options)
    {
        reader.Skip();
        return default(T);
    }

    public T Reconstruct(TinyhandSerializerOptions options) => default(T)!;

    public T? Clone(T? value, TinyhandSerializerOptions options) => default(T)!;
}
