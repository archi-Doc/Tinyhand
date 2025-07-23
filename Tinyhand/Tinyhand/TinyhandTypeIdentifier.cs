// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Arc.Collections;
using Arc.Crypto;
using FastExpressionCompiler;

namespace Tinyhand;

public static class TinyhandTypeIdentifier
{
    private static readonly ConcurrentDictionary<uint, Type> TypeIdentifierToType = new();
    private static readonly UInt32Hashtable<MethodClass> TypeIdentifierToMethodClass = new();
    private static readonly ThreadsafeTypeKeyHashtable<MethodClass> TypeToMethodClass = new();

    private class MethodClass
    {
        public uint TypeIdentifier { get; }

        private Type? type;

        private Func<object, TinyhandSerializerOptions?, BytePool.RentMemory>? serializeRentMemory;

        public Func<object, TinyhandSerializerOptions?, BytePool.RentMemory>? SerializeRentMemory => this.serializeRentMemory ??= this.CreateSerializeRentMemory();

        private Func<object, TinyhandSerializerOptions?, byte[]>? serialize;

        public Func<object, TinyhandSerializerOptions?, byte[]>? Serialize => this.serialize ??= this.CreateSerialize();

        public MethodClass(Type type)
        {
            this.TypeIdentifier = GetTypeIdentifier(type);
        }

        public MethodClass(uint typeIdentifier)
        {
            this.TypeIdentifier = typeIdentifier;
        }

        [MemberNotNullWhen(true, nameof(type))]
        private bool EnsureType()
        {
            if (this.type is not null)
            {
                return true;
            }

            if (TypeIdentifierToType.TryGetValue(this.TypeIdentifier, out var type))
            {
                this.type = type;
                return true;
            }
            else
            {
                return false;
            }
        }

        private Func<object, TinyhandSerializerOptions?, byte[]>? CreateSerialize()
        {
            if (!this.EnsureType())
            {
                return default;
            }

            var typeInfo = this.type.GetTypeInfo();
            var serialize = TinyhandHelper.GetSerializerMethod("Serialize", this.type, [null, typeof(TinyhandSerializerOptions)]);
            var param1 = Expression.Parameter(typeof(object), "value");
            var param2 = Expression.Parameter(typeof(TinyhandSerializerOptions), "options");

            var body = Expression.Call(
                null,
                serialize,
                typeInfo.IsValueType ? Expression.Unbox(param1, this.type) : Expression.Convert(param1, this.type),
                param2);
            return Expression.Lambda<Func<object, TinyhandSerializerOptions?, byte[]>>(body, param1, param2).CompileFast();
        }

        private Func<object, TinyhandSerializerOptions?, BytePool.RentMemory>? CreateSerializeRentMemory()
        {
            if (!this.EnsureType())
            {
                return default;
            }

            var typeInfo = this.type.GetTypeInfo();
            var serialize = TinyhandHelper.GetSerializerMethod("SerializeToRentMemory", this.type, [null, typeof(TinyhandSerializerOptions)]);
            var param1 = Expression.Parameter(typeof(object), "value");
            var param2 = Expression.Parameter(typeof(TinyhandSerializerOptions), "options");

            var body = Expression.Call(
                null,
                serialize,
                typeInfo.IsValueType ? Expression.Unbox(param1, this.type) : Expression.Convert(param1, this.type),
                param2);
            return Expression.Lambda<Func<object, TinyhandSerializerOptions?, BytePool.RentMemory>>(body, param1, param2).CompileFast();
        }
    }

    public static (uint TypeIdentifier, byte[]? ByteArray) TrySerialize<T>(T value, TinyhandSerializerOptions? options = null)
    {
        var methodClass = TypeToMethodClass.GetOrAdd(typeof(T), type => new(type));
        if (methodClass.Serialize is null)
        {
            return default;
        }

        try
        {
            var byteArray = methodClass.Serialize(value!, options);
            return (methodClass.TypeIdentifier, byteArray);
        }
        catch
        {
            return default;
        }
    }

    public static (uint TypeIdentifier, byte[] ByteArray) TrySerialize(uint typeIdentifier, object value, TinyhandSerializerOptions? options = null)
    {
        var methodClass = TypeIdentifierToMethodClass.GetOrAdd(typeIdentifier, typeIdentifier => new(typeIdentifier));
        if (methodClass.Serialize is null)
        {
            return default;
        }

        try
        {
            var byteArray = methodClass.Serialize(value!, options);
            return (methodClass.TypeIdentifier, byteArray);
        }
        catch
        {
            return default;
        }
    }

    public static (uint TypeIdentifier, BytePool.RentMemory RentMemory) TrySerializeRentMemory<T>(T value, TinyhandSerializerOptions? options = null)
    {
        var methodClass = TypeToMethodClass.GetOrAdd(typeof(T), type => new(type));
        if (methodClass.SerializeRentMemory is null)
        {
            return default;
        }

        try
        {
            var rentMemory = methodClass.SerializeRentMemory(value!, options);
            return (methodClass.TypeIdentifier, rentMemory);
        }
        catch
        {
            return default;
        }
    }

    public static (uint TypeIdentifier, BytePool.RentMemory RentMemory) TrySerializeRentMemory(uint typeIdentifier, object value, TinyhandSerializerOptions? options = null)
    {
        var methodClass = TypeIdentifierToMethodClass.GetOrAdd(typeIdentifier, typeIdentifier => new(typeIdentifier));
        if (methodClass.SerializeRentMemory is null)
        {
            return default;
        }

        try
        {
            var rentMemory = methodClass.SerializeRentMemory(value!, options);
            return (methodClass.TypeIdentifier, rentMemory);
        }
        catch
        {
            return default;
        }
    }

    public static void Register<T>()
        => Register(typeof(T));

    public static uint GetTypeIdentifier<T>()
        => GetTypeIdentifier(typeof(T));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint GetTypeIdentifier(Type type)
        => (uint)FarmHash.Hash64(type.FullName ?? string.Empty);

    public static bool Register(Type type)
    {
        if (type.IsAbstract || type.IsInterface || type.IsGenericTypeDefinition || type.IsArray || type.IsPointer || type == typeof(void))
        {// Not instantiable type
            return false;
        }

        if (TypeIdentifierToType.TryAdd(GetTypeIdentifier(type), type))
        {
            // Clear();
            return true;
        }
        else
        {
            return false;
        }
    }

    public static void Register(ReadOnlySpan<Type> types)
    {
        foreach (var type in types)
        {
            Register(type);
        }
    }

    /*private static void Clear()
    {
        typeToTypeIdentifier = default;
    }*/
}
