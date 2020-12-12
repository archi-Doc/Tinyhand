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
        {
            this.ReuseClass = new(true);
            this.ReuseClassFalse = new(true);
            this.NonReuseClass = new(true);
            this.NonReuseClassTrue = new(true);
        }

        public ReuseClass ReuseClass { get; set; } = default!;

        [ReuseInstance(false)]
        public ReuseClass ReuseClassFalse { get; set; } = default!;

        public NotReuseClass NonReuseClass { get; set; } = default!;

        [ReuseInstance(true)]
        public NotReuseClass NonReuseClassTrue { get; set; } = default!;
    }

    [TinyhandObject(ReuseInstance = true)]
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

    [TinyhandObject(ReuseInstance = false)]
    public partial class NotReuseClass
    {
        public NotReuseClass()
            : this(false)
        {
        }

        public NotReuseClass(bool flag)
        {
            this.Flag = flag;
        }

        [Key(0)]
        public int Int { get; set; }

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
            t.NonReuseClass.Flag.Is(false);
            t.NonReuseClassTrue.Flag.Is(true);

            var b = TinyhandSerializer.Serialize(t);
        }
    }
}
