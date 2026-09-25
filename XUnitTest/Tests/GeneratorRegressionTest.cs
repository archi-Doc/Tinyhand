// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Generic;
using Tinyhand;
using Tinyhand.IO;
using Xunit;
using XUnitTest.Tests.GeneratorRegressionOther;

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1403 // File may only contain a single namespace

namespace XUnitTest.Tests.GeneratorRegressionOther
{
    public enum OtherColor
    {
        Red,
        Green = -3,
    }

    [TinyhandObject]
    public partial class OtherBase
    {
        [Key(0)]
        public int X;
    }

    [TinyhandObject]
    public partial class OtherGenericBase<T>
    {
        [Key(0)]
        public T? X;
    }
}

namespace XUnitTest.Tests
{
    // The helpers for arrays and lists of enums are shared, so the class that writes names is declared first.
    [TinyhandObject(EnumAsString = true)]
    public partial class EnumStringMembers
    {
        [Key(0)]
        public TestEnum? Nullable { get; set; }

        [Key(1)]
        public TestEnum[] Array { get; set; } = [];

        [Key(2)]
        public List<TestEnum> List { get; set; } = new();
    }

    [TinyhandObject]
    public partial class EnumNumberMembers
    {
        [Key(0)]
        public TestEnum? Nullable { get; set; }

        [Key(1)]
        public TestEnum[] Array { get; set; } = [];

        [Key(2)]
        public List<TestEnum> List { get; set; } = new();
    }

    // The initializers use a type imported by a using directive, and literals that need escapes.
    [TinyhandObject(ImplicitMemberNameAsKey = true)]
    public partial class GeneratedLiteralClass
    {
        public OtherColor Color { get; set; } = OtherColor.Green;

        public char Quote { get; set; } = '\'';

        public string Text { get; set; } = "a\"b\u2028c\0";

        public int Negative { get; set; } = -1;
    }

    // Hidden members of base classes in another namespace, including a constructed generic base.
    [TinyhandObject]
    public partial class DerivedFromOtherBase : OtherBase
    {
        [Key(1)]
        public new int X;
    }

    [TinyhandObject]
    public partial class DerivedFromOtherGenericBase : OtherGenericBase<int>
    {
        [Key(1)]
        public new int X;
    }

    [TinyhandUnion("a\"b", typeof(EscapedUnionA))]
    [TinyhandUnion("c\\d", typeof(EscapedUnionB))]
    public partial interface IEscapedUnion
    {
    }

    [TinyhandObject]
    public partial class EscapedUnionA : IEscapedUnion
    {
        [Key(0)]
        public int A { get; set; }
    }

    [TinyhandObject]
    public partial class EscapedUnionB : IEscapedUnion
    {
        [Key(0)]
        public int B { get; set; }
    }

    public class GeneratorRegressionTest
    {
        [Fact]
        public void EnumAsStringNullableAndCollections()
        {
            var s = new EnumStringMembers { Nullable = TestEnum.Test, Array = [TestEnum.Test], List = [TestEnum.Test], };
            var s2 = TinyhandSerializer.Deserialize<EnumStringMembers>(TinyhandSerializer.Serialize(s))!;
            s2.Nullable.Is(TestEnum.Test);
            s2.Array.Is(TestEnum.Test);
            s2.List.Is(TestEnum.Test);

            s.Nullable = null;
            TinyhandSerializer.Deserialize<EnumStringMembers>(TinyhandSerializer.Serialize(s))!.Nullable.IsNull();

            // EnumAsString of another class does not change the format of this one.
            var bytes = TinyhandSerializer.Serialize(new EnumNumberMembers { Nullable = TestEnum.Test, Array = [TestEnum.Test], List = [TestEnum.Test], });
            var reader = new TinyhandReader(bytes);
            reader.ReadArrayHeader().Is(3);
            reader.ReadInt32().Is(1);
            reader.ReadArrayHeader().Is(1);
            reader.ReadInt32().Is(1);
            reader.ReadArrayHeader().Is(1);
            reader.ReadInt32().Is(1);
        }

        [Fact]
        public void DefaultLiterals()
        {
            var c = TinyhandSerializer.Deserialize<GeneratedLiteralClass>(TinyhandSerializer.Serialize(new GeneratedLiteralClass()))!;
            c.Color.Is(OtherColor.Green);
            c.Quote.Is('\'');
            c.Text.Is("a\"b\u2028c\0");
            c.Negative.Is(-1);

            var d = new GeneratedLiteralClass { Color = OtherColor.Red, Quote = 'x', Text = "y", Negative = 2, };
            TinyhandSerializer.Deserialize<GeneratedLiteralClass>(TinyhandSerializer.Serialize(d)).IsStructuralEqual(d);
        }

        [Fact]
        public void HiddenMembersOfBaseClassesInAnotherNamespace()
        {
            var d = new DerivedFromOtherBase { X = 2, };
            ((OtherBase)d).X = 1;
            var d2 = TinyhandSerializer.Deserialize<DerivedFromOtherBase>(TinyhandSerializer.Serialize(d))!;
            ((OtherBase)d2).X.Is(1);
            d2.X.Is(2);

            var g = new DerivedFromOtherGenericBase { X = 2, };
            ((OtherGenericBase<int>)g).X = 1;
            var g2 = TinyhandSerializer.Clone(g)!;
            ((OtherGenericBase<int>)g2).X.Is(1);
            g2.X.Is(2);
        }

        [Fact]
        public void UnionStringKeysAreEscaped()
        {
            IEscapedUnion a = new EscapedUnionA { A = 1, };
            IEscapedUnion b = new EscapedUnionB { B = 2, };
            TinyhandSerializer.Deserialize<IEscapedUnion>(TinyhandSerializer.Serialize(a)).IsInstanceOf<EscapedUnionA>().A.Is(1);
            TinyhandSerializer.Deserialize<IEscapedUnion>(TinyhandSerializer.Serialize(b)).IsInstanceOf<EscapedUnionB>().B.Is(2);
        }
    }
}
