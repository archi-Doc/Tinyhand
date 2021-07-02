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
    [TinyhandUnion(1, typeof(ComplexTestClass2<int, string>))]
    // [TinyhandUnion(3, typeof(ComplexTestClass3<double>))]
    public abstract partial class ComplexTestBase<T>
    {
        public T BaseT { get; set; } = default!;
    }

    [TinyhandObject(ImplicitKeyAsName = true)]
    public partial class ComplexTestClass : ComplexTestBase<int>
    {
        public string String { get; set; } = default!;
    }

    [TinyhandObject(ImplicitKeyAsName = true)]
    public partial class ComplexTestClass2<T, U> : ComplexTestBase<T>
    {
        public U ClassU { get; set; } = default!;
    }

    [TinyhandObject(ImplicitKeyAsName = true)]
    public abstract class ComplexTestAbstract
    {
    }

    [TinyhandObject(ImplicitKeyAsName = true)]
    [TinyhandUnionTo(2, typeof(ComplexTestBase<>), typeof(ComplexTestClass3<double>))]
    public partial class ComplexTestClass3<U> : ComplexTestBase<int>
    {
        public U ClassU { get; set; } = default!;
    }

    [TinyhandObject(ImplicitKeyAsName = true)]
    [TinyhandUnionTo(4, typeof(ComplexTestBase<>), typeof(ComplexTestClass4))]
    public partial class ComplexTestClass4 : ComplexTestClass
    {
        public double Age { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Sandbox");
            Console.WriteLine();

            // Normal class derived from generic class.
            var c = new ComplexTestClass();
            c.BaseT = 1;
            c.String = "test";
            var b = TinyhandSerializer.Serialize(c);
            var c2 = TinyhandSerializer.Deserialize<ComplexTestClass>(b);

            var c3 = (ComplexTestBase<int>)c;
            b = TinyhandSerializer.Serialize(c3);
            var c4 = TinyhandSerializer.Deserialize<ComplexTestBase<int>>(b);

            // Generic class derived from generic class.
            var d = new ComplexTestClass2<int, string>();
            d.BaseT = 1;
            d.ClassU = "test";
            b = TinyhandSerializer.Serialize(d);
            var d2 = TinyhandSerializer.Deserialize<ComplexTestClass2<int, string>>(b);

            var d3 = (ComplexTestBase<int>)d;
            b = TinyhandSerializer.Serialize(d3);
            var d4 = TinyhandSerializer.Deserialize<ComplexTestBase<int>>(b);

            // Generic class derived from generic class.
            var e = new ComplexTestClass3<double>();
            e.BaseT = 1;
            e.ClassU = 12;
            b = TinyhandSerializer.Serialize(e);
            var e2 = TinyhandSerializer.Deserialize<ComplexTestClass3<double>>(b);

            var e3 = (ComplexTestBase<int>)e;
            b = TinyhandSerializer.Serialize(e3);
            var e4 = TinyhandSerializer.Deserialize<ComplexTestBase<int>>(b);
        }
    }
}
