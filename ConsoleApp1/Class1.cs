// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tinyhand;

#pragma warning disable SA1401 // Fields should be private

namespace XUnitTest
{
    [TinyhandObject]
    public partial class PrimitiveIntKeyClass
    {
        [Key(0)]
        public bool BoolField = true;

        [Key(1)]
        public bool BoolProperty { get; set; } = false;

        [Key(2)]
        public byte ByteField = 10;

        [Key(3)]
        public byte ByteProperty { get; set; } = 20;

        [Key(4)]
        public sbyte SByteField = -11;

        [Key(5)]
        public sbyte SByteProperty { get; set; } = 22;

        [Key(6)]
        public short ShortField = -100;

        [Key(7)]
        public short ShortProperty { get; set; } = 220;

        [Key(8)]
        public ushort UShortField = 110;

        [Key(9)]
        public ushort UShortProperty { get; set; } = 222;

        [Key(10)]
        public int IntField = 1100;

        [Key(11)]
        public int IntProperty { get; set; } = -2200;

        [Key(12)]
        public uint UIntField = 11001;

        [Key(13)]
        public uint UIntProperty { get; set; } = 22001;

        [Key(14)]
        public long LongField = -1111222;

        [Key(15)]
        public long LongProperty { get; set; } = 211112222;

        [Key(16)]
        public ulong ULongField = 110011222;

        [Key(17)]
        public ulong ULongProperty { get; set; } = 1100112221;

        [Key(18)]
        public float FloatField = 1.1f;

        [Key(19)]
        public float FloatProperty { get; set; } = 22f;

        [Key(20)]
        public double DoubleField = 11d;

        [Key(21)]
        public double DoubleProperty { get; set; } = 22d;

        [Key(22)]
        public decimal DecimalField = 111m;

        [Key(23)]
        public decimal DecimalProperty { get; set; } = 222m;

        [Key(24)]
        public string StringField = "test";

        [Key(25)]
        public string StringProperty { get; set; } = "head";

        [Key(26)]
        public char CharField = 'c';

        [Key(27)]
        public char CharProperty { get; set; } = '@';

        [Key(28)]
        public DateTime DateTimeField = DateTime.Now;

        [Key(29)]
        public DateTime DateTimeProperty { get; set; } = DateTime.UtcNow;
    }

    [TinyhandObject(KeyAsPropertyName = true)]
    public partial class PrimitiveStringKeyClass
    {
        public bool BoolField = true;

        public bool BoolProperty { get; set; } = false;

        public byte ByteField = 10;

        public byte ByteProperty { get; set; } = 20;

        public sbyte SByteField = -11;

        public sbyte SByteProperty { get; set; } = 22;

        public short ShortField = -100;

        public short ShortProperty { get; set; } = 220;

        public ushort UShortField = 110;

        public ushort UShortProperty { get; set; } = 222;

        public int IntField = 1100;

        public int IntProperty { get; set; } = -2200;

        public uint UIntField = 11001;

        public uint UIntProperty { get; set; } = 22001;

        public long LongField = -1111222;

        public long LongProperty { get; set; } = 211112222;

        public ulong ULongField = 110011222;

        public ulong ULongProperty { get; set; } = 1100112221;

        public float FloatField = 1.1f;

        public float FloatProperty { get; set; } = 22f;

        public double DoubleField = 11d;

        public double DoubleProperty { get; set; } = 22d;

        public decimal DecimalField = 111m;

        public decimal DecimalProperty { get; set; } = 222m;

        public string StringField = "test";

        public string StringProperty { get; set; } = "head";

        public char CharField = 'c';

        public char CharProperty { get; set; } = '@';

        public DateTime DateTimeField = DateTime.Now;

        public DateTime DateTimeProperty { get; set; } = DateTime.UtcNow;
    }
}
