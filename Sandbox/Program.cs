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

        public void Clear()
        {
        }
    }

    [TinyhandObject]
    public partial class InternalTestClass2 : InternalTestBase // ConsoleApp1.InternalTestClass
    {
        [Key(2)]
        internal int InternalInt2 = 4;

        [Key(3)]
        private int PrivateInt2 = 5;

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

            var t = typeof(InternalTestBase);
            var field = t.GetField("PrivateInt", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var targetExp = Expression.Parameter(t);
            var valueExp = Expression.Parameter(typeof(int));
            var fieldExp = Expression.Field(targetExp, field);
            BinaryExpression assignExp = Expression.Assign(fieldExp, valueExp);
            var setter = Expression.Lambda<Action<InternalTestClass2, int>>(assignExp, targetExp, valueExp).Compile();

            var subject = new InternalTestClass2();
            setter(subject, 333);

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
