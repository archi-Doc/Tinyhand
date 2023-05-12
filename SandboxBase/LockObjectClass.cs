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

[TinyhandObject(LockObject = "semaphore")]
public partial class PropertyTestClass
{
    [Key(0, PropertyName = "Id")]
    public int id;

    protected readonly SemaphoreLock semaphore = new();
}
