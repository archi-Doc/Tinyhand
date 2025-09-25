using System;
using CrystalData;
using Tinyhand;
using Tinyhand.IO;
using ValueLink;

namespace Playground;

[TinyhandObject(Structual = true)]
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

[TinyhandObject(Structual = true)]
public partial class StructualClass
{
    [Key(0)]
    public int A { get; set; }

    [Key(1)]
    public StructualClass? B { get; set; }

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
