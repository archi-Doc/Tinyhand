// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Linq;
using Xunit;

namespace Tinyhand.Tests;

[TinyhandObject]
public partial class LevelTestClass
{
    public LevelTestClass()
    {
    }

    public LevelTestClass(int @default, int level0, int level1, int level2)
    {
        this.Default = @default;
        this.Level0 = level0;
        this.Level1 = level1;
        this.Level2 = level2;
    }

    [Key(0)]
    public int Default { get; set; }

    [Key(1, Level = 0)]
    public int Level0 { get; set; }

    [Key(2, Level = 1)]
    public int Level1 { get; set; }

    [Key(3, Level = 2)]
    public int Level2 { get; set; }
}

[TinyhandObject]
public partial class LevelTestClass2
{
    public LevelTestClass2()
    {
        this.Default = new();
        this.Level0 = new();
        this.Level1 = new();
    }

    public LevelTestClass2(int x0, int x1, int x2, int x3, int x4,
        int x5, int x6, int x7, int x8, int x9, int x10, int x11)
    {
        this.Default = new(x0, x1, x2, x3);
        this.Level0 = new(x4, x5, x6, x7);
        this.Level1 = new(x8, x9, x10, x11);
    }

    [Key(0)]
    LevelTestClass Default { get; set; }

    [Key(1, Level = 4)]
    LevelTestClass Level0 { get; set; }

    [Key(2, Level = 8)]
    LevelTestClass Level1 { get; set; }
}

public class LevelTest
{
    [Fact]
    public void Test1()
    {
        LevelTestClass tc;
        LevelTestClass tc2;
        byte[] sig;

        tc = new LevelTestClass(1, 2, 3, 4);
        tc2 = TinyhandSerializer.Deserialize<LevelTestClass>(TinyhandSerializer.Serialize(tc));
        tc2.IsStructuralEqual(new LevelTestClass(1, 2, 3, 4));

        sig = TinyhandSerializer.SerializeSignature(tc, -1);
        sig.SequenceEqual(TinyhandSerializer.SerializeSignature(new LevelTestClass(1, 0, 0, 0), -1)).IsTrue();

        sig = TinyhandSerializer.SerializeSignature(tc, 0);
        sig.SequenceEqual(TinyhandSerializer.SerializeSignature(new LevelTestClass(1, 2, 0, 0), 0)).IsTrue();

        sig = TinyhandSerializer.SerializeSignature(tc, 1);
        sig.SequenceEqual(TinyhandSerializer.SerializeSignature(new LevelTestClass(1, 2, 3, 0), 1)).IsTrue();

        sig = TinyhandSerializer.SerializeSignature(tc, 2);
        sig.SequenceEqual(TinyhandSerializer.SerializeSignature(new LevelTestClass(1, 2, 3, 4), 2)).IsTrue();
    }

    [Fact]
    public void Test2()
    {
        LevelTestClass2 tc;
        LevelTestClass2 tc2;
        byte[] sig;

        tc = new LevelTestClass2(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12);
        tc2 = TinyhandSerializer.Deserialize<LevelTestClass2>(TinyhandSerializer.Serialize(tc));
        tc2.IsStructuralEqual(new LevelTestClass2(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12));

        sig = TinyhandSerializer.SerializeSignature(tc, -1);
        sig.SequenceEqual(TinyhandSerializer.SerializeSignature(new LevelTestClass2(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12), -1)).IsTrue();

        sig = TinyhandSerializer.SerializeSignature(tc, 0);
        sig.SequenceEqual(TinyhandSerializer.SerializeSignature(new LevelTestClass2(1, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0), 0)).IsTrue();

        sig = TinyhandSerializer.SerializeSignature(tc, 1);
        sig.SequenceEqual(TinyhandSerializer.SerializeSignature(new LevelTestClass2(1, 2, 3, 0, 0, 0, 0, 0, 0, 0, 0, 0), 1)).IsTrue();

        sig = TinyhandSerializer.SerializeSignature(tc, 2);
        sig.SequenceEqual(TinyhandSerializer.SerializeSignature(new LevelTestClass2(1, 2, 3, 4, 0, 0, 0, 0, 0, 0, 0, 0), 2)).IsTrue();

        sig = TinyhandSerializer.SerializeSignature(tc, 3);
        sig.SequenceEqual(TinyhandSerializer.SerializeSignature(new LevelTestClass2(1, 2, 3, 4, 5, 0, 0, 0, 0, 0, 0, 0), 3)).IsTrue();

        sig = TinyhandSerializer.SerializeSignature(tc, 4);
        sig.SequenceEqual(TinyhandSerializer.SerializeSignature(new LevelTestClass2(1, 2, 3, 4, 5, 6, 0, 0, 0, 0, 0, 0), 4)).IsTrue();

        sig = TinyhandSerializer.SerializeSignature(tc, 5);
        sig.SequenceEqual(TinyhandSerializer.SerializeSignature(new LevelTestClass2(1, 2, 3, 4, 5, 6, 7, 0, 0, 0, 0, 0), 5)).IsTrue();

        sig = TinyhandSerializer.SerializeSignature(tc, 6);
        sig.SequenceEqual(TinyhandSerializer.SerializeSignature(new LevelTestClass2(1, 2, 3, 4, 5, 6, 7, 8, 0, 0, 0, 0), 6)).IsTrue();

        sig = TinyhandSerializer.SerializeSignature(tc, 7);
        sig.SequenceEqual(TinyhandSerializer.SerializeSignature(new LevelTestClass2(1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 0, 0), 7)).IsTrue();

        sig = TinyhandSerializer.SerializeSignature(tc, 8);
        sig.SequenceEqual(TinyhandSerializer.SerializeSignature(new LevelTestClass2(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 0, 0), 8)).IsTrue();

        sig = TinyhandSerializer.SerializeSignature(tc, 9);
        sig.SequenceEqual(TinyhandSerializer.SerializeSignature(new LevelTestClass2(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 0), 9)).IsTrue();

        sig = TinyhandSerializer.SerializeSignature(tc, 10);
        sig.SequenceEqual(TinyhandSerializer.SerializeSignature(new LevelTestClass2(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12), 10)).IsTrue();
    }
}
