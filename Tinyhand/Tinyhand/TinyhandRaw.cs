// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Tinyhand.IO;

namespace Tinyhand;

public static class TinyhandRaw
{
    public static void Serialize<T>(ref TinyhandWriter writer, in T? value, TinyhandSerializerOptions? options = null)
        where T : ITinyhandSerialize<T>
    {
        options = options ?? TinyhandSerializer.DefaultOptions;
        T.Serialize(ref writer, ref Unsafe.AsRef(value), options);
    }

    public static void Deserialize<T>(ref TinyhandReader reader, scoped ref T? value, TinyhandSerializerOptions? options = null)
        where T : ITinyhandSerialize<T>
    {
        options = options ?? TinyhandSerializer.DefaultOptions;
        T.Deserialize(ref reader, ref value, options);
    }

    public static void Reconstruct<T>([NotNull] scoped ref T? value, TinyhandSerializerOptions? options = null)
        where T : ITinyhandReconstruct<T>
    {
        options = options ?? TinyhandSerializer.DefaultOptions;
        T.Reconstruct(ref value, options);
    }

    [return: NotNullIfNotNull("value")]
    public static T? Clone<T>(in T? value, TinyhandSerializerOptions? options = null)
        where T : ITinyhandClone<T>
    {
        options = options ?? TinyhandSerializer.DefaultOptions;
        return T.Clone(ref Unsafe.AsRef(value), options);
    }
}
