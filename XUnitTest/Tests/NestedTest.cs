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
    [TinyhandObject]
    public partial class OuterClass
    {
        [Key(0)]
        public List<int> IntList;

        [Key(1)]
        public List<InnerClass> MyList;
    }

    [TinyhandObject]
    public partial class InnerClass
    {
        [Key(2)]
        public int i;
    }

    public class NestedTest
    {
        [Fact]
        public void Test1()
        {
            var test = new OuterClass();
            test.IntList = new List<int>() { 1 };
            test.MyList = new List<InnerClass>() { new InnerClass { i = 25 } };
            var bytes = Tinyhand.TinyhandSerializer.Serialize<OuterClass>(test);
            var test2 = Tinyhand.TinyhandSerializer.Deserialize<OuterClass>(bytes);
            test2.IsStructuralEqual(test);
        }
    }
}
