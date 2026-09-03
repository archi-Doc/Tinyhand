// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Arc.Collections;
using Tinyhand.Formatters;

namespace Tinyhand.Resolvers;

/// <summary>Statically typed entry points used by generated registration code.</summary>
public sealed partial class GeneratedResolver
{
    public static void RegisterListFormatter<T1>()
        => Register<List<T1>, ListFormatter<T1>>();

    public static void RegisterLinkedListFormatter<T1>()
        => Register<LinkedList<T1>, LinkedListFormatter<T1>>();

    public static void RegisterQueueFormatter<T1>()
        => Register<Queue<T1>, QueueFormatter<T1>>();

    public static void RegisterStackFormatter<T1>()
        => Register<Stack<T1>, StackFormatter<T1>>();

    public static void RegisterHashSetFormatter<T1>()
        => Register<HashSet<T1>, HashSetFormatter<T1>>();

    public static void RegisterReadOnlyCollectionFormatter<T1>()
        => Register<ReadOnlyCollection<T1>, ReadOnlyCollectionFormatter<T1>>();

    public static void RegisterInterfaceListFormatter2<T1>()
        => Register<IList<T1>, InterfaceListFormatter2<T1>>();

    public static void RegisterInterfaceCollectionFormatter2<T1>()
        => Register<ICollection<T1>, InterfaceCollectionFormatter2<T1>>();

    public static void RegisterInterfaceEnumerableFormatter<T1>()
        => Register<IEnumerable<T1>, InterfaceEnumerableFormatter<T1>>();

    public static void RegisterDictionaryFormatter<T1, T2>()
        where T1 : notnull
        => Register<Dictionary<T1, T2>, DictionaryFormatter<T1, T2>>();

    public static void RegisterInterfaceDictionaryFormatter<T1, T2>()
        where T1 : notnull
        => Register<IDictionary<T1, T2>, InterfaceDictionaryFormatter<T1, T2>>();

    public static void RegisterSortedDictionaryFormatter<T1, T2>()
        where T1 : notnull
        => Register<SortedDictionary<T1, T2>, SortedDictionaryFormatter<T1, T2>>();

    public static void RegisterSortedListFormatter<T1, T2>()
        where T1 : notnull
        => Register<SortedList<T1, T2>, SortedListFormatter<T1, T2>>();

    public static void RegisterInterfaceLookupFormatter<T1, T2>()
        where T1 : notnull
        => Register<ILookup<T1, T2>, InterfaceLookupFormatter<T1, T2>>();

    public static void RegisterInterfaceGroupingFormatter<T1, T2>()
        => Register<IGrouping<T1, T2>, InterfaceGroupingFormatter<T1, T2>>();

    public static void RegisterObservableCollectionFormatter<T1>()
        => Register<ObservableCollection<T1>, ObservableCollectionFormatter<T1>>();

    public static void RegisterReadOnlyObservableCollectionFormatter<T1>()
        => Register<ReadOnlyObservableCollection<T1>, ReadOnlyObservableCollectionFormatter<T1>>();

    public static void RegisterInterfaceReadOnlyListFormatter<T1>()
        => Register<IReadOnlyList<T1>, InterfaceReadOnlyListFormatter<T1>>();

    public static void RegisterInterfaceReadOnlyCollectionFormatter<T1>()
        => Register<IReadOnlyCollection<T1>, InterfaceReadOnlyCollectionFormatter<T1>>();

    public static void RegisterInterfaceSetFormatter<T1>()
        => Register<ISet<T1>, InterfaceSetFormatter<T1>>();

    public static void RegisterConcurrentBagFormatter<T1>()
        => Register<System.Collections.Concurrent.ConcurrentBag<T1>, ConcurrentBagFormatter<T1>>();

    public static void RegisterConcurrentQueueFormatter<T1>()
        => Register<System.Collections.Concurrent.ConcurrentQueue<T1>, ConcurrentQueueFormatter<T1>>();

    public static void RegisterConcurrentStackFormatter<T1>()
        => Register<System.Collections.Concurrent.ConcurrentStack<T1>, ConcurrentStackFormatter<T1>>();

    public static void RegisterReadOnlyDictionaryFormatter<T1, T2>()
        where T1 : notnull
        => Register<ReadOnlyDictionary<T1, T2>, ReadOnlyDictionaryFormatter<T1, T2>>();

    public static void RegisterInterfaceReadOnlyDictionaryFormatter<T1, T2>()
        where T1 : notnull
        => Register<IReadOnlyDictionary<T1, T2>, InterfaceReadOnlyDictionaryFormatter<T1, T2>>();

    public static void RegisterConcurrentDictionaryFormatter<T1, T2>()
        where T1 : notnull
        => Register<System.Collections.Concurrent.ConcurrentDictionary<T1, T2>, ConcurrentDictionaryFormatter<T1, T2>>();

    public static void RegisterLazyFormatter<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T1>()
        => Register<Lazy<T1>, LazyFormatter<T1>>();

    public static void RegisterImmutableArrayFormatter<T1>()
        => Register<ImmutableArray<T1>, ImmutableArrayFormatter<T1>>();

    public static void RegisterImmutableListFormatter<T1>()
        => Register<ImmutableList<T1>, ImmutableListFormatter<T1>>();

    public static void RegisterImmutableDictionaryFormatter<T1, T2>()
        where T1 : notnull
        => Register<ImmutableDictionary<T1, T2>, ImmutableDictionaryFormatter<T1, T2>>();

    public static void RegisterImmutableHashSetFormatter<T1>()
        => Register<ImmutableHashSet<T1>, ImmutableHashSetFormatter<T1>>();

    public static void RegisterImmutableSortedDictionaryFormatter<T1, T2>()
        where T1 : notnull
        => Register<ImmutableSortedDictionary<T1, T2>, ImmutableSortedDictionaryFormatter<T1, T2>>();

    public static void RegisterImmutableSortedSetFormatter<T1>()
        => Register<ImmutableSortedSet<T1>, ImmutableSortedSetFormatter<T1>>();

    public static void RegisterImmutableQueueFormatter<T1>()
        => Register<ImmutableQueue<T1>, ImmutableQueueFormatter<T1>>();

    public static void RegisterImmutableStackFormatter<T1>()
        => Register<ImmutableStack<T1>, ImmutableStackFormatter<T1>>();

    public static void RegisterInterfaceImmutableListFormatter<T1>()
        => Register<IImmutableList<T1>, InterfaceImmutableListFormatter<T1>>();

    public static void RegisterInterfaceImmutableDictionaryFormatter<T1, T2>()
        where T1 : notnull
        => Register<IImmutableDictionary<T1, T2>, InterfaceImmutableDictionaryFormatter<T1, T2>>();

    public static void RegisterInterfaceImmutableQueueFormatter<T1>()
        => Register<IImmutableQueue<T1>, InterfaceImmutableQueueFormatter<T1>>();

    public static void RegisterInterfaceImmutableSetFormatter<T1>()
        => Register<IImmutableSet<T1>, InterfaceImmutableSetFormatter<T1>>();

    public static void RegisterInterfaceImmutableStackFormatter<T1>()
        => Register<IImmutableStack<T1>, InterfaceImmutableStackFormatter<T1>>();

    public static void RegisterOrderedMapFormatter<T1, T2>()
        => Register<OrderedMap<T1, T2>, OrderedMapFormatter<T1, T2>>();

    public static void RegisterOrderedSetFormatter<T1>()
        => Register<OrderedSet<T1>, OrderedSetFormatter<T1>>();

    public static void RegisterOrderedMultiMapFormatter<T1, T2>()
        => Register<OrderedMultiMap<T1, T2>, OrderedMultiMapFormatter<T1, T2>>();

    public static void RegisterOrderedMultiSetFormatter<T1>()
        => Register<OrderedMultiSet<T1>, OrderedMultiSetFormatter<T1>>();

    public static void RegisterUnorderedMapFormatter<T1, T2>()
        => Register<UnorderedMap<T1, T2>, UnorderedMapFormatter<T1, T2>>();

    public static void RegisterUnorderedSetFormatter<T1>()
        => Register<UnorderedSet<T1>, UnorderedSetFormatter<T1>>();

    public static void RegisterOrderedKeyValueListFormatter<T1, T2>()
        where T1 : notnull
        => Register<OrderedKeyValueList<T1, T2>, OrderedKeyValueListFormatter<T1, T2>>();

    public static void RegisterOrderedListFormatter<T1>()
        => Register<OrderedList<T1>, OrderedListFormatter<T1>>();

    public static void RegisterUnorderedListFormatter<T1>()
        => Register<UnorderedList<T1>, UnorderedListFormatter<T1>>();

    public static void RegisterUnorderedLinkedListFormatter<T1>()
        => Register<UnorderedLinkedList<T1>, UnorderedLinkedListFormatter<T1>>();

    public static void RegisterUtf16HashtableFormatter<T1>()
        => Register<Utf16Hashtable<T1>, Utf16HashtableFormatter<T1>>();

    public static void RegisterKeyValuePairFormatter<T1, T2>()
        => Register<KeyValuePair<T1, T2>, KeyValuePairFormatter<T1, T2>>();

    public static void RegisterKeyValueListFormatter<T1, T2>()
        => Register<KeyValueList<T1, T2>, KeyValueListFormatter<T1, T2>>();

    public static void RegisterArraySegmentFormatter<T1>()
        => Register<ArraySegment<T1>, ArraySegmentFormatter<T1>>();

    public static void RegisterMemoryFormatter<T1>()
        => Register<Memory<T1>, MemoryFormatter<T1>>();

    public static void RegisterReadOnlyMemoryFormatter<T1>()
        => Register<ReadOnlyMemory<T1>, ReadOnlyMemoryFormatter<T1>>();

    public static void RegisterReadOnlySequenceFormatter<T1>()
        => Register<ReadOnlySequence<T1>, ReadOnlySequenceFormatter<T1>>();

    public static void RegisterNullableFormatter<T1>()
        where T1 : struct
        => Register<T1?, NullableFormatter<T1>>();

    public static void RegisterTupleFormatter<T1>()
        => Register<Tuple<T1>, TupleFormatter<T1>>();

    public static void RegisterTupleFormatter<T1, T2>()
        => Register<Tuple<T1, T2>, TupleFormatter<T1, T2>>();

    public static void RegisterTupleFormatter<T1, T2, T3>()
        => Register<Tuple<T1, T2, T3>, TupleFormatter<T1, T2, T3>>();

    public static void RegisterTupleFormatter<T1, T2, T3, T4>()
        => Register<Tuple<T1, T2, T3, T4>, TupleFormatter<T1, T2, T3, T4>>();

    public static void RegisterTupleFormatter<T1, T2, T3, T4, T5>()
        => Register<Tuple<T1, T2, T3, T4, T5>, TupleFormatter<T1, T2, T3, T4, T5>>();

    public static void RegisterTupleFormatter<T1, T2, T3, T4, T5, T6>()
        => Register<Tuple<T1, T2, T3, T4, T5, T6>, TupleFormatter<T1, T2, T3, T4, T5, T6>>();

    public static void RegisterTupleFormatter<T1, T2, T3, T4, T5, T6, T7>()
        => Register<Tuple<T1, T2, T3, T4, T5, T6, T7>, TupleFormatter<T1, T2, T3, T4, T5, T6, T7>>();

    public static void RegisterTupleFormatter<T1, T2, T3, T4, T5, T6, T7, T8>()
        where T8 : notnull
        => Register<Tuple<T1, T2, T3, T4, T5, T6, T7, T8>, TupleFormatter<T1, T2, T3, T4, T5, T6, T7, T8>>();

    public static void RegisterValueTupleFormatter<T1>()
        => Register<ValueTuple<T1>, ValueTupleFormatter<T1>>();

    public static void RegisterValueTupleFormatter<T1, T2>()
        => Register<ValueTuple<T1, T2>, ValueTupleFormatter<T1, T2>>();

    public static void RegisterValueTupleFormatter<T1, T2, T3>()
        => Register<ValueTuple<T1, T2, T3>, ValueTupleFormatter<T1, T2, T3>>();

    public static void RegisterValueTupleFormatter<T1, T2, T3, T4>()
        => Register<ValueTuple<T1, T2, T3, T4>, ValueTupleFormatter<T1, T2, T3, T4>>();

    public static void RegisterValueTupleFormatter<T1, T2, T3, T4, T5>()
        => Register<ValueTuple<T1, T2, T3, T4, T5>, ValueTupleFormatter<T1, T2, T3, T4, T5>>();

    public static void RegisterValueTupleFormatter<T1, T2, T3, T4, T5, T6>()
        => Register<ValueTuple<T1, T2, T3, T4, T5, T6>, ValueTupleFormatter<T1, T2, T3, T4, T5, T6>>();

    public static void RegisterValueTupleFormatter<T1, T2, T3, T4, T5, T6, T7>()
        => Register<ValueTuple<T1, T2, T3, T4, T5, T6, T7>, ValueTupleFormatter<T1, T2, T3, T4, T5, T6, T7>>();

    public static void RegisterValueTupleFormatter<T1, T2, T3, T4, T5, T6, T7, T8>()
        where T8 : struct
        => Register<ValueTuple<T1, T2, T3, T4, T5, T6, T7, T8>, ValueTupleFormatter<T1, T2, T3, T4, T5, T6, T7, T8>>();

    public static void RegisterArray<T>() => Register<T[], ArrayFormatter<T>>();

    public static void RegisterArray2<T>() => Register<T[,], TwoDimensionalArrayFormatter<T>>();

    public static void RegisterArray3<T>() => Register<T[,,], ThreeDimensionalArrayFormatter<T>>();

    public static void RegisterArray4<T>() => Register<T[,,,], FourDimensionalArrayFormatter<T>>();

    public static void RegisterEnum<T>()
        where T : struct, Enum
        => Register<T, GenericEnumFormatter<T>>();

    public static void RegisterObject<T>()
        where T : ITinyhandSerializable<T>, ITinyhandReconstructable<T>, ITinyhandCloneable<T>
        => Register<T, TinyhandObjectFormatter<T>>();

    public static void RegisterCollection<TElement, TCollection>()
        where TCollection : ICollection<TElement>, new()
        => Register<TCollection, GenericCollectionFormatter<TElement, TCollection>>();

    public static void RegisterDictionary<TKey, TValue, TDictionary>(Func<int, IEqualityComparer<TKey>, TDictionary> factory)
        where TDictionary : IDictionary<TKey, TValue>, new()
    {
        ArgumentNullException.ThrowIfNull(factory);
        if (FormatterCache<TDictionary>.Formatter is null)
        {
            System.Threading.Interlocked.CompareExchange(ref FormatterCache<TDictionary>.Formatter, new GenericDictionaryFormatter<TKey, TValue, TDictionary>(factory), null);
            TinyhandTypeIdentifier.Register<TDictionary>();
        }
    }
}
