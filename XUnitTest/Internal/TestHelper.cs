// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Xunit;

namespace Tinyhand.Tests;

public static class TestHelper
{
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
