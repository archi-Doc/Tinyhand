// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using Tinyhand;

namespace Tinyhand.Tests;

public partial class ObjectCache<TKey, TObject>
    where TKey : IEquatable<TKey>
{
    [TinyhandObject]
    public partial class TestClass
    {
    }

    [TinyhandObject]
    private partial class Item
    {
        public Item()
        {
        }

        public Item(TKey key, TObject obj)
        {
            this.Key = key;
            this.Object = obj;
        }

#pragma warning disable SA1401 // Fields should be private
        [Key(0)]
        internal TKey Key = default!;

        [Key(1)]
        internal TObject Object = default!;
#pragma warning restore SA1401 // Fields should be private
    }

    public ObjectCache(uint cacheSize)
    {
    }
}
