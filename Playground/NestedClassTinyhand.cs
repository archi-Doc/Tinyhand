using System.Collections.Generic;
using System.Threading;
using Tinyhand;
using ValueLink;

namespace Playground.NestedClass;

public sealed partial class NestedClassA
{
    [TinyhandObject(LockObject = "lockObject")]
    private sealed partial class Data
    {
        [TinyhandObject]
        [ValueLinkObject]
        private sealed partial class NestedClassB
        {
            public NestedClassB()
            {
            }

            [Key(0)]
            [Link(Unique = true, Primary = true, Type = ChainType.Unordered)]
            public uint Plane { get; private set; }
        }

        #region FieldAndProperty

        private readonly Lock lockObject = new();

        [Key(0)]
        private readonly HashSet<ulong> previouslyStoredIdentifiers = new();

        [Key(1)]
        private readonly NestedClassB.GoshujinClass planeItems = new();

        #endregion
    }
}
