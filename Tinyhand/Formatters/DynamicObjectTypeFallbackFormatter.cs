// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Linq.Expressions;
using System.Reflection;
using Tinyhand.IO;

#pragma warning disable SA1009 // Closing parenthesis should be spaced correctly

namespace Tinyhand.Formatters;

/// <summary>
/// This formatter can serialize any value whose static type is <see cref="object"/>
/// for which another resolver can provide a formatter for the runtime type.
/// Its deserialization is limited to forwarding all calls to the <see cref="PrimitiveObjectFormatter"/>.
/// </summary>
public sealed class DynamicObjectTypeFallbackFormatter : ITinyhandFormatter<object>
{
    public static readonly ITinyhandFormatter<object> Instance = new DynamicObjectTypeFallbackFormatter();

    private delegate void SerializeMethod(object dynamicFormatter, ref TinyhandWriter writer, object value, TinyhandSerializerOptions options);

    private static readonly Internal.ThreadsafeTypeKeyHashTable<SerializeMethod> SerializerDelegates = new();

    private DynamicObjectTypeFallbackFormatter()
    {
    }

    public void Serialize(ref TinyhandWriter writer, object? value, TinyhandSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNil();
            return;
        }

        Type type = value.GetType();
        TypeInfo ti = type.GetTypeInfo();

        if (type == typeof(object))
        {
            // serialize to empty map
            writer.WriteMapHeader(0);
            return;
        }

        if (PrimitiveObjectFormatter.IsSupportedType(type, ti, value))
        {
            if (!(value is System.Collections.IDictionary || value is System.Collections.ICollection))
            {
                PrimitiveObjectFormatter.Instance.Serialize(ref writer, value, options);
                return;
            }
        }

        object formatter = options.Resolver.GetFormatterDynamic(type);
        if (!SerializerDelegates.TryGetValue(type, out var serializerDelegate))
        {
            lock (SerializerDelegates)
            {
                if (!SerializerDelegates.TryGetValue(type, out serializerDelegate))
                {
                    Type formatterType = typeof(ITinyhandFormatter<>).MakeGenericType(type);
                    ParameterExpression param0 = Expression.Parameter(typeof(object), "formatter");
                    ParameterExpression param1 = Expression.Parameter(typeof(TinyhandWriter).MakeByRefType(), "writer");
                    ParameterExpression param2 = Expression.Parameter(typeof(object), "value");
                    ParameterExpression param3 = Expression.Parameter(typeof(TinyhandSerializerOptions), "options");

                    MethodInfo serializeMethodInfo = formatterType.GetRuntimeMethod("Serialize", new[] { typeof(TinyhandWriter).MakeByRefType(), type, typeof(TinyhandSerializerOptions) })!;

                    MethodCallExpression body = Expression.Call(
                        Expression.Convert(param0, formatterType),
                        serializeMethodInfo,
                        param1,
                        ti.IsValueType ? Expression.Unbox(param2, type) : Expression.Convert(param2, type),
                        param3);

                    serializerDelegate = Expression.Lambda<SerializeMethod>(body, param0, param1, param2, param3).Compile();

                    SerializerDelegates.TryAdd(type, serializerDelegate);
                }
            }
        }

        serializerDelegate(formatter, ref writer, value, options);
    }

    public object? Deserialize(ref TinyhandReader reader, TinyhandSerializerOptions options)
    {
        return PrimitiveObjectFormatter.Instance.Deserialize(ref reader, options);
    }

    public object Reconstruct(TinyhandSerializerOptions options)
    {
        return default!;
    }

    public object? Clone(object? value, TinyhandSerializerOptions options)
    {
        var w = default(TinyhandWriter);
        this.Serialize(ref w, value, options);
        var r = new TinyhandReader(w.FlushAndGetReadOnlySequence());
        return this.Deserialize(ref r, options);
    }
}
