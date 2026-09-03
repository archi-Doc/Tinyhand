// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using Tinyhand.Resolvers;

namespace Tinyhand.Formatters;

internal static class CollectionCloneHelper
{
    internal static T[] Clone<T>(ReadOnlySpan<T> source, TinyhandSerializerOptions options)
    {
        if (source.IsEmpty)
        {
            return Array.Empty<T>();
        }

        var result = new T[source.Length];
        CloneTo(source, result, options.Resolver.GetFormatter<T>(), options);
        return result;
    }

    internal static T[] Clone<T>(in ReadOnlySequence<T> source, TinyhandSerializerOptions options)
    {
        if (source.IsEmpty)
        {
            return Array.Empty<T>();
        }

        var result = new T[checked((int)source.Length)];
        var formatter = options.Resolver.GetFormatter<T>();
        var offset = 0;
        foreach (var segment in source)
        {
            CloneTo(segment.Span, result.AsSpan(offset), formatter, options);
            offset += segment.Length;
        }

        return result;
    }

    private static void CloneTo<T>(ReadOnlySpan<T> source, Span<T> destination, ITinyhandFormatter<T> formatter, TinyhandSerializerOptions options)
    {
        if ((!RuntimeHelpers.IsReferenceOrContainsReferences<T>() || typeof(T) == typeof(string)) &&
            ReferenceEquals(formatter, BuiltinResolver.Instance.TryGetFormatter<T>()))
        {
            source.CopyTo(destination);
            return;
        }

        for (var i = 0; i < source.Length; i++)
        {
            destination[i] = formatter.Clone(source[i], options)!;
        }
    }
}
