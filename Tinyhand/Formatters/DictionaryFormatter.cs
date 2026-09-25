// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Tinyhand.IO;

#pragma warning disable SA1009 // Closing parenthesis should be spaced correctly

namespace Tinyhand.Formatters;

// unfortunately, can't use IDictionary<KVP> because supports IReadOnlyDictionary.
internal abstract class DictionaryFormatterBase<TKey, TValue, TIntermediate, TEnumerator, TDictionary> : ITinyhandFormatter<TDictionary>
    where TDictionary : IEnumerable<KeyValuePair<TKey, TValue>>
    where TEnumerator : IEnumerator<KeyValuePair<TKey, TValue>>
{
    public void Serialize(ref TinyhandWriter writer, TDictionary? value, TinyhandSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNil();
        }
        else
        {
            IFormatterResolver resolver = options.Resolver;
            ITinyhandFormatter<TKey> keyFormatter = resolver.GetFormatter<TKey>();
            ITinyhandFormatter<TValue> valueFormatter = resolver.GetFormatter<TValue>();

            if (this.GetSnapshot(value) is { } snapshot)
            {
                writer.WriteMapHeader(snapshot.Length);
                foreach (var item in snapshot)
                {
                    keyFormatter.Serialize(ref writer, item.Key, options);
                    valueFormatter.Serialize(ref writer, item.Value, options);
                }

                return;
            }

            int count;
            {
                var col = value as ICollection<KeyValuePair<TKey, TValue>>;
                if (col != null)
                {
                    count = col.Count;
                }
                else
                {
                    var col2 = value as IReadOnlyCollection<KeyValuePair<TKey, TValue>>;
                    if (col2 != null)
                    {
                        count = col2.Count;
                    }
                    else
                    {
                        throw new TinyhandException("DictionaryFormatterBase's TDictionary supports only ICollection<KVP> or IReadOnlyCollection<KVP>");
                    }
                }
            }

            writer.WriteMapHeader(count);

            TEnumerator e = this.GetSourceEnumerator(value);
            try
            {
                while (e.MoveNext())
                {
                    KeyValuePair<TKey, TValue> item = e.Current;
                    keyFormatter.Serialize(ref writer, item.Key, options);
                    valueFormatter.Serialize(ref writer, item.Value, options);
                }
            }
            finally
            {
                e.Dispose();
            }
        }
    }

    public void Deserialize(ref TinyhandReader reader, ref TDictionary? value, TinyhandSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
        }
        else
        {
            var resolver = options.Resolver;
            var keyFormatter = resolver.GetFormatter<TKey>();
            var valueFormatter = resolver.GetFormatter<TValue>();

            var len = reader.ReadMapHeaderOrEmptyArray();

            TIntermediate dict = this.Create(value, len, options);
            options.Security.IncrementDepth(ref reader);
            try
            {
                for (int i = 0; i < len; i++)
                {
                    var key = keyFormatter.Deserialize(ref reader, options);
                    var v = valueFormatter.Deserialize(ref reader, options);
                    this.Add(dict, i, key!, v!, options);
                }
            }
            finally
            {
                reader.Depth--;
            }

            value = this.Complete(dict);
        }
    }

    public TDictionary Reconstruct(TinyhandSerializerOptions options)
    {
        return this.Complete(this.Create(default, 0, options));
    }

    public TDictionary? Clone(TDictionary? value, TinyhandSerializerOptions options)
    {
        if (value == null)
        {
            return default(TDictionary);
        }

        var resolver = options.Resolver;
        var keyFormatter = resolver.GetFormatter<TKey>();
        var valueFormatter = resolver.GetFormatter<TValue>();

        if (this.GetSnapshot(value) is { } snapshot)
        {
            var clone = this.CreateForClone(value, snapshot.Length, options);
            for (var i = 0; i < snapshot.Length; i++)
            {
                this.Add(clone, i, keyFormatter.Clone(snapshot[i].Key, options)!, valueFormatter.Clone(snapshot[i].Value, options)!, options);
            }

            return this.Complete(clone);
        }

        int count;
        {
            var col = value as ICollection<KeyValuePair<TKey, TValue>>;
            if (col != null)
            {
                count = col.Count;
            }
            else
            {
                var col2 = value as IReadOnlyCollection<KeyValuePair<TKey, TValue>>;
                if (col2 != null)
                {
                    count = col2.Count;
                }
                else
                {
                    throw new TinyhandException("DictionaryFormatterBase's TDictionary supports only ICollection<KVP> or IReadOnlyCollection<KVP>");
                }
            }
        }

        var dict = this.CreateForClone(value, count, options);
        var e = this.GetSourceEnumerator(value);
        try
        {
            var i = 0;
            while (e.MoveNext())
            {
                var item = e.Current;
                this.Add(dict, i++, keyFormatter.Clone(item.Key, options)!, valueFormatter.Clone(item.Value, options)!, options);
            }
        }
        finally
        {
            e.Dispose();
        }

        return this.Complete(dict);
    }

    // abstraction for serialize

    // A concurrent dictionary can change between reading its count and enumerating it, so it provides an atomic snapshot instead.
    protected virtual KeyValuePair<TKey, TValue>[]? GetSnapshot(TDictionary source) => null;

    // A clone keeps the comparer of the source dictionary.
    protected virtual TIntermediate CreateForClone(TDictionary source, int count, TinyhandSerializerOptions options) => this.Create(default, count, options);

    // Some collections can use struct iterator, this is optimization path
    protected abstract TEnumerator GetSourceEnumerator(TDictionary source);

    // abstraction for deserialize
    // A reused instance must be emptied: the result has the deserialized entries only (an initializer's entries must not survive).
    protected abstract TIntermediate Create(TDictionary? reuse, int count, TinyhandSerializerOptions options);

    protected abstract void Add(TIntermediate collection, int index, TKey key, TValue value, TinyhandSerializerOptions options);

    protected abstract TDictionary Complete(TIntermediate intermediateCollection);
}

internal abstract class DictionaryFormatterBase<TKey, TValue, TIntermediate, TDictionary> : DictionaryFormatterBase<TKey, TValue, TIntermediate, IEnumerator<KeyValuePair<TKey, TValue>>, TDictionary>
    where TDictionary : IEnumerable<KeyValuePair<TKey, TValue>>
{
    protected override IEnumerator<KeyValuePair<TKey, TValue>> GetSourceEnumerator(TDictionary source)
    {
        return source.GetEnumerator();
    }
}

internal abstract class DictionaryFormatterBase<TKey, TValue, TDictionary> : DictionaryFormatterBase<TKey, TValue, TDictionary, TDictionary>
    where TDictionary : IDictionary<TKey, TValue>
{
    protected override TDictionary Complete(TDictionary intermediateCollection)
    {
        return intermediateCollection;
    }
}

internal sealed class DictionaryFormatter<TKey, TValue> : DictionaryFormatterBase<TKey, TValue, Dictionary<TKey, TValue>, Dictionary<TKey, TValue>.Enumerator, Dictionary<TKey, TValue>>
    where TKey : notnull
{
    protected override void Add(Dictionary<TKey, TValue> collection, int index, TKey key, TValue value, TinyhandSerializerOptions options)
    {
        collection.TryAdd(key, value);
    }

    protected override Dictionary<TKey, TValue> Complete(Dictionary<TKey, TValue> intermediateCollection)
    {
        return intermediateCollection;
    }

    protected override Dictionary<TKey, TValue> Create(Dictionary<TKey, TValue>? reuse, int count, TinyhandSerializerOptions options)
    {
        var comparer = options.Security.GetEqualityComparer<TKey>(); // Rejects unsupported keys of untrusted data, even when reusing.
        if (reuse is not null)
        {
            reuse.Clear();
            return reuse;
        }

        return new Dictionary<TKey, TValue>(count, comparer);
    }

    protected override Dictionary<TKey, TValue> CreateForClone(Dictionary<TKey, TValue> source, int count, TinyhandSerializerOptions options)
    {
        options.Security.GetEqualityComparer<TKey>(); // Rejects unsupported keys of untrusted data, as Create() does.
        return new Dictionary<TKey, TValue>(count, source.Comparer);
    }

    protected override Dictionary<TKey, TValue>.Enumerator GetSourceEnumerator(Dictionary<TKey, TValue> source)
    {
        return source.GetEnumerator();
    }
}

internal sealed class GenericDictionaryFormatter<TKey, TValue, TDictionary> : DictionaryFormatterBase<TKey, TValue, TDictionary>
    where TDictionary : IDictionary<TKey, TValue>, new()
{
    private readonly Func<int, IEqualityComparer<TKey>, TDictionary> factory;

    public GenericDictionaryFormatter(Func<int, IEqualityComparer<TKey>, TDictionary> factory)
    {
        this.factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    protected override void Add(TDictionary collection, int index, TKey key, TValue value, TinyhandSerializerOptions options)
    {
        collection.TryAdd(key, value);
    }

    protected override TDictionary Create(TDictionary? reuse, int count, TinyhandSerializerOptions options)
    {
        var comparer = options.Security.GetEqualityComparer<TKey>(); // Rejects unsupported keys of untrusted data, even when reusing.
        if (reuse is not null)
        {
            reuse.Clear();
            return reuse;
        }

        return this.factory(count, comparer);
    }
}

internal sealed class InterfaceDictionaryFormatter<TKey, TValue> : DictionaryFormatterBase<TKey, TValue, Dictionary<TKey, TValue>, IDictionary<TKey, TValue>>
    where TKey : notnull
{
    protected override void Add(Dictionary<TKey, TValue> collection, int index, TKey key, TValue value, TinyhandSerializerOptions options)
    {
        collection.TryAdd(key, value);
    }

    protected override Dictionary<TKey, TValue> Create(IDictionary<TKey, TValue>? reuse, int count, TinyhandSerializerOptions options)
    {
        var comparer = options.Security.GetEqualityComparer<TKey>(); // Rejects unsupported keys of untrusted data, even when reusing.
        if (reuse is Dictionary<TKey, TValue> dictionary)
        {
            dictionary.Clear();
            return dictionary;
        }

        return new Dictionary<TKey, TValue>(count, comparer);
    }

    protected override IDictionary<TKey, TValue> Complete(Dictionary<TKey, TValue> intermediateCollection)
    {
        return intermediateCollection;
    }
}

internal sealed class SortedListFormatter<TKey, TValue> : DictionaryFormatterBase<TKey, TValue, SortedList<TKey, TValue>>
    where TKey : notnull
{
    protected override void Add(SortedList<TKey, TValue> collection, int index, TKey key, TValue value, TinyhandSerializerOptions options)
    {
        collection.TryAdd(key, value);
    }

    protected override SortedList<TKey, TValue> Create(SortedList<TKey, TValue>? reuse, int count, TinyhandSerializerOptions options)
    {
        if (reuse is not null)
        {
            reuse.Clear();
            return reuse;
        }

        return new SortedList<TKey, TValue>(count);
    }

    protected override SortedList<TKey, TValue> CreateForClone(SortedList<TKey, TValue> source, int count, TinyhandSerializerOptions options)
        => new SortedList<TKey, TValue>(count, source.Comparer);
}

internal sealed class SortedDictionaryFormatter<TKey, TValue> : DictionaryFormatterBase<TKey, TValue, SortedDictionary<TKey, TValue>, SortedDictionary<TKey, TValue>.Enumerator, SortedDictionary<TKey, TValue>>
    where TKey : notnull
{
    protected override void Add(SortedDictionary<TKey, TValue> collection, int index, TKey key, TValue value, TinyhandSerializerOptions options)
    {
        collection.TryAdd(key, value);
    }

    protected override SortedDictionary<TKey, TValue> Complete(SortedDictionary<TKey, TValue> intermediateCollection)
    {
        return intermediateCollection;
    }

    protected override SortedDictionary<TKey, TValue> Create(SortedDictionary<TKey, TValue>? reuse, int count, TinyhandSerializerOptions options)
    {
        if (reuse is not null)
        {
            reuse.Clear();
            return reuse;
        }

        return new SortedDictionary<TKey, TValue>();
    }

    protected override SortedDictionary<TKey, TValue> CreateForClone(SortedDictionary<TKey, TValue> source, int count, TinyhandSerializerOptions options)
        => new SortedDictionary<TKey, TValue>(source.Comparer);

    protected override SortedDictionary<TKey, TValue>.Enumerator GetSourceEnumerator(SortedDictionary<TKey, TValue> source)
    {
        return source.GetEnumerator();
    }
}

internal sealed class ReadOnlyDictionaryFormatter<TKey, TValue> : DictionaryFormatterBase<TKey, TValue, Dictionary<TKey, TValue>, ReadOnlyDictionary<TKey, TValue>>
    where TKey : notnull
{
    protected override void Add(Dictionary<TKey, TValue> collection, int index, TKey key, TValue value, TinyhandSerializerOptions options)
    {
        collection.TryAdd(key, value);
    }

    protected override ReadOnlyDictionary<TKey, TValue> Complete(Dictionary<TKey, TValue> intermediateCollection)
    {
        return new ReadOnlyDictionary<TKey, TValue>(intermediateCollection);
    }

    protected override Dictionary<TKey, TValue> Create(ReadOnlyDictionary<TKey, TValue>? reuse, int count, TinyhandSerializerOptions options)
    {// A read-only dictionary cannot be reused.
        return new Dictionary<TKey, TValue>(count, options.Security.GetEqualityComparer<TKey>());
    }
}

internal sealed class InterfaceReadOnlyDictionaryFormatter<TKey, TValue> : DictionaryFormatterBase<TKey, TValue, Dictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>>
    where TKey : notnull
{
    protected override void Add(Dictionary<TKey, TValue> collection, int index, TKey key, TValue value, TinyhandSerializerOptions options)
    {
        collection.TryAdd(key, value);
    }

    protected override IReadOnlyDictionary<TKey, TValue> Complete(Dictionary<TKey, TValue> intermediateCollection)
    {
        return intermediateCollection;
    }

    protected override Dictionary<TKey, TValue> Create(IReadOnlyDictionary<TKey, TValue>? reuse, int count, TinyhandSerializerOptions options)
    {// A read-only dictionary cannot be reused.
        return new Dictionary<TKey, TValue>(count, options.Security.GetEqualityComparer<TKey>());
    }
}

internal sealed class ConcurrentDictionaryFormatter<TKey, TValue> : DictionaryFormatterBase<TKey, TValue, System.Collections.Concurrent.ConcurrentDictionary<TKey, TValue>>
    where TKey : notnull
{
    protected override KeyValuePair<TKey, TValue>[]? GetSnapshot(ConcurrentDictionary<TKey, TValue> source) => source.ToArray();

    protected override void Add(ConcurrentDictionary<TKey, TValue> collection, int index, TKey key, TValue value, TinyhandSerializerOptions options)
    {
        collection.TryAdd(key, value);
    }

    protected override ConcurrentDictionary<TKey, TValue> Create(ConcurrentDictionary<TKey, TValue>? reuse, int count, TinyhandSerializerOptions options)
    {
        var comparer = options.Security.GetEqualityComparer<TKey>(); // Rejects unsupported keys of untrusted data, even when reusing.
        if (reuse is not null)
        {
            reuse.Clear();
            return reuse;
        }

        // concurrent dictionary can't access defaultConcurrecyLevel so does not use count overload.
        return new ConcurrentDictionary<TKey, TValue>(comparer);
    }

    protected override ConcurrentDictionary<TKey, TValue> CreateForClone(ConcurrentDictionary<TKey, TValue> source, int count, TinyhandSerializerOptions options)
    {
        options.Security.GetEqualityComparer<TKey>(); // Rejects unsupported keys of untrusted data, as Create() does.
        return new ConcurrentDictionary<TKey, TValue>(source.Comparer);
    }
}
