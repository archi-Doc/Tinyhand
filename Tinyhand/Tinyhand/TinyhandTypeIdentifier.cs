﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Arc.Collections;
using Arc.Crypto;
using FastExpressionCompiler;
using Tinyhand.IO;

namespace Tinyhand;

public static class TinyhandTypeIdentifier
{
    private static readonly ConcurrentDictionary<uint, Type> TypeIdentifierToType = new();
    private static readonly UInt32Hashtable<MethodClass> TypeIdentifierToMethodClass = new();
    private static readonly ThreadsafeTypeKeyHashtable<uint> TypeToTypeIdentifier = new();
    private static readonly ThreadsafeTypeKeyHashtable<MethodClass> TypeToMethodClass = new();

    private class MethodClass
    {
        internal delegate void SerializeWriterDelegate(ref TinyhandWriter writer, object value, TinyhandSerializerOptions? options);

        internal delegate object? DeserializeReaderDelegate(ref TinyhandReader reader, TinyhandSerializerOptions? options);

        public uint TypeIdentifier { get; }

        private Type? type;

        private Func<object, TinyhandSerializerOptions?, BytePool.RentMemory>? serializeRentMemory;

        public Func<object, TinyhandSerializerOptions?, BytePool.RentMemory>? SerializeRentMemory => this.serializeRentMemory ??= this.CreateSerializeRentMemory();

        private Func<object, TinyhandSerializerOptions?, byte[]>? serialize;

        public Func<object, TinyhandSerializerOptions?, byte[]>? Serialize => this.serialize ??= this.CreateSerialize();

        private SerializeWriterDelegate? serializeWriter;

        public SerializeWriterDelegate? SerializeWriter => this.serializeWriter ??= this.CreateSerializeWriter();

        private Func<ReadOnlySpan<byte>, TinyhandSerializerOptions?, object?>? deserialize;

        public Func<ReadOnlySpan<byte>, TinyhandSerializerOptions?, object?>? Deserialize => this.deserialize ??= this.CreateDeserialize();

        private DeserializeReaderDelegate? deserializeReader;

        public DeserializeReaderDelegate? DeserializeReader => this.deserializeReader ??= this.CreateDeserializeReader();

        private Func<TinyhandSerializerOptions?, object?>? reconstruct;

        public Func<TinyhandSerializerOptions?, object?>? Reconstruct => this.reconstruct ??= this.CreateReconstruct();

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
            var method = TinyhandHelper.GetSerializerMethod("Serialize", this.type, [null, typeof(TinyhandSerializerOptions)]);
            var param1 = Expression.Parameter(typeof(object), "value");
            var param2 = Expression.Parameter(typeof(TinyhandSerializerOptions), "options");

            var body = Expression.Call(
                null,
                method,
                typeInfo.IsValueType ? Expression.Unbox(param1, this.type) : Expression.Convert(param1, this.type),
                param2);
            return Expression.Lambda<Func<object, TinyhandSerializerOptions?, byte[]>>(body, param1, param2).CompileFast();
        }

        private SerializeWriterDelegate? CreateSerializeWriter()
        {
            if (!this.EnsureType())
            {
                return default;
            }

            var typeInfo = this.type.GetTypeInfo();
            var method = TinyhandHelper.GetSerializerMethod("Serialize", this.type, [typeof(TinyhandWriter).MakeByRefType(), null, typeof(TinyhandSerializerOptions)]);
            var param1 = Expression.Parameter(typeof(TinyhandWriter).MakeByRefType(), "writer");
            var param2 = Expression.Parameter(typeof(object), "value");
            var param3 = Expression.Parameter(typeof(TinyhandSerializerOptions), "options");

            var body = Expression.Call(
                null,
                method,
                param1,
                typeInfo.IsValueType ? Expression.Unbox(param2, this.type) : Expression.Convert(param2, this.type),
                param3);
            return Expression.Lambda<SerializeWriterDelegate>(body, param1, param2, param3).CompileFast();
        }

        private Func<object, TinyhandSerializerOptions?, BytePool.RentMemory>? CreateSerializeRentMemory()
        {
            if (!this.EnsureType())
            {
                return default;
            }

            var typeInfo = this.type.GetTypeInfo();
            var method = TinyhandHelper.GetSerializerMethod("SerializeToRentMemory", this.type, [null, typeof(TinyhandSerializerOptions)]);
            var param1 = Expression.Parameter(typeof(object), "value");
            var param2 = Expression.Parameter(typeof(TinyhandSerializerOptions), "options");

            var body = Expression.Call(
                null,
                method,
                typeInfo.IsValueType ? Expression.Unbox(param1, this.type) : Expression.Convert(param1, this.type),
                param2);
            return Expression.Lambda<Func<object, TinyhandSerializerOptions?, BytePool.RentMemory>>(body, param1, param2).CompileFast();
        }

        private Func<ReadOnlySpan<byte>, TinyhandSerializerOptions?, object?>? CreateDeserialize()
        {
            if (!this.EnsureType())
            {
                return default;
            }

            var typeInfo = this.type.GetTypeInfo();
            var method = TinyhandHelper.GetSerializerMethod("Deserialize", this.type, [typeof(ReadOnlySpan<byte>), typeof(TinyhandSerializerOptions)]);
            var param1 = Expression.Parameter(typeof(ReadOnlySpan<byte>), "buffer");
            var param2 = Expression.Parameter(typeof(TinyhandSerializerOptions), "options");
            var body = Expression.Convert(Expression.Call(null, method, param1, param2), typeof(object));
            return Expression.Lambda<Func<ReadOnlySpan<byte>, TinyhandSerializerOptions?, object?>>(body, param1, param2).CompileFast();
        }

        private DeserializeReaderDelegate? CreateDeserializeReader()
        {
            if (!this.EnsureType())
            {
                return default;
            }

            var typeInfo = this.type.GetTypeInfo();
            var method = TinyhandHelper.GetSerializerMethod("Deserialize", this.type, [typeof(TinyhandReader).MakeByRefType(), typeof(TinyhandSerializerOptions)]);
            var param1 = Expression.Parameter(typeof(TinyhandReader).MakeByRefType(), "reader");
            var param2 = Expression.Parameter(typeof(TinyhandSerializerOptions), "options");
            var body = Expression.Convert(Expression.Call(null, method, param1, param2), typeof(object));
            return Expression.Lambda<DeserializeReaderDelegate>(body, param1, param2).CompileFast();
        }

        private Func<TinyhandSerializerOptions?, object?>? CreateReconstruct()
        {
            if (!this.EnsureType())
            {
                return default;
            }

            var typeInfo = this.type.GetTypeInfo();
            var method = TinyhandHelper.GetSerializerMethod("Reconstruct", this.type, [typeof(TinyhandSerializerOptions)]);
            var param1 = Expression.Parameter(typeof(TinyhandSerializerOptions), "options");
            var body = Expression.Convert(Expression.Call(null, method, param1), typeof(object));
            return Expression.Lambda<Func<TinyhandSerializerOptions?, object?>>(body, param1).CompileFast();
        }
    }

    /// <summary>
    /// Tries to serialize the specified value of type <typeparamref name="T"/> using the registered type identifier.
    /// </summary>
    /// <typeparam name="T">The type of the value to serialize.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">The serializer options. Set <see langword="null"/> to use default options.</param>
    /// <returns>
    /// A tuple containing the type identifier and the serialized byte array, or the default tuple if serialization fails.
    /// </returns>
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

    /// <summary>
    /// Tries to serialize the specified value using the given type identifier.
    /// </summary>
    /// <param name="typeIdentifier">The type identifier associated with the value's type.</param>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">The serializer options. Set <see langword="null"/> to use default options.</param>
    /// <returns>
    /// A tuple containing the type identifier and the serialized byte array, or the default tuple if serialization fails.
    /// </returns>
    public static (uint TypeIdentifier, byte[]? ByteArray) TrySerialize(uint typeIdentifier, object value, TinyhandSerializerOptions? options = null)
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

    /// <summary>
    /// Tries to serialize the specified value of type <typeparamref name="T"/> using the registered type identifier.
    /// </summary>
    /// <typeparam name="T">The type of the value to serialize.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">The serializer options. Set <see langword="null"/> to use default options.</param>
    /// <returns>
    /// A tuple containing the type identifier and the serialized <see cref="BytePool.RentMemory" />, or the default tuple if serialization fails.
    /// </returns>
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

    /// <summary>
    /// Tries to serialize the specified value using the given type identifier.
    /// </summary>
    /// <param name="typeIdentifier">The type identifier associated with the value's type.</param>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">The serializer options. Set <see langword="null"/> to use default options.</param>
    /// <returns>
    /// A tuple containing the type identifier and the serialized <see cref="BytePool.RentMemory" />, or the default tuple if serialization fails.
    /// </returns>
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

    /// <summary>
    /// Tries to serialize the specified value using the given type identifier.
    /// </summary>
    /// <param name="writer">The buffer writer to serialize with.</param>
    /// <param name="typeIdentifier">The type identifier associated with the value's type.</param>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">The serializer options. Set <see langword="null"/> to use default options.</param>
    /// <returns>
    /// <c>true</c> if the value was successfully serialized; otherwise, <c>false</c>.
    /// </returns>
    public static bool TrySerializeWriter(ref TinyhandWriter writer, uint typeIdentifier, object value, TinyhandSerializerOptions? options = null)
    {
        var methodClass = TypeIdentifierToMethodClass.GetOrAdd(typeIdentifier, typeIdentifier => new(typeIdentifier));
        if (methodClass.SerializeWriter is null)
        {
            return false;
        }

        try
        {
            methodClass.SerializeWriter(ref writer, value!, options);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Tries to deserialize the specified byte source into an object using the given type identifier.
    /// </summary>
    /// <param name="typeIdentifier">The type identifier associated with the target type.</param>
    /// <param name="source">The byte source to deserialize.</param>
    /// <param name="options">The serializer options. Set <see langword="null"/> to use default options.</param>
    /// <returns>
    /// The deserialized object, or <c>null</c> if deserialization fails.
    /// </returns>
    public static object? TryDeserialize(uint typeIdentifier, ReadOnlySpan<byte> source, TinyhandSerializerOptions? options = null)
    {
        var methodClass = TypeIdentifierToMethodClass.GetOrAdd(typeIdentifier, typeIdentifier => new(typeIdentifier));
        if (methodClass.Deserialize is null)
        {
            return default;
        }

        try
        {
            return methodClass.Deserialize(source, options);
        }
        catch
        {
            return default;
        }
    }

    /// <summary>
    /// Tries to deserialize the specified byte source into an object using the given type identifier.
    /// </summary>
    /// <param name="typeIdentifier">The type identifier associated with the target type.</param>
    /// <param name="reader">The reader to deserialize from.</param>
    /// <param name="options">The serializer options. Set <see langword="null"/> to use default options.</param>
    /// <returns>
    /// The deserialized object, or <c>null</c> if deserialization fails.
    /// </returns>
    public static object? TryDeserializeReader(uint typeIdentifier, ref TinyhandReader reader, TinyhandSerializerOptions? options = null)
    {
        var methodClass = TypeIdentifierToMethodClass.GetOrAdd(typeIdentifier, typeIdentifier => new(typeIdentifier));
        if (methodClass.DeserializeReader is null)
        {
            return default;
        }

        try
        {
            return methodClass.DeserializeReader(ref reader, options);
        }
        catch
        {
            return default;
        }
    }

    /// <summary>
    /// Create a new instance of the given type.
    /// </summary>
    /// <param name="typeIdentifier">The type identifier associated with the target type.</param>
    /// <param name="options">The options. Set <see langword="null"/> to use default options.</param>
    /// <returns>The created instance.</returns>
    public static object? TryReconstruct(uint typeIdentifier, TinyhandSerializerOptions? options = null)
    {
        var methodClass = TypeIdentifierToMethodClass.GetOrAdd(typeIdentifier, typeIdentifier => new(typeIdentifier));
        if (methodClass.Reconstruct is null)
        {
            return default;
        }

        try
        {
            return methodClass.Reconstruct(options);
        }
        catch
        {
            return default;
        }
    }

    /// <summary>
    /// Registers the specified type <typeparamref name="T"/> for type identifier mapping.
    /// </summary>
    /// <typeparam name="T">The type to register for type identifier mapping.</typeparam>
    public static void Register<T>()
        => Register(typeof(T));

    /// <summary>
    /// Gets the type identifier of the specified type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type for which to get the identifier.</typeparam>
    /// <returns>The type identifier as a <see cref="uint"/>.</returns>
    public static uint GetTypeIdentifier<T>()
        => GetTypeIdentifier(typeof(T));

    /// <summary>
    /// Gets the type identifier for the specified <see cref="Type"/>.
    /// </summary>
    /// <param name="type">The type for which to get the identifier.</param>
    /// <returns>The type identifier as a <see cref="uint"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint GetTypeIdentifier(Type type)
        => TypeToTypeIdentifier.GetOrAdd(type, x => (uint)FarmHash.Hash64(x.FullName ?? string.Empty));

    /// <summary>
    /// Registers the specified <see cref="Type"/> for type identifier mapping.
    /// </summary>
    /// <param name="type">The type to register.</param>
    /// <returns><c>true</c> if the type was successfully registered; otherwise, <c>false</c>.</returns>
    public static bool Register(Type type)
    {
        if (type.IsAbstract || type.IsInterface || type.IsGenericTypeDefinition || type.IsArray || type.IsPointer || type == typeof(void))
        {// Not instantiable type
            return false;
        }

        if (TypeIdentifierToType.TryAdd(GetTypeIdentifier(type), type))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// Registers a collection of types for type identifier mapping.
    /// </summary>
    /// <param name="types">A span of types to register.</param>
    public static void Register(ReadOnlySpan<Type> types)
    {
        foreach (var type in types)
        {
            Register(type);
        }
    }
}
