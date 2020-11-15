using System;
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
