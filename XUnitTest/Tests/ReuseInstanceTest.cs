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
    public partial class ReuseInstanceClass
    {
        public ReuseInstanceClass()
            : this(true)
        {
        }

        public ReuseInstanceClass(bool flag)
        {
            this.ReuseClass = new(flag);
            this.ReuseClassFalse = new(flag);
            this.ReuseStruct = new(flag);
            this.ReuseStructFalse = new(flag);
        }

        public ReuseClass ReuseClass { get; set; } = default!;

        [Reuse(false)]
        public ReuseClass ReuseClassFalse { get; set; } = default!;

        public ReuseStruct ReuseStruct { get; set; } = default!;

        [Reuse(false)]
        public ReuseStruct ReuseStructFalse { get; set; } = default!;
    }

    [TinyhandObject]
    public partial class ReuseClass
    {
        public ReuseClass()
            : this(false)
        {
        }

        public ReuseClass(bool flag)
        {
            this.Flag = flag;
        }

        [Key(0)]
        public int Int { get; set; }

        [IgnoreMember]
        public bool Flag { get; set; }
    }

    [TinyhandObject]
    public partial struct ReuseStruct
    {
        public ReuseStruct(bool flag)
        {
            this.Double = 0d;
            this.Flag = flag;
        }

        [Key(0)]
        public double Double { get; set; }

        [IgnoreMember]
        public bool Flag { get; set; }
    }

    public class ReuseInstanceTest
    {
        [Fact]
        public void Test1()
        {
            var t = TinyhandSerializer.Reconstruct<ReuseInstanceClass>();
            t.ReuseClass.Flag.Is(true);
            t.ReuseClassFalse.Flag.Is(false);
            t.ReuseStruct.Flag.Is(true);
            t.ReuseStructFalse.Flag.Is(false);

            t = TinyhandSerializer.Deserialize<ReuseInstanceClass>(TinyhandSerializer.Serialize(new Empty2()));
            t.ReuseClass.Flag.Is(true);
            t.ReuseClassFalse.Flag.Is(false);
            t.ReuseStruct.Flag.Is(true);
            t.ReuseStructFalse.Flag.Is(false);

            // t = TinyhandSerializer.DeserializeWith<ReuseInstanceClass>(new ReuseInstanceClass(false), TinyhandSerializer.Serialize(new ReuseInstanceClass()));
            var t2 = new ReuseInstanceClass(false);
            var reader = new Tinyhand.IO.TinyhandReader(TinyhandSerializer.Serialize(new ReuseInstanceClass()));
            t2.Deserialize(ref reader, TinyhandSerializerOptions.Standard);
            t2.ReuseClass.Flag.Is(false);
            t2.ReuseClassFalse.Flag.Is(false);
            t2.ReuseStruct.Flag.Is(false);
            t2.ReuseStructFalse.Flag.Is(false);

            // t = TinyhandSerializer.DeserializeWith<ReuseInstanceClass>(new ReuseInstanceClass(true), TinyhandSerializer.Serialize(new ReuseInstanceClass()));
            t2 = new ReuseInstanceClass(true);
            reader = new Tinyhand.IO.TinyhandReader(TinyhandSerializer.Serialize(new ReuseInstanceClass()));
            t2.Deserialize(ref reader, TinyhandSerializerOptions.Standard);
            t2.ReuseClass.Flag.Is(true);
            t2.ReuseClassFalse.Flag.Is(false);
            t2.ReuseStruct.Flag.Is(true);
            t2.ReuseStructFalse.Flag.Is(false);
        }
    }
}
