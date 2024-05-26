// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace Tinyhand;

[StructLayout(LayoutKind.Explicit)]
public readonly struct Id128
{
    [FieldOffset(0)]
    private readonly ulong lower;

    [FieldOffset(8)]
    private readonly ulong upper;

    public Id128()
        : this(DateTimeOffset.UtcNow)
    {
    }

    public Id128(DateTimeOffset dateTimeOffset)
    {
    }
}
