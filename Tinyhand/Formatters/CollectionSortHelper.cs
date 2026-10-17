// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;

namespace Tinyhand.Formatters;

internal readonly struct CollectionSortEntry<T>
{
    internal readonly T Value;
    internal readonly int Index;

    internal CollectionSortEntry(T value, int index)
    {
        this.Value = value;
        this.Index = index;
    }
}

internal static class CollectionSortHelper
{
    internal static void Sort<TKey, T>(CollectionSortEntry<T>[] entries, Func<T, TKey> keySelector, IComparer<TKey> comparer, bool arcOrder)
    {
        // Arc's default search also supports a runtime IComparable<TKey> when the declared key type does not.
        var runtimeComparable = arcOrder && ReferenceEquals(comparer, Comparer<TKey>.Default) &&
            !typeof(TKey).IsValueType && !typeof(IComparable<TKey>).IsAssignableFrom(typeof(TKey));
        try
        {
            Array.Sort(entries, (x, y) =>
            {
                var left = keySelector(x.Value);
                var right = keySelector(y.Value);
                var comparison = runtimeComparable && left is IComparable<TKey> comparable
                    ? (right is null ? 1 : comparable.CompareTo(right))
                    : comparer.Compare(left, right);
                return comparison != 0 ? comparison : x.Index.CompareTo(y.Index);
            });
        }
        catch (InvalidOperationException exception) when (exception.InnerException is not null)
        {
            // Preserve the exception exposed by an individual collection insertion.
            ExceptionDispatchInfo.Throw(exception.InnerException);
        }
    }
}
