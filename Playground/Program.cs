using System;
using Tinyhand;

namespace Playground;

[TinyhandObject]
public partial class TestClass
{
    [Key(0)]
    public int X0 { get; set; }

    [Key(1)]
    public partial int X1 { get; private set; }
}

public partial class TestClass
{
    public partial int X1
    {
        get => field;
        private set => field = value;
    }
}

internal class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
    }
}
