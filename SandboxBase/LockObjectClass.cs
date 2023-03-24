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
