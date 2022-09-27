// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;

namespace Tinyhand;

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
