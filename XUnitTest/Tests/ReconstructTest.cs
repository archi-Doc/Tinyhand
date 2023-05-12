// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.ComponentModel;
using Xunit;

namespace Tinyhand.Tests;

[TinyhandObject(ImplicitKeyAsName = true)]
public partial class ReconstructTestClass
{
    [DefaultValue(12)]
    public int Int { get; set; } // 12

    public EmptyClass EmptyClass { get; set; } // new()

    [Reconstruct(false)]
    public EmptyClass EmptyClassOff { get; set; } // null

    public EmptyClass? EmptyClass2 { get; set; } // null

    [Reconstruct(true)]
    public EmptyClass? EmptyClassOn { get; set; } // new()

    /* Error. A class to be reconstructed must have a default constructor.
    [IgnoreMember]
    [Reconstruct(true)]
    public ClassWithoutDefaultConstructor WithoutClass { get; set; }*/

    [IgnoreMember]
    [Reconstruct(true)]
    public ClassWithDefaultConstructor WithClass { get; set; }
}

public class ClassWithoutDefaultConstructor
{
    public string Name = string.Empty;

    public ClassWithoutDefaultConstructor(string name)
    {
        this.Name = name;
    }
}

public class ClassWithDefaultConstructor
{
    public string Name = string.Empty;

    public ClassWithDefaultConstructor(string name)
    {
        this.Name = name;
    }

    public ClassWithDefaultConstructor()
        : this(string.Empty)
    {
    }
}

public class ReconstructTest
{
    [Fact]
    public void Test1()
    {
        var t = TinyhandSerializer.Reconstruct<ReconstructTestClass>();

        t.Int.Is(12);
        t.EmptyClass.IsNotNull();
        t.EmptyClassOff.IsNull();
        t.EmptyClass2.IsNull();
        t.EmptyClassOn.IsNotNull();
    }
}
