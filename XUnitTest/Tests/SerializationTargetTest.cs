// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.ComponentModel;
using Xunit;

namespace Tinyhand.Tests;

[TinyhandObject] // Integer key
public partial class SerializationTargetClass1
{
    [Key(0)]
    [DefaultValue(1)]
    public int A = 1; // Serialize

    [IgnoreMember]
    [DefaultValue(1)]
    public int B = 1; // Not

    [DefaultValue(1)]
    private int C = 1; // Not

    public void Test(SerializationTargetClass1 target)
    {
        this.A.Is(target.A);
        this.B.Is(target.B);
        this.C.Is(target.C);
    }
}

[TinyhandObject] // String key
public partial class SerializationTargetClass2
{
    [Key("A")]
    [DefaultValue(1)]
    public int A = 1; // Serialize

    [IgnoreMember]
    [DefaultValue(1)]
    public int B = 1; // Not

    [DefaultValue(1)]
    private int C; // Not

    [KeyAsName]
    [DefaultValue(1)]
    public int D = 1; // Serialize

    public void Test(SerializationTargetClass2 target)
    {
        this.A.Is(target.A);
        this.B.Is(target.B);
        this.C.Is(target.C);
        this.D.Is(target.D);
    }
}

public partial class SerializationGenericClass<T>
{
    public T Value { get; set; }

    [TinyhandObject(ExplicitKeyOnly = true)] // ExplicitKeyOnly
    public partial class TestClass
    {
        [Key(0)]
        [DefaultValue(1)]
        public int A = 1; // Serialize

        public SerializationGenericClass<T> Parent { get; set; }
    }
}

[TinyhandObject(ExplicitKeyOnly = true)] // ExplicitKeyOnly
public partial class SerializationTargetClass3
{
    [Key(0)]
    [DefaultValue(1)]
    public int A = 1; // Serialize

    [DefaultValue(1)]
    public int B = 1; // Not

    [DefaultValue(1)]
    private int C = 1; // Not

    [Key(1)]
    [DefaultValue(1)]
    private int D = 1; // Serialize

    [DefaultValue(1)]
    public int E { get; set; } = 1;

    [DefaultValue(1)]
    public int F { get; private set; } = 1;

    public void Test(SerializationTargetClass3 target)
    {
        this.A.Is(target.A);
        this.B.Is(target.B);
        this.C.Is(target.C);
        this.D.Is(target.D);
        this.E.Is(target.E);
        this.F.Is(target.F);
    }
}

[TinyhandObject(ImplicitKeyAsName = true)] // ImplicitKeyAsName
public partial class SerializationTargetClass4
{
    [Key("A")]
    [DefaultValue(1)]
    public int A = 1; // Serialize

    [DefaultValue(1)]
    public int B = 1; // Serialize

    [KeyAsName]
    [DefaultValue(1)]
    private int C = 1; // Serialize

    [DefaultValue(1)]
    private int D = 1; // Not

    public void Test(SerializationTargetClass4 target)
    {
        this.A.Is(target.A);
        this.B.Is(target.B);
        this.C.Is(target.C);
        this.D.Is(target.D);
    }
}

public class SerializationTargetTest
{
    [Fact]
    public void Test1()
    {
        var c = TinyhandSerializer.Deserialize<SerializationTargetClass1>(TinyhandSerializer.Serialize(TinyhandSerializer.Reconstruct<SerializationTargetClass1>()));
        var target = TinyhandSerializer.Reconstruct<SerializationTargetClass1>();
        c.Test(target);
    }

    [Fact]
    public void Test2()
    {
        var c = TinyhandSerializer.Deserialize<SerializationTargetClass2>(TinyhandSerializer.Serialize(TinyhandSerializer.Reconstruct<SerializationTargetClass2>()));
        var target = TinyhandSerializer.Reconstruct<SerializationTargetClass2>();
        c.Test(target);
    }

    [Fact]
    public void Test3()
    {
        var c = TinyhandSerializer.Deserialize<SerializationTargetClass3>(TinyhandSerializer.Serialize(TinyhandSerializer.Reconstruct<SerializationTargetClass3>()));
        var target = TinyhandSerializer.Reconstruct<SerializationTargetClass3>();
        c.Test(target);
    }

    [Fact]
    public void Test4()
    {
        var c = TinyhandSerializer.Deserialize<SerializationTargetClass4>(TinyhandSerializer.Serialize(TinyhandSerializer.Reconstruct<SerializationTargetClass4>()));
        var target = TinyhandSerializer.Reconstruct<SerializationTargetClass4>();
        c.Test(target);
    }
}
