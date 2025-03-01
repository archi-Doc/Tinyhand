﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Tinyhand.IO;

namespace Tinyhand.Formatters;

public class TinyhandObjectFormatter<T> : ITinyhandFormatter<T>
    where T : ITinyhandSerializable<T>, ITinyhandReconstructable<T>, ITinyhandCloneable<T>
{
    public void Serialize(ref TinyhandWriter writer, T? v, TinyhandSerializerOptions options)
        => T.Serialize(ref writer, ref v, options);

    public void Deserialize(ref TinyhandReader reader, ref T? value, TinyhandSerializerOptions options)
        => T.Deserialize(ref reader, ref value, options);

    public T Reconstruct(TinyhandSerializerOptions options)
    {
        var v = default(T);
        T.Reconstruct(ref v, options);
        return v;
    }

    public T? Clone(T? value, TinyhandSerializerOptions options)
        => T.Clone(ref value, options);
}
