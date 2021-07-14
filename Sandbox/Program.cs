using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using CrossLink;
using DryIoc;
using Tinyhand;

namespace Sandbox
{
    [TinyhandObject]
    public partial class InternalTestBase
    {
        [Key(0)]
        internal int InternalInt = 4;

        [Key(1)]
        private int PrivateInt = 4;

        public InternalTestBase()
        {
        }
    }

    [TinyhandObject]
    public partial class InternalTestClass2 : ConsoleApp1.InternalTestClass
    {
        [Key(2)]
        internal int InternalInt2 = 4;

        public InternalTestClass2()
        {
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Sandbox");
            Console.WriteLine();

            var t = typeof(InternalTestClass2);
            var array = t.GetAllMembers(true).ToArray();
            var a = new ConsoleApp1.InternalTestClass();
            var a2 = TinyhandSerializer.Deserialize<ConsoleApp1.InternalTestClass>(TinyhandSerializer.Serialize(a));

            var b = new InternalTestClass2();
            var b2 = TinyhandSerializer.Deserialize<InternalTestClass2>(TinyhandSerializer.Serialize(b));
            b.Clear();
            var b3 = TinyhandSerializer.Deserialize<InternalTestClass2>(TinyhandSerializer.Serialize(b));
        }
    }
}
