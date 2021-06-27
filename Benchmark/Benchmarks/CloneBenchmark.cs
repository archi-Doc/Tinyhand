// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using Tinyhand;
using Tinyhand.IO;

namespace Benchmark.Clone
{
    [TinyhandObject]
    public partial class CloneTestClass
    {
        [Key(0)]
        public int X { get; set; }

        [Key(1)]
        public int Y { get; set; }

        [Key(2)]
        public int[] Array { get; set; } = default!;

        [Key(3)]
        public List<int> List { get; set; } = default!;

        public CloneTestClass()
        {
        }
    }

    [Config(typeof(BenchmarkConfig))]
    public class CloneBenchmark
    {
        private CloneTestClass testClass;

        public CloneBenchmark()
        {
            this.testClass = new() { Y = 2 };
            this.testClass.X = 1;
            this.testClass.Array = new int[] { 3, 4, 5, 6, 7, 8, 9, 10, };
            this.testClass.List = new() { 11, 12, 13, 14, 15, 16 };
        }

        [GlobalSetup]
        public void Setup()
        {
        }

        [Benchmark]
        public CloneTestClass Clone_Raw()
        {
            var t = new CloneTestClass();
            t.X = this.testClass.X;
            t.Y = this.testClass.Y;
            t.Array = new int[this.testClass.Array.Length];
            // Array.Copy(this.testClass.Array, t.Array, this.testClass.Array.Length);
            for (var n = 0; n < this.testClass.Array.Length; n++)
            {
                t.Array[n] = this.testClass.Array[n];
            }

            if (this.testClass.List == null)
            {
                t.List = null!;
            }
            else
            {
                t.List = new(this.testClass.List);
            }

            return t;
        }

        [Benchmark]
        public CloneTestClass Clone_SerializeDeserialize()
        {
            return TinyhandSerializer.Deserialize<CloneTestClass>(TinyhandSerializer.Serialize(this.testClass))!;
        }

        /*[Benchmark]
        public CloneTestClass Clone_Generated()
        {
            return this.testClass.DeepClone(TinyhandSerializerOptions.Standard);
        }*/

        [Benchmark]
        public CloneTestClass Clone_Clone()
        {
            return TinyhandSerializer.Clone(this.testClass);
        }
    }
}
