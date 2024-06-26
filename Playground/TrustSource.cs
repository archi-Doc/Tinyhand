// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Collections;
using Tinyhand;
using ValueLink;

namespace Playground;

[TinyhandObject]
public sealed partial class TrustSource<T>
{
    public enum TrustState
    {
        Unfixed,
        Fixed,
    }

    public TrustSource(int capacity, int trustMinimum)
    {
        this.Capacity = capacity;
        this.TrustMinimum = trustMinimum;
        // this.itemPool = new(() => new(), this.Capacity);
        this.counterPool = new(() => new(), this.Capacity);
    }

    public TrustSource()
    {
        // this.itemPool = default!;
        this.counterPool = default!;
    }

    [TinyhandObject]
    [ValueLinkObject]
    private partial class Item
    {
        [Link(Primary = true, Type = ChainType.QueueList, Name = "Queue")]
        public Item()
        {
            this.Value = default!;
        }

        public Item(T value)
        {
            this.Value = value;
        }

        [Key(0)]
        public T Value { get; set; }

        // [Key(1)]
        // public long AddedMics { get; set; }

        public override string ToString()
            => $"Item {this.Value?.ToString()}";
    }

    [TinyhandObject]
    [ValueLinkObject]
    private partial class Counter
    {
        public Counter()
        {
            this.Value = default!;
        }

        public Counter(T value)
        {
            this.Value = value;
        }

        [Key(0)]
        [Link(Primary = true, Type = ChainType.Unordered, AddValue = false)]
        public T Value { get; set; }

        [Key(1)]
        [Link(Type = ChainType.Ordered)]
        public long Count { get; set; }

        public override string ToString()
            => $"{this.Count} x {this.Value?.ToString()}";
    }

    #region FieldAndProperty

    [IgnoreMember]
    public int Capacity { get; private set; }

    [IgnoreMember]
    public int TrustMinimum { get; private set; }

    public bool IsFixed => this.isFixed;

    public T? FixedOrDefault => this.fixedValue;

    private readonly object syncObject = new();
    // private readonly ObjectPool<Item> itemPool;
    private readonly ObjectPool<Counter> counterPool;

    [Key(0)]
    private Item.GoshujinClass items = new(); // lock (this.syncObject)

    [Key(1)]
    private Counter.GoshujinClass counters = new(); // lock (this.syncObject)

    [Key(2)]
    private bool isFixed;

    [Key(3)]
    private T? fixedValue;

    #endregion

    public void Add(T value)
    {
        lock (this.syncObject)
        {
            Counter? counter;
            if (this.counters.ValueChain.TryGetValue(value, out counter))
            {// Increment counter
                counter.CountValue++;
            }
            else
            {// New counter
                counter = this.counterPool.Get();
                counter.Value = value;
                counter.Count = 1;
                counter.Goshujin = this.counters;
            }

            Item item;
            if (this.items.Count >= this.Capacity)
            {
                item = this.items.QueueChain.Dequeue();
                if (this.counters.ValueChain.TryGetValue(item.Value, out var counter2))
                {
                    counter2.CountValue--;
                    if (counter2.CountValue == 0)
                    {// Return counter
                        counter2.Goshujin = null;
                        this.counterPool.Return(counter2);
                    }
                }
            }
            else
            {
                item = new();
            }

            item.Value = value;
            item.Goshujin = this.items;

            if (this.isFixed)
            {// Fixed
                if (this.fixedValue.Equals(value))
                {// Identical
                    return;
                }

                var last = this.counters.CountChain.Last;
                if (last is not null && !last.Value.Equals(this.fixedValue))
                {// Fix -> Unfix
                }
            }
            else
            {// Not fixed
                var last = this.counters.CountChain.Last;
                if (last is not null)
                {// Fix
                    // this.ClearInternal(false);
                    this.isFixed = true;
                    this.fixedValue = last.Value;
                }
            }
        }
    }
}
