// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Arc.Collections;
using Tinyhand.IO;

#pragma warning disable SA1009 // Closing parenthesis should be spaced correctly

namespace Tinyhand.Formatters;

public sealed class OrderedMapFormatter<TKey, TValue> : ITinyhandFormatter<OrderedMap<TKey, TValue>>
{
    public OrderedMapFormatter()
    {
    }

    public void Serialize(ref TinyhandWriter writer, OrderedMap<TKey, TValue>? value, TinyhandSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNil();
            return;
        }

        var keyFormatter = options.Resolver.GetFormatter<TKey>();
        var valueFormatter = options.Resolver.GetFormatter<TValue>();

        writer.WriteMapHeader(value.Count);

        var e = value.GetEnumerator();
        try
        {
            while (e.MoveNext())
            {
                var pair = e.Current;
                keyFormatter.Serialize(ref writer, pair.Key, options);
                valueFormatter.Serialize(ref writer, pair.Value, options);
            }
        }
        finally
        {
            e.Dispose();
        }
    }

    public void Deserialize(ref TinyhandReader reader, ref OrderedMap<TKey, TValue>? value, TinyhandSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
            return;
        }

        var keyFormatter = options.Resolver.GetFormatter<TKey>();
        var valueFormatter = options.Resolver.GetFormatter<TValue>();

        var count = reader.ReadMapHeader2();
        if (value is null)
        {
            value = new();
        }
        else
        {
            value.Clear();
        }

        options.Security.DepthStep(ref reader);
        try
        {
            for (var i = 0; i < count; i++)
            {
                var key = keyFormatter.Deserialize(ref reader, options);
                var v = valueFormatter.Deserialize(ref reader, options);
                value.Add(key!, v!);
            }
        }
        finally
        {
            reader.Depth--;
        }
    }

    public OrderedMap<TKey, TValue> Reconstruct(TinyhandSerializerOptions options)
    {
        return new();
    }

    [return: NotNullIfNotNull(nameof(value))]
    public OrderedMap<TKey, TValue>? Clone(OrderedMap<TKey, TValue>? value, TinyhandSerializerOptions options)
    {
        if (value is null)
        {
            return null;
        }

        return new(value, value.Comparer, value.Reverse);
    }
}

public sealed class OrderedSetFormatter<T> : ITinyhandFormatter<OrderedSet<T>>
{
    public OrderedSetFormatter()
    {
    }

    public void Serialize(ref TinyhandWriter writer, OrderedSet<T>? value, TinyhandSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNil();
            return;
        }

        writer.WriteArrayHeader(value.Count);

        var formatter = options.Resolver.GetFormatter<T>();
        var e = value.GetEnumerator();
        try
        {
            while (e.MoveNext())
            {
                formatter.Serialize(ref writer, e.Current, options);
            }
        }
        finally
        {
            e.Dispose();
        }
    }

    public void Deserialize(ref TinyhandReader reader, ref OrderedSet<T>? value, TinyhandSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
            return;
        }

        var formatter = options.Resolver.GetFormatter<T>();

        var count = reader.ReadArrayHeader();
        if (value is null)
        {
            value = new();
        }
        else
        {
            value.Clear();
        }

        options.Security.DepthStep(ref reader);
        try
        {
            for (var i = 0; i < count; i++)
            {
                var v = formatter.Deserialize(ref reader, options);
                value.Add(v!);
            }
        }
        finally
        {
            reader.Depth--;
        }
    }

    public OrderedSet<T> Reconstruct(TinyhandSerializerOptions options)
    {
        return new();
    }

    [return: NotNullIfNotNull(nameof(value))]
    public OrderedSet<T>? Clone(OrderedSet<T>? value, TinyhandSerializerOptions options)
    {
        if (value is null)
        {
            return null;
        }

        return new(value, value.Comparer, value.Reverse);
    }
}

public sealed class OrderedMultiMapFormatter<TKey, TValue> : ITinyhandFormatter<OrderedMultiMap<TKey, TValue>>
{
    public OrderedMultiMapFormatter()
    {
    }

    public void Serialize(ref TinyhandWriter writer, OrderedMultiMap<TKey, TValue>? value, TinyhandSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNil();
            return;
        }

        var keyFormatter = options.Resolver.GetFormatter<TKey>();
        var valueFormatter = options.Resolver.GetFormatter<TValue>();

        writer.WriteMapHeader(value.Count);

        var e = value.GetEnumerator();
        try
        {
            while (e.MoveNext())
            {
                var pair = e.Current;
                keyFormatter.Serialize(ref writer, pair.Key, options);
                valueFormatter.Serialize(ref writer, pair.Value, options);
            }
        }
        finally
        {
            e.Dispose();
        }
    }

    public void Deserialize(ref TinyhandReader reader, ref OrderedMultiMap<TKey, TValue>? value, TinyhandSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
            return;
        }

        var keyFormatter = options.Resolver.GetFormatter<TKey>();
        var valueFormatter = options.Resolver.GetFormatter<TValue>();

        var count = reader.ReadMapHeader2();
        if (value is null)
        {
            value = new();
        }
        else
        {
            value.Clear();
        }

        options.Security.DepthStep(ref reader);
        try
        {
            for (var i = 0; i < count; i++)
            {
                var key = keyFormatter.Deserialize(ref reader, options);
                var v = valueFormatter.Deserialize(ref reader, options);
                value.Add(key!, v!);
            }
        }
        finally
        {
            reader.Depth--;
        }
    }

    public OrderedMultiMap<TKey, TValue> Reconstruct(TinyhandSerializerOptions options)
    {
        return new();
    }

    [return: NotNullIfNotNull(nameof(value))]
    public OrderedMultiMap<TKey, TValue>? Clone(OrderedMultiMap<TKey, TValue>? value, TinyhandSerializerOptions options)
    {
        if (value is null)
        {
            return null;
        }

        var newValue = new OrderedMultiMap<TKey, TValue>(value.Comparer, value.Reverse);
        foreach (var x in value)
        {
            newValue.Add(x.Key, x.Value);
        }

        return newValue;
    }
}

public sealed class OrderedMultiSetFormatter<T> : ITinyhandFormatter<OrderedMultiSet<T>>
{
    public OrderedMultiSetFormatter()
    {
    }

    public void Serialize(ref TinyhandWriter writer, OrderedMultiSet<T>? value, TinyhandSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNil();
            return;
        }

        writer.WriteArrayHeader(value.Count);

        var formatter = options.Resolver.GetFormatter<T>();
        var e = value.GetEnumerator();
        try
        {
            while (e.MoveNext())
            {
                formatter.Serialize(ref writer, e.Current, options);
            }
        }
        finally
        {
            e.Dispose();
        }
    }

    public void Deserialize(ref TinyhandReader reader, ref OrderedMultiSet<T>? value, TinyhandSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
            return;
        }

        var formatter = options.Resolver.GetFormatter<T>();

        var count = reader.ReadArrayHeader();
        if (value is null)
        {
            value = new();
        }
        else
        {
            value.Clear();
        }

        options.Security.DepthStep(ref reader);
        try
        {
            for (var i = 0; i < count; i++)
            {
                var v = formatter.Deserialize(ref reader, options);
                value.Add(v!);
            }
        }
        finally
        {
            reader.Depth--;
        }
    }

    public OrderedMultiSet<T> Reconstruct(TinyhandSerializerOptions options)
    {
        return new();
    }

    [return: NotNullIfNotNull(nameof(value))]
    public OrderedMultiSet<T>? Clone(OrderedMultiSet<T>? value, TinyhandSerializerOptions options)
    {
        if (value is null)
        {
            return null;
        }

        return new(value, value.Comparer, value.Reverse);
    }
}

public sealed class UnorderedMapFormatter<TKey, TValue> : ITinyhandFormatter<UnorderedMap<TKey, TValue>>
{
    public UnorderedMapFormatter()
    {
    }

    public void Serialize(ref TinyhandWriter writer, UnorderedMap<TKey, TValue>? value, TinyhandSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNil();
            return;
        }

        var keyFormatter = options.Resolver.GetFormatter<TKey>();
        var valueFormatter = options.Resolver.GetFormatter<TValue>();

        writer.WriteMapHeader(value.Count);

        var e = value.GetEnumerator();
        try
        {
            while (e.MoveNext())
            {
                var pair = e.Current;
                keyFormatter.Serialize(ref writer, pair.Key, options);
                valueFormatter.Serialize(ref writer, pair.Value, options);
            }
        }
        finally
        {
            e.Dispose();
        }
    }

    public void Deserialize(ref TinyhandReader reader, ref UnorderedMap<TKey, TValue>? value, TinyhandSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
            return;
        }

        var keyFormatter = options.Resolver.GetFormatter<TKey>();
        var valueFormatter = options.Resolver.GetFormatter<TValue>();

        var count = reader.ReadMapHeader2();
        if (value is null)
        {
            value = new();
        }
        else
        {
            value.Clear();
        }

        options.Security.DepthStep(ref reader);
        try
        {
            for (var i = 0; i < count; i++)
            {
                var key = keyFormatter.Deserialize(ref reader, options);
                var v = valueFormatter.Deserialize(ref reader, options);
                value.Add(key!, v!);
            }
        }
        finally
        {
            reader.Depth--;
        }
    }

    public UnorderedMap<TKey, TValue> Reconstruct(TinyhandSerializerOptions options)
    {
        return new();
    }

    [return: NotNullIfNotNull(nameof(value))]
    public UnorderedMap<TKey, TValue>? Clone(UnorderedMap<TKey, TValue>? value, TinyhandSerializerOptions options)
    {
        if (value is null)
        {
            return null;
        }

        var newValue = new UnorderedMap<TKey, TValue>(value.Capacity, value.Comparer, value.AllowDuplicate);
        foreach (var x in value)
        {
            newValue.Add(x.Key, x.Value);
        }

        return newValue;
    }
}

public sealed class UnorderedSetFormatter<T> : ITinyhandFormatter<UnorderedSet<T>>
{
    public UnorderedSetFormatter()
    {
    }

    public void Serialize(ref TinyhandWriter writer, UnorderedSet<T>? value, TinyhandSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNil();
            return;
        }

        writer.WriteArrayHeader(value.Count);

        var formatter = options.Resolver.GetFormatter<T>();
        var e = value.GetEnumerator();
        try
        {
            while (e.MoveNext())
            {
                formatter.Serialize(ref writer, e.Current, options);
            }
        }
        finally
        {
            e.Dispose();
        }
    }

    public void Deserialize(ref TinyhandReader reader, ref UnorderedSet<T>? value, TinyhandSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
            return;
        }

        var formatter = options.Resolver.GetFormatter<T>();

        var count = reader.ReadArrayHeader();
        if (value is null)
        {
            value = new();
        }
        else
        {
            value.Clear();
        }

        options.Security.DepthStep(ref reader);
        try
        {
            for (var i = 0; i < count; i++)
            {
                var v = formatter.Deserialize(ref reader, options);
                value.Add(v!);
            }
        }
        finally
        {
            reader.Depth--;
        }
    }

    public UnorderedSet<T> Reconstruct(TinyhandSerializerOptions options)
    {
        return new();
    }

    [return: NotNullIfNotNull(nameof(value))]
    public UnorderedSet<T>? Clone(UnorderedSet<T>? value, TinyhandSerializerOptions options)
    {
        if (value is null)
        {
            return null;
        }

        return new(value, value.Comparer, value.AllowDuplicate);
    }
}

public sealed class OrderedListFormatter<T> : ITinyhandFormatter<OrderedList<T>>
{
    public void Serialize(ref TinyhandWriter writer, OrderedList<T>? value, TinyhandSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNil();
        }
        else
        {
            var formatter = options.Resolver.GetFormatter<T>();

            var c = value.Count;
            writer.WriteArrayHeader(c);
            for (var i = 0; i < c; i++)
            {
                formatter.Serialize(ref writer, value[i], options);
            }
        }
    }

    public void Deserialize(ref TinyhandReader reader, ref OrderedList<T>? value, TinyhandSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
        }
        else
        {
            var formatter = options.Resolver.GetFormatter<T>();

            var len = reader.ReadArrayHeader();
            value ??= new OrderedList<T>((int)len);
            options.Security.DepthStep(ref reader);
            try
            {
                for (var i = 0; i < len; i++)
                {
                    value.Add(formatter.Deserialize(ref reader, options)!);
                }
            }
            finally
            {
                reader.Depth--;
            }
        }
    }

    public OrderedList<T> Reconstruct(TinyhandSerializerOptions options)
    {
        return new OrderedList<T>();
    }

    public OrderedList<T>? Clone(OrderedList<T>? value, TinyhandSerializerOptions options)
    {
        if (value == null)
        {
            return default;
        }
        else
        {
            var formatter = options.Resolver.GetFormatter<T>();

            var len = value.Count;
            var list = new OrderedList<T>(len);
            for (var i = 0; i < len; i++)
            {
                list.Add(formatter.Clone(value[i], options)!);
            }

            return list;
        }
    }
}

public sealed class UnorderedListFormatter<T> : ITinyhandFormatter<UnorderedList<T>>
{
    public void Serialize(ref TinyhandWriter writer, UnorderedList<T>? value, TinyhandSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNil();
        }
        else
        {
            var formatter = options.Resolver.GetFormatter<T>();

            var c = value.Count;
            writer.WriteArrayHeader(c);
            for (var i = 0; i < c; i++)
            {
                formatter.Serialize(ref writer, value[i], options);
            }
        }
    }

    public void Deserialize(ref TinyhandReader reader, ref UnorderedList<T>? value, TinyhandSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
        }
        else
        {
            var formatter = options.Resolver.GetFormatter<T>();

            var len = reader.ReadArrayHeader();
            value ??= new UnorderedList<T>((int)len);
            options.Security.DepthStep(ref reader);
            try
            {
                for (var i = 0; i < len; i++)
                {
                    value.Add(formatter.Deserialize(ref reader, options)!);
                }
            }
            finally
            {
                reader.Depth--;
            }
        }
    }

    public UnorderedList<T> Reconstruct(TinyhandSerializerOptions options)
    {
        return new UnorderedList<T>();
    }

    public UnorderedList<T>? Clone(UnorderedList<T>? value, TinyhandSerializerOptions options)
    {
        if (value == null)
        {
            return default;
        }
        else
        {
            var formatter = options.Resolver.GetFormatter<T>();

            var len = value.Count;
            var list = new UnorderedList<T>(len);
            for (var i = 0; i < len; i++)
            {
                list.Add(formatter.Clone(value[i], options)!);
            }

            return list;
        }
    }
}

public sealed class UnorderedLinkedListFormatter<T> : ITinyhandFormatter<UnorderedLinkedList<T>>
{
    public void Serialize(ref TinyhandWriter writer, UnorderedLinkedList<T>? value, TinyhandSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNil();
        }
        else
        {
            var formatter = options.Resolver.GetFormatter<T>();

            var c = value.Count;
            writer.WriteArrayHeader(c);
            foreach (var x in value)
            {
                formatter.Serialize(ref writer, x, options);
            }
        }
    }

    public void Deserialize(ref TinyhandReader reader, ref UnorderedLinkedList<T>? value, TinyhandSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
        }
        else
        {
            var formatter = options.Resolver.GetFormatter<T>();

            var len = reader.ReadArrayHeader();
            value ??= new UnorderedLinkedList<T>();
            options.Security.DepthStep(ref reader);
            try
            {
                for (var i = 0; i < len; i++)
                {
                    value.AddLast(formatter.Deserialize(ref reader, options)!);
                }
            }
            finally
            {
                reader.Depth--;
            }
        }
    }

    public UnorderedLinkedList<T> Reconstruct(TinyhandSerializerOptions options)
    {
        return new UnorderedLinkedList<T>();
    }

    public UnorderedLinkedList<T>? Clone(UnorderedLinkedList<T>? value, TinyhandSerializerOptions options)
    {
        if (value == null)
        {
            return default;
        }
        else
        {
            var formatter = options.Resolver.GetFormatter<T>();

            var len = value.Count;
            var list = new UnorderedLinkedList<T>();
            foreach (var x in value)
            {
                list.AddLast(formatter.Clone(x, options)!);
            }

            return list;
        }
    }
}

public sealed class OrderedKeyValueListFormatter<TKey, TValue> : DictionaryFormatterBase<TKey, TValue, OrderedKeyValueList<TKey, TValue>, OrderedKeyValueList<TKey, TValue>.Enumerator, OrderedKeyValueList<TKey, TValue>>
    where TKey : notnull
{
    protected override void Add(OrderedKeyValueList<TKey, TValue> collection, int index, TKey key, TValue value, TinyhandSerializerOptions options)
    {
        collection.Add(key, value);
    }

    protected override OrderedKeyValueList<TKey, TValue> Complete(OrderedKeyValueList<TKey, TValue> intermediateCollection)
    {
        return intermediateCollection;
    }

    protected override OrderedKeyValueList<TKey, TValue> Create(OrderedKeyValueList<TKey, TValue>? reuse, int count, TinyhandSerializerOptions options)
    {
        return reuse ?? new OrderedKeyValueList<TKey, TValue>();
    }

    protected override OrderedKeyValueList<TKey, TValue>.Enumerator GetSourceEnumerator(OrderedKeyValueList<TKey, TValue> source)
    {
        return source.GetEnumerator();
    }
}
