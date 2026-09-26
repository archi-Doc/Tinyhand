// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using Xunit;

namespace Tinyhand.Tests;

public static class TestHelper
{
    /// <summary>
    /// Measures the bytes that an action allocates on the current thread in 1,000 invocations after a warm-up.
    /// </summary>
    /// <param name="action">The action to measure.</param>
    /// <returns>The smallest allocation of three batches.</returns>
    public static long MeasureAllocation(Action action)
    {
        for (var i = 0; i < 100; i++)
        {
            action();
        }

        // Runtime bookkeeping can add a one-off allocation of a few hundred bytes to either measurement (seen in freshly started test processes).
        // The minimum of repeated batches still exposes allocations on every invocation.
        var minimum = long.MaxValue;
        for (var batch = 0; batch < 3; batch++)
        {
            var start = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 1000; i++)
            {
                action();
            }

            minimum = Math.Min(minimum, GC.GetAllocatedBytesForCurrentThread() - start);
        }

        return minimum;
    }

    public static T? Convert<T>(T obj) => TinyhandSerializer.Deserialize<T>(TinyhandSerializer.Serialize<T>(obj));

    public static T? TestRoundtrip<T>(T obj, bool testClone = true)
    {
        var b = TinyhandSerializer.Serialize<T>(obj, TinyhandSerializerOptions.Standard);
        var t = TinyhandSerializer.Deserialize<T>(b, TinyhandSerializerOptions.Standard);
        obj.IsStructuralEqual(t);

        t = TinyhandSerializer.Deserialize<T>(TinyhandSerializer.Serialize<T>(obj, TinyhandSerializerOptions.Lz4), TinyhandSerializerOptions.Lz4);
        obj.IsStructuralEqual(t);

        var st = TinyhandSerializer.SerializeToString<T>(obj);
        t = TinyhandSerializer.DeserializeFromString<T>(st);
        obj.IsStructuralEqual(t);

        if (testClone)
        {// Clone
            obj.IsStructuralEqual(TinyhandSerializer.Clone(obj));
        }

        return t;
    }
}
