// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Generic;
using Arc.Collections;

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

/*public sealed class OrderedSetFormatter<TKey> : DictionaryFormatterBase<TKey, int, OrderedSet<TKey>, OrderedMap<TKey, int>.KeyCollection.Enumerator, OrderedSet<TKey>>
{
    protected override void Add(OrderedSet<TKey> collection, int index, TKey key, int value, TinyhandSerializerOptions options)
    {
        collection.Add(key);
    }

    protected override OrderedSet<TKey> Complete(OrderedSet<TKey> intermediateCollection)
    {
        return intermediateCollection;
    }

    protected override OrderedSet<TKey> Create(int count, TinyhandSerializerOptions options)
    {
        return new OrderedSet<TKey>();
    }

    protected override OrderedMap<TKey, int>.KeyCollection.Enumerator GetSourceEnumerator(OrderedSet<TKey> source)
    {
        return source.GetEnumerator();
    }
}*/

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
