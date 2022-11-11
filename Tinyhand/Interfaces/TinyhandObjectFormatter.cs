// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Tinyhand.IO;

namespace Tinyhand;

public class TinyhandObjectFormatter<T> : ITinyhandFormatter<T>
    where T : ITinyhandObject<T>
{
    public void Serialize(ref TinyhandWriter writer, T? v, TinyhandSerializerOptions options)
        => T.Serialize(ref writer, ref v, options);

    public T? Deserialize(ref TinyhandReader reader, TinyhandSerializerOptions options)
    {
        var v = default(T);
        // T.Deserialize(ref reader, ref v, options);
        return v;
    }

    public T Reconstruct(TinyhandSerializerOptions options)
        => default!; // T.Reconstruct(options);

    public T? Clone(T? value, TinyhandSerializerOptions options)
        => default; // T.Clone(ref value, options);
}
