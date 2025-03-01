﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Tinyhand.IO;

#pragma warning disable SA1009 // Closing parenthesis should be spaced correctly

namespace Tinyhand.Formatters;

public sealed class ArrayFormatter<T> : ITinyhandFormatter<T[]>
{
    public void Serialize(ref TinyhandWriter writer, T[]? value, TinyhandSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNil();
        }
        else
        {
            ITinyhandFormatter<T> formatter = options.Resolver.GetFormatter<T>();

            writer.WriteArrayHeader(value.Length);

            for (int i = 0; i < value.Length; i++)
            {
                formatter.Serialize(ref writer, value[i], options);
            }
        }
    }

    public void Deserialize(ref TinyhandReader reader, ref T[]? value, TinyhandSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
        }
        else
        {
            ITinyhandFormatter<T> formatter = options.Resolver.GetFormatter<T>();

            var len = reader.ReadArrayHeader();
            value = new T[len];
            options.Security.DepthStep(ref reader);
            try
            {
                for (int i = 0; i < value.Length; i++)
                {
                    value[i] = formatter.Deserialize(ref reader, options) ?? formatter.Reconstruct(options);
                }
            }
            finally
            {
                reader.Depth--;
            }
        }
    }

    public T[] Reconstruct(TinyhandSerializerOptions options)
    {
        return new T[0];
    }

    public T[]? Clone(T[]? value, TinyhandSerializerOptions options)
    {
        if (value == null)
        {
            return default;
        }
        else
        {
            var formatter = options.Resolver.GetFormatter<T>();

            var len = value.Length;
            var array = new T[len];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = formatter.Clone(value[i], options)!;
            }

            return array;
        }
    }
}

public sealed class ByteMemoryFormatter : ITinyhandFormatter<Memory<byte>>
{
    public static readonly ByteMemoryFormatter Instance = new();

    private ByteMemoryFormatter()
    {
    }

    public void Serialize(ref TinyhandWriter writer, Memory<byte> value, TinyhandSerializerOptions options)
    {
        writer.Write(value.Span);
    }

    public void Deserialize(ref TinyhandReader reader, ref Memory<byte> value, TinyhandSerializerOptions options)
    {
        value = new Memory<byte>(reader.ReadBytesToArray());
    }

    public Memory<byte> Reconstruct(TinyhandSerializerOptions options)
    {
        return Memory<byte>.Empty;
    }

    public Memory<byte> Clone(Memory<byte> value, TinyhandSerializerOptions options)
        => new Memory<byte>(value.ToArray());
}

public sealed class ByteReadOnlyMemoryFormatter : ITinyhandFormatter<ReadOnlyMemory<byte>>
{
    public static readonly ByteReadOnlyMemoryFormatter Instance = new ByteReadOnlyMemoryFormatter();

    private ByteReadOnlyMemoryFormatter()
    {
    }

    public void Serialize(ref TinyhandWriter writer, ReadOnlyMemory<byte> value, TinyhandSerializerOptions options)
    {
        writer.Write(value.Span);
    }

    public void Deserialize(ref TinyhandReader reader, ref ReadOnlyMemory<byte> value, TinyhandSerializerOptions options)
    {
        value = new ReadOnlyMemory<byte>(reader.ReadBytesToArray());
    }

    public ReadOnlyMemory<byte> Reconstruct(TinyhandSerializerOptions options)
    {
        return ReadOnlyMemory<byte>.Empty;
    }

    public ReadOnlyMemory<byte> Clone(ReadOnlyMemory<byte> value, TinyhandSerializerOptions options)
        => new ReadOnlyMemory<byte>(value.ToArray());
}

public sealed class ByteReadOnlySequenceFormatter : ITinyhandFormatter<ReadOnlySequence<byte>>
{
    public static readonly ByteReadOnlySequenceFormatter Instance = new ByteReadOnlySequenceFormatter();

    private ByteReadOnlySequenceFormatter()
    {
    }

    public void Serialize(ref TinyhandWriter writer, ReadOnlySequence<byte> value, TinyhandSerializerOptions options)
    {
        writer.WriteBinHeader(checked((int)value.Length));
        foreach (ReadOnlyMemory<byte> segment in value)
        {
            writer.WriteSpan(segment.Span);
        }
    }

    public void Deserialize(ref TinyhandReader reader, ref ReadOnlySequence<byte> value, TinyhandSerializerOptions options)
    {
        value = new ReadOnlySequence<byte>(reader.ReadBytesToArray());
    }

    public ReadOnlySequence<byte> Reconstruct(TinyhandSerializerOptions options)
    {
        return ReadOnlySequence<byte>.Empty;
    }

    public ReadOnlySequence<byte> Clone(ReadOnlySequence<byte> value, TinyhandSerializerOptions options) => new ReadOnlySequence<byte>(value.ToArray());
}

public sealed class ByteArraySegmentFormatter : ITinyhandFormatter<ArraySegment<byte>>
{
    public static readonly ByteArraySegmentFormatter Instance = new ByteArraySegmentFormatter();

    private ByteArraySegmentFormatter()
    {
    }

    public void Serialize(ref TinyhandWriter writer, ArraySegment<byte> value, TinyhandSerializerOptions options)
    {
        if (value.Array == null)
        {
            writer.WriteNil();
        }
        else
        {
            writer.Write(value);
        }
    }

    public void Deserialize(ref TinyhandReader reader, ref ArraySegment<byte> value, TinyhandSerializerOptions options)
    {
        value = new ArraySegment<byte>(reader.ReadBytesToArray());
    }

    public ArraySegment<byte> Reconstruct(TinyhandSerializerOptions options)
    {
        return ArraySegment<byte>.Empty;
    }

    public ArraySegment<byte> Clone(ArraySegment<byte> value, TinyhandSerializerOptions options) => new ArraySegment<byte>(value.ToArray());
}

public sealed class MemoryFormatter<T> : ITinyhandFormatter<Memory<T>>
{
    public void Serialize(ref TinyhandWriter writer, Memory<T> value, TinyhandSerializerOptions options)
    {
        var formatter = options.Resolver.GetFormatter<ReadOnlyMemory<T>>();
        formatter.Serialize(ref writer, value, options);
    }

    public void Deserialize(ref TinyhandReader reader, ref Memory<T> value, TinyhandSerializerOptions options)
    {
        value = options.Resolver.GetFormatter<T[]>().Deserialize(ref reader, options);
    }

    public Memory<T> Reconstruct(TinyhandSerializerOptions options)
    {
        return Memory<T>.Empty;
    }

    public Memory<T> Clone(Memory<T> value, TinyhandSerializerOptions options) => options.Resolver.GetFormatter<T[]>().Clone(value.ToArray(), options);
}

public sealed class ReadOnlyMemoryFormatter<T> : ITinyhandFormatter<ReadOnlyMemory<T>>
{
    public void Serialize(ref TinyhandWriter writer, ReadOnlyMemory<T> value, TinyhandSerializerOptions options)
    {
        ITinyhandFormatter<T> formatter = options.Resolver.GetFormatter<T>();

        var span = value.Span;
        writer.WriteArrayHeader(span.Length);

        for (int i = 0; i < span.Length; i++)
        {
            formatter.Serialize(ref writer, span[i], options);
        }
    }

    public void Deserialize(ref TinyhandReader reader, ref ReadOnlyMemory<T> value, TinyhandSerializerOptions options)
    {
        value = options.Resolver.GetFormatter<T[]>().Deserialize(ref reader, options);
    }

    public ReadOnlyMemory<T> Reconstruct(TinyhandSerializerOptions options)
    {
        return ReadOnlyMemory<T>.Empty;
    }

    public ReadOnlyMemory<T> Clone(ReadOnlyMemory<T> value, TinyhandSerializerOptions options) => options.Resolver.GetFormatter<T[]>().Clone(value.ToArray(), options);
}

public sealed class ReadOnlySequenceFormatter<T> : ITinyhandFormatter<ReadOnlySequence<T>>
{
    public void Serialize(ref TinyhandWriter writer, ReadOnlySequence<T> value, TinyhandSerializerOptions options)
    {
        ITinyhandFormatter<T> formatter = options.Resolver.GetFormatter<T>();

        writer.WriteArrayHeader(checked((int)value.Length));
        foreach (ReadOnlyMemory<T> segment in value)
        {
            ReadOnlySpan<T> span = segment.Span;
            for (int i = 0; i < span.Length; i++)
            {
                formatter.Serialize(ref writer, span[i], options);
            }
        }
    }

    public void Deserialize(ref TinyhandReader reader, ref ReadOnlySequence<T> value, TinyhandSerializerOptions options)
    {
        value = new ReadOnlySequence<T>(options.Resolver.GetFormatter<T[]>().Deserialize(ref reader, options) ?? Array.Empty<T>());
    }

    public ReadOnlySequence<T> Reconstruct(TinyhandSerializerOptions options)
    {
        return ReadOnlySequence<T>.Empty;
    }

    public ReadOnlySequence<T> Clone(ReadOnlySequence<T> value, TinyhandSerializerOptions options) => new ReadOnlySequence<T>(options.Resolver.GetFormatter<T[]>().Clone(value.ToArray(), options) ?? Array.Empty<T>());
}

public sealed class ArraySegmentFormatter<T> : ITinyhandFormatter<ArraySegment<T>>
{
    public void Serialize(ref TinyhandWriter writer, ArraySegment<T> value, TinyhandSerializerOptions options)
    {
        if (value.Array == null)
        {
            writer.WriteNil();
        }
        else
        {
            var formatter = options.Resolver.GetFormatter<Memory<T>>();
            formatter.Serialize(ref writer, value, options);
        }
    }

    public void Deserialize(ref TinyhandReader reader, ref ArraySegment<T> value, TinyhandSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
        }
        else
        {
            var array = options.Resolver.GetFormatter<T[]>().Deserialize(ref reader, options);
            value = array == null ? ArraySegment<T>.Empty : new ArraySegment<T>(array);
        }
    }

    public ArraySegment<T> Reconstruct(TinyhandSerializerOptions options)
    {
        return ArraySegment<T>.Empty;
    }

    public ArraySegment<T> Clone(ArraySegment<T> value, TinyhandSerializerOptions options)
    {
        var array = options.Resolver.GetFormatter<T[]>().Clone(value.ToArray(), options);
        return array == null ? ArraySegment<T>.Empty : new ArraySegment<T>(array);
    }
}

// List<T> is popular format, should avoid abstraction.
public sealed class ListFormatter<T> : ITinyhandFormatter<List<T>>
{
    public void Serialize(ref TinyhandWriter writer, List<T>? value, TinyhandSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNil();
        }
        else
        {
            ITinyhandFormatter<T> formatter = options.Resolver.GetFormatter<T>();

            var c = value.Count;
            writer.WriteArrayHeader(c);

            for (int i = 0; i < c; i++)
            {
                formatter.Serialize(ref writer, value[i], options);
            }
        }
    }

    public void Deserialize(ref TinyhandReader reader, ref List<T>? value, TinyhandSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
        }
        else
        {
            ITinyhandFormatter<T> formatter = options.Resolver.GetFormatter<T>();

            var len = reader.ReadArrayHeader();
            value ??= new List<T>((int)len);
            options.Security.DepthStep(ref reader);
            try
            {
                for (int i = 0; i < len; i++)
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

    public List<T> Reconstruct(TinyhandSerializerOptions options)
    {
        return new List<T>();
    }

    public List<T>? Clone(List<T>? value, TinyhandSerializerOptions options)
    {
        if (value == null)
        {
            return default;
        }
        else
        {
            var formatter = options.Resolver.GetFormatter<T>();

            var len = value.Count;
            var list = new List<T>(len);
            for (int i = 0; i < len; i++)
            {
                list.Add(formatter.Clone(value[i], options)!);
            }

            return list;
        }
    }
}

public abstract class CollectionFormatterBase<TElement, TIntermediate, TEnumerator, TCollection> : ITinyhandFormatter<TCollection>
    where TCollection : IEnumerable<TElement>
    where TEnumerator : IEnumerator<TElement>
{
    public void Serialize(ref TinyhandWriter writer, TCollection? value, TinyhandSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNil();
        }
        else
        {
            ITinyhandFormatter<TElement> formatter = options.Resolver.GetFormatter<TElement>();

            // Optimize iteration(array is fastest)
            if (value is TElement[] array)
            {
                writer.WriteArrayHeader(array.Length);

                foreach (TElement item in array)
                {
                    formatter.Serialize(ref writer, item, options);
                }
            }
            else
            {
                // knows count or not.
                var seqCount = this.GetCount(value);
                if (seqCount != null)
                {
                    writer.WriteArrayHeader(seqCount.Value);

                    // Unity's foreach struct enumerator causes boxing so iterate manually.
                    using (var e = this.GetSourceEnumerator(value))
                    {
                        while (e.MoveNext())
                        {
                            formatter.Serialize(ref writer, e.Current, options);
                        }
                    }
                }
                else
                {
                    var scratchWriter = writer.Clone();
                    try
                    {
                        var count = 0;
                        using (var e = this.GetSourceEnumerator(value))
                        {
                            while (e.MoveNext())
                            {
                                count++;
                                formatter.Serialize(ref scratchWriter, e.Current, options);
                            }
                        }

                        writer.WriteArrayHeader(count);
                        writer.WriteSequence(scratchWriter.FlushAndGetReadOnlySequence());
                    }
                    finally
                    {
                        scratchWriter.Dispose();
                    }
                }
            }
        }
    }

    public void Deserialize(ref TinyhandReader reader, ref TCollection? value, TinyhandSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
        }
        else
        {
            ITinyhandFormatter<TElement> formatter = options.Resolver.GetFormatter<TElement>();

            var len = reader.ReadArrayHeader();

            TIntermediate list = this.Create(len, options);
            options.Security.DepthStep(ref reader);
            try
            {
                for (int i = 0; i < len; i++)
                {
                    this.Add(list, i, formatter.Deserialize(ref reader, options)!, options);
                }
            }
            finally
            {
                reader.Depth--;
            }

            value = this.Complete(list);
        }
    }

    public TCollection Reconstruct(TinyhandSerializerOptions options)
    {
        return this.Complete(this.Create(0, options));
    }

    public TCollection? Clone(TCollection? value, TinyhandSerializerOptions options)
    {
        if (value == null)
        {
            return default(TCollection);
        }

        var formatter = options.Resolver.GetFormatter<TElement>();

        TIntermediate list;
        int length;
        if (value is TElement[] array)
        {
            length = array.Length;
            list = this.Create(length, options);
            for (int i = 0; i < length; i++)
            {
                this.Add(list, i, formatter.Clone(array[i], options)!, options);
            }
        }
        else
        {
            var seqCount = this.GetCount(value);
            if (seqCount != null)
            {
                length = seqCount.Value;
            }
            else
            {
                length = 0;
                using (var e = this.GetSourceEnumerator(value))
                {
                    while (e.MoveNext())
                    {
                        length++;
                    }
                }
            }

            // Unity's foreach struct enumerator causes boxing so iterate manually.
            list = this.Create(length, options);
            var i = 0;
            using (var e = this.GetSourceEnumerator(value))
            {
                while (e.MoveNext())
                {
                    this.Add(list, i++, formatter.Clone(e.Current, options)!, options);
                }
            }
        }

        return this.Complete(list);
    }

    // abstraction for serialize
    protected virtual int? GetCount(TCollection sequence)
    {
        var collection = sequence as ICollection<TElement>;
        if (collection != null)
        {
            return collection.Count;
        }
        else
        {
            var c2 = sequence as IReadOnlyCollection<TElement>;
            if (c2 != null)
            {
                return c2.Count;
            }
        }

        return null;
    }

    // Some collections can use struct iterator, this is optimization path
    protected abstract TEnumerator GetSourceEnumerator(TCollection source);

    // abstraction for deserialize
    protected abstract TIntermediate Create(int count, TinyhandSerializerOptions options);

    protected abstract void Add(TIntermediate collection, int index, TElement value, TinyhandSerializerOptions options);

    protected abstract TCollection Complete(TIntermediate intermediateCollection);
}

public abstract class CollectionFormatterBase<TElement, TIntermediate, TCollection> : CollectionFormatterBase<TElement, TIntermediate, IEnumerator<TElement>, TCollection>
    where TCollection : IEnumerable<TElement>
{
    protected override IEnumerator<TElement> GetSourceEnumerator(TCollection source)
    {
        return source.GetEnumerator();
    }
}

public abstract class CollectionFormatterBase<TElement, TCollection> : CollectionFormatterBase<TElement, TCollection, TCollection>
    where TCollection : IEnumerable<TElement>
{
    protected sealed override TCollection Complete(TCollection intermediateCollection)
    {
        return intermediateCollection;
    }
}

public sealed class GenericCollectionFormatter<TElement, TCollection> : CollectionFormatterBase<TElement, TCollection>
     where TCollection : ICollection<TElement>, new()
{
    protected override TCollection Create(int count, TinyhandSerializerOptions options)
    {
        return new TCollection();
    }

    protected override void Add(TCollection collection, int index, TElement value, TinyhandSerializerOptions options)
    {
        collection.Add(value);
    }
}

public sealed class LinkedListFormatter<T> : CollectionFormatterBase<T, LinkedList<T>, LinkedList<T>.Enumerator, LinkedList<T>>
{
    protected override void Add(LinkedList<T> collection, int index, T value, TinyhandSerializerOptions options)
    {
        collection.AddLast(value);
    }

    protected override LinkedList<T> Complete(LinkedList<T> intermediateCollection)
    {
        return intermediateCollection;
    }

    protected override LinkedList<T> Create(int count, TinyhandSerializerOptions options)
    {
        return new LinkedList<T>();
    }

    protected override LinkedList<T>.Enumerator GetSourceEnumerator(LinkedList<T> source)
    {
        return source.GetEnumerator();
    }
}

public sealed class QueueFormatter<T> : CollectionFormatterBase<T, Queue<T>, Queue<T>.Enumerator, Queue<T>>
{
    protected override int? GetCount(Queue<T> sequence)
    {
        return sequence.Count;
    }

    protected override void Add(Queue<T> collection, int index, T value, TinyhandSerializerOptions options)
    {
        collection.Enqueue(value);
    }

    protected override Queue<T> Create(int count, TinyhandSerializerOptions options)
    {
        return new Queue<T>(count);
    }

    protected override Queue<T>.Enumerator GetSourceEnumerator(Queue<T> source)
    {
        return source.GetEnumerator();
    }

    protected override Queue<T> Complete(Queue<T> intermediateCollection)
    {
        return intermediateCollection;
    }
}

// should deserialize reverse order.
public sealed class StackFormatter<T> : CollectionFormatterBase<T, T[], Stack<T>.Enumerator, Stack<T>>
{
    protected override int? GetCount(Stack<T> sequence)
    {
        return sequence.Count;
    }

    protected override void Add(T[] collection, int index, T value, TinyhandSerializerOptions options)
    {
        // add reverse
        collection[collection.Length - 1 - index] = value;
    }

    protected override T[] Create(int count, TinyhandSerializerOptions options)
    {
        return count == 0 ? Array.Empty<T>() : new T[count];
    }

    protected override Stack<T>.Enumerator GetSourceEnumerator(Stack<T> source)
    {
        return source.GetEnumerator();
    }

    protected override Stack<T> Complete(T[] intermediateCollection)
    {
        return new Stack<T>(intermediateCollection);
    }
}

public sealed class HashSetFormatter<T> : CollectionFormatterBase<T, HashSet<T>, HashSet<T>.Enumerator, HashSet<T>>
{
    protected override int? GetCount(HashSet<T> sequence)
    {
        return sequence.Count;
    }

    protected override void Add(HashSet<T> collection, int index, T value, TinyhandSerializerOptions options)
    {
        collection.Add(value);
    }

    protected override HashSet<T> Complete(HashSet<T> intermediateCollection)
    {
        return intermediateCollection;
    }

    protected override HashSet<T> Create(int count, TinyhandSerializerOptions options)
    {
        return new HashSet<T>(options.Security.GetEqualityComparer<T>());
    }

    protected override HashSet<T>.Enumerator GetSourceEnumerator(HashSet<T> source)
    {
        return source.GetEnumerator();
    }
}

public sealed class ReadOnlyCollectionFormatter<T> : CollectionFormatterBase<T, T[], ReadOnlyCollection<T>>
{
    protected override void Add(T[] collection, int index, T value, TinyhandSerializerOptions options)
    {
        collection[index] = value;
    }

    protected override ReadOnlyCollection<T> Complete(T[] intermediateCollection)
    {
        return new ReadOnlyCollection<T>(intermediateCollection);
    }

    protected override T[] Create(int count, TinyhandSerializerOptions options)
    {
        return count == 0 ? Array.Empty<T>() : new T[count];
    }
}

[Obsolete("Use " + nameof(InterfaceListFormatter2<int>) + " instead.")]
public sealed class InterfaceListFormatter<T> : CollectionFormatterBase<T, T[], IList<T>>
{
    protected override void Add(T[] collection, int index, T value, TinyhandSerializerOptions options)
    {
        collection[index] = value;
    }

    protected override T[] Create(int count, TinyhandSerializerOptions options)
    {
        return count == 0 ? Array.Empty<T>() : new T[count];
    }

    protected override IList<T> Complete(T[] intermediateCollection)
    {
        return intermediateCollection;
    }
}

[Obsolete("Use " + nameof(InterfaceCollectionFormatter2<int>) + " instead.")]
public sealed class InterfaceCollectionFormatter<T> : CollectionFormatterBase<T, T[], ICollection<T>>
{
    protected override void Add(T[] collection, int index, T value, TinyhandSerializerOptions options)
    {
        collection[index] = value;
    }

    protected override T[] Create(int count, TinyhandSerializerOptions options)
    {
        return count == 0 ? Array.Empty<T>() : new T[count];
    }

    protected override ICollection<T> Complete(T[] intermediateCollection)
    {
        return intermediateCollection;
    }
}

public sealed class InterfaceListFormatter2<T> : CollectionFormatterBase<T, List<T>, IList<T>>
{
    protected override void Add(List<T> collection, int index, T value, TinyhandSerializerOptions options)
    {
        collection.Add(value);
    }

    protected override List<T> Create(int count, TinyhandSerializerOptions options)
    {
        return new List<T>(count);
    }

    protected override IList<T> Complete(List<T> intermediateCollection)
    {
        return intermediateCollection;
    }
}

public sealed class InterfaceCollectionFormatter2<T> : CollectionFormatterBase<T, List<T>, ICollection<T>>
{
    protected override void Add(List<T> collection, int index, T value, TinyhandSerializerOptions options)
    {
        collection.Add(value);
    }

    protected override List<T> Create(int count, TinyhandSerializerOptions options)
    {
        return new List<T>(count);
    }

    protected override ICollection<T> Complete(List<T> intermediateCollection)
    {
        return intermediateCollection;
    }
}

public sealed class InterfaceEnumerableFormatter<T> : CollectionFormatterBase<T, T[], IEnumerable<T>>
{
    protected override void Add(T[] collection, int index, T value, TinyhandSerializerOptions options)
    {
        collection[index] = value;
    }

    protected override T[] Create(int count, TinyhandSerializerOptions options)
    {
        return count == 0 ? Array.Empty<T>() : new T[count];
    }

    protected override IEnumerable<T> Complete(T[] intermediateCollection)
    {
        return intermediateCollection;
    }
}

// [Key, [Array]]
public sealed class InterfaceGroupingFormatter<TKey, TElement> : ITinyhandFormatter<IGrouping<TKey, TElement>>
{
    public void Serialize(ref TinyhandWriter writer, IGrouping<TKey, TElement>? value, TinyhandSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNil();
        }
        else
        {
            writer.WriteArrayHeader(2);
            options.Resolver.GetFormatter<TKey>().Serialize(ref writer, value.Key, options);
            options.Resolver.GetFormatter<IEnumerable<TElement>>().Serialize(ref writer, value, options);
        }
    }

    public void Deserialize(ref TinyhandReader reader, ref IGrouping<TKey, TElement>? value, TinyhandSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
        }
        else
        {
            var count = reader.ReadArrayHeader();

            if (count != 2)
            {
                throw new TinyhandException("Invalid Grouping format.");
            }

            options.Security.DepthStep(ref reader);
            try
            {
                var key = options.Resolver.GetFormatter<TKey>().Deserialize(ref reader, options);
                var element = options.Resolver.GetFormatter<IEnumerable<TElement>>().Deserialize(ref reader, options);
                value = new Grouping<TKey, TElement>(key!, element!);
            }
            finally
            {
                reader.Depth--;
            }
        }
    }

    public IGrouping<TKey, TElement> Reconstruct(TinyhandSerializerOptions options)
    {
        return new Grouping<TKey, TElement>(default!, Enumerable.Empty<TElement>());
    }

    public IGrouping<TKey, TElement>? Clone(IGrouping<TKey, TElement>? value, TinyhandSerializerOptions options)
    {
        if (value == null)
        {
            return null;
        }

        var k = options.Resolver.GetFormatter<TKey>().Clone(value.Key, options);
        var v = options.Resolver.GetFormatter<IEnumerable<TElement>>().Clone(value, options);
        return new Grouping<TKey, TElement>(k!, v!);
    }
}

public sealed class InterfaceLookupFormatter<TKey, TElement> : CollectionFormatterBase<IGrouping<TKey, TElement>, Dictionary<TKey, IGrouping<TKey, TElement>>, ILookup<TKey, TElement>>
    where TKey : notnull
{
    protected override void Add(Dictionary<TKey, IGrouping<TKey, TElement>> collection, int index, IGrouping<TKey, TElement> value, TinyhandSerializerOptions options)
    {
        collection.Add(value.Key, value);
    }

    protected override ILookup<TKey, TElement> Complete(Dictionary<TKey, IGrouping<TKey, TElement>> intermediateCollection)
    {
        return new Lookup<TKey, TElement>(intermediateCollection);
    }

    protected override Dictionary<TKey, IGrouping<TKey, TElement>> Create(int count, TinyhandSerializerOptions options)
    {
        return new Dictionary<TKey, IGrouping<TKey, TElement>>(count);
    }
}

internal class Grouping<TKey, TElement> : IGrouping<TKey, TElement>
{
    private readonly TKey key;
    private readonly IEnumerable<TElement> elements;

    public Grouping(TKey key, IEnumerable<TElement> elements)
    {
        this.key = key;
        this.elements = elements;
    }

    public TKey Key
    {
        get
        {
            return this.key;
        }
    }

    public IEnumerator<TElement> GetEnumerator()
    {
        return this.elements.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return this.elements.GetEnumerator();
    }
}

internal class Lookup<TKey, TElement> : ILookup<TKey, TElement>
    where TKey : notnull
{
    private readonly Dictionary<TKey, IGrouping<TKey, TElement>> groupings;

    public Lookup(Dictionary<TKey, IGrouping<TKey, TElement>> groupings)
    {
        this.groupings = groupings;
    }

    public IEnumerable<TElement> this[TKey key]
    {
        get
        {
            return this.groupings[key];
        }
    }

    public int Count
    {
        get
        {
            return this.groupings.Count;
        }
    }

    public bool Contains(TKey key)
    {
        return this.groupings.ContainsKey(key);
    }

    public IEnumerator<IGrouping<TKey, TElement>> GetEnumerator()
    {
        return this.groupings.Values.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return this.groupings.Values.GetEnumerator();
    }
}

/* NonGenerics */

#pragma warning disable SA1202 // Elements should be ordered by access
public sealed class NonGenericListFormatter<T> : ITinyhandFormatter<T>
#pragma warning restore SA1202 // Elements should be ordered by access
    where T : class, IList, new()
{
    public void Serialize(ref TinyhandWriter writer, T? value, TinyhandSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNil();
            return;
        }

        ITinyhandFormatter<object> formatter = options.Resolver.GetFormatter<object>();

        writer.WriteArrayHeader(value.Count);
        foreach (var item in value)
        {
            formatter.Serialize(ref writer, item, options);
        }
    }

    public void Deserialize(ref TinyhandReader reader, ref T? value, TinyhandSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
        }

        ITinyhandFormatter<object> formatter = options.Resolver.GetFormatter<object>();

        var count = reader.ReadArrayHeader();

        value ??= new T();
        options.Security.DepthStep(ref reader);
        try
        {
            for (int i = 0; i < count; i++)
            {
                value.Add(formatter.Deserialize(ref reader, options));
            }
        }
        finally
        {
            reader.Depth--;
        }
    }

    public T Reconstruct(TinyhandSerializerOptions options)
    {
        return new T();
    }

    public T? Clone(T? value, TinyhandSerializerOptions options)
    {
        if (value == null)
        {
            return default(T);
        }

        var formatter = options.Resolver.GetFormatter<object>();

        var count = value.Count;
        var list = new T();
        foreach (var item in value)
        {
            list.Add(formatter.Clone(item, options));
        }

        return list;
    }
}

public sealed class NonGenericInterfaceCollectionFormatter : ITinyhandFormatter<ICollection>
{
    public static readonly ITinyhandFormatter<ICollection> Instance = new NonGenericInterfaceCollectionFormatter();

    private NonGenericInterfaceCollectionFormatter()
    {
    }

    public void Serialize(ref TinyhandWriter writer, ICollection? value, TinyhandSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNil();
            return;
        }

        ITinyhandFormatter<object> formatter = options.Resolver.GetFormatter<object>();

        writer.WriteArrayHeader(value.Count);
        foreach (var item in value)
        {
            formatter.Serialize(ref writer, item, options);
        }
    }

    public void Deserialize(ref TinyhandReader reader, ref ICollection? value, TinyhandSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
            return;
        }

        var count = reader.ReadArrayHeader();
        if (count == 0)
        {
            value ??= Array.Empty<object>();
            return;
        }

        ITinyhandFormatter<object> formatter = options.Resolver.GetFormatter<object>();

        var list = new object[count];
        options.Security.DepthStep(ref reader);
        try
        {
            for (int i = 0; i < count; i++)
            {
                list[i] = formatter.Deserialize(ref reader, options)!;
            }
        }
        finally
        {
            reader.Depth--;
        }

        value = list;
    }

    public ICollection Reconstruct(TinyhandSerializerOptions options)
    {
        return Array.Empty<object>();
    }

    public ICollection? Clone(ICollection? value, TinyhandSerializerOptions options)
    {
        if (value == null)
        {
            return default(ICollection);
        }

        var formatter = options.Resolver.GetFormatter<object>();

        var count = value.Count;
        var list = new object[count];
        var i = 0;
        foreach (var item in value)
        {
            list[i] = formatter.Clone(item, options)!;
        }

        return list;
    }
}

public sealed class NonGenericInterfaceEnumerableFormatter : ITinyhandFormatter<IEnumerable>
{
    public static readonly ITinyhandFormatter<IEnumerable> Instance = new NonGenericInterfaceEnumerableFormatter();

    private NonGenericInterfaceEnumerableFormatter()
    {
    }

    public void Serialize(ref TinyhandWriter writer, IEnumerable? value, TinyhandSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNil();
            return;
        }

        ITinyhandFormatter<object> formatter = options.Resolver.GetFormatter<object>();

        var scratchWriter = writer.Clone();
        try
        {
            var count = 0;
            var e = value.GetEnumerator();
            try
            {
                while (e.MoveNext())
                {
                    count++;
                    formatter.Serialize(ref scratchWriter, e.Current, options);
                }
            }
            finally
            {
                if (e is IDisposable d)
                {
                    d.Dispose();
                }
            }

            writer.WriteArrayHeader(count);
            writer.WriteSequence(scratchWriter.FlushAndGetReadOnlySequence());
        }
        finally
        {
            scratchWriter.Dispose();
        }
    }

    public void Deserialize(ref TinyhandReader reader, ref IEnumerable? value, TinyhandSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
            return;
        }

        var count = reader.ReadArrayHeader();
        if (count == 0)
        {
            value ??= Array.Empty<object>();
            return;
        }

        ITinyhandFormatter<object> formatter = options.Resolver.GetFormatter<object>();

        var list = new object[count];
        options.Security.DepthStep(ref reader);
        try
        {
            for (int i = 0; i < count; i++)
            {
                list[i] = formatter.Deserialize(ref reader, options)!;
            }
        }
        finally
        {
            reader.Depth--;
        }

        value = list;
    }

    public IEnumerable Reconstruct(TinyhandSerializerOptions options)
    {
        return Array.Empty<object>();
    }

    public IEnumerable? Clone(IEnumerable? value, TinyhandSerializerOptions options)
    {
        if (value == null)
        {
            return default(IEnumerable);
        }

        var formatter = options.Resolver.GetFormatter<object>();
        var list = new List<object>();
        foreach (var item in value)
        {
            list.Add(formatter.Clone(item, options)!);
        }

        return list.ToArray();
    }
}

public sealed class NonGenericInterfaceListFormatter : ITinyhandFormatter<IList>
{
    public static readonly ITinyhandFormatter<IList> Instance = new NonGenericInterfaceListFormatter();

    private NonGenericInterfaceListFormatter()
    {
    }

    public void Serialize(ref TinyhandWriter writer, IList? value, TinyhandSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNil();
            return;
        }

        ITinyhandFormatter<object> formatter = options.Resolver.GetFormatter<object>();

        writer.WriteArrayHeader(value.Count);
        foreach (var item in value)
        {
            formatter.Serialize(ref writer, item, options);
        }
    }

    public void Deserialize(ref TinyhandReader reader, ref IList? value, TinyhandSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
            return;
        }

        var count = reader.ReadArrayHeader();
        if (count == 0)
        {
            value ??= Array.Empty<object>();
            return;
        }

        ITinyhandFormatter<object> formatter = options.Resolver.GetFormatter<object>();

        var list = new object[count];
        options.Security.DepthStep(ref reader);
        try
        {
            for (int i = 0; i < count; i++)
            {
                list[i] = formatter.Deserialize(ref reader, options)!;
            }
        }
        finally
        {
            reader.Depth--;
        }

        value = list;
    }

    public IList Reconstruct(TinyhandSerializerOptions options)
    {
        return new object[0];
    }

    public IList? Clone(IList? value, TinyhandSerializerOptions options)
    {
        if (value == null)
        {
            return default(IList);
        }

        var formatter = options.Resolver.GetFormatter<object>();

        var count = value.Count;
        var list = new object[count];
        var i = 0;
        foreach (var item in value)
        {
            list[i++] = formatter.Clone(item, options)!;
        }

        return list;
    }
}

public sealed class NonGenericDictionaryFormatter<T> : ITinyhandFormatter<T>
    where T : class, IDictionary, new()
{
    public void Serialize(ref TinyhandWriter writer, T? value, TinyhandSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNil();
            return;
        }

        ITinyhandFormatter<object> formatter = options.Resolver.GetFormatter<object>();

        writer.WriteMapHeader(value.Count);
        foreach (DictionaryEntry item in value)
        {
            formatter.Serialize(ref writer, item.Key, options);
            formatter.Serialize(ref writer, item.Value, options);
        }
    }

    public void Deserialize(ref TinyhandReader reader, ref T? value, TinyhandSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
            return;
        }

        ITinyhandFormatter<object> formatter = options.Resolver.GetFormatter<object>();

        var count = reader.ReadMapHeader2();

        var dict = CollectionHelpers<T, IEqualityComparer>.CreateHashCollection(count, options.Security.GetEqualityComparer());
        options.Security.DepthStep(ref reader);
        try
        {
            for (int i = 0; i < count; i++)
            {
                var key = formatter.Deserialize(ref reader, options)!;
                var v = formatter.Deserialize(ref reader, options);
                dict.Add(key, v);
            }
        }
        finally
        {
            reader.Depth--;
        }

        value = dict;
    }

    public T Reconstruct(TinyhandSerializerOptions options)
    {
        return CollectionHelpers<T, IEqualityComparer>.CreateHashCollection(0, options.Security.GetEqualityComparer());
    }

    public T? Clone(T? value, TinyhandSerializerOptions options)
    {
        if (value == null)
        {
            return null;
        }

        var formatter = options.Resolver.GetFormatter<object>();
        var count = value.Count;

        var dict = CollectionHelpers<T, IEqualityComparer>.CreateHashCollection(count, options.Security.GetEqualityComparer());
        foreach (DictionaryEntry item in value)
        {
            dict.Add(formatter.Clone(item.Key, options) ?? formatter.Reconstruct(options), formatter.Clone(item.Value, options));
        }

        return dict;
    }
}

public sealed class NonGenericInterfaceDictionaryFormatter : ITinyhandFormatter<IDictionary>
{
    public static readonly ITinyhandFormatter<IDictionary> Instance = new NonGenericInterfaceDictionaryFormatter();

    private NonGenericInterfaceDictionaryFormatter()
    {
    }

    public void Serialize(ref TinyhandWriter writer, IDictionary? value, TinyhandSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNil();
            return;
        }

        ITinyhandFormatter<object> formatter = options.Resolver.GetFormatter<object>();

        writer.WriteMapHeader(value.Count);
        foreach (DictionaryEntry item in value)
        {
            formatter.Serialize(ref writer, item.Key, options);
            formatter.Serialize(ref writer, item.Value, options);
        }
    }

    public void Deserialize(ref TinyhandReader reader, ref IDictionary? value, TinyhandSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
            return;
        }

        ITinyhandFormatter<object> formatter = options.Resolver.GetFormatter<object>();

        var count = reader.ReadMapHeader2();

        var dict = new Dictionary<object, object>(count, options.Security.GetEqualityComparer<object>());
        options.Security.DepthStep(ref reader);
        try
        {
            for (int i = 0; i < count; i++)
            {
                var key = formatter.Deserialize(ref reader, options)!;
                var v = formatter.Deserialize(ref reader, options)!;
                dict.Add(key, v);
            }
        }
        finally
        {
            reader.Depth--;
        }

        value = dict;
    }

    public IDictionary Reconstruct(TinyhandSerializerOptions options)
    {
        return new Dictionary<object, object>(options.Security.GetEqualityComparer<object>());
    }

    public IDictionary? Clone(IDictionary? value, TinyhandSerializerOptions options)
    {
        if (value == null)
        {
            return null;
        }

        var formatter = options.Resolver.GetFormatter<object>();
        var count = value.Count;

        var dict = new Dictionary<object, object>(count, options.Security.GetEqualityComparer<object>());
        foreach (DictionaryEntry item in value)
        {
            dict.Add(formatter.Clone(item.Key, options)!, formatter.Clone(item.Value, options)!);
        }

        return dict;
    }
}

public sealed class ObservableCollectionFormatter<T> : CollectionFormatterBase<T, ObservableCollection<T>>
{
    protected override void Add(ObservableCollection<T> collection, int index, T value, TinyhandSerializerOptions options)
    {
        collection.Add(value);
    }

    protected override ObservableCollection<T> Create(int count, TinyhandSerializerOptions options)
    {
        return new ObservableCollection<T>();
    }
}

public sealed class ReadOnlyObservableCollectionFormatter<T> : CollectionFormatterBase<T, ObservableCollection<T>, ReadOnlyObservableCollection<T>>
{
    protected override void Add(ObservableCollection<T> collection, int index, T value, TinyhandSerializerOptions options)
    {
        collection.Add(value);
    }

    protected override ObservableCollection<T> Create(int count, TinyhandSerializerOptions options)
    {
        return new ObservableCollection<T>();
    }

    protected override ReadOnlyObservableCollection<T> Complete(ObservableCollection<T> intermediateCollection)
    {
        return new ReadOnlyObservableCollection<T>(intermediateCollection);
    }
}

public sealed class InterfaceReadOnlyListFormatter<T> : CollectionFormatterBase<T, T[], IReadOnlyList<T>>
{
    protected override void Add(T[] collection, int index, T value, TinyhandSerializerOptions options)
    {
        collection[index] = value;
    }

    protected override T[] Create(int count, TinyhandSerializerOptions options)
    {
        return count == 0 ? Array.Empty<T>() : new T[count];
    }

    protected override IReadOnlyList<T> Complete(T[] intermediateCollection)
    {
        return intermediateCollection;
    }
}

public sealed class InterfaceReadOnlyCollectionFormatter<T> : CollectionFormatterBase<T, T[], IReadOnlyCollection<T>>
{
    protected override void Add(T[] collection, int index, T value, TinyhandSerializerOptions options)
    {
        collection[index] = value;
    }

    protected override T[] Create(int count, TinyhandSerializerOptions options)
    {
        return count == 0 ? Array.Empty<T>() : new T[count];
    }

    protected override IReadOnlyCollection<T> Complete(T[] intermediateCollection)
    {
        return intermediateCollection;
    }
}

public sealed class InterfaceSetFormatter<T> : CollectionFormatterBase<T, HashSet<T>, ISet<T>>
{
    protected override void Add(HashSet<T> collection, int index, T value, TinyhandSerializerOptions options)
    {
        collection.Add(value);
    }

    protected override ISet<T> Complete(HashSet<T> intermediateCollection)
    {
        return intermediateCollection;
    }

    protected override HashSet<T> Create(int count, TinyhandSerializerOptions options)
    {
        return new HashSet<T>(options.Security.GetEqualityComparer<T>());
    }
}

public sealed class ConcurrentBagFormatter<T> : CollectionFormatterBase<T, System.Collections.Concurrent.ConcurrentBag<T>>
{
    protected override int? GetCount(ConcurrentBag<T> sequence)
    {
        return sequence.Count;
    }

    protected override void Add(ConcurrentBag<T> collection, int index, T value, TinyhandSerializerOptions options)
    {
        collection.Add(value);
    }

    protected override ConcurrentBag<T> Create(int count, TinyhandSerializerOptions options)
    {
        return new ConcurrentBag<T>();
    }
}

public sealed class ConcurrentQueueFormatter<T> : CollectionFormatterBase<T, System.Collections.Concurrent.ConcurrentQueue<T>>
{
    protected override int? GetCount(ConcurrentQueue<T> sequence)
    {
        return sequence.Count;
    }

    protected override void Add(ConcurrentQueue<T> collection, int index, T value, TinyhandSerializerOptions options)
    {
        collection.Enqueue(value);
    }

    protected override ConcurrentQueue<T> Create(int count, TinyhandSerializerOptions options)
    {
        return new ConcurrentQueue<T>();
    }
}

public sealed class ConcurrentStackFormatter<T> : CollectionFormatterBase<T, T[], ConcurrentStack<T>>
{
    protected override int? GetCount(ConcurrentStack<T> sequence)
    {
        return sequence.Count;
    }

    protected override void Add(T[] collection, int index, T value, TinyhandSerializerOptions options)
    {
        // add reverse
        collection[collection.Length - 1 - index] = value;
    }

    protected override T[] Create(int count, TinyhandSerializerOptions options)
    {
        return count == 0 ? Array.Empty<T>() : new T[count];
    }

    protected override ConcurrentStack<T> Complete(T[] intermediateCollection)
    {
        return new ConcurrentStack<T>(intermediateCollection);
    }
}
