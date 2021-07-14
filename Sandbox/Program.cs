using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq.Expressions;
using CrossLink;
using DryIoc;
using Tinyhand;

namespace Sandbox
{
    [TinyhandObject]
    public abstract class AbstractTestBase<TIdentifier>
        where TIdentifier : notnull
    {
        [Key(0)]
        public int Number { get; set; }

        [Key(1)]
        protected TIdentifier Identifier { get; set; } = default!;
    }

    [TinyhandObject]
    public abstract class AbstractTestBase2<TIdentifier, TState> : AbstractTestBase<TIdentifier>
        where TIdentifier : notnull
        where TState : struct
    {
        [Key(2)]
        public string Text { get; set; } = default!;
    }

    [TinyhandObject]
    public partial class AbstractTestClass : AbstractTestBase2<int, long>
    {
    }

    [TinyhandObject]
    public partial class InternalTestClass2 : ConsoleApp1.InternalTestClass
    {
        [Key(2)]
        internal int InternalInt2 = 3;
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Sandbox");
            Console.WriteLine();

            var a = new ConsoleApp1.InternalTestClass();
            var a2 = TinyhandSerializer.Deserialize<ConsoleApp1.InternalTestClass>(TinyhandSerializer.Serialize(a));

            var b = new InternalTestClass2();
            var b2 = TinyhandSerializer.Deserialize<InternalTestClass2>(TinyhandSerializer.Serialize(a));

            var c = new AbstractTestClass();
            var c2 = TinyhandSerializer.Deserialize<AbstractTestClass>(TinyhandSerializer.Serialize(c));
        }
    }
}
