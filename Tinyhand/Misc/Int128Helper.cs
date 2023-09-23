// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Runtime.CompilerServices;

namespace Tinyhand;

public static class Int128Helper
{
    private const double DoubleToIntThreshold = 1_000_000_000_000_000_000d;

    public static double ToDouble(this Int128 value)
    {
        var ripper = Unsafe.As<Int128, Int128Ripper>(ref value);
        if (ripper.Upper == 0)
        {
            return (double)ripper.Lower;
        }
        else if (~ripper.Upper == 0)
        {
            return (double)(long)ripper.Lower;
        }
        else
        {
            return (double)value;
        }
    }

    public static double ToDouble(this UInt128 value)
    {
        var ripper = Unsafe.As<UInt128, Int128Ripper>(ref value);
        if (ripper.Upper == 0)
        {
            return (double)ripper.Lower;
        }
        else
        {
            return (double)value;
        }
    }

    public static Int128 ToInt128(this double value)
    {
        if (value >= -DoubleToIntThreshold && value <= +DoubleToIntThreshold)
        {
            return (Int128)(long)value;
        }

        return (Int128)value;
    }

    public static UInt128 ToUInt128(this double value)
    {
        if (value >= 0d && value <= +DoubleToIntThreshold)
        {
            return (UInt128)(long)value;
        }

        return (UInt128)value;
    }
}

#pragma warning disable CS0649
internal readonly struct Int128Ripper
{
#if BIGENDIAN
    public readonly ulong Upper;
    public readonly ulong Lower;
#else
    public readonly ulong Lower;
    public readonly ulong Upper;
#endif

    public override string ToString()
        => $"({this.Upper}, {this.Lower})";
}
#pragma warning restore CS0649
