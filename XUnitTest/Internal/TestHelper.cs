// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tinyhand;
using Xunit;

#pragma warning disable SA1401 // Fields should be private
#pragma warning disable SA1009

namespace Tinyhand.Tests
{
    public static class TestHelper
    {
        public static T? Convert<T>(T obj) => TinyhandSerializer.Deserialize<T>(TinyhandSerializer.Serialize<T>(obj));

        public static T? TestWithMessagePack<T>(T obj)
        {
            var b = TinyhandSerializer.Serialize<T>(obj);
            var b2 = MessagePack.MessagePackSerializer.Serialize<T>(obj);
            // var json = MessagePack.MessagePackSerializer.ConvertToJson(b2);
            b.IsStructuralEqual(b2);

            var t = TinyhandSerializer.Deserialize<T>(b);
            t = MessagePack.MessagePackSerializer.Deserialize<T>(b);
            obj.IsStructuralEqual(t);

            return t;
        }

        public static T? TestWithMessagePackWithoutCompareObject<T>(T obj)
        {
            var b = TinyhandSerializer.Serialize<T>(obj);
            var b2 = MessagePack.MessagePackSerializer.Serialize<T>(obj);
            b.IsStructuralEqual(b2);

            var t = TinyhandSerializer.Deserialize<T>(b)!;
            var b3 = MessagePack.MessagePackSerializer.Serialize<T>(t);
            b.IsStructuralEqual(b3);

            return t;
        }
    }
}
