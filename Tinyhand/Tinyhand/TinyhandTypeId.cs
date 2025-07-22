// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Concurrent;

namespace Tinyhand;

public static class TinyhandTypeId
{
    private static readonly ConcurrentDictionary<uint, Type> TypeIdToType = new();

    public static void Register<T>()
    {
        Register(typeof(T));
    }

    public static void Register(Type type)
    {
        TypeIdToType.TryAdd((uint)FarmHash.Hash64(type.FullName ?? string.Empty), type);
    }

    public static void Register(ReadOnlySpan<Type> types)
    {
        foreach (var type in types)
        {
            TypeIdToType.TryAdd((uint)FarmHash.Hash64(type.FullName ?? string.Empty), type);
        }
    }
}
