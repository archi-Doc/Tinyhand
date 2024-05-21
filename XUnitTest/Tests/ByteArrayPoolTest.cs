﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Text;
using Arc.Unit;
using Tinyhand;
using Tinyhand.IO;
using Xunit;

namespace XUnitTest.Tests;

public class ByteArrayPoolTest
{
    [Fact]
    public void Test1()
    {
        var initialBuffer = new byte[4];
        byte[] destination;
        byte[] destination2;
        ByteArrayPool.MemoryOwner memoryOwner;

        using (var w = new TinyhandWriter(initialBuffer))
        {
            w.WriteInt16(1234);
            destination = w.FlushAndGetArray();
        }

        var r = new TinyhandReader(destination);
        r.ReadInt16().Is((short)1234);
        r.End.IsTrue();

        using (var w = TinyhandWriter.CreateFromByteArrayPool())
        {
            w.WriteInt16(1234);
            destination2 = w.FlushAndGetArray();
        }

        r = new TinyhandReader(destination);
        r.ReadInt16().Is((short)1234);
        r.End.IsTrue();

        using (var w = TinyhandWriter.CreateFromByteArrayPool())
        {
            w.WriteInt16(1234);
            memoryOwner = w.FlushAndGetMemoryOwner();
        }

        r = new TinyhandReader(memoryOwner.Memory.Span);
        r.ReadInt16().Is((short)1234);
        r.End.IsTrue();
        memoryOwner = memoryOwner.Return();
    }
}
