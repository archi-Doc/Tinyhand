﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Tinyhand.IO;

#pragma warning disable SA1009 // Closing parenthesis should be spaced correctly

namespace Tinyhand.Formatters;

// Immutablearray<T>.Enumerator is 'not' IEnumerator<T>, can't use abstraction layer.
public class ImmutableArrayFormatter<T> : ITinyhandFormatter<ImmutableArray<T>>
{
    public void Serialize(ref TinyhandWriter writer, ImmutableArray<T> value, TinyhandSerializerOptions options)
    {
        if (value.IsDefault)
        {
            writer.WriteNil();
        }
        else if (value.IsEmpty)
        {
            writer.WriteArrayHeader(0);
        }
        else
        {
            ITinyhandFormatter<T> formatter = options.Resolver.GetFormatter<T>();

            writer.WriteArrayHeader(value.Length);
            foreach (T item in value)
            {
                formatter.Serialize(ref writer, item, options);
            }
        }
    }

    public void Deserialize(ref TinyhandReader reader, ref ImmutableArray<T> value, TinyhandSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
        }
        else
        {
            var len = reader.ReadArrayHeader();
            if (len == 0)
            {
                return;
            }

            ITinyhandFormatter<T> formatter = options.Resolver.GetFormatter<T>();
            ImmutableArray<T>.Builder builder = ImmutableArray.CreateBuilder<T>(len);
            options.Security.DepthStep(ref reader);
            try
            {
                for (int i = 0; i < len; i++)
                {
                    builder.Add(formatter.Deserialize(ref reader, options)!);
                }
            }
            finally
            {
                reader.Depth--;
            }

            value = builder.MoveToImmutable();
        }
    }

    public ImmutableArray<T> Reconstruct(TinyhandSerializerOptions options)
    {
        return default;
    }

    public ImmutableArray<T> Clone(ImmutableArray<T> value, TinyhandSerializerOptions options)
    {
        var len = value.Length;
        if (len == 0)
        {
            return ImmutableArray<T>.Empty;
        }
        else
        {
            var formatter = options.Resolver.GetFormatter<T>();
            var builder = ImmutableArray.CreateBuilder<T>(len);
            for (int i = 0; i < len; i++)
            {
                builder.Add(formatter.Clone(value[i], options)!);
            }

            return builder.MoveToImmutable();
        }
    }
}

public class ImmutableListFormatter<T> : CollectionFormatterBase<T, ImmutableList<T>.Builder, ImmutableList<T>.Enumerator, ImmutableList<T>>
{
    protected override void Add(ImmutableList<T>.Builder collection, int index, T value, TinyhandSerializerOptions options)
    {
        collection.Add(value);
    }

    protected override ImmutableList<T> Complete(ImmutableList<T>.Builder intermediateCollection)
    {
        return intermediateCollection.ToImmutable();
    }

    protected override ImmutableList<T>.Builder Create(int count, TinyhandSerializerOptions options)
    {
        return ImmutableList.CreateBuilder<T>();
    }

    protected override ImmutableList<T>.Enumerator GetSourceEnumerator(ImmutableList<T> source)
    {
        return source.GetEnumerator();
    }
}

public class ImmutableDictionaryFormatter<TKey, TValue> : DictionaryFormatterBase<TKey, TValue, ImmutableDictionary<TKey, TValue>.Builder, ImmutableDictionary<TKey, TValue>.Enumerator, ImmutableDictionary<TKey, TValue>>
    where TKey : notnull
{
    protected override void Add(ImmutableDictionary<TKey, TValue>.Builder collection, int index, TKey key, TValue value, TinyhandSerializerOptions options)
    {
        collection.TryAdd(key, value);
    }

    protected override ImmutableDictionary<TKey, TValue> Complete(ImmutableDictionary<TKey, TValue>.Builder intermediateCollection)
    {
        return intermediateCollection.ToImmutable();
    }

    protected override ImmutableDictionary<TKey, TValue>.Builder Create(ImmutableDictionary<TKey, TValue>? reuse, int count, TinyhandSerializerOptions options)
    {
        var builder = ImmutableDictionary.CreateBuilder<TKey, TValue>(options.Security.GetEqualityComparer<TKey>());
        if (reuse is not null)
        {
            builder.AddRange(reuse);
        }

        return builder;
    }

    protected override ImmutableDictionary<TKey, TValue>.Enumerator GetSourceEnumerator(ImmutableDictionary<TKey, TValue> source)
    {
        return source.GetEnumerator();
    }
}

public class ImmutableHashSetFormatter<T> : CollectionFormatterBase<T, ImmutableHashSet<T>.Builder, ImmutableHashSet<T>.Enumerator, ImmutableHashSet<T>>
{
    protected override void Add(ImmutableHashSet<T>.Builder collection, int index, T value, TinyhandSerializerOptions options)
    {
        collection.Add(value);
    }

    protected override ImmutableHashSet<T> Complete(ImmutableHashSet<T>.Builder intermediateCollection)
    {
        return intermediateCollection.ToImmutable();
    }

    protected override ImmutableHashSet<T>.Builder Create(int count, TinyhandSerializerOptions options)
    {
        return ImmutableHashSet.CreateBuilder<T>(options.Security.GetEqualityComparer<T>());
    }

    protected override ImmutableHashSet<T>.Enumerator GetSourceEnumerator(ImmutableHashSet<T> source)
    {
        return source.GetEnumerator();
    }
}

public class ImmutableSortedDictionaryFormatter<TKey, TValue> : DictionaryFormatterBase<TKey, TValue, ImmutableSortedDictionary<TKey, TValue>.Builder, ImmutableSortedDictionary<TKey, TValue>.Enumerator, ImmutableSortedDictionary<TKey, TValue>>
    where TKey : notnull
{
    protected override void Add(ImmutableSortedDictionary<TKey, TValue>.Builder collection, int index, TKey key, TValue value, TinyhandSerializerOptions options)
    {
        collection.TryAdd(key, value);
    }

    protected override ImmutableSortedDictionary<TKey, TValue> Complete(ImmutableSortedDictionary<TKey, TValue>.Builder intermediateCollection)
    {
        return intermediateCollection.ToImmutable();
    }

    protected override ImmutableSortedDictionary<TKey, TValue>.Builder Create(ImmutableSortedDictionary<TKey, TValue>? reuse, int count, TinyhandSerializerOptions options)
    {
        var builder = ImmutableSortedDictionary.CreateBuilder<TKey, TValue>();
        if (reuse is not null)
        {
            builder.AddRange(reuse);
        }

        return builder;
    }

    protected override ImmutableSortedDictionary<TKey, TValue>.Enumerator GetSourceEnumerator(ImmutableSortedDictionary<TKey, TValue> source)
    {
        return source.GetEnumerator();
    }
}

public class ImmutableSortedSetFormatter<T> : CollectionFormatterBase<T, ImmutableSortedSet<T>.Builder, ImmutableSortedSet<T>.Enumerator, ImmutableSortedSet<T>>
{
    protected override void Add(ImmutableSortedSet<T>.Builder collection, int index, T value, TinyhandSerializerOptions options)
    {
        collection.Add(value);
    }

    protected override ImmutableSortedSet<T> Complete(ImmutableSortedSet<T>.Builder intermediateCollection)
    {
        return intermediateCollection.ToImmutable();
    }

    protected override ImmutableSortedSet<T>.Builder Create(int count, TinyhandSerializerOptions options)
    {
        return ImmutableSortedSet.CreateBuilder<T>();
    }

    protected override ImmutableSortedSet<T>.Enumerator GetSourceEnumerator(ImmutableSortedSet<T> source)
    {
        return source.GetEnumerator();
    }
}

// not best for performance(does not use ImmutableQueue<T>.Enumerator)
public class ImmutableQueueFormatter<T> : CollectionFormatterBase<T, ImmutableQueueBuilder<T>, ImmutableQueue<T>>
{
    protected override void Add(ImmutableQueueBuilder<T> collection, int index, T value, TinyhandSerializerOptions options)
    {
        collection.Add(value);
    }

    protected override ImmutableQueue<T> Complete(ImmutableQueueBuilder<T> intermediateCollection)
    {
        return intermediateCollection.Q;
    }

    protected override ImmutableQueueBuilder<T> Create(int count, TinyhandSerializerOptions options)
    {
        return new ImmutableQueueBuilder<T>();
    }
}

// not best for performance(does not use ImmutableQueue<T>.Enumerator)
public class ImmutableStackFormatter<T> : CollectionFormatterBase<T, T[], ImmutableStack<T>>
{
    protected override void Add(T[] collection, int index, T value, TinyhandSerializerOptions options)
    {
        collection[collection.Length - 1 - index] = value;
    }

    protected override ImmutableStack<T> Complete(T[] intermediateCollection)
    {
        return ImmutableStack.CreateRange(intermediateCollection);
    }

    protected override T[] Create(int count, TinyhandSerializerOptions options)
    {
        return count == 0 ? Array.Empty<T>() : new T[count];
    }
}

public class InterfaceImmutableListFormatter<T> : CollectionFormatterBase<T, ImmutableList<T>.Builder, IImmutableList<T>>
{
    protected override void Add(ImmutableList<T>.Builder collection, int index, T value, TinyhandSerializerOptions options)
    {
        collection.Add(value);
    }

    protected override IImmutableList<T> Complete(ImmutableList<T>.Builder intermediateCollection)
    {
        return intermediateCollection.ToImmutable();
    }

    protected override ImmutableList<T>.Builder Create(int count, TinyhandSerializerOptions options)
    {
        return ImmutableList.CreateBuilder<T>();
    }
}

public class InterfaceImmutableDictionaryFormatter<TKey, TValue> : DictionaryFormatterBase<TKey, TValue, ImmutableDictionary<TKey, TValue>.Builder, IImmutableDictionary<TKey, TValue>>
    where TKey : notnull
{
    protected override void Add(ImmutableDictionary<TKey, TValue>.Builder collection, int index, TKey key, TValue value, TinyhandSerializerOptions options)
    {
        collection.TryAdd(key, value);
    }

    protected override IImmutableDictionary<TKey, TValue> Complete(ImmutableDictionary<TKey, TValue>.Builder intermediateCollection)
    {
        return intermediateCollection.ToImmutable();
    }

    protected override ImmutableDictionary<TKey, TValue>.Builder Create(IImmutableDictionary<TKey, TValue>? reuse, int count, TinyhandSerializerOptions options)
    {
        var builder = ImmutableDictionary.CreateBuilder<TKey, TValue>(options.Security.GetEqualityComparer<TKey>());
        if (reuse is not null)
        {
            builder.AddRange(reuse);
        }

        return builder;
    }
}

public class InterfaceImmutableSetFormatter<T> : CollectionFormatterBase<T, ImmutableHashSet<T>.Builder, IImmutableSet<T>>
{
    protected override void Add(ImmutableHashSet<T>.Builder collection, int index, T value, TinyhandSerializerOptions options)
    {
        collection.Add(value);
    }

    protected override IImmutableSet<T> Complete(ImmutableHashSet<T>.Builder intermediateCollection)
    {
        return intermediateCollection.ToImmutable();
    }

    protected override ImmutableHashSet<T>.Builder Create(int count, TinyhandSerializerOptions options)
    {
        return ImmutableHashSet.CreateBuilder<T>(options.Security.GetEqualityComparer<T>());
    }
}

public class InterfaceImmutableQueueFormatter<T> : CollectionFormatterBase<T, ImmutableQueueBuilder<T>, IImmutableQueue<T>>
{
    protected override void Add(ImmutableQueueBuilder<T> collection, int index, T value, TinyhandSerializerOptions options)
    {
        collection.Add(value);
    }

    protected override IImmutableQueue<T> Complete(ImmutableQueueBuilder<T> intermediateCollection)
    {
        return intermediateCollection.Q;
    }

    protected override ImmutableQueueBuilder<T> Create(int count, TinyhandSerializerOptions options)
    {
        return new ImmutableQueueBuilder<T>();
    }
}

public class InterfaceImmutableStackFormatter<T> : CollectionFormatterBase<T, T[], IImmutableStack<T>>
{
    protected override void Add(T[] collection, int index, T value, TinyhandSerializerOptions options)
    {
        collection[collection.Length - 1 - index] = value;
    }

    protected override IImmutableStack<T> Complete(T[] intermediateCollection)
    {
        return ImmutableStack.CreateRange(intermediateCollection);
    }

    protected override T[] Create(int count, TinyhandSerializerOptions options)
    {
        return count == 0 ? Array.Empty<T>() : new T[count];
    }
}

// pseudo builders
public class ImmutableQueueBuilder<T>
{
    public ImmutableQueue<T> Q { get; set; } = ImmutableQueue<T>.Empty;

    public void Add(T value)
    {
        this.Q = this.Q.Enqueue(value);
    }
}
