﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Arc.Collections;
using Tinyhand.Formatters;
using Tinyhand.Internal;

#pragma warning disable SA1009 // Closing parenthesis should be spaced correctly
#pragma warning disable SA1401 // Fields should be private

namespace Tinyhand.Resolvers
{
    /// <summary>
    /// Default composited resolver.
    /// </summary>
    public sealed class GenericsResolver : IFormatterResolver
    {
        /// <summary>
        /// The singleton instance that can be used.
        /// </summary>
        public static readonly GenericsResolver Instance = new();

        private GenericsResolver()
        {
        }

        public ITinyhandFormatter<T>? TryGetFormatter<T>()
        {
            return FormatterCache<T>.Formatter;
        }

        public void RegisterInstantiableTypes()
        {
        }

        private static class FormatterCache<T>
        {
            public static ITinyhandFormatter<T>? Formatter;

            static FormatterCache()
            {
                Formatter = (ITinyhandFormatter<T>?)GenericsResolverHelper.GetFormatter(typeof(T));
            }
        }
    }
}

#pragma warning disable SA1403 // File may only contain a single namespace
namespace Tinyhand.Internal
#pragma warning restore SA1403 // File may only contain a single namespace
{
    internal static class GenericsResolverHelper
    {
        private static readonly Dictionary<Type, Type> FormatterMap = new Dictionary<Type, Type>()
        {
            { typeof(List<>), typeof(ListFormatter<>) },
            { typeof(LinkedList<>), typeof(LinkedListFormatter<>) },
            { typeof(Queue<>), typeof(QueueFormatter<>) },
            { typeof(Stack<>), typeof(StackFormatter<>) },
            { typeof(HashSet<>), typeof(HashSetFormatter<>) },
            { typeof(ReadOnlyCollection<>), typeof(ReadOnlyCollectionFormatter<>) },
            { typeof(IList<>), typeof(InterfaceListFormatter2<>) },
            { typeof(ICollection<>), typeof(InterfaceCollectionFormatter2<>) },
            { typeof(IEnumerable<>), typeof(InterfaceEnumerableFormatter<>) },
            { typeof(Dictionary<,>), typeof(DictionaryFormatter<,>) },
            { typeof(IDictionary<,>), typeof(InterfaceDictionaryFormatter<,>) },
            { typeof(SortedDictionary<,>), typeof(SortedDictionaryFormatter<,>) },
            { typeof(SortedList<,>), typeof(SortedListFormatter<,>) },
            { typeof(ILookup<,>), typeof(InterfaceLookupFormatter<,>) },
            { typeof(IGrouping<,>), typeof(InterfaceGroupingFormatter<,>) },
            { typeof(ObservableCollection<>), typeof(ObservableCollectionFormatter<>) },
            { typeof(ReadOnlyObservableCollection<>), typeof(ReadOnlyObservableCollectionFormatter<>) },
            { typeof(IReadOnlyList<>), typeof(InterfaceReadOnlyListFormatter<>) },
            { typeof(IReadOnlyCollection<>), typeof(InterfaceReadOnlyCollectionFormatter<>) },
            { typeof(ISet<>), typeof(InterfaceSetFormatter<>) },
            { typeof(System.Collections.Concurrent.ConcurrentBag<>), typeof(ConcurrentBagFormatter<>) },
            { typeof(System.Collections.Concurrent.ConcurrentQueue<>), typeof(ConcurrentQueueFormatter<>) },
            { typeof(System.Collections.Concurrent.ConcurrentStack<>), typeof(ConcurrentStackFormatter<>) },
            { typeof(ReadOnlyDictionary<,>), typeof(ReadOnlyDictionaryFormatter<,>) },
            { typeof(IReadOnlyDictionary<,>), typeof(InterfaceReadOnlyDictionaryFormatter<,>) },
            { typeof(System.Collections.Concurrent.ConcurrentDictionary<,>), typeof(ConcurrentDictionaryFormatter<,>) },
            { typeof(Lazy<>), typeof(LazyFormatter<>) },
            { typeof(ImmutableArray<>), typeof(ImmutableArrayFormatter<>) },
            { typeof(ImmutableList<>), typeof(ImmutableListFormatter<>) },
            { typeof(ImmutableDictionary<,>), typeof(ImmutableDictionaryFormatter<,>) },
            { typeof(ImmutableHashSet<>), typeof(ImmutableHashSetFormatter<>) },
            { typeof(ImmutableSortedDictionary<,>), typeof(ImmutableSortedDictionaryFormatter<,>) },
            { typeof(ImmutableSortedSet<>), typeof(ImmutableSortedSetFormatter<>) },
            { typeof(ImmutableQueue<>), typeof(ImmutableQueueFormatter<>) },
            { typeof(ImmutableStack<>), typeof(ImmutableStackFormatter<>) },
            { typeof(IImmutableList<>), typeof(InterfaceImmutableListFormatter<>) },
            { typeof(IImmutableDictionary<,>), typeof(InterfaceImmutableDictionaryFormatter<,>) },
            { typeof(IImmutableQueue<>), typeof(InterfaceImmutableQueueFormatter<>) },
            { typeof(IImmutableSet<>), typeof(InterfaceImmutableSetFormatter<>) },
            { typeof(IImmutableStack<>), typeof(InterfaceImmutableStackFormatter<>) },
            { typeof(OrderedMap<,>), typeof(OrderedMapFormatter<,>) },
            { typeof(OrderedSet<>), typeof(OrderedSetFormatter<>) },
            { typeof(OrderedMultiMap<,>), typeof(OrderedMultiMapFormatter<,>) },
            { typeof(OrderedMultiSet<>), typeof(OrderedMultiSetFormatter<>) },
            { typeof(UnorderedMap<,>), typeof(UnorderedMapFormatter<,>) },
            { typeof(UnorderedSet<>), typeof(UnorderedSetFormatter<>) },
            { typeof(UnorderedMultiMap<,>), typeof(UnorderedMultiMapFormatter<,>) },
            { typeof(UnorderedMultiSet<>), typeof(UnorderedMultiSetFormatter<>) },
            { typeof(OrderedKeyValueList<,>), typeof(OrderedKeyValueListFormatter<,>) },
            { typeof(OrderedList<>), typeof(OrderedListFormatter<>) },
            { typeof(UnorderedList<>), typeof(UnorderedListFormatter<>) },
            { typeof(UnorderedLinkedList<>), typeof(UnorderedLinkedListFormatter<>) },
        };

        // Reduce IL2CPP code generate size(don't write long code in <T>)
        internal static object? GetFormatter(Type t)
        {
            TypeInfo ti = t.GetTypeInfo();

            if (t.IsArray)
            {
                var rank = t.GetArrayRank();
                if (rank == 1)
                {
                    if (t.GetElementType() == typeof(byte))
                    {
                        // byte[] is also supported in builtin formatter.
                        return ByteArrayFormatter.Instance;
                    }

                    return Activator.CreateInstance(typeof(ArrayFormatter<>).MakeGenericType(t.GetElementType()!));
                }
                else if (rank == 2)
                {
                    return Activator.CreateInstance(typeof(TwoDimensionalArrayFormatter<>).MakeGenericType(t.GetElementType()!));
                }
                else if (rank == 3)
                {
                    return Activator.CreateInstance(typeof(ThreeDimensionalArrayFormatter<>).MakeGenericType(t.GetElementType()!));
                }
                else if (rank == 4)
                {
                    return Activator.CreateInstance(typeof(FourDimensionalArrayFormatter<>).MakeGenericType(t.GetElementType()!));
                }
                else
                {
                    return null; // not supported built-in
                }
            }
            else if (ti.IsGenericType)
            {
                Type genericType = ti.GetGenericTypeDefinition();
                TypeInfo genericTypeInfo = genericType.GetTypeInfo();

                if (genericType == typeof(KeyValuePair<,>))
                {// KeyValuePair
                    return CreateInstance(typeof(KeyValuePairFormatter<,>), ti.GenericTypeArguments);
                }
                else if (genericType == typeof(KeyValueList<,>))
                {// KeyValueList
                    return CreateInstance(typeof(KeyValueListFormatter<,>), ti.GenericTypeArguments);
                }
                else if (ti.FullName?.StartsWith("System.Tuple") == true)
                {// Tuple
                    Type? tupleFormatterType = null;
                    switch (ti.GenericTypeArguments.Length)
                    {
                        case 1:
                            tupleFormatterType = typeof(TupleFormatter<>);
                            break;
                        case 2:
                            tupleFormatterType = typeof(TupleFormatter<,>);
                            break;
                        case 3:
                            tupleFormatterType = typeof(TupleFormatter<,,>);
                            break;
                        case 4:
                            tupleFormatterType = typeof(TupleFormatter<,,,>);
                            break;
                        case 5:
                            tupleFormatterType = typeof(TupleFormatter<,,,,>);
                            break;
                        case 6:
                            tupleFormatterType = typeof(TupleFormatter<,,,,,>);
                            break;
                        case 7:
                            tupleFormatterType = typeof(TupleFormatter<,,,,,,>);
                            break;
                        case 8:
                            tupleFormatterType = typeof(TupleFormatter<,,,,,,,>);
                            break;
                        default:
                            break;
                    }

                    if (tupleFormatterType != null)
                    {
                        return CreateInstance(tupleFormatterType, ti.GenericTypeArguments);
                    }
                }
                else if (ti.FullName?.StartsWith("System.ValueTuple") == true)
                {// ValueTuple
                    Type? tupleFormatterType = null;
                    switch (ti.GenericTypeArguments.Length)
                    {
                        case 1:
                            tupleFormatterType = typeof(ValueTupleFormatter<>);
                            break;
                        case 2:
                            tupleFormatterType = typeof(ValueTupleFormatter<,>);
                            break;
                        case 3:
                            tupleFormatterType = typeof(ValueTupleFormatter<,,>);
                            break;
                        case 4:
                            tupleFormatterType = typeof(ValueTupleFormatter<,,,>);
                            break;
                        case 5:
                            tupleFormatterType = typeof(ValueTupleFormatter<,,,,>);
                            break;
                        case 6:
                            tupleFormatterType = typeof(ValueTupleFormatter<,,,,,>);
                            break;
                        case 7:
                            tupleFormatterType = typeof(ValueTupleFormatter<,,,,,,>);
                            break;
                        case 8:
                            tupleFormatterType = typeof(ValueTupleFormatter<,,,,,,,>);
                            break;
                        default:
                            break;
                    }

                    if (tupleFormatterType != null)
                    {
                        return CreateInstance(tupleFormatterType, ti.GenericTypeArguments);
                    }
                }
                else if (genericType == typeof(ArraySegment<>))
                {// ArraySegment
                    if (ti.GenericTypeArguments[0] == typeof(byte))
                    {
                        return ByteArraySegmentFormatter.Instance;
                    }
                    else
                    {
                        return CreateInstance(typeof(ArraySegmentFormatter<>), ti.GenericTypeArguments);
                    }
                }
                else if (genericType == typeof(Memory<>))
                {// Memory
                    if (ti.GenericTypeArguments[0] == typeof(byte))
                    {
                        return ByteMemoryFormatter.Instance;
                    }
                    else
                    {
                        return CreateInstance(typeof(MemoryFormatter<>), ti.GenericTypeArguments);
                    }
                }
                else if (genericType == typeof(ReadOnlyMemory<>))
                {// ReadOnlyMemory
                    if (ti.GenericTypeArguments[0] == typeof(byte))
                    {
                        return ByteReadOnlyMemoryFormatter.Instance;
                    }
                    else
                    {
                        return CreateInstance(typeof(ReadOnlyMemoryFormatter<>), ti.GenericTypeArguments);
                    }
                }
                else if (genericType == typeof(ReadOnlySequence<>))
                {// ReadOnlySequence
                    if (ti.GenericTypeArguments[0] == typeof(byte))
                    {
                        return ByteReadOnlySequenceFormatter.Instance;
                    }
                    else
                    {
                        return CreateInstance(typeof(ReadOnlySequenceFormatter<>), ti.GenericTypeArguments);
                    }
                }
                else if (genericType == typeof(Nullable<>))
                {// Standard Nullable
                    return CreateInstance(typeof(NullableFormatter<>), new[] { ti.GenericTypeArguments[0] });
                }
                else if (FormatterMap.TryGetValue(genericType, out var formatterType))
                {// Mapped formatter
                    return CreateInstance(formatterType, ti.GenericTypeArguments);
                }
            }
            else if (ti.IsEnum)
            {
                return CreateInstance(typeof(GenericEnumFormatter<>), new[] { t });
            }
            else
            {
                // NonGeneric Collection
                if (t == typeof(IEnumerable))
                {
                    return NonGenericInterfaceEnumerableFormatter.Instance;
                }
                else if (t == typeof(ICollection))
                {
                    return NonGenericInterfaceCollectionFormatter.Instance;
                }
                else if (t == typeof(IList))
                {
                    return NonGenericInterfaceListFormatter.Instance;
                }
                else if (t == typeof(IDictionary))
                {
                    return NonGenericInterfaceDictionaryFormatter.Instance;
                }

                if (typeof(IList).GetTypeInfo().IsAssignableFrom(ti) && ti.DeclaredConstructors.Any(x => x.GetParameters().Length == 0))
                {
                    return Activator.CreateInstance(typeof(NonGenericListFormatter<>).MakeGenericType(t));
                }
                else if (typeof(IDictionary).GetTypeInfo().IsAssignableFrom(ti) && ti.DeclaredConstructors.Any(x => x.GetParameters().Length == 0))
                {
                    return Activator.CreateInstance(typeof(NonGenericDictionaryFormatter<>).MakeGenericType(t));
                }
            }

            // check inherited types(e.g. Foo : ICollection<>, Bar<T> : ICollection<T>)
            {
                // generic dictionary, x => x.GetTypeInfo().IsConstructedGenericType()
                var dictionaryDef = ti.ImplementedInterfaces.FirstOrDefault(x => x.IsConstructedGenericType && x.GetGenericTypeDefinition() == typeof(IDictionary<,>));
                if (dictionaryDef != null && ti.DeclaredConstructors.Any(x => x.GetParameters().Length == 0))
                {
                    Type keyType = dictionaryDef.GenericTypeArguments[0];
                    Type valueType = dictionaryDef.GenericTypeArguments[1];
                    return CreateInstance(typeof(GenericDictionaryFormatter<,,>), new[] { keyType, valueType, t });
                }

                // generic collection
                var collectionDef = ti.ImplementedInterfaces.FirstOrDefault(x => x.IsConstructedGenericType && x.GetGenericTypeDefinition() == typeof(ICollection<>));
                if (collectionDef != null && ti.DeclaredConstructors.Any(x => x.GetParameters().Length == 0))
                {
                    Type elemType = collectionDef.GenericTypeArguments[0];
                    return CreateInstance(typeof(GenericCollectionFormatter<,>), new[] { elemType, t });
                }
            }

            return null;
        }

        private static object CreateInstance(Type genericType, Type[] genericTypeArguments, params object[] arguments)
        {
            return Activator.CreateInstance(genericType.MakeGenericType(genericTypeArguments), arguments)!;
        }
    }
}
