using Tinyhand;

namespace TestAssembly;

[TinyhandObject]
public partial class TestClass
{
    public TestClass()
    {
    }

    [Key(0)]
    public string Name { get; set; } = string.Empty;

    [Key(1)]
    public int Age { get; private set; }
}
