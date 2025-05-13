using System;
using System.Diagnostics.CodeAnalysis;
using Tinyhand;
using Tinyhand.IO;
using ValueLink;

namespace Playground;

[TinyhandObject]
public partial class UnsafeConstructorTestClass2<T>
{
    internal UnsafeConstructorTestClass2(int x)
    {
    }

    [Key(0)]
    private T Value { get; set; } = default!;
}

[TinyhandObject]
public partial class UnsafeConstructorTestClass3 : UnsafeConstructorTestClass2<int>
{
    private UnsafeConstructorTestClass3()
        : base(1)
    {
    }
}

public abstract class BaseClass
{
}

public partial class BaseClass2 : ITinyhandSerializable<BaseClass2>
{
    [IgnoreMember]
    [Reconstruct(true)]
    public BaseClass BaseClass { get; protected set; } = default!;

    public static void Deserialize(ref TinyhandReader reader, scoped ref BaseClass2? value, TinyhandSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    public static void Serialize(ref TinyhandWriter writer, scoped ref BaseClass2? value, TinyhandSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}

[TinyhandObject]
public partial class DerivedClass : BaseClass2
{
}

[ValueLinkObject]
[TinyhandObject]
public partial class TestClass1
{
    [Key(0)]
    [Link(Primary = true, Type = ChainType.Ordered)]
    public int X { get; set; } = 1;
}

[TinyhandObject]
public partial class TestClass
{
    [Key(4)]
    public ExternalClass ExternalClass { get; set; } = new();

    [Key(5)]
    private readonly TestClass1.GoshujinClass c1 = new();
    // private readonly UnsafeConstructorTestClass2<int> class2 = new(1);
}

[TinyhandObject(External = true)]
public partial class ExternalClass : ITinyhandSerializable<ExternalClass>, ITinyhandReconstructable<ExternalClass>, ITinyhandCloneable<ExternalClass>
{
    static ExternalClass? ITinyhandCloneable<ExternalClass>.Clone(scoped ref ExternalClass? value, TinyhandSerializerOptions options)
    {
        return default;
    }

    static void ITinyhandSerializable<ExternalClass>.Deserialize(ref TinyhandReader reader, scoped ref ExternalClass? value, TinyhandSerializerOptions options)
    {
    }

    static void ITinyhandReconstructable<ExternalClass>.Reconstruct([NotNull] scoped ref ExternalClass? value, TinyhandSerializerOptions options)
    {
        value = default!;
    }

    static void ITinyhandSerializable<ExternalClass>.Serialize(ref TinyhandWriter writer, scoped ref ExternalClass? value, TinyhandSerializerOptions options)
    {
    }
}

internal class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");

        var test = new TestClass();
        var bin = TinyhandSerializer.Serialize(test);
    }
}
