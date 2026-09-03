// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Generic;

namespace Tinyhand;

/// <summary>
/// Stores ordered key-value pairs and serializes them as a Tinyhand map.
/// </summary>
/// <typeparam name="TKey">The type of the key. </typeparam>
/// <typeparam name="TValue">The type of the value.</typeparam>
public class KeyValueList<TKey, TValue> : List<KeyValuePair<TKey, TValue>>
{
    public KeyValueList()
        : base()
    {
    }

    public KeyValueList(int capacity)
        : base(capacity)
    {
    }

    public KeyValueList(IEnumerable<KeyValuePair<TKey, TValue>> collection)
        : base(collection)
    {
    }
}
