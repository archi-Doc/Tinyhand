// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace XUnitTest
{
    public class PrimitiveTest
    {
        [Fact]
        public void Test1()
        {
            var t = new PrimitiveClass() { IntField = 10, };
            var t2 = TestHelper.Convert(t);
            t.IsStructuralEqual(t2);
        }
    }
}
