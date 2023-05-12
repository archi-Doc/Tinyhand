// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Generic;

namespace Tinyhand;

/// <summary>
/// A collection of key value pairs (List&lt;<see cref="KeyValuePair{TKey, TValue}"/>&gt;).<br/>
/// Represents a <see cref="KeyValuePair{TKey, TValue}"/> as a map structure (key = { value }).
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
