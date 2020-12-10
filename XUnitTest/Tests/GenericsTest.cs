// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tinyhand;
using Xunit;

#pragma warning disable SA1139

namespace Tinyhand.Tests
{
    [TinyhandObject(KeyAsPropertyName = true)]
    public partial class GenericsTestClass<T>
    {
        [DefaultValue(12)]
        public int Int { get; set; } // 12

        public T TValue { get; set; }

        /*public partial class GenericsNestedClass<U>
        {
            [DefaultValue("TH")]
            public string String { get; set; } // 12

            public U UValue { get; set; }
        }

        public GenericsTestClass2<int> ClassInt { get; set; } = new();*/
    }

    [TinyhandObject(KeyAsPropertyName = true)]
    public partial class GenericsTestClass2<V>
    {
        public V VValue { get; set; }
    }

    public class GenericsTest
    {
        [Fact]
        public void Test1()
        {
            var t = TinyhandSerializer.Reconstruct<GenericsTestClass<string>>();
        }
    }
}
