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
    [TinyhandObject(KeyAsPropertyName = true, ReconstructMember = false)]
    public partial class ReconstructTestClass
    {
        [DefaultValue(12)]
        public int Int { get; set; } // 12

        public EmptyClass EmptyClass { get; set; } // new()

        [Reconstruct(false)]
        public EmptyClass EmptyClassOff { get; set; } // null

        public EmptyClass? EmptyClass2 { get; set; } // null

        [Reconstruct(true)]
        public EmptyClass? EmptyClassOn { get; set; } // new()

        /*[IgnoreMember]
        [Reconstruct(true)]
        public ClassWithoutDefaultConstructor WithoutClass { get; set; }

        [IgnoreMember]
        [Reconstruct(true)]
        public ClassWithDefaultConstructor WithClass { get; set; }*/
    }

    public class ClassWithoutDefaultConstructor
    {
        public string Name = string.Empty;

        public ClassWithoutDefaultConstructor(string name)
        {
            this.Name = name;
        }
    }

    public class ClassWithDefaultConstructor
    {
        public string Name = string.Empty;

        public ClassWithDefaultConstructor(string name)
        {
            this.Name = name;
        }

        public ClassWithDefaultConstructor()
            : this(string.Empty)
        {
        }
    }

    public class ReconstructTest
    {
        [Fact]
        public void Test1()
        {
            var t = TinyhandSerializer.Reconstruct<ReconstructTestClass>();

            t.Int.Is(12);
            t.EmptyClass.IsNotNull();
            t.EmptyClassOff.IsNull();
            t.EmptyClass2.IsNull();
            t.EmptyClassOn.IsNotNull();
        }
    }
}
