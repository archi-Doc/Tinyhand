using System;
using Arc.Collections;
using Tinyhand;
using Tinyhand.IO;
using ValueLink;

namespace Playground;

[TinyhandObject]
public partial class TestClass
{
    public TestClass()
    {
    }

    [Key(0)]
    public string Name { get; set; } = string.Empty;
}

[TinyhandObject]
[ValueLinkObject(Integrality = true)]
public partial class GenericIntegralityClass2<T>
    where T : ITinyhandSerialize<T>
{
    [Link(Primary = true, Unique = true, Type = ChainType.Unordered, TargetMember = "Id2")]
    public GenericIntegralityClass2()
    {
    }

    public int Id2 => this.id2;

    [Key(1)]
    public T Value { get; set; } = default!;

    [Key(0)]
    private int id2;

    [Key(2, AddProperty = "Name")]
    private string name = string.Empty;//
}

[TinyhandObject]
public partial class GenericTestClass3 : GenericTestClass2<TestClass>
{
}

[TinyhandObject]
public partial class GenericTestClass2<T>
    where T : ITinyhandSerialize<T>
{
    public GenericTestClass2()
    {
    }

    [Key(0)]
    public char Id { get; private set; }

    [Key(1)]
    public T Value { get; set; } = default!;
}


internal class Program
{
    static void Main(string[] args)
    {
        using (var writer = new TinyhandWriter(BytePool.Default.Rent(10)))
        {
            writer.WriteInt8(123);
            var array = writer.FlushAndGetArray();
            var memoryOwner = writer.FlushAndGetRentMemory();
            memoryOwner.Return();
        }

        var ts = System.Diagnostics.Stopwatch.GetTimestamp();

        Console.WriteLine("Hello, World!");
    }
}
