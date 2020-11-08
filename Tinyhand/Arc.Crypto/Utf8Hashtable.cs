// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

#pragma warning disable SA1202 // Elements should be ordered by access
#pragma warning disable SA1401 // Fields should be private
#pragma warning disable SA1649 // File name should match first type name

namespace Arc.Crypto
{
    public class Utf8Hashtable<TValue>
    { // HashTable for UTF-8 (ReadOnlySpan<byte>).
        private readonly object cs = new object();

        private Item?[] hashTable;
        private uint numberOfItems;

        public Utf8Hashtable(uint capacity = 4)
        {
            var size = CalculateCapacity(capacity);
            this.hashTable = new Item[size];
        }

        private void RebuildTable()
        {
            var nextCapacity = this.hashTable.Length * 2;
            var nextTable = new Item[nextCapacity];
            for (int i = 0; i < this.hashTable.Length; i++)
            {
                var e = this.hashTable[i];
                while (e != null)
                {
                    var newItem = new Item(e.Key, e.Value, e.Hash);
                    this.AddItem(nextTable, newItem);
                    e = e.Next;
                }
            }

            // replace field(threadsafe for read)
            System.Threading.Volatile.Write(ref this.hashTable, nextTable);
        }

        public void Clear()
        {
            lock (this.cs)
            {
                for (var n = 0; n < this.hashTable.Length; n++)
                {
                    System.Threading.Volatile.Write(ref this.hashTable[n], null);
                }
            }
        }

        private bool AddItem(Item[] table, Item item)
        { // lock(cs) required.
            var h = item.Hash & (table.Length - 1);

            if (table[h] == null)
            {
                System.Threading.Volatile.Write(ref table[h], item);
            }
            else
            {
                var i = table[h];
                while (true)
                {
                    if (i.Key.SequenceEqual(item.Key) == true)
                    {// Identical
                        return false;
                    }

                    if (i.Next == null)
                    { // Last item.
                        break;
                    }

                    i = i.Next;
                }

                System.Threading.Volatile.Write(ref i.Next, item);
            }

            return true;
        }

        public bool TryAdd(ReadOnlySpan<byte> key, TValue value)
        {
            lock (this.cs)
            {
                bool successAdd;

                if ((this.numberOfItems * 2) > this.hashTable.Length)
                {// rehash
                    this.RebuildTable();
                }

                // add entry(insert last is thread safe for read)
                successAdd = this.AddKeyValue(key, value);

                if (successAdd)
                {
                    this.numberOfItems++;
                }

                return successAdd;
            }
        }

        private bool AddKeyValue(ReadOnlySpan<byte> key, TValue value)
        { // lock(cs) required.
            var table = this.hashTable;
            var hash = unchecked((int)Arc.Crypto.FarmHash.Hash64(key));
            var h = hash & (table.Length - 1);

            if (table[h] == null)
            {
                var item = new Item(key.ToArray(), value, hash);
                System.Threading.Volatile.Write(ref table[h], item);
            }
            else
            {
                var i = table[h]!;
                while (true)
                {
                    if (key.SequenceEqual(i.Key) == true)
                    {// Identical
                        return false;
                    }

                    if (i.Next == null)
                    { // Last item.
                        break;
                    }

                    i = i.Next;
                }

                var item = new Item(key.ToArray(), value, hash);
                System.Threading.Volatile.Write(ref i.Next, item);
            }

            return true;
        }

        public bool TryAdd(byte[] key, TValue value)
        {
            lock (this.cs)
            {
                bool successAdd;

                if ((this.numberOfItems * 2) > this.hashTable.Length)
                {// rehash
                    this.RebuildTable();
                }

                // add entry(insert last is thread safe for read)
                successAdd = this.AddKeyValue(key, value);

                if (successAdd)
                {
                    this.numberOfItems++;
                }

                return successAdd;
            }
        }

        private bool AddKeyValue(byte[] key, TValue value)
        { // lock(cs) required.
            var table = this.hashTable;
            var hash = unchecked((int)Arc.Crypto.FarmHash.Hash64(key));
            var h = hash & (table.Length - 1);

            if (table[h] == null)
            {
                var item = new Item(key, value, hash);
                System.Threading.Volatile.Write(ref table[h], item);
            }
            else
            {
                var i = table[h]!;
                while (true)
                {
                    if (key.SequenceEqual(i.Key) == true)
                    {// Identical
                        return false;
                    }

                    if (i.Next == null)
                    { // Last item.
                        break;
                    }

                    i = i.Next;
                }

                var item = new Item(key, value, hash);
                System.Threading.Volatile.Write(ref i.Next, item);
            }

            return true;
        }

        public bool TryGetValue(ReadOnlySpan<byte> key, [MaybeNullWhen(false)] out TValue value)
        {
            var table = this.hashTable;
            var hash = unchecked((int)Arc.Crypto.FarmHash.Hash64(key));
            var item = table[hash & (table.Length - 1)];

            while (item != null)
            {
                if (key.SequenceEqual(item.Key) == true)
                { // Identical. alternative: (key == item.Key).
                    value = item.Value;
                    return true;
                }

                item = item.Next;
            }

            value = default;
            return false;
        }

        private static uint CalculateCapacity(uint collectionSize)
        {
            collectionSize *= 2;
            uint capacity = 1;
            while (capacity < collectionSize)
            {
                capacity <<= 1;
            }

            if (capacity < 8)
            {
                return 8;
            }

            return capacity;
        }

        internal class Item
        {
            public byte[] Key;
            public TValue Value;
            public int Hash;
            public Item? Next;

            public Item(byte[] key, TValue value, int hash)
            {
                this.Key = key;
                this.Value = value;
                this.Hash = hash;
            }
        }
    }
}
