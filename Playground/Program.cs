using System;
using CrystalData;
using Tinyhand;
using Tinyhand.IO;
using ValueLink;

namespace Playground;

[TinyhandObject(Structural = true)]
[ValueLinkObject]
public partial class ChildClass
{
    [TinyhandObject(External = true)]
    public partial class GoshujinClass
    {
    }

    [Link(Primary = true, Unique = true, Type = ChainType.Unordered)]
    [Key(0)]
    public int Id { get; set; }
}

[TinyhandObject]
public partial class TestClass
{
    [Key("A")]
    public int A { get; set; }

    [Key("B")]
    public int B { get; set; }
}

[TinyhandObject(Structural = true, DualKey = true)]
public partial class StructuralClass
{
    [Key(0, Alternate = "AA")]
    public int A { get; set; }

    [Key(1)]
    public StructuralClass? B { get; set; }

    [Key(2)]
    public ChildClass C { get; set; } = new();

    [Key(3)]
    public ChildClass.GoshujinClass D { get; set; } = new();
}

internal class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
    }
}
