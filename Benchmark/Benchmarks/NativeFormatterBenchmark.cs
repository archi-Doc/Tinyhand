﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using Tinyhand;
using Tinyhand.IO;
using Tinyhand.Formatters;

namespace Benchmark.NativeFormatter
{
    [TinyhandObject]
    public partial class DateTimeClass
    {
        [Key(0)]
        public DateTime DateTime { get; set; }

        public DateTimeClass()
        {
            this.DateTime = DateTime.UtcNow;
        }
    }

    [Config(typeof(BenchmarkConfig))]
    public class NativeFormatterBenchmark
    {
        public byte[] ByteBuffer { get; private set; } = new byte[256];

        public DateTime DateTime { get; private set; }

        public ReadOnlySequence<byte> DateTimeByte { get; private set; }

        public ReadOnlySequence<byte> NativeDateTimeByte { get; private set; }

        public Guid Guid { get; private set; }

        public ReadOnlySequence<byte> GuidByte { get; private set; }

        public ReadOnlySequence<byte> NativeGuidByte { get; private set; }

        public Decimal Decimal { get; private set; }

        public ReadOnlySequence<byte> DecimalByte { get; private set; }

        public ReadOnlySequence<byte> NativeDecimalByte { get; private set; }

        public NativeFormatterBenchmark()
        {
        }

        [GlobalSetup]
        public void Setup()
        {
            this.DateTime = DateTime.UtcNow;
            var w = new TinyhandWriter();
            DateTimeFormatter.Instance.Serialize(ref w, this.DateTime, TinyhandSerializerOptions.Standard);
            this.DateTimeByte = w.FlushAndGetReadOnlySequence();
            w = new TinyhandWriter();
            NativeDateTimeFormatter.Instance.Serialize(ref w, this.DateTime, TinyhandSerializerOptions.Standard);
            this.NativeDateTimeByte = w.FlushAndGetReadOnlySequence();

            this.Guid = Guid.NewGuid();
            w = new TinyhandWriter();
            GuidFormatter.Instance.Serialize(ref w, this.Guid, TinyhandSerializerOptions.Standard);
            this.GuidByte = w.FlushAndGetReadOnlySequence();
            w = new TinyhandWriter();
            NativeGuidFormatter.Instance.Serialize(ref w, this.Guid, TinyhandSerializerOptions.Standard);
            this.NativeGuidByte = w.FlushAndGetReadOnlySequence();

            this.Decimal = new Decimal(1341, 53156, 61, true, 3);
            w = new TinyhandWriter();
            DecimalFormatter.Instance.Serialize(ref w, this.Decimal, TinyhandSerializerOptions.Standard);
            this.DecimalByte = w.FlushAndGetReadOnlySequence();
            w = new TinyhandWriter();
            NativeDecimalFormatter.Instance.Serialize(ref w, this.Decimal, TinyhandSerializerOptions.Standard);
            this.NativeDecimalByte = w.FlushAndGetReadOnlySequence();
        }

        [Benchmark]
        public ReadOnlySequence<byte> SerializeDateTime()
        {
            var w = new TinyhandWriter(this.ByteBuffer);
            try
            {
                DateTimeFormatter.Instance.Serialize(ref w, this.DateTime, TinyhandSerializerOptions.Standard);
                return w.FlushAndGetReadOnlySequence();
            }
            finally
            {
                w.Dispose();
            }
        }

        [Benchmark]
        public ReadOnlySequence<byte> SerializeNativeDateTime()
        {
            var w = new TinyhandWriter(this.ByteBuffer);
            try
            {
                NativeDateTimeFormatter.Instance.Serialize(ref w, this.DateTime, TinyhandSerializerOptions.Standard);
                return w.FlushAndGetReadOnlySequence();
            }
            finally
            {
                w.Dispose();
            }
        }

        [Benchmark]
        public DateTime DeserializeDateTime()
        {
            var r = new TinyhandReader(this.DateTimeByte);
            return DateTimeFormatter.Instance.Deserialize(ref r, null!);
        }

        [Benchmark]
        public DateTime DeserializeNativeDateTime()
        {
            var r = new TinyhandReader(this.NativeDateTimeByte);
            return NativeDateTimeFormatter.Instance.Deserialize(ref r, null!);
        }

        [Benchmark]
        public ReadOnlySequence<byte> SerializeGuid()
        {
            var w = new TinyhandWriter(this.ByteBuffer);
            try
            {
                GuidFormatter.Instance.Serialize(ref w, this.Guid, TinyhandSerializerOptions.Standard);
                return w.FlushAndGetReadOnlySequence();
            }
            finally
            {
                w.Dispose();
            }
        }

        [Benchmark]
        public ReadOnlySequence<byte> SerializeNativeGuid()
        {
            var w = new TinyhandWriter(this.ByteBuffer);
            try
            {
                NativeGuidFormatter.Instance.Serialize(ref w, this.Guid, TinyhandSerializerOptions.Standard);
                return w.FlushAndGetReadOnlySequence();
            }
            finally
            {
                w.Dispose();
            }
        }

        [Benchmark]
        public Guid DeserializeGuid()
        {
            var r = new TinyhandReader(this.GuidByte);
            return GuidFormatter.Instance.Deserialize(ref r, null!);
        }

        [Benchmark]
        public Guid DeserializeNativeGuid()
        {
            var r = new TinyhandReader(this.NativeGuidByte);
            return NativeGuidFormatter.Instance.Deserialize(ref r, null!);
        }

        [Benchmark]
        public ReadOnlySequence<byte> SerializeDecimal()
        {
            var w = new TinyhandWriter(this.ByteBuffer);
            try
            {
                DecimalFormatter.Instance.Serialize(ref w, this.Decimal, TinyhandSerializerOptions.Standard);
                return w.FlushAndGetReadOnlySequence();
            }
            finally
            {
                w.Dispose();
            }
        }

        [Benchmark]
        public ReadOnlySequence<byte> SerializeNativeDecimal()
        {
            var w = new TinyhandWriter(this.ByteBuffer);
            try
            {
                NativeDecimalFormatter.Instance.Serialize(ref w, this.Decimal, TinyhandSerializerOptions.Standard);
                return w.FlushAndGetReadOnlySequence();
            }
            finally
            {
                w.Dispose();
            }
        }

        [Benchmark]
        public Decimal DeserializeDecimal()
        {
            var r = new TinyhandReader(this.DecimalByte);
            return DecimalFormatter.Instance.Deserialize(ref r, null!);
        }

        [Benchmark]
        public Decimal DeserializeNativeDecimal()
        {
            var r = new TinyhandReader(this.NativeDecimalByte);
            return NativeDecimalFormatter.Instance.Deserialize(ref r, null!);
        }
    }
}
