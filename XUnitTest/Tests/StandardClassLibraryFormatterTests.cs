// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Xunit;

namespace Tinyhand.Tests
{
    public class StandardClassLibraryFormatterTests : TestBase
    {
        [Fact]
        public void SystemType_Serializable()
        {
            Type type = typeof(string);
            byte[] msgpack = TinyhandSerializer.Serialize(type, TinyhandSerializerOptions.Standard);
            Type type2 = TinyhandSerializer.Deserialize<Type>(msgpack, TinyhandSerializerOptions.Standard);
            Assert.Equal(type, type2);
        }

        [Fact]
        public void SystemType_Serializable_Null()
        {
            Type type = null;
            byte[] msgpack = TinyhandSerializer.Serialize(type, TinyhandSerializerOptions.Standard);
            Type type2 = TinyhandSerializer.Deserialize<Type>(msgpack, TinyhandSerializerOptions.Standard);
            Assert.Equal(type, type2);
        }
    }
}
