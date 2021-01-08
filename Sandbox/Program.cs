using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using Tinyhand;

namespace Sandbox
{
    [TinyhandUnion(0, typeof(UnionTestClassA))]
    [TinyhandUnion(1, typeof(UnionTestClassB))]
    public interface IUnionTestInterface
    {
    }

    [TinyhandObject]
    public partial class UnionTestClassA : IUnionTestInterface
    {
        [Key(0)]
        public int X { get; set; }
    }

    [TinyhandObject]
    public partial class UnionTestClassB : IUnionTestInterface
    {
        [Key(0)]
        public string Name { get; set; } = default!;
    }

    [TinyhandObject]
    public partial class UnionTestClassC
    {
        [Key(0)]
        public IUnionTestInterface IUnion = default!;

        [Key(1)]
        public IUnionTestInterface? IUnionNullable = default!;

        [Key(2)]
        [Reuse(false)]
        public IUnionTestInterface IUnionNoReuse = default!;
    }

    [TinyhandUnion(0, typeof(UnionTestSubA))]
    [TinyhandUnion(1, typeof(UnionTestSubB))]
    public abstract partial class UnionTestBase
    {
        [Key(0)]
        public int ID { get; set; }
    }

    [TinyhandObject]
    public partial class UnionTestSubA : UnionTestBase
    {
        [Key(1)]
        public string Name { get; set; } = default!;
    }

    [TinyhandObject]
    public partial class UnionTestSubB : UnionTestBase
    {
        [Key(1)]
        public double Height { get; set; }
    }

    [TinyhandObject(EnableTextSerialization = true)]
    public partial class TextSerializeClass
    {
        [Key(1)]
        public double Height { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var c = new TextSerializeClass();
            TinyhandSerializer.TextSerialize(c);

            /* var classA = new UnionTestClassA() { X = 10, };
            var classB = new UnionTestClassB() { Name = "test", };

            var b = TinyhandSerializer.Serialize(classA);
            var classA2 = TinyhandSerializer.Deserialize<UnionTestClassA>(b);

            b = TinyhandSerializer.Serialize((IUnionTestInterface)classA);
            var IUnionTest = TinyhandSerializer.Deserialize<IUnionTestInterface>(b);

            b = TinyhandSerializer.Serialize((IUnionTestInterface)classB);
            IUnionTest = TinyhandSerializer.Deserialize<IUnionTestInterface>(b);*/
        }
    }
}
