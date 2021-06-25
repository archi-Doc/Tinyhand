// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tinyhand;
using Xunit;

namespace Tinyhand.Tests
{
    public class FormatterResolverTest
    {
        [Fact]
        public void Test1()
        {
            var t = new FormatterResolverClass();
            var t2 = TestHelper.TestWithMessagePackWithoutCompareObject(t, false);
        }
    }
}
