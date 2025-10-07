using System;
using CrystalData;
using Tinyhand;
using Tinyhand.IO;
using ValueLink;

namespace Playground;

[TinyhandObject(AddAlternateKey = true)]
public partial class DualTestClass2
{
    [Key(2)]
    public string C { get; set; } = "Test";
}

[TinyhandObject(AddAlternateKey = true)]
public partial class DualTestClass
{
    [Key(0)]
    public int A { get; set; }

    [Key(1)]
    public string B { get; set; } = "Test";
}

internal class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
    }
}
