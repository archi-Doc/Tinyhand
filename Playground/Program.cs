using System;
using Tinyhand;

namespace Playground;

[TinyhandObject]
public partial class TestClass
{
    public TestClass()
    {
    }

    [Key(0)]
    public int Id { get; set; }
}

[TinyhandObject]
public partial class TestClass2
{
    public TestClass2()
    {
    }

    [Key(0)]
    public TestClass Class { get; set; } = default!;

    [Key(1)]
    public TestClass[] Array { get; set; } = default!;
}

internal class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
    }
}
