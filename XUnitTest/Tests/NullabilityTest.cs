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
    [TinyhandObject(KeyAsPropertyName = true)]
    public partial class NullableTestClass
    {
        public int Int { get; set; } = default;
        public int? NullableInt { get; set; } = default;
        public string String { get; set; } = default;
        public string? NullableString { get; set; } = default;
    }

    public class NullabilityTest
    {
        [Fact]
        public void Test1()
        {
            var t = new NullableTestClass();

            var t2 = TestHelper.Convert(t);

            t2.Int.Is(0);
            t2.NullableInt.Is(null);
            t2.String.Is(string.Empty);
            t2.NullableString.Is(null);
        }
    }
}
