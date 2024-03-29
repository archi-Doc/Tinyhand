﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using Xunit;

#pragma warning disable SA1009

namespace Tinyhand.Tests;

public static class TestHelper
{
    public static T? Convert<T>(T obj) => TinyhandSerializer.Deserialize<T>(TinyhandSerializer.Serialize<T>(obj));

    public static object? ConvertNonGeneric(Type type, object obj) => TinyhandSerializer.Deserialize(type, TinyhandSerializer.Serialize(type, obj));

    public static T? TestWithMessagePack<T>(T obj, bool testClone = true)
    {
        var b = TinyhandSerializer.Serialize<T>(obj, TinyhandSerializerOptions.Compatible);
        var b2 = MessagePack.MessagePackSerializer.Serialize<T>(obj);
        // var json = MessagePack.MessagePackSerializer.ConvertToJson(b2);
        b.IsStructuralEqual(b2);

        var t = TinyhandSerializer.Deserialize<T>(b, TinyhandSerializerOptions.Compatible);
        obj.IsStructuralEqual(t);

        t = TinyhandSerializer.Deserialize<T>(TinyhandSerializer.Serialize<T>(obj, TinyhandSerializerOptions.Lz4), TinyhandSerializerOptions.Lz4);
        obj.IsStructuralEqual(t);

        var st = TinyhandSerializer.SerializeToString<T>(obj);
        t = TinyhandSerializer.DeserializeFromString<T>(st);
        obj.IsStructuralEqual(t);

        // t = TinyhandSerializer.Deserialize<T>(MessagePack.MessagePackSerializer.Serialize<T>(obj, MessagePack.MessagePackSerializerOptions.Standard.WithCompression(MessagePack.MessagePackCompression.Lz4BlockArray)), TinyhandSerializerOptions.Lz4);
        // obj.IsStructuralEqual(t);

        if (testClone)
        {// Clone
            obj.IsStructuralEqual(TinyhandSerializer.Clone(obj));
        }

        return t;
    }

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

    public static T? TestWithMessagePackWithoutCompareObject<T>(T obj, bool testClone = true)
    {
        var b = TinyhandSerializer.Serialize<T>(obj, TinyhandSerializerOptions.Compatible);
        var b2 = MessagePack.MessagePackSerializer.Serialize<T>(obj);
        b.IsStructuralEqual(b2);

        var t = TinyhandSerializer.Deserialize<T>(b, TinyhandSerializerOptions.Compatible)!;
        var b3 = MessagePack.MessagePackSerializer.Serialize<T>(t);
        b.IsStructuralEqual(b3);

        if (testClone)
        {// Clone
            obj.IsStructuralEqual(TinyhandSerializer.Clone(obj));
        }

        return t;
    }
}
