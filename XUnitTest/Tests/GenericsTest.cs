// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.ComponentModel;
using Xunit;

namespace Tinyhand.Tests;

[TinyhandObject(ImplicitMemberNameAsKey = true)]
public partial class GenericsTestClass<T>
{
    public int Int { get; set; } = 12; // 12

    public T TValue { get; set; } = default!;

    [TinyhandObject]
    public partial class GenericsNestedClass<U>
    {
        [Key(0)]
        public string String { get; set; } = "TH"; // 12

        [Key(1)]
        public U UValue { get; set; } = default!;
    }

    [TinyhandObject]
    public partial class GenericsNestedClass2
    {
        [Key(0)]
        public string String { get; set; } = string.Empty; // 12
    }

    public GenericsNestedClass<double> NestedClass { get; set; } = new();

    public GenericsNestedClass2 NestedClass2 { get; set; } = new();

    public GenericsTestClass2<int> ClassInt { get; set; } = new();
}

[TinyhandObject(ImplicitMemberNameAsKey = true)]
public partial class GenericsTestClass2<V>
{
    public V VValue { get; set; } = default!;
}

public class GenericsTest
{
    [Fact]
    public void TestReconstruct()
    {
        var t = TinyhandSerializer.Reconstruct<GenericsTestClass<string>>();
        t.Int.Is(12);
        // t.TValue.Is(string.Empty); // Disable closed generic
        t.NestedClass.String.Is("TH");
        t.NestedClass2.String.Is(string.Empty);

        var t2 = TinyhandSerializer.Reconstruct<GenericsTestClass<long>>();
        t2.Int.Is(12);
        t2.TValue.Is(0);
        t2.NestedClass.String.Is("TH");
        t2.NestedClass2.String.Is(string.Empty);
    }

    [Fact]
    public void TestSerialize()
    {
        var t = TinyhandSerializer.Reconstruct<GenericsTestClass<string>>();
        t.Int = 13;
        t.TValue = "ya";
        t.NestedClass.String = "na";
        t.NestedClass.UValue = 1.23d;
        t.NestedClass2.String = "te";
        t.ClassInt.VValue = 23;
        var tt = TestHelper.Convert(t);
        tt.IsStructuralEqual(t);
        tt = (GenericsTestClass<string>)TestHelper.ConvertNonGeneric(t.GetType(), (object)t);
        tt.IsStructuralEqual(t);

        var t2 = TinyhandSerializer.Reconstruct<GenericsTestClass<long>>();
        t2.Int = 13;
        t2.TValue = 789456;
        t2.NestedClass.String = "na";
        t2.NestedClass.UValue = 1.23d;
        t2.NestedClass2.String = "te";
        t2.ClassInt.VValue = 23;
        var tt2 = TestHelper.Convert(t2);
        tt2.IsStructuralEqual(t2);

        tt2 = (GenericsTestClass<long>)TestHelper.ConvertNonGeneric(t2.GetType(), (object)t2);
        tt2.IsStructuralEqual(t2);
    }
}
