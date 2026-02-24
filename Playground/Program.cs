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

public partial class DualTestClass3
{
}

internal class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");

        var tc = new DualTestClass2();
        var tc2 = new DualTestClass3();
        var r = TinyhandTypeIdentifier.TrySerialize(tc);
        r = TinyhandTypeIdentifier.TrySerialize(new DualTestClass3());

        var b = TinyhandTypeIdentifier.IsRegistered<DualTestClass2>();
        b = TinyhandTypeIdentifier.IsRegistered<DualTestClass3>();
        b = TinyhandTypeIdentifier.IsRegistered(typeof(DualTestClass2));
        b = TinyhandTypeIdentifier.IsRegistered(typeof(DualTestClass3));

        var r2 = TinyhandTypeIdentifier.TrySerializeToString(tc);
        r2 = TinyhandTypeIdentifier.TrySerializeToString(tc);
        r2 = TinyhandTypeIdentifier.TrySerializeToString(tc2);

        var r3 = TinyhandTypeIdentifier.TryDeserializeFromString(TinyhandTypeIdentifier.GetTypeIdentifier<DualTestClass3>(), "");
        r3 = TinyhandTypeIdentifier.TryDeserializeFromString(TinyhandTypeIdentifier.GetTypeIdentifier<DualTestClass2>(), "C=abc");
        r3 = TinyhandTypeIdentifier.TryDeserializeFromString(TinyhandTypeIdentifier.GetTypeIdentifier<DualTestClass2>(), "C=null");
    }
}
