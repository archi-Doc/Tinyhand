// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

#pragma warning disable SA1401 // Fields should be private
#pragma warning disable SA1125

namespace Tinyhand.Tests;

/*[TinyhandObject]
public partial class NoDefaultConstructorClass : NoDefaultConstructorClass2
{
    public NoDefaultConstructorClass(int x)
        : base()
    {
    }
}

public partial class NoDefaultConstructorClass2
{
    public NoDefaultConstructorClass2()
    {
    }
}*/

public enum ByteEnum : byte { A, B, C, D, E }

public enum SByteEnum : sbyte { A, B, C, D, E }

public enum ShortEnum : short { A, B, C, D, E }

public enum UShortEnum : ushort { A, B, C, D, E }

public enum IntEnum : int { A, B, C, D, E }

public enum UIntEnum : uint { A, B, C, D, E }

public enum LongEnum : long { A, B, C, D, E }

public enum ULongEnum : ulong { A, B, C, D, E }

[TinyhandObject(ImplicitKeyAsName = true)]
[MessagePack.MessagePackObject(true)]
public partial class FormatterResolverClass
{
    public FormatterResolverClass()
    {
        this.SortedDictionaryIntByte.Add(4, 2);
        this.SortedDictionaryIntByte.Add(3, 44);
        this.SortedListIntString.Add(1, "t");
        this.SortedListIntString.Add(10, "tes");

        this.ImmutableDictionaryIntInt = this.ImmutableDictionaryIntInt.Add(1, 10);
        this.ImmutableDictionaryIntInt = this.ImmutableDictionaryIntInt.Add(2, 22);
        this.ImmutableSortedDictionaryIntString = this.ImmutableSortedDictionaryIntString.Add(3, "3");
        this.ImmutableSortedDictionaryIntString = this.ImmutableSortedDictionaryIntString.Add(34, "34");

        this.ReadOnlyObservableCollectionInt = new ReadOnlyObservableCollection<int>(this.ObservableCollectionInt);
        this.ReadOnlyDictionaryIntString = new ReadOnlyDictionary<int, string>(this.DictionaryIntString);
        this.IReadOnlyDictionaryIntString = this.ReadOnlyDictionaryIntString;
        this.IImmutableListString = this.ImmutableListString;
        this.IImmutableDictionaryIntInt = this.ImmutableDictionaryIntInt;
        this.IImmutableQueueInt = this.ImmutableQueueInt;
        this.IImmutableSetInt = this.ImmutableHashSetInt;
        this.IImmutableStackShort = this.ImmutableStackShort;
    }

    public decimal Decimal = 123M;
    public TimeSpan TimeSpan = new(4, 5, 6);
    public DateTimeOffset DateTimeOffset = DateTimeOffset.Now;
    public Guid Guid = new();
    // public Uri Uri = new("https://google.com");
    public Version Version = new(10, 23);
    public StringBuilder StringBuilder = new("test sb");
    public System.Collections.BitArray BitArray = new(new bool[] { true, false, true });
    public System.Numerics.BigInteger BigInteger = new(456d);
    public System.Numerics.Complex Complex = new(11d, 44d);
    public Type Type = typeof(string);

    public object[] ObjectArray { get; set; } = { 1, 2, "Test", 1.4d };
    public List<object> ObjectList { get; set; } = new() { 1, 2, "Test", 1.4d };
    public Memory<byte> MemoryByte { get; set; } = new(new byte[] { 1, 10, 20, });
    public ReadOnlyMemory<byte> ReadOnlyMemoryByte { get; set; } = new(new byte[] { 1, 10, 20, });
    public ReadOnlySequence<byte> ReadOnlySequenceByte { get; set; } = new(new byte[] { 1, 10, 20, });
    public ArraySegment<byte> ArraySegmentByte { get; set; } = new(new byte[] { 11, 12, 201, });

    public Nullable<int> NullableInt { get; set; } = null!;
    public Nullable<int> NullableInt2 { get; set; } = 123;
    public KeyValuePair<int, string> KeyValuePair { get; set; } = new(23, "tes");
    public KeyValuePair<int, string>? KeyValuePair2 { get; set; } = new(231, "test");
    public ArraySegment<long> ArraySegmentLong { get; set; } = new(new long[] { 2, 4, 2000, });
    public Memory<string> MemoryString { get; set; } = new(new string[] { "a", "test", "", });
    public ReadOnlyMemory<int> ReadOnlyMemoryInt { get; set; } = new(new int[] { -3, 0, 123 });
    public ReadOnlySequence<string> ReadOnlySequenceString { get; set; } = new(new string[] { "b", "fu", "br", });
    public List<int> IntList { get; set; } = new() { 1, 2, -100, };
    public LinkedList<short> LinkedListShort { get; set; } = new(new short[] { -30, 11, 30 });
    public Queue<byte> QueueByte { get; set; } = new(new byte[] { 3, 5, 44 });
    public Stack<ushort> StackUShort { get; set; } = new(new ushort[] { 33, 444, 555, });
    public HashSet<int> HashSetInt { get; set; } = new();
    public ReadOnlyCollection<int> ReadOnlyCollectionInt { get; set; } = new(new int[] { -44, 0, 334 });
    public IList<double> IListDouble { get; set; } = new List<double>(new double[] { 0d, 33d, -3330d, });
    public ICollection<sbyte> ICollectionSByte { get; set; } = new Collection<sbyte>(new sbyte[] { 4, 6, 7 });
    public IEnumerable<int> IEnumerableInt { get; set; } = new int[] { 1, 2, -100, };
    public Dictionary<int, string> DictionaryIntString { get; set; } = new(new KeyValuePair<int, string>[] { new KeyValuePair<int, string>(33, "rr") });
    public IDictionary<string, double> IDictionaryStringDouble { get; set; } = new Dictionary<string, double>(new KeyValuePair<string, double>[] { new KeyValuePair<string, double>("test", 33d) });
    public SortedDictionary<int, byte> SortedDictionaryIntByte { get; set; } = new();
    public SortedList<int, string> SortedListIntString { get; set; } = new();
    public ILookup<bool, int> ILookupBoolInt { get; set; } = (ILookup<bool, int>)Enumerable.Range(1, 100).ToLookup(x => x % 2 == 0);
    // public IGrouping<int, string>
    public ObservableCollection<int> ObservableCollectionInt { get; set; } = new(new int[] { -444, 0, 334 });
    public ReadOnlyObservableCollection<int> ReadOnlyObservableCollectionInt { get; set; } = default!;
    public IReadOnlyList<int> IReadOnlyListInt { get; set; } = new[] { 4, 2, 4, };
    public IReadOnlyCollection<string> IReadOnlyCollectionString { get; set; } = new[] { "4", "tes", "to" };
    public ISet<int> ISetInt { get; set; } = new HashSet<int>(new int[] { -444,  });
    public System.Collections.Concurrent.ConcurrentBag<int> ConcurrentBag { get; set; } = new();
    public System.Collections.Concurrent.ConcurrentQueue<double> ConcurrentQueueDouble { get; set; } = new(new[] { 3d, 55d, -331d });
    public System.Collections.Concurrent.ConcurrentStack<string> ConcurrentStackString { get; set; } = new(new[] { "tes", "44", "fin" });
    public ReadOnlyDictionary<int, string> ReadOnlyDictionaryIntString { get; set; } = default!;
    public IReadOnlyDictionary<int, string> IReadOnlyDictionaryIntString { get; set; }
    public System.Collections.Concurrent.ConcurrentDictionary<int, long> ConcurrentDictionaryIntLong { get; set; } = new();
    public Lazy<int> LazyInt { get; set; } = new(() => 4);
    public ImmutableArray<int> ImmutableArrayLong { get; set; } = ImmutableArray.Create<int>(new[] { 1, 2, -100, });
    public ImmutableList<string> ImmutableListString { get; set; } = ImmutableList.Create<string>(new[] { "a", "k", "test", });
    public ImmutableDictionary<int, int> ImmutableDictionaryIntInt { get; set; } = ImmutableDictionary.Create<int, int>();
    public ImmutableHashSet<int> ImmutableHashSetInt { get; set; } = ImmutableHashSet.Create<int>();
    public ImmutableSortedDictionary<int, string> ImmutableSortedDictionaryIntString { get; set; } = ImmutableSortedDictionary.Create<int, string>();
    public ImmutableSortedSet<int> ImmutableSortedSetInt { get; set; } = ImmutableSortedSet.Create<int>(new[] { 1, 2, -100, });
    public ImmutableQueue<int> ImmutableQueueInt { get; set; } = ImmutableQueue.Create<int>(new[] { 1, 2, -100, });
    public ImmutableQueue<int>? ImmutableQueueInt2 { get; set; } = ImmutableQueue.Create<int>(new[] { 11, 12, -1100, });
    public ImmutableStack<short> ImmutableStackShort { get; set; } = ImmutableStack.Create<short>(new short[] { 1, 2, -100, });
    public IImmutableList<string> IImmutableListString { get; set; }
    public IImmutableDictionary<int, int> IImmutableDictionaryIntInt { get; set; }
    public IImmutableQueue<int> IImmutableQueueInt { get; set; }
    public IImmutableSet<int> IImmutableSetInt { get; set; }
    public IImmutableStack<short> IImmutableStackShort { get; set; }
}

[TinyhandObject]
public partial class SimpleIntKeyData
{
    [Key(0)]
    ////[MessagePackFormatter(typeof(OreOreFormatter))]
    public int Prop1 { get; set; }

    [Key(1)]
    public ByteEnum Prop2 { get; set; }

    [Key(2)]
    public string? Prop3 { get; set; }

    [Key(3)]
    public SimpleStringKeyData? Prop4 { get; set; }

    [Key(4)]
    public SimpleStructIntKeyData Prop5 { get; set; }

    [Key(5)]
    public SimpleStructStringKeyData Prop6 { get; set; }

    [Key(6)]
    public byte[]? BytesSpecial { get; set; }

    ////[Key(7)]
    ////[MessagePackFormatter(typeof(OreOreFormatter2), 100, "hogehoge")]
    ////[MessagePackFormatter(typeof(OreOreFormatter))]
    ////public int Prop7 { get; set; }
}

[TinyhandObject(ImplicitKeyAsName = true)]
public partial class SimpleStringKeyData
{
    public int Prop1 { get; set; }

    public ByteEnum Prop2 { get; set; }

    public int Prop3 { get; set; }
}

[TinyhandObject]
public partial struct SimpleStructIntKeyData
{
    [Key(0)]
    public int X { get; set; }

    [Key(1)]
    public int Y { get; set; }

    [Key(2)]
    public byte[] BytesSpecial { get; set; }
}

[TinyhandObject]
public partial struct SimpleStructStringKeyData
{
    [Key("key-X")]
    public int X { get; set; }

    [Key("key-Y")]
    public int[] Y { get; set; }
}

[TinyhandObject]
public partial struct Vector2
{
    [Key(0)]
    public float X;
    [Key(1)]
    public float Y;

    public Vector2(float x, float y)
    {
        this.X = x;
        this.Y = y;
    }
}

[TinyhandObject]
[MessagePack.MessagePackObject]
public partial class EmptyClass
{
}

[TinyhandObject(ImplicitKeyAsName = true)]
[MessagePack.MessagePackObject(true)]
public partial class EmptyClass2
{
}

[TinyhandObject]
[MessagePack.MessagePackObject]
public partial struct EmptyStruct
{
}

[TinyhandObject(ImplicitKeyAsName = true)]
[MessagePack.MessagePackObject(true)]
public partial struct EmptyStruct2
{
}

[TinyhandObject]
public partial class Version1
{
    [Key(3)]
    public int MyProperty1 { get; set; }

    [Key(4)]
    public int MyProperty2 { get; set; }

    [Key(5)]
    public int MyProperty3 { get; set; }
}

[TinyhandObject]
public partial class Version2
{
    [Key(3)]
    public int MyProperty1 { get; set; }

    [Key(4)]
    public int MyProperty2 { get; set; }

    [Key(5)]
    public int MyProperty3 { get; set; }

    // [Key(6)]
    // public int MyProperty4 { get; set; }
    [Key(7)]
    public int MyProperty5 { get; set; }
}

[TinyhandObject]
public partial class Version0
{
    [Key(3)]
    public int MyProperty1 { get; set; }
}

[TinyhandObject]
public partial class HolderV1
{
    [Key(0)]
    public Version1 MyProperty1 { get; set; }

    [Key(1)]
    public int After { get; set; }
}

[TinyhandObject]
public partial class HolderV2
{
    [Key(0)]
    public Version2 MyProperty1 { get; set; }

    [Key(1)]
    public int After { get; set; }
}

[TinyhandObject]
public partial class HolderV0
{
    [Key(0)]
    public Version0 MyProperty1 { get; set; }

    [Key(1)]
    public int After { get; set; }
}

[TinyhandObject]
public partial class Callback1 : ITinyhandSerializationCallback
{
    [Key(0)]
    public int X { get; set; }

    [IgnoreMember]
    public bool CalledBefore { get; private set; }

    [IgnoreMember]
    public bool CalledAfter { get; private set; }

    public Callback1(int x)
    {
    }

    public Callback1()
    {
    }

    public void OnBeforeSerialize()
    {
        this.CalledBefore = true;
    }

    public void OnAfterDeserialize()
    {
        this.CalledAfter = true;
    }
}

[TinyhandObject]
public partial class Callback1_2 : ITinyhandSerializationCallback
{
    [Key(0)]
    public int X { get; set; }

    [IgnoreMember]
    public bool CalledBefore { get; private set; }

    [IgnoreMember]
    public bool CalledAfter { get; private set; }

    public Callback1_2(int x)
    {
        this.X = x;
    }

    public Callback1_2()
    {
    }

    void ITinyhandSerializationCallback.OnBeforeSerialize()
    {
        this.CalledBefore = true;
    }

    void ITinyhandSerializationCallback.OnAfterDeserialize()
    {
        this.CalledAfter = true;
    }
}

[TinyhandObject(ImplicitKeyAsName = true)]
public partial struct Callback2 : ITinyhandSerializationCallback
{
    public static bool CalledAfter = false;

    public int X { get; set; }

    private Action onBefore;
    private Action onAfter;

    public Callback2(int x)
        : this(x, () => { }, () => { })
    {
    }

    public Callback2(int x, Action onBefore, Action onAfter)
    {
        this.X = x;
        this.onBefore = onBefore;
        this.onAfter = onAfter;
    }

    public void OnBeforeSerialize()
    {
        this.onBefore();
    }

    public void OnAfterDeserialize()
    {
        CalledAfter = true;
    }
}

[TinyhandObject(ImplicitKeyAsName = true)]
public partial struct Callback2_2 : ITinyhandSerializationCallback
{
    public int X { get; set; }

    public static bool CalledAfter = false;

    public Callback2_2(int x)
        : this(x, () => { }, () => { })
    {
    }

    private Action onBefore;
    private Action onAfter;

    public Callback2_2(int x, Action onBefore, Action onAfter)
    {
        this.X = x;
        this.onBefore = onBefore;
        this.onAfter = onAfter;
    }

    void ITinyhandSerializationCallback.OnBeforeSerialize()
    {
        this.onBefore();
    }

    void ITinyhandSerializationCallback.OnAfterDeserialize()
    {
        CalledAfter = true;
    }
}

[TinyhandObject]
public partial class MyClass
{
    [Key(0)]
    public int MyProperty1 { get; set; }

    [Key(1)]
    public int MyProperty2 { get; set; }

    [Key(2)]
    public int MyProperty3 { get; set; }
}

[TinyhandObject]
public partial class Empty1
{
}

[TinyhandObject(ImplicitKeyAsName = true)]
public partial class Empty2
{
}

[TinyhandObject]
public partial class NonEmpty1
{
    [Key(0)]
    public int MyProperty { get; set; }
}

[TinyhandObject(ImplicitKeyAsName = true)]
public partial class NonEmpty2
{
    public int MyProperty { get; set; }
}

[TinyhandObject]
public partial class GenericClass<T1, T2>
{
    [Key(0)]
    public T1 MyProperty0 { get; set; }

    [Key(1)]
    public T2 MyProperty1 { get; set; }
}

[TinyhandObject]
public partial struct GenericStruct<T1, T2>
{
    [Key(0)]
    public T1 MyProperty0 { get; set; }

    [Key(1)]
    public T2 MyProperty1 { get; set; }
}

[TinyhandObject]
public partial class VersionBlockTest
{
    [Key(0)]
    public int MyProperty { get; set; }

    [Key(1)]
    public MyClass UnknownBlock { get; set; }

    [Key(2)]
    public int MyProperty2 { get; set; }
}

[TinyhandObject]
public partial class UnVersionBlockTest
{
    [Key(0)]
    public int MyProperty { get; set; }

    ////[Key(1)]
    ////public MyClass UnknownBlock { get; set; }

    [Key(2)]
    public int MyProperty2 { get; set; }
}

public partial class NestParent
{
    [TinyhandObject]
    public partial class NestContract
    {
        [Key(0)]
        public int MyProperty { get; set; }
    }

    public class NestContractless
    {
        public int MyProperty { get; set; }
    }
}

[TinyhandObject]
public partial class WithIndexer
{
    [Key(0)]
    public int Data1 { get; set; }

    [Key(1)]
    public string Data2 { get; set; }

    // [Key(2)]
    // public int this[int i] => 0;
}

public partial class WithIndexerContractless
{
    public int Data1 { get; set; }

    public string Data2 { get; set; }

    public int this[int i] => 0;
}

[TinyhandObject]
[MessagePack.MessagePackObject]
public partial class PrimitiveIntKeyClass
{
    [Key(0)]
    [MessagePack.Key(0)]
    public bool BoolField = true;

    [Key(1)]
    [MessagePack.Key(1)]
    public bool BoolProperty { get; set; } = false;

    [Key(2)]
    [MessagePack.Key(2)]
    public byte ByteField = 10;

    [Key(3)]
    [MessagePack.Key(3)]
    public byte ByteProperty { get; set; } = 20;

    [Key(4)]
    [MessagePack.Key(4)]
    public sbyte SByteField = -11;

    [Key(5)]
    [MessagePack.Key(5)]
    public sbyte SByteProperty { get; set; } = 22;

    [Key(6)]
    [MessagePack.Key(6)]
    public short ShortField = -100;

    [Key(7)]
    [MessagePack.Key(7)]
    public short ShortProperty { get; set; } = 220;

    [Key(8)]
    [MessagePack.Key(8)]
    public ushort UShortField = 110;

    [Key(9)]
    [MessagePack.Key(9)]
    public ushort UShortProperty { get; set; } = 222;

    [Key(10)]
    [MessagePack.Key(10)]
    public int IntField = 1100;

    [Key(11)]
    [MessagePack.Key(11)]
    public int IntProperty { get; set; } = -2200;

    [Key(12)]
    [MessagePack.Key(12)]
    public uint UIntField = 11001;

    [Key(13)]
    [MessagePack.Key(13)]
    public uint UIntProperty { get; set; } = 22001;

    [Key(14)]
    [MessagePack.Key(14)]
    public long LongField = -1111222;

    [Key(15)]
    [MessagePack.Key(15)]
    public long LongProperty { get; set; } = 211112222;

    [Key(16)]
    [MessagePack.Key(16)]
    public ulong ULongField = 110011222;

    [Key(17)]
    [MessagePack.Key(17)]
    public ulong ULongProperty { get; set; } = 1100112221;

    [Key(18)]
    [MessagePack.Key(18)]
    public float FloatField = 1.1f;

    [Key(19)]
    [MessagePack.Key(19)]
    public float FloatProperty { get; set; } = 22f;

    [Key(20)]
    [MessagePack.Key(20)]
    public double DoubleField = 11d;

    [Key(21)]
    [MessagePack.Key(21)]
    public double DoubleProperty { get; set; } = 22d;

    [Key(22)]
    [MessagePack.Key(22)]
    public decimal DecimalField = 111m;

    [Key(23)]
    [MessagePack.Key(23)]
    public decimal DecimalProperty { get; set; } = 222m;

    [Key(24)]
    [MessagePack.Key(24)]
    public string StringField = "test";

    [Key(25)]
    [MessagePack.Key(25)]
    public string StringProperty { get; set; } = "head";

    [Key(26)]
    [MessagePack.Key(26)]
    public char CharField = 'c';

    [Key(27)]
    [MessagePack.Key(27)]
    public char CharProperty { get; set; } = '@';

    [Key(28)]
    [MessagePack.Key(28)]
    public DateTime DateTimeField = DateTime.UtcNow;

    [Key(29)]
    [MessagePack.Key(29)]
    public DateTime DateTimeProperty { get; set; } = DateTime.UtcNow;
}

[TinyhandObject(ImplicitKeyAsName = true)]
[MessagePack.MessagePackObject(true)]
public partial class PrimitiveStringKeyClass
{
    public bool BoolField = true;

    public bool BoolProperty { get; set; } = false;

    public byte ByteField = 10;

    public byte ByteProperty { get; set; } = 20;

    public sbyte SByteField = -11;

    public sbyte SByteProperty { get; set; } = 22;

    public short ShortField = -100;

    public short ShortProperty { get; set; } = 220;

    public ushort UShortField = 110;

    public ushort UShortProperty { get; set; } = 222;

    public int IntField = 1100;

    public int IntProperty { get; set; } = -2200;

    public uint UIntField = 11001;

    public uint UIntProperty { get; set; } = 22001;

    public long LongField = -1111222;

    public long LongProperty { get; set; } = 211112222;

    public ulong ULongField = 110011222;

    public ulong ULongProperty { get; set; } = 1100112221;

    public float FloatField = 1.1f;

    public float FloatProperty { get; set; } = 22f;

    public double DoubleField = 11d;

    public double DoubleProperty { get; set; } = 22d;

    public decimal DecimalField = 111m;

    public decimal DecimalProperty { get; set; } = 222m;

    public string StringField = "test";

    public string StringProperty { get; set; } = "head";

    public char CharField = 'c';

    public char CharProperty { get; set; } = '@';

    public DateTime DateTimeField = DateTime.UtcNow;

    public DateTime DateTimeProperty { get; set; } = DateTime.UtcNow;
}

[TinyhandObject(ImplicitKeyAsName = true)]
[MessagePack.MessagePackObject(true)]
public partial class PrimitiveArrayClass
{
    public bool[] BoolArray { get; set; } = { true, false, true, };

    public byte[] ByteArray { get; set; } = { 1, 10, 200, };

    public sbyte[] SByteArray { get; set; } = { -10, 10, 100, };

    public short[] ShortArray { get; set; } = { -200, 123, 300, };

    public ushort[] UShortrray { get; set; } = { 10, 123, 456, };

    public int[] IntArray { get; set; } = { -100, 12, 5956, };

    public uint[] UIntArray { get; set; } = { 0, 33, 333, };

    public long[] LongArray { get; set; } = { -444, 55, 666, 5, };

    public ulong[] ULongArray { get; set; } = { 444, 5555, 6666, };

    public float[] FloatArray { get; set; } = { -1f, 213f, 456789f, };

    public double[] DoubleArray { get; set; } = { -100d, 0d, 123456d, 456789d, };

    public decimal[] DecimalArray { get; set; } = { -144m, 456m, 78998m, };

    public string[] StringArray { get; set; } = { string.Empty, "test", "head", };

    public char[] CharArray { get; set; } = { 't', 'h', 'e', };

    public DateTime[] DateTimeArray { get; set; } = { DateTime.UtcNow, DateTime.UtcNow, };
}

[TinyhandObject(ImplicitKeyAsName = true)]
[MessagePack.MessagePackObject(true)]
public partial class PrimitiveNullableArrayClass
{
    public bool?[] BoolArray { get; set; } = { true, false, true, null, };

    public byte?[] ByteArray { get; set; } = { 1, null, 10, 200, };

    public sbyte?[] SByteArray { get; set; } = { null, -10, 10, 100, };

    public short?[] ShortArray { get; set; } = { -200, null, 123, 300, };

    public ushort?[] UShortrray { get; set; } = { 10, 123, 456, null, };

    public int?[] IntArray { get; set; } = { -100, 12, null, 5956, };

    public uint?[] UIntArray { get; set; } = { 0, 33, null, 333, };

    public long?[] LongArray { get; set; } = { -444, 55, 666, null, 5, };

    public ulong?[] ULongArray { get; set; } = { 444, null, 5555, 6666, };

    public float?[] FloatArray { get; set; } = { null, -1f, 213f, 456789f, };

    public double?[] DoubleArray { get; set; } = { -100d, 0d, 123456d, 456789d, null, };

    public decimal?[] DecimalArray { get; set; } = { -144m, 456m, null, 78998m, };

    public string?[] StringArray { get; set; } = { string.Empty, "test", "head", null, };

    public char?[] CharArray { get; set; } = { 't', null, 'h', 'e', };

    public DateTime?[] DateTimeArray { get; set; } = { DateTime.UtcNow, null, DateTime.UtcNow, };
}

[TinyhandObject(ImplicitKeyAsName = true)]
[MessagePack.MessagePackObject(true)]
public partial class PrimitiveNullableArrayClass2
{
    public bool?[]? BoolArray { get; set; } = { true, false, true, null, };

    public byte?[]? ByteArray { get; set; } = { 1, null, 10, 200, };

    public sbyte?[]? SByteArray { get; set; } = { null, -10, 10, 100, };

    public short?[]? ShortArray { get; set; } = { -200, null, 123, 300, };

    public ushort?[]? UShortrray { get; set; } = { 10, 123, 456, null, };

    public int?[]? IntArray { get; set; } = { -100, 12, null, 5956, };

    public uint?[]? UIntArray { get; set; } = { 0, 33, null, 333, };

    public long?[]? LongArray { get; set; } = { -444, 55, 666, null, 5, };

    public ulong?[]? ULongArray { get; set; } = { 444, null, 5555, 6666, };

    public float?[]? FloatArray { get; set; } = { null, -1f, 213f, 456789f, };

    public double?[]? DoubleArray { get; set; } = { -100d, 0d, 123456d, 456789d, null, };

    public decimal?[]? DecimalArray { get; set; } = { -144m, 456m, null, 78998m, };

    public string?[]? StringArray { get; set; } = { string.Empty, "test", "head", null, };

    public char?[]? CharArray { get; set; } = { 't', null, 'h', 'e', };

    public DateTime?[]? DateTimeArray { get; set; } = { DateTime.UtcNow, null, DateTime.UtcNow, };
}
