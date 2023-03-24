// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tinyhand;
using Xunit;

namespace Tinyhand.Tests;

[TinyhandObject(ImplicitKeyAsName = true)]
[MessagePack.MessagePackObject(true)]
public partial class LargeDataClass
{
    public const int M = 456;
    public const int N = 1_234_567;

    public void Prepare()
    {
        this.Int = new int[N];
        for (var n = 0; n < N; n++)
        {
            this.Int[n] = n;
        }

        this.String = new string[M];
        this.String[0] = "test";
        for (var n = 1; n < M; n++)
        {
            this.String[n] = this.String[n - 1] + n.ToString();
        }
    }

    public int[] Int { get; set; } = default!;

    public string[] String { get; set; } = default!;
}

public class LargeDataTest
{
    [Fact]
    public void Test()
    {
        var c = new LargeDataClass();
        c.Prepare();

        TestHelper.TestWithMessagePack(c);
    }
}
