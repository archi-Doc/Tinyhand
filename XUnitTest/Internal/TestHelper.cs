// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using Xunit;

#pragma warning disable SA1009

namespace Tinyhand.Tests;

public static class TestHelper
{
    public static T? Convert<T>(T obj) => TinyhandSerializer.Deserialize<T>(TinyhandSerializer.Serialize<T>(obj));

    public static object? ConvertNonGeneric(Type type, object obj) => TinyhandSerializer.Deserialize(type, TinyhandSerializer.Serialize(type, obj));

    public static T? TestWithoutMessagePack<T>(T obj, bool testClone = true)
    {
        var b = TinyhandSerializer.Serialize<T>(obj, TinyhandSerializerOptions.Compatible);
        var t = TinyhandSerializer.Deserialize<T>(b, TinyhandSerializerOptions.Compatible);
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
