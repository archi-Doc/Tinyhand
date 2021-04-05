// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using Tinyhand;

namespace Benchmark.InitOnly
{
    [TinyhandObject]
    public partial class NormalIntClass
    {
        [Key(0)]
        public int X { get; set; }

        [Key(1)]
        public int Y { get; set; }

        [Key(2)]
        public string A { get; set; } = default!;

        [Key(3)]
        public string B { get; set; } = default!;

        public NormalIntClass(int x, int y, string a, string b)
        {
            this.X = x;
            this.Y = y;
            this.A = a;
            this.B = b;
        }

        public NormalIntClass()
        {
        }
    }

    // [TinyhandObject]
    public partial record NormalIntRecord(int X, int Y, string A, string B);

    [Config(typeof(BenchmarkConfig))]
    public class InitOnlyBenchmark
    {
        private MethodInfo setY = default!;
        private Action<NormalIntClass, int> setDelegate = default!;
        private NormalIntClass normalInt = default!;
        private byte[] normalIntByte = default!;

        public InitOnlyBenchmark()
        {
        }

        [GlobalSetup]
        public void Setup()
        {
            this.normalInt = new(1, 2, "A", "B");
            this.normalIntByte = TinyhandSerializer.Serialize(this.normalInt);

            this.setY = typeof(NormalIntClass).GetMethod("set_Y")!;

            var targetObject = Expression.Parameter(typeof(NormalIntClass));
            var tagetMember = Expression.Parameter(typeof(int));
            this.setDelegate = Expression.Lambda<Action<NormalIntClass, int>>(
                Expression.Call(
                    targetObject,
                    this.setY!,
                    tagetMember),
                targetObject,
                tagetMember)
                .Compile();
        }

        [Benchmark]
        public NormalIntClass? NormalInt()
        {
            return TinyhandSerializer.Deserialize<NormalIntClass>(this.normalIntByte);
        }

        [Benchmark]
        public NormalIntClass? NormalInt2()
        {
            this.setY.Invoke(this.normalInt, new object?[] { 2 });
            return TinyhandSerializer.Deserialize<NormalIntClass>(this.normalIntByte);
        }

        [Benchmark]
        public NormalIntClass? NormalInt3()
        {
            this.setDelegate(this.normalInt, 2);
            this.setDelegate(this.normalInt, 3);
            this.setDelegate(this.normalInt, 3);
            this.setDelegate(this.normalInt, 2);
            return TinyhandSerializer.Deserialize<NormalIntClass>(this.normalIntByte);
        }
    }
}
