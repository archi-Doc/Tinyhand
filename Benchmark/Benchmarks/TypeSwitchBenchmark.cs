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

namespace Benchmark.TypeSwitch
{
    public class BaseClass
    {
        public int X { get; set; }
    }

    public class ClassA : BaseClass
    {
        public string A { get; set; } = string.Empty;
    }

    public class ClassB : BaseClass
    {
        public string B { get; set; } = string.Empty;
    }

    public class ClassC : BaseClass
    {
        public string C { get; set; } = string.Empty;
    }

    public class ClassD : ClassC
    {
        public string D{ get; set; } = string.Empty;
    }

    [Config(typeof(BenchmarkConfig))]
    public class SwitchBenchmark
    {
        private BaseClass testClass;

        public SwitchBenchmark()
        {
            this.testClass = new ClassD();
        }

        [GlobalSetup]
        public void Setup()
        {
        }

        [Benchmark]
        public string Switch_Is()
        {
            switch (this.testClass)
            {
                case ClassA a:
                    return a.A;

                case ClassB b:
                    return b.B;

                case ClassD d:
                    return d.D;

                case ClassC c:
                    return c.C;
            }

            return string.Empty;
        }

        [Benchmark]
        public string Switch_Type()
        {
            var type = this.testClass.GetType();
            if (type == typeof(ClassA))
            {
                return ((ClassA)this.testClass).A;
            }
            else if (type == typeof(ClassB))
            {
                return ((ClassB)this.testClass).B;
            }
            else if (type == typeof(ClassC))
            {
                return ((ClassC)this.testClass).C;
            }
            else if (type == typeof(ClassD))
            {
                return ((ClassD)this.testClass).D;
            }

            return string.Empty;
        }
    }
}
