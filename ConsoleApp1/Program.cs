using System;
using System.Collections.Generic;
using Tinyhand;

namespace ConsoleApp1
{
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
        public int[,,] z = { { { 0 }, { 1 } }, { { 3 }, { 2 } } , { { 10 }, { 11 } }, { { 13 }, { 12 } } };

        [Key(8)]
        public KeyValuePair<int, string> kvp = new KeyValuePair<int, string>(3, "test");

        [Key(9)]
        public Version version = new Version(1, 2);

        [Key(10)]
        public Lazy<string> ls = new Lazy<string>(() => "test2");
 
        public void Print()
        {
            Console.WriteLine(x);
        }
    }

     // [TinyhandGeneratorOption(AttachDebugger = false, GenerateToFile = true)]
    class Program
    {
        static void Main(string[] args)
        {
            var tc = new TestClass() { x = 10 };
            var b = TinyhandSerializer.Serialize(tc);
            var tc2 = TinyhandSerializer.Deserialize<TestClass>(b);
            tc2?.Print();
        }
    }
}
