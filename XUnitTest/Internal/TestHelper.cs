// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tinyhand;
using Xunit;

#pragma warning disable SA1401 // Fields should be private

namespace XUnitTest
{
    public static class TestHelper
    {
        public static T? Convert<T>(T obj) => TinyhandSerializer.Deserialize<T>(TinyhandSerializer.Serialize<T>(obj));

        public static T? TestWithMessagePack<T>(T obj)
        {
            var b = TinyhandSerializer.Serialize<T>(obj);
            var b2 = MessagePack.MessagePackSerializer.Serialize<T>(obj);
            // b.IsStructuralEqual(b2);

            var t = TinyhandSerializer.Deserialize<T>(b);
            obj.IsStructuralEqual(t);

            return t;
        }
    }
}
