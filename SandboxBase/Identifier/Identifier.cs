// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Tinyhand;

namespace LP;

/// <summary>
/// Immutable identifier of objects in LP.
/// </summary>
[TinyhandObject]
public partial class Identifier : IEquatable<Identifier>
{
    public const string Name = "Identifier";

    public static Identifier Zero { get; } = new();

    public static Identifier One { get; } = new(1);

    public static Identifier Two { get; } = new(2);

    public static Identifier Three { get; } = new(3);

    public Identifier()
    {
    }

    public Identifier(int id0)
    {
        this.Id0 = (ulong)id0;
    }

    public Identifier(ulong id0)
    {
        this.Id0 = id0;
    }

    public Identifier(ulong id0, ulong id1, ulong id2, ulong id3)
    {
        this.Id0 = id0;
        this.Id1 = id1;
        this.Id2 = id2;
        this.Id3 = id3;
    }

    public Identifier((ulong Id0, ulong Id1, ulong Id2, ulong Id3) id)
    {
        this.Id0 = id.Id0;
        this.Id1 = id.Id1;
        this.Id2 = id.Id2;
        this.Id3 = id.Id3;
    }

    public Identifier(Identifier identifier)
    {
        this.Id0 = identifier.Id0;
        this.Id1 = identifier.Id1;
        this.Id2 = identifier.Id2;
        this.Id3 = identifier.Id3;
    }


    [Key(0)]
    public ulong Id0 { get; private set; }

    [Key(1)]
    public ulong Id1 { get; private set; }

    [Key(2)]
    public ulong Id2 { get; private set; }

    [Key(3)]
    public ulong Id3 { get; private set; }

    public bool Equals(Identifier? other)
    {
        if (other == null)
        {
            return false;
        }

        return this.Id0 == other.Id0 && this.Id1 == other.Id1 && this.Id2 == other.Id2 && this.Id3 == other.Id3;
    }

    public override int GetHashCode() => (int)this.Id0; // HashCode.Combine(this.Id0, this.Id1, this.Id2, this.Id3);

    public override string ToString() => this.Id0 switch
    {
        0 => $"{Name} Zero",
        1 => $"{Name} One",
        2 => $"{Name} Two",
        3 => $"{Name} Three",
        _ => $"{Name} {this.Id0:D4}",
    };
}
