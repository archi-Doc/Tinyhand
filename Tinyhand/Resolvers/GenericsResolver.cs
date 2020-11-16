﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Tinyhand.Formatters;
using Tinyhand.Internal;

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
            /* { typeof(List<>), typeof(ListFormatter<>) },
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
            { typeof(System.Collections.Concurrent.ConcurrentDictionary<,>), typeof(ConcurrentDictionaryFormatter<,>) },*/
            { typeof(Lazy<>), typeof(LazyFormatter<>) },
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

                    return Activator.CreateInstance(typeof(ArrayFormatter<>).MakeGenericType(t.GetElementType()));
                }
                else if (rank == 2)
                {
                    return Activator.CreateInstance(typeof(TwoDimensionalArrayFormatter<>).MakeGenericType(t.GetElementType()));
                }
                else if (rank == 3)
                {
                    return Activator.CreateInstance(typeof(ThreeDimensionalArrayFormatter<>).MakeGenericType(t.GetElementType()));
                }
                else if (rank == 4)
                {
                    return Activator.CreateInstance(typeof(FourDimensionalArrayFormatter<>).MakeGenericType(t.GetElementType()));
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
                else if (ti.FullName.StartsWith("System.Tuple"))
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

                // ValueTuple
                else if (ti.FullName.StartsWith("System.ValueTuple"))
                {
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
                { // ArraySegment
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

            return null;
        }

        private static object CreateInstance(Type genericType, Type[] genericTypeArguments, params object[] arguments)
        {
            return Activator.CreateInstance(genericType.MakeGenericType(genericTypeArguments), arguments);
        }
    }
}
