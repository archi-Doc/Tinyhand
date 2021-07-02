using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq.Expressions;
using CrossLink;
using Tinyhand;

namespace Sandbox
{
    [TinyhandObject(ImplicitKeyAsName = true)]
    [TinyhandUnion(0, typeof(ComplexTestClass))]
    public abstract partial class ComplexTestBase<T>
    {
        public T BaseT { get; set; } = default!;
    }

    [TinyhandObject(ImplicitKeyAsName = true)]
    public partial class ComplexTestClass : ComplexTestBase<int>
    {
        public string String { get; set; } = default!;
    }

    /*[TinyhandObject(ImplicitKeyAsName = true)]
    public partial class ComplexTestClass2<T, U> : ComplexTestBase<T>
    {
        public U ClassU { get; set; } = default!;
    }*/

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Sandbox");
            Console.WriteLine();

            var c = new ComplexTestClass();
            c.BaseT = 1;
            c.String = "test";
            var b = TinyhandSerializer.Serialize(c);
            var c2 = TinyhandSerializer.Deserialize<ComplexTestClass>(b);

            var c3 = (ComplexTestBase<int>)c;
            b = TinyhandSerializer.Serialize(c3);
            var c4 = TinyhandSerializer.Deserialize<ComplexTestBase<int>>(b);
        }
    }
}
