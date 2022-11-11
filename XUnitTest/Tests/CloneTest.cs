// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tinyhand;
using Xunit;

namespace Tinyhand.Tests;

[TinyhandObject(ImplicitKeyAsName = true)]
public partial class CloneTestClass1
{
    public Memory<byte> MemoryByte { get; set; } = new(new byte[] { 1, 10, 20, });
    public ReadOnlyMemory<byte> ReadOnlyMemoryByte { get; set; } = new(new byte[] { 1, 10, 20, });
    public ReadOnlySequence<byte> ReadOnlySequenceByte { get; set; } = new(new byte[] { 1, 10, 20, });
}

[TinyhandObject(ImplicitKeyAsName = true)]
public partial class CloneTestClass2 : ITinyhandClone<CloneTestClass2>
{
    public int X { get; set; }

    public int Y { get; set; } = 10;

    public static CloneTestClass2? Clone(ref CloneTestClass2? value, TinyhandSerializerOptions options)
    {
        return new() { X = 2, };
    }
}

[TinyhandObject(ImplicitKeyAsName = true)]
public partial class CloneTestClass3 : ITinyhandClone<CloneTestClass3>
{
    public int X { get; set; }

    public int Y { get; set; } = 10;

    public static CloneTestClass3? Clone(ref CloneTestClass3? value, TinyhandSerializerOptions options)
    {
        return new() { X = 3, };
    }
}

public class CloneTest
{
    [Fact]
    public void Test1()
    {
        var c1 = new CloneTestClass1();
        var c2 = TinyhandSerializer.Deserialize<CloneTestClass1>(TinyhandSerializer.Serialize(c1));
        var c3 = TinyhandSerializer.Clone(c1);
        // c1.IsStructuralEqual(c2);
    }

    [Fact]
    public void Test2()
    {
        var c1 = new CloneTestClass2();
        c1.X = 1;
        var c2 = TinyhandSerializer.Clone(c1);
        c2.X.Is(2);
        c2.Y.Is(10);
    }

    [Fact]
    public void Test3()
    {
        var c1 = new CloneTestClass3();
        c1.X = 1;
        var c2 = TinyhandSerializer.Clone(c1);
        c2.X.Is(3);
        c2.Y.Is(10);
    }
}
