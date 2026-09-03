// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;
using Arc;
using Arc.Collections;
using Tinyhand.IO;

#pragma warning disable SA1401 // Generic caches are shared by the containing type.
#pragma warning disable SA1204 // Group the per-type cache with its adapter.

namespace Tinyhand;

public static class TinyhandTypeIdentifier
{
    private static readonly ConcurrentDictionary<uint, MethodClass> Methods = new();
    private static readonly ThreadsafeTypeKeyHashtable<uint> TypeToTypeIdentifier = new();

    static TinyhandTypeIdentifier()
    {
        Resolvers.BuiltinResolver.Instance.RegisterInstantiableTypes();
    }

    // One statically compiled adapter per registered type replaces nine compiled
    // expression delegates. Generic calls bypass this adapter to avoid boxing.
    private abstract class MethodClass
    {
        public abstract Type Type { get; }

        public abstract byte[] Serialize(object value, TinyhandSerializerOptions? options);

        public abstract BytePool.RentMemory SerializeRentMemory(object value, TinyhandSerializerOptions? options);

        public abstract void SerializeWriter(ref TinyhandWriter writer, object value, TinyhandSerializerOptions? options);

        public abstract object? Deserialize(ReadOnlySpan<byte> source, TinyhandSerializerOptions? options);

        public abstract object? DeserializeReader(ref TinyhandReader reader, TinyhandSerializerOptions? options);

        public abstract object? TryDeserializeFromString(ReadOnlySpan<char> utf16, TinyhandSerializerOptions? options);

        public abstract object? TryParseOrDeserializeFromString(ReadOnlySpan<char> utf16, TinyhandSerializerOptions? options);

        public abstract object? Reconstruct(TinyhandSerializerOptions? options);
    }

    private sealed class MethodClass<T> : MethodClass
    {
        internal static readonly MethodClass<T> Instance = new();

        internal Func<ReadOnlySpan<char>, TinyhandSerializerOptions?, T?>? Parser;

        public override Type Type => typeof(T);

        public override byte[] Serialize(object value, TinyhandSerializerOptions? options) => TinyhandSerializer.Serialize((T)value, options);

        public override BytePool.RentMemory SerializeRentMemory(object value, TinyhandSerializerOptions? options) => TinyhandSerializer.SerializeToRentMemory((T)value, options);

        public override void SerializeWriter(ref TinyhandWriter writer, object value, TinyhandSerializerOptions? options) => TinyhandSerializer.Serialize(ref writer, (T)value, options);

        public override object? Deserialize(ReadOnlySpan<byte> source, TinyhandSerializerOptions? options) => TinyhandSerializer.Deserialize<T>(source, options);

        public override object? DeserializeReader(ref TinyhandReader reader, TinyhandSerializerOptions? options) => TinyhandSerializer.Deserialize<T>(ref reader, options);

        public override object? TryDeserializeFromString(ReadOnlySpan<char> utf16, TinyhandSerializerOptions? options) => TinyhandSerializer.TryDeserializeFromString<T>(utf16, options);

        public override object? TryParseOrDeserializeFromString(ReadOnlySpan<char> utf16, TinyhandSerializerOptions? options)
        {
            var parser = Volatile.Read(ref this.Parser);
            return parser is null ? TinyhandSerializer.TryDeserializeFromString<T>(utf16, options) : parser(utf16, options);
        }

        public override object? Reconstruct(TinyhandSerializerOptions? options) => TinyhandSerializer.Reconstruct<T>(options);
    }

    private static class TypeCache<T>
    {
        internal static readonly uint Identifier = GetTypeIdentifier(typeof(T));
        internal static bool Registered;
    }

    /// <summary>
    /// Determines whether the specified type <typeparamref name="T"/> is registered with the serializer.
    /// </summary>
    /// <typeparam name="T">The type to check for registration.</typeparam>
    /// <returns>
    /// <c>true</c> if the type is registered; otherwise, <c>false</c>.
    /// </returns>
    public static bool IsRegistered<T>()
    {
        return Volatile.Read(ref TypeCache<T>.Registered);
    }

    /// <summary>
    /// Determines whether the specified <see cref="Type"/> is registered with the serializer.
    /// </summary>
    /// <param name="type">The type to check for registration.</param>
    /// <returns>
    /// <c>true</c> if the type is registered; otherwise, <c>false</c>.
    /// </returns>
    public static bool IsRegistered(Type type)
    {
        return Methods.TryGetValue(GetTypeIdentifier(type), out var methods) && methods.Type == type;
    }

    /// <summary>
    /// Determines whether the specified type identifier is registered with the serializer.
    /// </summary>
    /// <param name="typeIdentifier">The type identifier to check for registration.</param>
    /// <returns>
    /// <c>true</c> if the type identifier is registered; otherwise, <c>false</c>.
    /// </returns>
    public static bool IsRegistered(uint typeIdentifier)
    {
        return Methods.ContainsKey(typeIdentifier);
    }

    /// <summary>
    /// Tries to serialize the specified value of type <typeparamref name="T"/> to a UTF-16 string.
    /// </summary>
    /// <typeparam name="T">The type of the value to serialize.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">The serializer options. Set <see langword="null"/> to use default options.</param>
    /// <returns>
    /// A tuple containing the type identifier and the serialized string, or the default tuple if serialization fails.
    /// </returns>
    public static (uint TypeIdentifier, string? Utf16) TrySerializeToString<T>(T value, TinyhandSerializerOptions? options = null)
    {
        if (!IsRegistered<T>())
        {
            return default;
        }

        try
        {
            return (GetTypeIdentifier<T>(), TinyhandSerializer.SerializeToString(value, options));
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
    /// A tuple containing the type identifier and the serialized byte array, or the default tuple if serialization fails.
    /// </returns>
    public static (uint TypeIdentifier, byte[]? ByteArray) TrySerialize<T>(T value, TinyhandSerializerOptions? options = null)
    {
        if (!IsRegistered<T>())
        {
            return default;
        }

        try
        {
            var byteArray = TinyhandSerializer.Serialize(value, options);
            return (GetTypeIdentifier<T>(), byteArray);
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
        if (!Methods.TryGetValue(typeIdentifier, out var methodClass))
        {
            return default;
        }

        try
        {
            var byteArray = methodClass.Serialize(value!, options);
            return (typeIdentifier, byteArray);
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
        if (!IsRegistered<T>())
        {
            return default;
        }

        try
        {
            var rentMemory = TinyhandSerializer.SerializeToRentMemory(value, options);
            return (GetTypeIdentifier<T>(), rentMemory);
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
        if (!Methods.TryGetValue(typeIdentifier, out var methodClass))
        {
            return default;
        }

        try
        {
            var rentMemory = methodClass.SerializeRentMemory(value!, options);
            return (typeIdentifier, rentMemory);
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
        if (!Methods.TryGetValue(typeIdentifier, out var methodClass))
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
    /// Tries to deserialize the specified UTF-16 string into an object using the given type identifier.
    /// </summary>
    /// <param name="typeIdentifier">The type identifier associated with the target type.</param>
    /// <param name="utf16">The UTF-16 string to deserialize.</param>
    /// <param name="options">The serializer options. Set <see langword="null"/> to use default options.</param>
    /// <returns>
    /// The deserialized object, or <c>null</c> if deserialization fails.
    /// </returns>
    public static object? TryDeserializeFromString(uint typeIdentifier, ReadOnlySpan<char> utf16, TinyhandSerializerOptions? options = null)
    {
        if (!Methods.TryGetValue(typeIdentifier, out var methodClass))
        {
            return default;
        }

        return methodClass.TryDeserializeFromString(utf16, options);
    }

    public static object? TryParseOrDeserializeFromString(uint typeIdentifier, ReadOnlySpan<char> utf16, TinyhandSerializerOptions? options = null)
    {
        if (!Methods.TryGetValue(typeIdentifier, out var methodClass))
        {
            return default;
        }

        return methodClass.TryParseOrDeserializeFromString(utf16, options);
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
        if (!Methods.TryGetValue(typeIdentifier, out var methodClass))
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
        if (!Methods.TryGetValue(typeIdentifier, out var methodClass))
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
        if (!Methods.TryGetValue(typeIdentifier, out var methodClass))
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
    {
        if (Volatile.Read(ref TypeCache<T>.Registered))
        {
            return;
        }

        var methods = MethodClass<T>.Instance;
        if (!Methods.TryAdd(TypeCache<T>.Identifier, methods) && Methods[TypeCache<T>.Identifier].Type != typeof(T))
        {
            throw new InvalidOperationException($"Type identifier collision: {typeof(T)}.");
        }

        Volatile.Write(ref TypeCache<T>.Registered, true);
    }

    /// <summary>Registers a statically compiled parser for a string-convertible type.</summary>
    /// <typeparam name="T">The serializable type providing a static parser.</typeparam>
    public static void RegisterStringConvertible<T>()
        where T : ITinyhandSerializable<T>, IStringConvertible<T>
    {
        Register<T>();
        Volatile.Write(ref MethodClass<T>.Instance.Parser, TinyhandSerializer.TryParseOrDeserializeFromString<T>);
    }

    /// <summary>
    /// Gets the type identifier of the specified type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type for which to get the identifier.</typeparam>
    /// <returns>The type identifier as a <see cref="uint"/>.</returns>
    public static uint GetTypeIdentifier<T>()
        => TypeCache<T>.Identifier;

    /// <summary>
    /// Gets the type identifier for the specified <see cref="Type"/>.
    /// </summary>
    /// <param name="type">The type for which to get the identifier.</param>
    /// <returns>The type identifier as a <see cref="uint"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint GetTypeIdentifier(Type type)
        => TypeToTypeIdentifier.GetOrAdd(type, x => (uint)FarmHash.Hash64(x.FullName ?? string.Empty));
}
