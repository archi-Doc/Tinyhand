// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Tinyhand.Formatters;

/// <summary>
/// Provides general helpers for creating collections (including dictionaries).
/// </summary>
/// <typeparam name="TCollection">The concrete type of collection to create.</typeparam>
/// <typeparam name="TEqualityComparer">The type of equality comparer that we would hope to pass into the collection's constructor.</typeparam>
internal static class CollectionHelpers<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TCollection, TEqualityComparer>
    where TCollection : new()
{
    /// <summary>
    /// The constructor accepting capacity and an equality comparer, if available.
    /// </summary>
    private static readonly ConstructorInfo? CollectionConstructor = typeof(TCollection).GetConstructor(new Type[] { typeof(int), typeof(TEqualityComparer) });

    /// <summary>
    /// Initializes a new instance of the <typeparamref name="TCollection"/> collection.
    /// </summary>
    /// <param name="count">The number of elements the collection should be prepared to receive.</param>
    /// <param name="equalityComparer">The equality comparer to initialize the collection with.</param>
    /// <returns>The newly initialized collection.</returns>
    /// <remarks>
    /// Use of the <paramref name="count"/> and <paramref name="equalityComparer"/> are a best effort.
    /// If we can't find a constructor on the collection in the expected shape, we'll just instantiate the collection with its default constructor.
    /// </remarks>
    internal static TCollection CreateHashCollection(int count, TEqualityComparer equalityComparer)
    {
        if (CollectionConstructor is null)
        {
            return new TCollection();
        }

        // Preserve constructor exceptions without wrapping them in TargetInvocationException.
        return (TCollection)CollectionConstructor.Invoke(BindingFlags.DoNotWrapExceptions, binder: null, parameters: new object?[] { count, equalityComparer }, culture: null);
    }
}
