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
    [TinyhandUnion(0, typeof(UnionTestClassA))]
    [TinyhandUnion(1, typeof(UnionTestClassC))]
    public interface IUnionTestInterface
    {
    }

    [TinyhandObject]
    public partial class UnionTestClassA : IUnionTestInterface
    {
        [Key(0)]
        public int X { get; set; }
    }

    [TinyhandObject]
    public partial class UnionTestClassB : IUnionTestInterface
    {
        [Key(0)]
        public string Name { get; set; }
    }

    public partial class UnionTestClassC
    {
        [Key(0)]
        public string Name { get; set; }
    }

    [TinyhandUnion(0, typeof(UnionTestClassA))]
    [TinyhandUnion(1, typeof(UnionTestClassC))]
    public abstract partial class UnionTestBase
    {
        [Key(0)]
        public int ID { get; set; }
    }

    [TinyhandObject]
    public partial class UnionTestSubA : UnionTestBase
    {
        [Key(1)]
        public string Name { get; set; }
    }

    [TinyhandObject]
    public partial class UnionTestSubB : UnionTestBase
    {
        [Key(1)]
        public double Height { get; set; }
    }

    public class UnionTest
    {
        [Fact]
        public void TestInterface()
        {
        }
    }
}
