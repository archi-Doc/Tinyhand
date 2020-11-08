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

        public void Print()
        {
            Console.WriteLine(x);
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var tc = new TestClass() { x = 10 };
            var b = TinyhandSerializer.Serialize(tc);
            var tc2 = TinyhandSerializer.Deserialize<TestClass>(b);
            tc2.Print();
        }
    }
}
