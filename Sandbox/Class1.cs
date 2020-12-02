using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tinyhand;

/*namespace ConsoleApp1
{
    [TinyhandObject]
    public partial class Callback1_2 : ITinyhandSerializationCallback
    {
        [Key(0)]
        public int X { get; set; }

        [Key(1)]
        public GenericClass<int> GenericInt { get; set; } = default!;

        [IgnoreMember]
        public bool CalledBefore { get; private set; }

        [IgnoreMember]
        public bool CalledAfter { get; private set; }

        [TinyhandObject]
        internal partial class ChildClass
        {
            [Key(0)]
            public string Name { get; set; } = default!;
        }

        public Callback1_2(int x)
        {
            this.X = x;
        }

        public Callback1_2()
        {
        }

        public void OnBeforeSerialize()
        {
            this.CalledBefore = true;
        }

        void ITinyhandSerializationCallback.OnAfterDeserialize()
        {
            this.CalledAfter = true;
        }
    }

    [TinyhandObject]
    public partial class GenericClass<T>
    {
        [Key(0)]
        public T? Member { get; set; }
    }

    [TinyhandObject]
    public partial class TestClass
    {
        [Key(0)]
        public int x;

        [Key(2)]
        public int y;

        [Key(3)]
        public string?[] stringList = default!;

        [Key(4)]
        public string?[]? stringList2 = default!;

        [Key(5)]
        public decimal?[] DecimalArray { get; set; } = { -144m, 456m, null, 78998m, };

        [Key(6)]
        public double?[] DoubleArray { get; set; } = { -100d, 0d, 123456d, 456789d, null, };

        [Key(7)]
        public int[,,] z = { { { 0 }, { 1 } }, { { 3 }, { 2 } }, { { 10 }, { 11 } }, { { 13 }, { 12 } } };

        [Key(8)]
        public KeyValuePair<int, string> kvp = new KeyValuePair<int, string>(3, "test");

        [Key(9)]
        public Version version = new Version(1, 2);

        [Key(10)]
        public Lazy<string> ls = new Lazy<string>(() => "test2");

        [Key(11)]
        public Type type = typeof(int);

        public void Print()
        {
            Console.WriteLine(x);
        }
    }
}
*/
