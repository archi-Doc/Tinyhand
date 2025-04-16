using System;
using Tinyhand;
using Tinyhand.IO;

namespace Playground;

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

[TinyhandObject]
public partial class TestClass
{
    [Key(0)]
    // [MaxLength(12)]
    public string X0 { get; set; } = string.Empty;

    [Key(1)]
    [MaxLength(12)]
    internal partial string X1 { get; private init; } = string.Empty;

    [IgnoreMember]
    internal partial string X2 { get; private init; } = string.Empty;

    [Key(3)]
    internal partial string X3 { get; init; } = string.Empty;

    internal partial string X2
    {
        get => field;
        private init => field = value;
    }
}

internal class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
    }
}
