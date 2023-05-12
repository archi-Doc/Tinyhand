// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Collections;
using Tinyhand.IO;

#pragma warning disable SA1009 // Closing parenthesis should be spaced correctly

namespace Tinyhand.Formatters;

public sealed class OrderedMapFormatter<TKey, TValue> : DictionaryFormatterBase<TKey, TValue, OrderedMap<TKey, TValue>, OrderedMap<TKey, TValue>.Enumerator, OrderedMap<TKey, TValue>>
{
    protected override void Add(OrderedMap<TKey, TValue> collection, int index, TKey key, TValue value, TinyhandSerializerOptions options)
    {
        collection.Add(key, value);
    }

    protected override OrderedMap<TKey, TValue> Complete(OrderedMap<TKey, TValue> intermediateCollection)
    {
        return intermediateCollection;
    }

    protected override OrderedMap<TKey, TValue> Create(int count, TinyhandSerializerOptions options)
    {
        return new OrderedMap<TKey, TValue>();
    }

    protected override OrderedMap<TKey, TValue>.Enumerator GetSourceEnumerator(OrderedMap<TKey, TValue> source)
    {
        return source.GetEnumerator();
    }
}

public sealed class OrderedSetFormatter<T> : CollectionFormatterBase<T, OrderedSet<T>, OrderedMap<T, int>.KeyCollection.Enumerator, OrderedSet<T>>
{
    protected override int? GetCount(OrderedSet<T> sequence)
    {
        return sequence.Count;
    }

    protected override void Add(OrderedSet<T> collection, int index, T value, TinyhandSerializerOptions options)
    {
        collection.Add(value);
    }

    protected override OrderedSet<T> Complete(OrderedSet<T> intermediateCollection)
    {
        return intermediateCollection;
    }

    protected override OrderedSet<T> Create(int count, TinyhandSerializerOptions options)
    {
        return new OrderedSet<T>();
    }

    protected override OrderedMap<T, int>.KeyCollection.Enumerator GetSourceEnumerator(OrderedSet<T> source)
    {
        return source.GetEnumerator();
    }
}

public sealed class OrderedMultiMapFormatter<TKey, TValue> : DictionaryFormatterBase<TKey, TValue, OrderedMultiMap<TKey, TValue>, OrderedMultiMap<TKey, TValue>.Enumerator, OrderedMultiMap<TKey, TValue>>
    where TKey : notnull
{
    protected override void Add(OrderedMultiMap<TKey, TValue> collection, int index, TKey key, TValue value, TinyhandSerializerOptions options)
    {
        collection.Add(key, value);
    }

    protected override OrderedMultiMap<TKey, TValue> Complete(OrderedMultiMap<TKey, TValue> intermediateCollection)
    {
        return intermediateCollection;
    }

    protected override OrderedMultiMap<TKey, TValue> Create(int count, TinyhandSerializerOptions options)
    {
        return new OrderedMultiMap<TKey, TValue>();
    }

    protected override OrderedMultiMap<TKey, TValue>.Enumerator GetSourceEnumerator(OrderedMultiMap<TKey, TValue> source)
    {
        return source.GetEnumerator();
    }
}

public sealed class OrderedMultiSetFormatter<T> : CollectionFormatterBase<T, OrderedMultiSet<T>, OrderedMultiMap<T, int>.KeyCollection.Enumerator, OrderedMultiSet<T>>
{
    protected override int? GetCount(OrderedMultiSet<T> sequence)
    {
        return sequence.Count;
    }

    protected override void Add(OrderedMultiSet<T> collection, int index, T value, TinyhandSerializerOptions options)
    {
        collection.Add(value);
    }

    protected override OrderedMultiSet<T> Complete(OrderedMultiSet<T> intermediateCollection)
    {
        return intermediateCollection;
    }

    protected override OrderedMultiSet<T> Create(int count, TinyhandSerializerOptions options)
    {
        return new OrderedMultiSet<T>();
    }

    protected override OrderedMultiMap<T, int>.KeyCollection.Enumerator GetSourceEnumerator(OrderedMultiSet<T> source)
    {
        return source.GetEnumerator();
    }
}

public sealed class UnorderedMapFormatter<TKey, TValue> : DictionaryFormatterBase<TKey, TValue, UnorderedMap<TKey, TValue>, UnorderedMap<TKey, TValue>.Enumerator, UnorderedMap<TKey, TValue>>
{
    protected override void Add(UnorderedMap<TKey, TValue> collection, int index, TKey key, TValue value, TinyhandSerializerOptions options)
    {
        collection.Add(key, value);
    }

    protected override UnorderedMap<TKey, TValue> Complete(UnorderedMap<TKey, TValue> intermediateCollection)
    {
        return intermediateCollection;
    }

    protected override UnorderedMap<TKey, TValue> Create(int count, TinyhandSerializerOptions options)
    {
        return new UnorderedMap<TKey, TValue>();
    }

    protected override UnorderedMap<TKey, TValue>.Enumerator GetSourceEnumerator(UnorderedMap<TKey, TValue> source)
    {
        return source.GetEnumerator();
    }
}

public sealed class UnorderedSetFormatter<T> : CollectionFormatterBase<T, UnorderedSet<T>, UnorderedMap<T, int>.KeyCollection.Enumerator, UnorderedSet<T>>
{
    protected override int? GetCount(UnorderedSet<T> sequence)
    {
        return sequence.Count;
    }

    protected override void Add(UnorderedSet<T> collection, int index, T value, TinyhandSerializerOptions options)
    {
        collection.Add(value);
    }

    protected override UnorderedSet<T> Complete(UnorderedSet<T> intermediateCollection)
    {
        return intermediateCollection;
    }

    protected override UnorderedSet<T> Create(int count, TinyhandSerializerOptions options)
    {
        return new UnorderedSet<T>();
    }

    protected override UnorderedMap<T, int>.KeyCollection.Enumerator GetSourceEnumerator(UnorderedSet<T> source)
    {
        return source.GetEnumerator();
    }
}

public sealed class UnorderedMultiMapFormatter<TKey, TValue> : DictionaryFormatterBase<TKey, TValue, UnorderedMultiMap<TKey, TValue>, UnorderedMultiMap<TKey, TValue>.Enumerator, UnorderedMultiMap<TKey, TValue>>
    where TKey : notnull
{
    protected override void Add(UnorderedMultiMap<TKey, TValue> collection, int index, TKey key, TValue value, TinyhandSerializerOptions options)
    {
        collection.Add(key, value);
    }

    protected override UnorderedMultiMap<TKey, TValue> Complete(UnorderedMultiMap<TKey, TValue> intermediateCollection)
    {
        return intermediateCollection;
    }

    protected override UnorderedMultiMap<TKey, TValue> Create(int count, TinyhandSerializerOptions options)
    {
        return new UnorderedMultiMap<TKey, TValue>();
    }

    protected override UnorderedMultiMap<TKey, TValue>.Enumerator GetSourceEnumerator(UnorderedMultiMap<TKey, TValue> source)
    {
        return source.GetEnumerator();
    }
}

public sealed class UnorderedMultiSetFormatter<T> : CollectionFormatterBase<T, UnorderedMultiSet<T>, UnorderedMultiMap<T, int>.KeyCollection.Enumerator, UnorderedMultiSet<T>>
{
    protected override int? GetCount(UnorderedMultiSet<T> sequence)
    {
        return sequence.Count;
    }

    protected override void Add(UnorderedMultiSet<T> collection, int index, T value, TinyhandSerializerOptions options)
    {
        collection.Add(value);
    }

    protected override UnorderedMultiSet<T> Complete(UnorderedMultiSet<T> intermediateCollection)
    {
        return intermediateCollection;
    }

    protected override UnorderedMultiSet<T> Create(int count, TinyhandSerializerOptions options)
    {
        return new UnorderedMultiSet<T>();
    }

    protected override UnorderedMultiMap<T, int>.KeyCollection.Enumerator GetSourceEnumerator(UnorderedMultiSet<T> source)
    {
        return source.GetEnumerator();
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
                writer.CancellationToken.ThrowIfCancellationRequested();
                formatter.Serialize(ref writer, value[i], options);
            }
        }
    }

    public OrderedList<T>? Deserialize(ref TinyhandReader reader, TinyhandSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
            return default;
        }
        else
        {
            var formatter = options.Resolver.GetFormatter<T>();

            var len = reader.ReadArrayHeader();
            var list = new OrderedList<T>((int)len);
            options.Security.DepthStep(ref reader);
            try
            {
                for (var i = 0; i < len; i++)
                {
                    reader.CancellationToken.ThrowIfCancellationRequested();
                    list.Add(formatter.Deserialize(ref reader, options)!);
                }
            }
            finally
            {
                reader.Depth--;
            }

            return list;
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
                writer.CancellationToken.ThrowIfCancellationRequested();
                formatter.Serialize(ref writer, value[i], options);
            }
        }
    }

    public UnorderedList<T>? Deserialize(ref TinyhandReader reader, TinyhandSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
            return default;
        }
        else
        {
            var formatter = options.Resolver.GetFormatter<T>();

            var len = reader.ReadArrayHeader();
            var list = new UnorderedList<T>((int)len);
            options.Security.DepthStep(ref reader);
            try
            {
                for (var i = 0; i < len; i++)
                {
                    reader.CancellationToken.ThrowIfCancellationRequested();
                    list.Add(formatter.Deserialize(ref reader, options)!);
                }
            }
            finally
            {
                reader.Depth--;
            }

            return list;
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
                writer.CancellationToken.ThrowIfCancellationRequested();
                formatter.Serialize(ref writer, x, options);
            }
        }
    }

    public UnorderedLinkedList<T>? Deserialize(ref TinyhandReader reader, TinyhandSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
            return default;
        }
        else
        {
            var formatter = options.Resolver.GetFormatter<T>();

            var len = reader.ReadArrayHeader();
            var list = new UnorderedLinkedList<T>();
            options.Security.DepthStep(ref reader);
            try
            {
                for (var i = 0; i < len; i++)
                {
                    reader.CancellationToken.ThrowIfCancellationRequested();
                    list.AddLast(formatter.Deserialize(ref reader, options)!);
                }
            }
            finally
            {
                reader.Depth--;
            }

            return list;
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

    protected override OrderedKeyValueList<TKey, TValue> Create(int count, TinyhandSerializerOptions options)
    {
        return new OrderedKeyValueList<TKey, TValue>();
    }

    protected override OrderedKeyValueList<TKey, TValue>.Enumerator GetSourceEnumerator(OrderedKeyValueList<TKey, TValue> source)
    {
        return source.GetEnumerator();
    }
}
