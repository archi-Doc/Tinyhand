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
    public partial class InternalTestBase<T>
    {
        [Key(0)]
        internal int InternalInt = 0;

        [Key(1)]
        private int PrivateInt = 1;

        [Key(2)]
        public int PublicPublic { get; set; } = 2;

        [Key(3)]
        public int PublicProtected { get; protected set; } = 3;

        [Key(4)]
        protected int PrivateProtected { private get; set; } = 4;

        [Key(5)]
        public int PublicPrivate { get; private set; } = 5;

        [Key(6)]
        private T PrivatePrivate { get; set; } = default!;


        public InternalTestBase()
        {
        }

        public void Clear()
        {
            this.InternalInt = 0;
            this.PrivateInt = 0;
            this.PublicPublic = 0;
            this.PublicProtected = 0;
            this.PrivateProtected = 0;
            this.PublicPrivate = 0;
            this.PrivatePrivate = default!;
        }
    }

    [TinyhandObject]
    public partial class InternalTestClass2<T> : InternalTestBase<T> // ConsoleApp1.InternalTestClass
    {
        [Key(7)]
        internal int InternalInt2 = 3;

        [Key(8)]
        private int PrivateInt2 = 4;

        public InternalTestClass2()
        {
        }

        public void Clear2()
        {
            this.InternalInt2 = 0;
            this.PrivateInt2 = 0;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Sandbox");
            Console.WriteLine();

            /*var t = typeof(InternalTestBase);
            var field = t.GetField("PrivateInt", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var targetExp = Expression.Parameter(t);
            var valueExp = Expression.Parameter(typeof(int));
            var fieldExp = Expression.Field(targetExp, field);
            var assignExp = Expression.Assign(fieldExp, valueExp);
            var setter = Expression.Lambda<Action<InternalTestClass2, int>>(assignExp, targetExp, valueExp).Compile();*/

            /*var t = typeof(InternalTestBase);
            var field = t.GetField("PrivateInt", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
            var exp = Expression.Parameter(t);
            var exp2 = Expression.Parameter(typeof(int));
            var setter = Expression.Lambda<Action<InternalTestClass2, int>>(Expression.Assign(Expression.Field(exp, "PrivateInt"), exp2), exp, exp2).Compile();

            var subject = new InternalTestClass2();
            setter(subject, 333);

            var getter = Expression.Lambda<Func<InternalTestClass2, int>>(Expression.Field(exp, field), exp).Compile();

            var aa = getter(subject);*/

            var a = new ConsoleApp1.InternalTestClass();
            var a2 = TinyhandSerializer.Deserialize<ConsoleApp1.InternalTestClass>(TinyhandSerializer.Serialize(a));

            var b = new InternalTestClass2<double>();
            var b2 = TinyhandSerializer.Deserialize<InternalTestClass2<double>>(TinyhandSerializer.Serialize(b));
            b.Clear();
            var b3 = TinyhandSerializer.Deserialize<InternalTestClass2<double>>(TinyhandSerializer.Serialize(b));
        }
    }
}
