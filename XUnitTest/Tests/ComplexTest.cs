using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq.Expressions;
using Tinyhand;
using Xunit;

namespace Tinyhand.Tests
{
    [TinyhandObject(ImplicitKeyAsName = true)]
    [TinyhandUnion<ComplexTestClass>(0)]
    [TinyhandUnion<ComplexTestClass2<int, string>>(1)]
    [TinyhandUnion<ComplexTestClass3<double>>(2)]
    [TinyhandUnion<ComplexTestClass4>(4)]
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
    // [TinyhandUnionTo(2, typeof(ComplexTestBase<>), typeof(ComplexTestClass3<double>))]
    public partial class ComplexTestClass3<U> : ComplexTestBase<int>
    {
        public U ClassU { get; set; } = default!;
    }

    [TinyhandObject(ImplicitKeyAsName = true)]
    // [TinyhandUnionTo(4, typeof(ComplexTestBase<>), typeof(ComplexTestClass4))]
    public partial class ComplexTestClass4 : ComplexTestClass
    {
        public double Age { get; set; }
    }

    [TinyhandObject]
    public partial class InheritanceTestBase
    {
        [Key(0)]
        internal int InternalInt;

        [Key(1)]
        private int PrivateInt;

        [Key(2)]
        public int PublicPublic { get; set; }

        [Key(3)]
        public int PublicProtected { get; protected set; }

        [Key(4)]
        protected int PrivateProtected { private get; set; }

        [Key(5)]
        public int PublicPrivate { get; private set; }

        [Key(6)]
        private int PrivatePrivate { get; set; }

        public InheritanceTestBase()
        {
        }

        public void Set()
        {
            this.InternalInt = 1;
            this.PrivateInt = 2;
            this.PublicPublic = 3;
            this.PublicProtected = 4;
            this.PrivateProtected = 5;
            this.PublicPrivate = 6;
            this.PrivatePrivate = 7;
        }
    }

    [TinyhandObject]
    public partial class InheritanceTestClass : InheritanceTestBase
    {
        [Key(7)]
        internal int InternalInt2;

        [Key(8)]
        private int PrivateInt2;

        [Key(9)]
        public int InitInt { get; init; }

        public InheritanceTestClass(int n = 0)
        {
            this.InitInt = n;
        }

        public void Set2()
        {
            this.InternalInt2 = 8;
            this.PrivateInt2 = 9;
        }
    }

    [TinyhandObject]
    public partial class InheritanceTestBase2<T>
    {
        [Key(0)]
        internal T InternalT;

        [Key(1)]
        private T PrivateT;

        [Key(2)]
        public T PublicPublic { get; set; }

        [Key(3)]
        public T PublicProtected { get; protected set; }

        [Key(4)]
        protected T PrivateProtected { private get; set; }

        [Key(5)]
        public T PublicPrivate { get; private set; }

        [Key(6)]
        private T PrivatePrivate { get; set; }

        public InheritanceTestBase2()
        {
        }

        public void Set(T t)
        {
            this.InternalT = t;
            this.PrivateT = t;
            this.PublicPublic = t;
            this.PublicProtected = t;
            this.PrivateProtected = t;
            this.PublicPrivate = t;
            this.PrivatePrivate = t;
        }
    }

    [TinyhandObject]
    public partial class InheritanceTestClass2<T> : InheritanceTestBase2<T>
    {
        [Key(7)]
        internal T InternalT2;

        [Key(8)]
        private T PrivateT2;

        [Key(9)]
        public T InitT { get; init; }

        public InheritanceTestClass2(T t = default)
        {
            this.InitT = t;
        }

        public void Set2(T t)
        {
            this.InternalT2 = t;
            this.PrivateT2 = t;
        }
    }

    public class ComplexTest
    {
        [Fact]
        public void TestInheritance()
        {
            var c1 = new InheritanceTestBase();
            var c2 = new InheritanceTestBase();
            c2.Set();

            var b = TinyhandSerializer.Serialize(c1);
            var c = TinyhandSerializer.Deserialize<InheritanceTestBase>(b);
            c.IsStructuralEqual(c1);

            b = TinyhandSerializer.Serialize(c2);
            c = TinyhandSerializer.Deserialize<InheritanceTestBase>(b);
            c.IsStructuralEqual(c2);

            var d1 = new InheritanceTestClass(0);
            var d2 = new InheritanceTestClass(10);
            d2.Set();
            d2.Set2();

            b = TinyhandSerializer.Serialize(d1);
            var d = TinyhandSerializer.Deserialize<InheritanceTestClass>(b);
            d.IsStructuralEqual(d1);

            b = TinyhandSerializer.Serialize(d2);
            d = TinyhandSerializer.Deserialize<InheritanceTestClass>(b);
            d.IsStructuralEqual(d2);
        }

        [Fact]
        public void TestInheritance2()
        {
            var c1 = new InheritanceTestBase2<double>();
            var c2 = new InheritanceTestBase2<double>();
            c2.Set(111);

            var b = TinyhandSerializer.Serialize(c1);
            var c = TinyhandSerializer.Deserialize<InheritanceTestBase2<double>>(b);
            c.IsStructuralEqual(c1);

            b = TinyhandSerializer.Serialize(c2);
            c = TinyhandSerializer.Deserialize<InheritanceTestBase2<double>>(b);
            c.IsStructuralEqual(c2);

            var d1 = new InheritanceTestClass2<double>(0);
            var d2 = new InheritanceTestClass2<double>(10);
            d2.Set(22);
            d2.Set2(333);

            b = TinyhandSerializer.Serialize(d1);
            var d = TinyhandSerializer.Deserialize<InheritanceTestClass2<double>>(b);
            d.IsStructuralEqual(d1);

            b = TinyhandSerializer.Serialize(d2);
            d = TinyhandSerializer.Deserialize<InheritanceTestClass2<double>>(b);
            d.IsStructuralEqual(d2);

            var e1 = TinyhandSerializer.Reconstruct<InheritanceTestClass2<string>>();
            var e2 = new InheritanceTestClass2<string>("a");
            e2.Set("22");
            e2.Set2("333");

            b = TinyhandSerializer.Serialize(e1);
            var e = TinyhandSerializer.Deserialize<InheritanceTestClass2<string>>(b);
            e.IsStructuralEqual(e1);

            b = TinyhandSerializer.Serialize(e2);
            e = TinyhandSerializer.Deserialize<InheritanceTestClass2<string>>(b);
            e.IsStructuralEqual(e2);
        }

        [Fact]
        public void Test1()
        {
            // Normal class derived from generic class.
            var c = new ComplexTestClass();
            c.BaseT = 1;
            c.String = "test";
            var b = TinyhandSerializer.Serialize(c);
            var c2 = TinyhandSerializer.Deserialize<ComplexTestClass>(b);
            c.IsStructuralEqual(c2);

            var c3 = (ComplexTestBase<int>)c;
            b = TinyhandSerializer.Serialize(c3);
            var c4 = TinyhandSerializer.Deserialize<ComplexTestBase<int>>(b);
            c.IsStructuralEqual(c4);

            // Generic class derived from generic class.
            var d = new ComplexTestClass2<int, string>();
            d.BaseT = 1;
            d.ClassU = "test";
            b = TinyhandSerializer.Serialize(d);
            var d2 = TinyhandSerializer.Deserialize<ComplexTestClass2<int, string>>(b);
            d.IsStructuralEqual(d2);

            var d3 = (ComplexTestBase<int>)d;
            b = TinyhandSerializer.Serialize(d3);
            var d4 = TinyhandSerializer.Deserialize<ComplexTestBase<int>>(b);
            d.IsStructuralEqual(d4);

            // Generic class derived from generic class.
            var e = new ComplexTestClass3<double>();
            e.BaseT = 1;
            e.ClassU = 12;
            b = TinyhandSerializer.Serialize(e);
            var e2 = TinyhandSerializer.Deserialize<ComplexTestClass3<double>>(b);
            e.IsStructuralEqual(e2);

            var e3 = (ComplexTestBase<int>)e;
            b = TinyhandSerializer.Serialize(e3);
            var e4 = TinyhandSerializer.Deserialize<ComplexTestBase<int>>(b);
            e.IsStructuralEqual(e4);
        }
    }
}
