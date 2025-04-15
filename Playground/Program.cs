﻿using System;
using Tinyhand;

namespace Playground;

[TinyhandObject]
public partial class TestClass
{
    [Key(0)]
    // [MaxLength(12)]
    public string X0 { get; set; } = string.Empty;

    [Key(1)]
    [MaxLength(12)]
    internal partial string X1 { get; private set; } = string.Empty;

    [IgnoreMember]
    internal partial string X2 { get; init; } = string.Empty;

    internal partial string X2
    {
        get => field;
        init => field = value;
    }
}

internal class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
    }
}
