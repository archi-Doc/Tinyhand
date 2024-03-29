﻿// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace Tinyhand.Tests;

#if !ENABLE_IL2CPP

public class ValueTupleTest
{
    private T Convert<T>(T value)
    {
        return TinyhandSerializer.Deserialize<T>(TinyhandSerializer.Serialize(value));
    }

    public static object[][] ValueTupleData = new object[][]
    {
        new object[] { (1, 2) },
        new object[] { (1, 2, 3) },
        new object[] { (1, 2, 3, 4) },
        new object[] { (1, 2, 3, 4, 5) },
        new object[] { (1, 2, 3, 4, 5, 6) },
        new object[] { (1, 2, 3, 4, 5, 6, 7) },
        new object[] { (1, 2, 3, 4, 5, 6, 7, 8) },
        new object[] { (1, 2, 3, 4, 5, 6, 7, 8, 9) },
        new object[] { (1, 2, 3, 4, 5, 6, 7, 8, 9, 10) },
        new object[] { (1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11) },
        new object[] { (1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12) },
        new object[] { (1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13) },
        new object[] { (1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14) },
        new object[] { (1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15) },
        new object[] { (1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16) },
        new object[] { (1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17) },
        new object[] { (1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18) },
        new object[] { (1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19) },
        new object[] { (1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20) },
    };

    [Theory]
    [MemberData(nameof(ValueTupleData))]
    public void ValueTuple<T>(T x)
    {
        this.Convert(x).Is(x);
    }
}

#endif
