// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using Arc.Visceral;
using Tinyhand.Generator;

namespace Tinyhand.Coders;

public sealed class FormatterResolver : ICoderResolver
{
    /// <summary>
    /// The singleton instance that can be used.
    /// </summary>
    public static readonly FormatterResolver Instance = new();

    public FormatterResolver()
    {
        // Several non-generic types which have formatters but not coders.
        this.AddFormatter("decimal");
        this.AddFormatter("decimal?");
        this.AddFormatter(typeof(System.TimeSpan));
        this.AddFormatter(typeof(System.DateTimeOffset));
        this.AddFormatter(typeof(System.Guid));
        this.AddFormatter(typeof(System.Uri));
        this.AddFormatter(typeof(System.Version));
        this.AddFormatter(typeof(System.Text.StringBuilder));
        this.AddFormatter(typeof(System.Collections.BitArray));
        this.AddFormatter(typeof(System.Numerics.BigInteger));
        this.AddFormatter(typeof(System.Numerics.Complex));
        this.AddFormatter(typeof(Type));

        this.AddFormatter("MessagePack.Nil");
        this.AddFormatter("MessagePack.Nil?");

        this.AddFormatter(typeof(object[]));
        this.AddFormatter(typeof(List<object>));

        this.AddFormatter(typeof(Memory<byte>));
        this.AddFormatter(typeof(Memory<byte>?));
        this.AddFormatter(typeof(ReadOnlyMemory<byte>));
        this.AddFormatter(typeof(ReadOnlyMemory<byte>?));
        this.AddFormatter(typeof(System.Buffers.ReadOnlySequence<byte>));
        this.AddFormatter(typeof(System.Buffers.ReadOnlySequence<byte>?));
        this.AddFormatter(typeof(ArraySegment<byte>));
        this.AddFormatter(typeof(ArraySegment<byte>?));

        this.AddGenericsType(typeof(Nullable<>));
        this.AddGenericsType(typeof(KeyValuePair<,>));
        this.AddGenericsType(typeof(ArraySegment<>));
        this.AddGenericsType(typeof(Memory<>));
        this.AddGenericsType(typeof(ReadOnlyMemory<>));
        this.AddGenericsType(typeof(ReadOnlySequence<>));
        this.AddGenericsType(typeof(List<>));
        this.AddGenericsType(typeof(LinkedList<>));
        this.AddGenericsType(typeof(Queue<>));
        this.AddGenericsType(typeof(Stack<>));
        this.AddGenericsType(typeof(HashSet<>));
        this.AddGenericsType(typeof(ReadOnlyCollection<>));
        this.AddGenericsType(typeof(IList<>));
        this.AddGenericsType(typeof(ICollection<>));
        this.AddGenericsType(typeof(IEnumerable<>));
        this.AddGenericsType(typeof(Dictionary<,>));
        this.AddGenericsType(typeof(IDictionary<,>));
        this.AddGenericsType(typeof(SortedDictionary<,>));
        this.AddGenericsType(typeof(SortedList<,>));
        this.AddGenericsType(typeof(ILookup<,>));
        this.AddGenericsType(typeof(IGrouping<,>));
        this.AddGenericsType(typeof(ObservableCollection<>));
        this.AddGenericsType(typeof(ReadOnlyObservableCollection<>));
        this.AddGenericsType(typeof(IReadOnlyList<>));
        this.AddGenericsType(typeof(IReadOnlyCollection<>));
        this.AddGenericsType(typeof(ISet<>));
        this.AddGenericsType(typeof(System.Collections.Concurrent.ConcurrentBag<>));
        this.AddGenericsType(typeof(System.Collections.Concurrent.ConcurrentQueue<>));
        this.AddGenericsType(typeof(System.Collections.Concurrent.ConcurrentStack<>));
        this.AddGenericsType(typeof(ReadOnlyDictionary<,>));
        this.AddGenericsType(typeof(IReadOnlyDictionary<,>));
        this.AddGenericsType(typeof(System.Collections.Concurrent.ConcurrentDictionary<,>));
        this.AddGenericsType(typeof(Lazy<>));
        this.AddGenericsType(typeof(ImmutableArray<>));
        this.AddGenericsType(typeof(ImmutableList<>));
        this.AddGenericsType(typeof(ImmutableDictionary<,>));
        this.AddGenericsType(typeof(ImmutableHashSet<>));
        this.AddGenericsType(typeof(ImmutableSortedDictionary<,>));
        this.AddGenericsType(typeof(ImmutableSortedSet<>));
        this.AddGenericsType(typeof(ImmutableQueue<>));
        this.AddGenericsType(typeof(ImmutableStack<>));
        this.AddGenericsType(typeof(IImmutableList<>));
        this.AddGenericsType(typeof(IImmutableDictionary<,>));
        this.AddGenericsType(typeof(IImmutableQueue<>));
        this.AddGenericsType(typeof(IImmutableSet<>));
        this.AddGenericsType(typeof(IImmutableStack<>));

        this.AddFormatter(typeof(System.Net.IPAddress));
        this.AddFormatter(typeof(System.Net.IPEndPoint));

        this.AddGenericsFullName("Arc.Collections.OrderedMap<TKey, TValue>");
        this.AddGenericsFullName("Arc.Collections.OrderedMultiMap<TKey, TValue>");
        this.AddGenericsFullName("Arc.Collections.OrderedSet<T>");
        this.AddGenericsFullName("Arc.Collections.OrderedMultiSet<T>");
        this.AddGenericsFullName("Arc.Collections.UnorderedMap<TKey, TValue>");
        this.AddGenericsFullName("Arc.Collections.UnorderedMultiMap<TKey, TValue>");
        this.AddGenericsFullName("Arc.Collections.UnorderedSet<T>");
        this.AddGenericsFullName("Arc.Collections.UnorderedMultiSet<T>");
        this.AddGenericsFullName("Arc.Collections.OrderedKeyValueList<TKey, TValue>");
        this.AddGenericsFullName("Arc.Collections.OrderedList<T>");
        this.AddGenericsFullName("Arc.Collections.UnorderedList<T>");
        this.AddGenericsFullName("Arc.Collections.UnorderedLinkedList<T>");
        this.AddGenericsFullName("Tinyhand.KeyValueList<TKey, TValue>");
    }

    public bool IsCoderOrFormatterAvailable(WithNullable<TinyhandObject> withNullable)
    {
        if (withNullable.Object.Kind == VisceralObjectKind.Error)
        {// Error type is tentatively treated as serializable.
            return true;
        }

        if (this.stringToCoder.ContainsKey(withNullable.FullNameWithNullable))
        {// Found
            return true;
        }

        // Several generic types which have formatters but not coders.
        if (withNullable.Object.Array_Rank >= 1 && withNullable.Object.Array_Rank <= 4)
        {// Array 1-4
            var elementWithNullable = withNullable.Array_ElementWithNullable;
            if (elementWithNullable != null)
            {
                return CoderResolver.Instance.IsCoderOrFormatterAvailable(elementWithNullable);
            }
        }
        else if (withNullable.Object.Generics_Kind == VisceralGenericsKind.ClosedGeneric && withNullable.Object.OriginalDefinition is { } baseObject)
        {// Generics
            var arguments = withNullable.Generics_ArgumentsWithNullable;
            if (this.genericsType.Contains(baseObject.FullName))
            {
                goto Check_GenericsArguments;
            }
            else if (withNullable.Object.SimpleName == "Tuple")
            {// Tuple
                goto Check_GenericsArguments;
            }
            else if (withNullable.Object.SimpleName == "ValueTuple")
            {// ValueTuple
                goto Check_GenericsArguments;
            }

            // Not supported generics type.
            return false;

Check_GenericsArguments:
            if (arguments.Length == 0)
            {// Unknown error.
                return false;
            }

            foreach (var x in arguments)
            {// Check all the arguments.
                if (!CoderResolver.Instance.IsCoderOrFormatterAvailable(x))
                {
                    return false;
                }
            }

            return true;
        }

        return false;
    }

    public ITinyhandCoder? TryGetCoder(WithNullable<TinyhandObject> withNullable)
    {
        this.stringToCoder.TryGetValue(withNullable.FullNameWithNullable, out var value);
        if (value != null)
        {
            return value;
        }

        if (this.IsCoderOrFormatterAvailable(withNullable))
        {
            value = this.AddFormatter(withNullable);
        }

        return value;
    }

    public ITinyhandCoder AddFormatter(string fullNameWithNullable, bool nonNullableReference = false)
    {
        if (!this.stringToCoder.TryGetValue(fullNameWithNullable, out var coder))
        {
            coder = new FormatterCoder(fullNameWithNullable, nonNullableReference);
            this.stringToCoder[fullNameWithNullable] = coder;
        }

        return coder;
    }

    public ITinyhandCoder AddFormatterForReferenceType(string fullNameWithNullable)
    {
        var coder = this.AddFormatter(fullNameWithNullable, true);
        this.AddFormatter(fullNameWithNullable + "?");
        return coder;
    }

    public void AddGenericsType(Type genericsType)
    {
        this.genericsType.Add(VisceralHelper.TypeToFullName(genericsType));
    }

    public void AddGenericsFullName(string fullName)
    {
        this.genericsType.Add(fullName);
        this.genericsType.Add(fullName + "?");
    }

    public ITinyhandCoder? AddFormatter(Type type)
    {
        var fullName = VisceralHelper.TypeToFullName(type);

        if (type.IsValueType)
        {// Value type
            return this.AddFormatter(fullName);
        }
        else
        {// Reference type
            var coder = this.AddFormatter(fullName, true);
            this.AddFormatter(fullName + "?");
            return coder;
        }
    }

    public ITinyhandCoder? AddFormatter(WithNullable<TinyhandObject> withNullable)
    {
        if (!withNullable.Object.Kind.IsType())
        {
            return null;
        }

        if (withNullable.Object.Kind.IsReferenceType())
        {// Reference type
            var fullName = withNullable.FullNameWithNullable.TrimEnd('?');
            var c = this.AddFormatter(fullName, true); // T (non-nullable)
            var c2 = this.AddFormatter(fullName + "?"); // T?

            if (withNullable.Nullable == NullableAnnotation.NotAnnotated)
            {// T
                return c;
            }
            else
            {// T?, None
                return c2;
            }
        }
        else
        {// Value type
            return this.AddFormatter(withNullable.FullNameWithNullable); // T
        }
    }

    public void AddFormatter(VisceralObjectKind kind, string typeName)
    {
        if (!kind.IsType())
        {
            return;
        }

        if (kind.IsReferenceType())
        {// Reference type
            var fullName = typeName.TrimEnd('?');
            this.AddFormatter(fullName, true); // T (non-nullable)
            this.AddFormatter(fullName + "?"); // T?
        }
        else
        {// Value type
            this.AddFormatter(typeName); // T
        }
    }

    private Dictionary<string, ITinyhandCoder> stringToCoder = new();

    private HashSet<string> genericsType = new();
}

internal class FormatterCoder : ITinyhandCoder
{
    public FormatterCoder(string fullNameWithNullable, bool nonNullableReference)
    {
        this.FullNameWithNullable = fullNameWithNullable;
        this.NonNullableReference = nonNullableReference;
    }

    public string FullNameWithNullable { get; }

    public bool NonNullableReference { get; }

    public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
    {
        ssb.AppendLine($"options.Resolver.GetFormatter<{this.FullNameWithNullable}>().Serialize(ref writer, {ssb.FullObject}, options);");
    }

    public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
    {
        if (!this.NonNullableReference)
        {// Value type or Nullable reference type
            ssb.AppendLine($"{ssb.FullObject} = options.Resolver.GetFormatter<{this.FullNameWithNullable}>().Deserialize(ref reader, options);");
        }
        else
        {// Non-nullable reference type
            ssb.AppendLine($"{ssb.FullObject} = options.DeserializeAndReconstruct<{this.FullNameWithNullable}>(ref reader);");
        }
    }

    public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
    {
        ssb.AppendLine($"{ssb.FullObject} = options.Resolver.GetFormatter<{this.FullNameWithNullable}>().Reconstruct(options);");
    }

    public void CodeClone(ScopingStringBuilder ssb, GeneratorInformation info, string sourceObject)
    {
        ssb.AppendLine($"{ssb.FullObject} = options.Resolver.GetFormatter<{this.FullNameWithNullable}>().Clone({sourceObject}, options)!;");
    }
}
