// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Tinyhand.IO;

#pragma warning disable SA1202 // Elements should be ordered by access
#pragma warning disable SA1604 // Element documentation should have summary
#pragma warning disable SA1611 // Element parameters should be documented
#pragma warning disable SA1615 // Element return value should be documented

namespace Tinyhand;

public partial class TinyhandSerializer
{
    private static readonly Func<Type, CompiledMethods> CreateCompiledMethods;
    private static readonly ThreadsafeTypeKeyHashtable<CompiledMethods> Serializes = new(capacity: 64);

    static TinyhandSerializer()
    {
        CreateCompiledMethods = t => new CompiledMethods(t);
        Tinyhand.Resolvers.StandardResolver.Instance.RegisterInstantiableTypes();
    }

    /// <seealso cref="Reconstruct{T}(TinyhandSerializerOptions?)"/>
    public static object Reconstruct(Type type, TinyhandSerializerOptions? options = null)
    {
        return GetOrAdd(type).Reconstruct_T_Options.Invoke(options);
    }

    /// <seealso cref="Serialize{T}(ref TinyhandWriter, T, TinyhandSerializerOptions?)"/>
    public static void Serialize(Type type, ref TinyhandWriter writer, object obj, TinyhandSerializerOptions? options = null)
    {
        GetOrAdd(type).Serialize_TinyhandWriter_T_Options.Invoke(ref writer, obj, options);
    }

    /// <seealso cref="Serialize{T}(IBufferWriter{byte}, T, TinyhandSerializerOptions?)"/>
    public static void Serialize(Type type, IBufferWriter<byte> writer, object obj, TinyhandSerializerOptions? options = null)
    {
        GetOrAdd(type).Serialize_IBufferWriter_T_Options_CancellationToken.Invoke(writer, obj, options);
    }

    /// <seealso cref="Serialize{T}(T, TinyhandSerializerOptions?)"/>
    public static byte[] Serialize(Type type, object obj, TinyhandSerializerOptions? options = null)
    {
        return GetOrAdd(type).Serialize_T_Options.Invoke(obj, options);
    }

    /// <seealso cref="Serialize{T}(Stream, T, TinyhandSerializerOptions?)"/>
    public static void Serialize(Type type, Stream stream, object obj, TinyhandSerializerOptions? options = null)
    {
        GetOrAdd(type).Serialize_Stream_T_Options_CancellationToken.Invoke(stream, obj, options);
    }

    /// <seealso cref="SerializeAsync{T}(Stream, T, TinyhandSerializerOptions?)"/>
    public static Task SerializeAsync(Type type, Stream stream, object obj, TinyhandSerializerOptions? options = null)
    {
        return GetOrAdd(type).SerializeAsync_Stream_T_Options_CancellationToken.Invoke(stream, obj, options);
    }

    /// <seealso cref="Deserialize{T}(ref TinyhandReader, TinyhandSerializerOptions?)"/>
    public static object? Deserialize(Type type, ref TinyhandReader reader, TinyhandSerializerOptions? options = null)
    {
        return GetOrAdd(type).Deserialize_TinyhandReader_Options.Invoke(ref reader, options);
    }

    /// <seealso cref="Deserialize{T}(Stream, TinyhandSerializerOptions?)"/>
    public static object? Deserialize(Type type, Stream stream, TinyhandSerializerOptions? options = null)
    {
        return GetOrAdd(type).Deserialize_Stream_Options_CancellationToken.Invoke(stream, options);
    }

    /// <seealso cref="DeserializeAsync{T}(Stream, TinyhandSerializerOptions?)"/>
    public static ValueTask<object?> DeserializeAsync(Type type, Stream stream, TinyhandSerializerOptions? options = null)
    {
        return GetOrAdd(type).DeserializeAsync_Stream_Options_CancellationToken.Invoke(stream, options);
    }

    /// <seealso cref="Deserialize{T}(ReadOnlySpan{byte}, TinyhandSerializerOptions?)"/>
    public static object? Deserialize(Type type, ReadOnlySpan<byte> bytes, TinyhandSerializerOptions? options = null)
    {
        return GetOrAdd(type).Deserialize_ReadOnlyMemory_Options.Invoke(bytes, options);
    }

    private static async ValueTask<object?> DeserializeObjectAsync<T>(Stream stream, TinyhandSerializerOptions? options) => await DeserializeAsync<T>(stream, options).ConfigureAwait(false);

    private static CompiledMethods GetOrAdd(Type type)
    {
        return Serializes.GetOrAdd(type, CreateCompiledMethods);
    }

    private class CompiledMethods
    {
        internal delegate void TinyhandWriterSerialize(ref TinyhandWriter writer, object value, TinyhandSerializerOptions? options);

        internal delegate object? TinyhandReaderDeserialize(ref TinyhandReader reader, TinyhandSerializerOptions? options);

        private const bool PreferInterpretation =
#if ENABLE_IL2CPP
            true;
#else
            false;
#endif

#pragma warning disable SA1310 // Field names should not contain underscore
#pragma warning disable SA1401 // Fields should be private
        internal readonly Func<TinyhandSerializerOptions?, object> Reconstruct_T_Options;
        internal readonly Func<object, TinyhandSerializerOptions?, byte[]> Serialize_T_Options;
        internal readonly Action<Stream, object, TinyhandSerializerOptions?> Serialize_Stream_T_Options_CancellationToken;
        internal readonly Func<Stream, object, TinyhandSerializerOptions?, Task> SerializeAsync_Stream_T_Options_CancellationToken;
        internal readonly TinyhandWriterSerialize Serialize_TinyhandWriter_T_Options;
        internal readonly Action<IBufferWriter<byte>, object, TinyhandSerializerOptions?> Serialize_IBufferWriter_T_Options_CancellationToken;

        internal readonly TinyhandReaderDeserialize Deserialize_TinyhandReader_Options;
        internal readonly Func<Stream, TinyhandSerializerOptions?, object?> Deserialize_Stream_Options_CancellationToken;
        internal readonly Func<Stream, TinyhandSerializerOptions?, ValueTask<object?>> DeserializeAsync_Stream_Options_CancellationToken;

        internal readonly Func<ReadOnlySpan<byte>, TinyhandSerializerOptions?, object?> Deserialize_ReadOnlyMemory_Options;
#pragma warning restore SA1401 // Fields should be private
#pragma warning restore SA1310 // Field names should not contain underscore

        internal CompiledMethods(Type type)
        {
            TypeInfo ti = type.GetTypeInfo();
            {
                // public static T Reconstruct<T>(TinyhandSerializerOptions? options)
                var reconstruct = GetMethod(nameof(Reconstruct), type, new Type?[] { typeof(TinyhandSerializerOptions) });
#if ENABLE_IL2CPP
                this.Reconstruct_T_Options = (x) => reconstruct.Invoke(null, new object?[] { x });
#else
                var param1 = Expression.Parameter(typeof(TinyhandSerializerOptions), "options");

                var body = Expression.Call(
                    null,
                    reconstruct,
                    param1);
                var lambda = Expression.Lambda<Func<TinyhandSerializerOptions?, object>>(body, param1).Compile(PreferInterpretation);

                this.Reconstruct_T_Options = lambda;
#endif
            }

            {
                // public static byte[] Serialize<T>(T obj, TinyhandSerializerOptions? options)
                var serialize = GetMethod(nameof(Serialize), type, new Type?[] { null, typeof(TinyhandSerializerOptions), });
#if ENABLE_IL2CPP
                this.Serialize_T_Options = (x, y, z) => (byte[])serialize.Invoke(null, new object?[] { x, y, z });
#else
                var param1 = Expression.Parameter(typeof(object), "obj");
                var param2 = Expression.Parameter(typeof(TinyhandSerializerOptions), "options");

                var body = Expression.Call(
                    null,
                    serialize,
                    ti.IsValueType ? Expression.Unbox(param1, type) : Expression.Convert(param1, type),
                    param2);
                var lambda = Expression.Lambda<Func<object, TinyhandSerializerOptions?, byte[]>>(body, param1, param2).Compile(PreferInterpretation);

                this.Serialize_T_Options = lambda;
#endif
            }

            {
                // public static void Serialize<T>(Stream stream, T obj, TinyhandSerializerOptions? options)
                MethodInfo serialize = GetMethod(nameof(Serialize), type, new Type?[] { typeof(Stream), null, typeof(TinyhandSerializerOptions), });
#if ENABLE_IL2CPP
                this.Serialize_Stream_T_Options_CancellationToken = (x, y, z, a) => serialize.Invoke(null, new object?[] { x, y, z, a });
#else
                var param1 = Expression.Parameter(typeof(Stream), "stream");
                var param2 = Expression.Parameter(typeof(object), "obj");
                var param3 = Expression.Parameter(typeof(TinyhandSerializerOptions), "options");

                var body = Expression.Call(
                    null,
                    serialize,
                    param1,
                    ti.IsValueType ? Expression.Unbox(param2, type) : Expression.Convert(param2, type),
                    param3);
                var lambda = Expression.Lambda<Action<Stream, object, TinyhandSerializerOptions?>>(body, param1, param2, param3).Compile(PreferInterpretation);

                this.Serialize_Stream_T_Options_CancellationToken = lambda;
#endif
            }

            {
                // public static Task SerializeAsync<T>(Stream stream, T obj, TinyhandSerializerOptions? options)
                var serialize = GetMethod(nameof(SerializeAsync), type, new Type?[] { typeof(Stream), null, typeof(TinyhandSerializerOptions), });
#if ENABLE_IL2CPP
                this.SerializeAsync_Stream_T_Options_CancellationToken = (x, y, z, a) => (Task)serialize.Invoke(null, new object?[] { x, y, z, a });
#else
                var param1 = Expression.Parameter(typeof(Stream), "stream");
                var param2 = Expression.Parameter(typeof(object), "obj");
                var param3 = Expression.Parameter(typeof(TinyhandSerializerOptions), "options");

                var body = Expression.Call(
                    null,
                    serialize,
                    param1,
                    ti.IsValueType ? Expression.Unbox(param2, type) : Expression.Convert(param2, type),
                    param3);
                var lambda = Expression.Lambda<Func<Stream, object, TinyhandSerializerOptions?, Task>>(body, param1, param2, param3).Compile(PreferInterpretation);

                this.SerializeAsync_Stream_T_Options_CancellationToken = lambda;
#endif
            }

            {
                // public static Task Serialize<T>(IBufferWriter<byte> writer, T obj, TinyhandSerializerOptions? options)
                var serialize = GetMethod(nameof(Serialize), type, new Type?[] { typeof(IBufferWriter<byte>), null, typeof(TinyhandSerializerOptions), });
#if ENABLE_IL2CPP
                this.Serialize_IBufferWriter_T_Options_CancellationToken = (x, y, z, a) => serialize.Invoke(null, new object?[] { x, y, z, a });
#else
                var param1 = Expression.Parameter(typeof(IBufferWriter<byte>), "writer");
                var param2 = Expression.Parameter(typeof(object), "obj");
                var param3 = Expression.Parameter(typeof(TinyhandSerializerOptions), "options");

                var body = Expression.Call(
                    null,
                    serialize,
                    param1,
                    ti.IsValueType ? Expression.Unbox(param2, type) : Expression.Convert(param2, type),
                    param3);
                var lambda = Expression.Lambda<Action<IBufferWriter<byte>, object, TinyhandSerializerOptions?>>(body, param1, param2, param3).Compile(PreferInterpretation);

                this.Serialize_IBufferWriter_T_Options_CancellationToken = lambda;
#endif
            }

            {
                // public static void Serialize<T>(ref TinyhandWriter writer, T obj, TinyhandSerializerOptions? options)
                var serialize = GetMethod(nameof(Serialize), type, new Type?[] { typeof(TinyhandWriter).MakeByRefType(), null, typeof(TinyhandSerializerOptions) });
#if ENABLE_IL2CPP
                this.Serialize_TinyhandWriter_T_Options = (ref TinyhandWriter x, object y, TinyhandSerializerOptions? z) => ThrowRefStructNotSupported();
#else
                var param1 = Expression.Parameter(typeof(TinyhandWriter).MakeByRefType(), "writer");
                var param2 = Expression.Parameter(typeof(object), "obj");
                var param3 = Expression.Parameter(typeof(TinyhandSerializerOptions), "options");

                var body = Expression.Call(
                    null,
                    serialize,
                    param1,
                    ti.IsValueType ? Expression.Unbox(param2, type) : Expression.Convert(param2, type),
                    param3);
                var lambda = Expression.Lambda<TinyhandWriterSerialize>(body, param1, param2, param3).Compile(PreferInterpretation);

                this.Serialize_TinyhandWriter_T_Options = lambda;
#endif
            }

            {
                // public static T Deserialize<T>(ref TinyhandReader reader, TinyhandSerializerOptions? options)
                var deserialize = GetMethod(nameof(Deserialize), type, new Type[] { typeof(TinyhandReader).MakeByRefType(), typeof(TinyhandSerializerOptions) });
#if ENABLE_IL2CPP
                this.Deserialize_TinyhandReader_Options = (ref TinyhandReader reader, TinyhandSerializerOptions? options) =>
                {
                    ThrowRefStructNotSupported();
                    return null;
                };
#else
                var param1 = Expression.Parameter(typeof(TinyhandReader).MakeByRefType(), "reader");
                var param2 = Expression.Parameter(typeof(TinyhandSerializerOptions), "options");
                var body = Expression.Convert(Expression.Call(null, deserialize, param1, param2), typeof(object));
                var lambda = Expression.Lambda<TinyhandReaderDeserialize>(body, param1, param2).Compile();

                this.Deserialize_TinyhandReader_Options = lambda;
#endif
            }

            {
                // public static T Deserialize<T>(Stream stream, TinyhandSerializerOptions? options)
                var deserialize = GetMethod(nameof(Deserialize), type, new Type[] { typeof(Stream), typeof(TinyhandSerializerOptions),  });
#if ENABLE_IL2CPP
                this.Deserialize_Stream_Options_CancellationToken = (x, y, z) => deserialize.Invoke(null, new object?[] { x, y, z });
#else
                var param1 = Expression.Parameter(typeof(Stream), "stream");
                var param2 = Expression.Parameter(typeof(TinyhandSerializerOptions), "options");
                var body = Expression.Convert(Expression.Call(null, deserialize, param1, param2), typeof(object));
                var lambda = Expression.Lambda<Func<Stream, TinyhandSerializerOptions?, object>>(body, param1, param2).Compile(PreferInterpretation);

                this.Deserialize_Stream_Options_CancellationToken = lambda;
#endif
            }

            {
                // public static ValueTask<object> DeserializeObjectAsync<T>(Stream stream, TinyhandSerializerOptions? options)
                var deserialize = GetMethod(nameof(DeserializeObjectAsync), type, new Type[] { typeof(Stream), typeof(TinyhandSerializerOptions), });
#if ENABLE_IL2CPP
                this.DeserializeAsync_Stream_Options_CancellationToken = (x, y, z) => (ValueTask<object?>)deserialize.Invoke(null, new object?[] { x, y, z });
#else
                var param1 = Expression.Parameter(typeof(Stream), "stream");
                var param2 = Expression.Parameter(typeof(TinyhandSerializerOptions), "options");
                var body = Expression.Convert(Expression.Call(null, deserialize, param1, param2), typeof(ValueTask<object>));
                var lambda = Expression.Lambda<Func<Stream, TinyhandSerializerOptions?, ValueTask<object?>>>(body, param1, param2).Compile(PreferInterpretation);

                this.DeserializeAsync_Stream_Options_CancellationToken = lambda;
#endif
            }

            {
                // public static T Deserialize<T>(ReadOnlySpan<byte> bytes, TinyhandSerializerOptions? options)
                var deserialize = GetMethod(nameof(Deserialize), type, new Type[] { typeof(ReadOnlySpan<byte>), typeof(TinyhandSerializerOptions), });
#if ENABLE_IL2CPP
                this.Deserialize_ReadOnlyMemory_Options = (x, y, z) => deserialize.Invoke(null, new object?[] { x, y, z });
#else
                var param1 = Expression.Parameter(typeof(ReadOnlySpan<byte>), "bytes");
                var param2 = Expression.Parameter(typeof(TinyhandSerializerOptions), "options");
                var body = Expression.Convert(Expression.Call(null, deserialize, param1, param2), typeof(object));
                var lambda = Expression.Lambda<Func<ReadOnlySpan<byte>, TinyhandSerializerOptions?, object>>(body, param1, param2).Compile(PreferInterpretation);

                this.Deserialize_ReadOnlyMemory_Options = lambda;
#endif
            }
        }

        private static void ThrowRefStructNotSupported()
        {
            // C# 8.0 is not supported call `ref struct` via reflection. (It is milestoned at .NET 6)
            throw new NotSupportedException("TinyhandWriter/Reader overload is not supported in TinyhandSerializer.NonGenerics.");
        }

        // null is generic type marker.
        private static MethodInfo GetMethod(string methodName, Type type, Type?[] parameters)
        {
            return typeof(TinyhandSerializer).GetRuntimeMethods().Single(x =>
            {
                if (methodName != x.Name)
                {
                    return false;
                }

                ParameterInfo[] ps = x.GetParameters();
                if (ps.Length != parameters.Length)
                {
                    return false;
                }

                for (int i = 0; i < ps.Length; i++)
                {
                    if (parameters[i] == null && ps[i].ParameterType.IsGenericParameter)
                    {
                        continue;
                    }

                    if (ps[i].ParameterType != parameters[i])
                    {
                        return false;
                    }
                }

                return true;
            })
            .MakeGenericMethod(type);
        }
    }
}
