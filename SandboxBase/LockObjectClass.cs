using Arc.Threading;
using Tinyhand;

namespace SandboxBase;

[TinyhandObject(LockObject = "semaphore")]
public partial class LockObjectClassA
{
    [Key(0)]
    public int X { get; set; }

    protected readonly SemaphoreLock semaphore = new();
}

[TinyhandObject(LockObject = "semaphore", ExplicitKeyOnly = true)]
public partial class PropertyTestClass
{
    [Key(0, PropertyName = "Id")]
    public int id;

    // [IgnoreMember]
    public string Name = string.Empty;

    protected readonly SemaphoreLock semaphore = new();
}
