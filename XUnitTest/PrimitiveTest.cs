// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tinyhand;
using Xunit;

namespace XUnitTest
{
    // [TinyhandGeneratorOption(AttachDebugger = false, GenerateToFile = true)]
    public class PrimitiveTest
    {
        [Fact]
        public void PrimitiveIntKeyTest()
        {
            var t = new PrimitiveIntKeyClass();
            var t2 = TestHelper.TestWithMessagePack(t);
        }

        [Fact]
        public void PrimitiveStringKeyTest()
        {
            var t = new PrimitiveStringKeyClass();
            var t2 = TestHelper.TestWithMessagePack(t);
        }

        [Fact]
        public void PrimitiveArrayTest()
        {
            var t = new PrimitiveArrayClass();
            var t2 = TestHelper.TestWithMessagePack(t);
        }
    }
}
