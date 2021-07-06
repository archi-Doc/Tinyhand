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
    // [TinyhandUnionTo(0, typeof(AbstractTestBase<int>), typeof(AbstractTestClass))]
    // [TinyhandUnionTo(1, typeof(ConsoleApp1.IUnionTestInterface), typeof(ConsoleApp1.UnionTestClassA))]
    public partial class AbstractTestClass : AbstractTestBase2<int, long>
    {
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Sandbox");
            Console.WriteLine();

            var c = new AbstractTestClass();
            var c2 = TinyhandSerializer.Deserialize<AbstractTestClass>(TinyhandSerializer.Serialize(c));
        }
    }
}
