// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using Arc.Collections;

namespace Tinyhand;

public static class TinyhandTypeId
{
    private static readonly ConcurrentDictionary<uint, Type> TypeIdToType = new();
    private static FrozenDictionary<Type, uint>? typeToTypeIdentifier;

    public static (uint TypeIdentifier, BytePool.RentMemory RentMemory) TrySerialize<T>(T value, TinyhandSerializerOptions? options = null)
    {
        if (typeToTypeIdentifier is null)
        {
            typeToTypeIdentifier = TypeIdToType.ToFrozenDictionary(pair => pair.Value, pair => pair.Key);
        }

        if (typeToTypeIdentifier.TryGetValue(typeof(T), out var typeIdentifier))
        {
            try
            {
                var rentMemory = TinyhandSerializer.SerializeToRentMemory(value, options);
                return (typeIdentifier, rentMemory);
            }
            catch
            {
                return default;
            }
        }
        else
        {
            return default;
        }
    }

    public static void Register<T>()
    {
        Register(typeof(T));
    }

    public static bool Register(Type type)
    {
        if (type.IsAbstract || type.IsInterface || type.IsGenericTypeDefinition || type.IsArray || type.IsPointer || type == typeof(void))
        {
            return false;
        }
        else
        {
            if (TypeIdToType.TryAdd((uint)FarmHash.Hash64(type.FullName ?? string.Empty), type))
            {
                Clear();
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    public static void Register(ReadOnlySpan<Type> types)
    {
        foreach (var type in types)
        {
            Register(type);
        }
    }

    private static void Clear()
    {
        typeToTypeIdentifier = default;
    }
}
