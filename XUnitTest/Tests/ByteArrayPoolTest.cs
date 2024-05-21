// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Text;
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

        using (var w = new TinyhandWriter(initialBuffer))
        {
            w.WriteInt16(1234);
            destination = w.FlushAndGetArray();
        }

        var r = new TinyhandReader(destination);
        r.ReadInt16().Is((short)1234);
        r.End.IsTrue();
    }
}
