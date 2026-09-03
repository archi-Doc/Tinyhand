// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Arc;
using Tinyhand.Formatters;

#pragma warning disable SA1401 // The private cache is initialized by its containing resolver.

namespace Tinyhand.Resolvers;

/// <summary>
/// Default composited resolver.
/// </summary>
internal sealed class BuiltinResolver : IFormatterResolver
{
    /// <summary>
    /// The singleton instance that can be used.
    /// </summary>
    public static readonly BuiltinResolver Instance = new BuiltinResolver();

    static BuiltinResolver()
    {
        FormatterCache<byte>.Formatter = UInt8Formatter.Instance;
        FormatterCache<sbyte>.Formatter = Int8Formatter.Instance;
        FormatterCache<ushort>.Formatter = UInt16Formatter.Instance;
        FormatterCache<short>.Formatter = Int16Formatter.Instance;
        FormatterCache<uint>.Formatter = UInt32Formatter.Instance;
        FormatterCache<int>.Formatter = Int32Formatter.Instance;
        FormatterCache<ulong>.Formatter = UInt64Formatter.Instance;
        FormatterCache<long>.Formatter = Int64Formatter.Instance;
        FormatterCache<float>.Formatter = SingleFormatter.Instance;
        FormatterCache<double>.Formatter = DoubleFormatter.Instance;
        FormatterCache<bool>.Formatter = BooleanFormatter.Instance;
        FormatterCache<string>.Formatter = StringFormatter.Instance;
        FormatterCache<char>.Formatter = CharFormatter.Instance;
        FormatterCache<DateTime>.Formatter = DateTimeFormatter.Instance;
        FormatterCache<byte?>.Formatter = NullableUInt8Formatter.Instance;
        FormatterCache<sbyte?>.Formatter = NullableInt8Formatter.Instance;
        FormatterCache<ushort?>.Formatter = NullableUInt16Formatter.Instance;
        FormatterCache<short?>.Formatter = NullableInt16Formatter.Instance;
        FormatterCache<uint?>.Formatter = NullableUInt32Formatter.Instance;
        FormatterCache<int?>.Formatter = NullableInt32Formatter.Instance;
        FormatterCache<ulong?>.Formatter = NullableUInt64Formatter.Instance;
        FormatterCache<long?>.Formatter = NullableInt64Formatter.Instance;
        FormatterCache<float?>.Formatter = NullableSingleFormatter.Instance;
        FormatterCache<double?>.Formatter = NullableDoubleFormatter.Instance;
        FormatterCache<bool?>.Formatter = NullableBooleanFormatter.Instance;
        FormatterCache<char?>.Formatter = NullableCharFormatter.Instance;
        FormatterCache<DateTime?>.Formatter = NullableDateTimeFormatter.Instance;
        FormatterCache<Int128>.Formatter = Int128Formatter.Instance;
        FormatterCache<UInt128>.Formatter = UInt128Formatter.Instance;
        FormatterCache<Int128?>.Formatter = NullableInt128Formatter.Instance;
        FormatterCache<UInt128?>.Formatter = NullableUInt128Formatter.Instance;
        FormatterCache<byte[]>.Formatter = ByteArrayFormatter.Instance;
        FormatterCache<sbyte[]>.Formatter = Int8ArrayFormatter.Instance;
        FormatterCache<ushort[]>.Formatter = UInt16ArrayFormatter.Instance;
        FormatterCache<short[]>.Formatter = Int16ArrayFormatter.Instance;
        FormatterCache<uint[]>.Formatter = UInt32ArrayFormatter.Instance;
        FormatterCache<int[]>.Formatter = Int32ArrayFormatter.Instance;
        FormatterCache<ulong[]>.Formatter = UInt64ArrayFormatter.Instance;
        FormatterCache<long[]>.Formatter = Int64ArrayFormatter.Instance;
        FormatterCache<float[]>.Formatter = SingleArrayFormatter.Instance;
        FormatterCache<double[]>.Formatter = DoubleArrayFormatter.Instance;
        FormatterCache<bool[]>.Formatter = BooleanArrayFormatter.Instance;
        FormatterCache<string[]>.Formatter = StringArrayFormatter.Instance;
        FormatterCache<char[]>.Formatter = CharArrayFormatter.Instance;
        FormatterCache<DateTime[]>.Formatter = DateTimeArrayFormatter.Instance;
        FormatterCache<Int128[]>.Formatter = Int128ArrayFormatter.Instance;
        FormatterCache<UInt128[]>.Formatter = UInt128ArrayFormatter.Instance;
        FormatterCache<List<byte>>.Formatter = ByteListFormatter.Instance;
        FormatterCache<List<sbyte>>.Formatter = Int8ListFormatter.Instance;
        FormatterCache<List<ushort>>.Formatter = UInt16ListFormatter.Instance;
        FormatterCache<List<short>>.Formatter = Int16ListFormatter.Instance;
        FormatterCache<List<uint>>.Formatter = UInt32ListFormatter.Instance;
        FormatterCache<List<int>>.Formatter = Int32ListFormatter.Instance;
        FormatterCache<List<ulong>>.Formatter = UInt64ListFormatter.Instance;
        FormatterCache<List<long>>.Formatter = Int64ListFormatter.Instance;
        FormatterCache<List<float>>.Formatter = SingleListFormatter.Instance;
        FormatterCache<List<double>>.Formatter = DoubleListFormatter.Instance;
        FormatterCache<List<bool>>.Formatter = BooleanListFormatter.Instance;
        FormatterCache<List<string>>.Formatter = StringListFormatter.Instance;
        FormatterCache<List<char>>.Formatter = CharListFormatter.Instance;
        FormatterCache<List<DateTime>>.Formatter = DateTimeListFormatter.Instance;
        FormatterCache<List<Int128>>.Formatter = Int128ListFormatter.Instance;
        FormatterCache<List<UInt128>>.Formatter = UInt128ListFormatter.Instance;
        FormatterCache<decimal>.Formatter = NativeDecimalFormatter.Instance;
        FormatterCache<decimal?>.Formatter = new StaticNullableFormatter<decimal>(NativeDecimalFormatter.Instance);
        FormatterCache<TimeSpan>.Formatter = TimeSpanFormatter.Instance;
        FormatterCache<TimeSpan?>.Formatter = new StaticNullableFormatter<TimeSpan>(TimeSpanFormatter.Instance);
        FormatterCache<DateTimeOffset>.Formatter = DateTimeOffsetFormatter.Instance;
        FormatterCache<DateTimeOffset?>.Formatter = new StaticNullableFormatter<DateTimeOffset>(DateTimeOffsetFormatter.Instance);
        FormatterCache<Guid>.Formatter = NativeGuidFormatter.Instance;
        FormatterCache<Guid?>.Formatter = new StaticNullableFormatter<Guid>(NativeGuidFormatter.Instance);
        FormatterCache<Uri>.Formatter = UriFormatter.Instance;
        FormatterCache<Version>.Formatter = VersionFormatter.Instance;
        FormatterCache<StringBuilder>.Formatter = StringBuilderFormatter.Instance;
        FormatterCache<BitArray>.Formatter = BitArrayFormatter.Instance;
        FormatterCache<System.Numerics.BigInteger>.Formatter = BigIntegerFormatter.Instance;
        FormatterCache<System.Numerics.BigInteger?>.Formatter = new StaticNullableFormatter<System.Numerics.BigInteger>(BigIntegerFormatter.Instance);
        FormatterCache<System.Numerics.Complex>.Formatter = ComplexFormatter.Instance;
        FormatterCache<System.Numerics.Complex?>.Formatter = new StaticNullableFormatter<System.Numerics.Complex>(ComplexFormatter.Instance);
        FormatterCache<Nil>.Formatter = NilFormatter.Instance;
        FormatterCache<Nil?>.Formatter = NullableNilFormatter.Instance;
        FormatterCache<object[]>.Formatter = new ArrayFormatter<object>();
        FormatterCache<List<object>>.Formatter = new ListFormatter<object>();
        FormatterCache<Memory<byte>>.Formatter = ByteMemoryFormatter.Instance;
        FormatterCache<Memory<byte>?>.Formatter = new StaticNullableFormatter<Memory<byte>>(ByteMemoryFormatter.Instance);
        FormatterCache<ReadOnlyMemory<byte>>.Formatter = ByteReadOnlyMemoryFormatter.Instance;
        FormatterCache<ReadOnlyMemory<byte>?>.Formatter = new StaticNullableFormatter<ReadOnlyMemory<byte>>(ByteReadOnlyMemoryFormatter.Instance);
        FormatterCache<ReadOnlySequence<byte>>.Formatter = ByteReadOnlySequenceFormatter.Instance;
        FormatterCache<ReadOnlySequence<byte>?>.Formatter = new StaticNullableFormatter<ReadOnlySequence<byte>>(ByteReadOnlySequenceFormatter.Instance);
        FormatterCache<ArraySegment<byte>>.Formatter = ByteArraySegmentFormatter.Instance;
        FormatterCache<ArraySegment<byte>?>.Formatter = new StaticNullableFormatter<ArraySegment<byte>>(ByteArraySegmentFormatter.Instance);
        FormatterCache<Memory<char>>.Formatter = CharMemoryFormatter.Instance;
        FormatterCache<ReadOnlyMemory<char>>.Formatter = CharReadOnlyMemoryFormatter.Instance;
        FormatterCache<IPAddress>.Formatter = IPAddressFormatter.Instance;
        FormatterCache<IPEndPoint>.Formatter = IPEndPointFormatter.Instance;
        FormatterCache<Struct128>.Formatter = Struct128Formatter.Instance;
        FormatterCache<Struct256>.Formatter = Struct256Formatter.Instance;
    }

    private BuiltinResolver()
    {
    }

    public ITinyhandFormatter<T>? TryGetFormatter<T>()
    {
        return FormatterCache<T>.Formatter;
    }

    public void RegisterInstantiableTypes()
    {
        TinyhandTypeIdentifier.Register<byte>();
        TinyhandTypeIdentifier.Register<sbyte>();
        TinyhandTypeIdentifier.Register<ushort>();
        TinyhandTypeIdentifier.Register<short>();
        TinyhandTypeIdentifier.Register<uint>();
        TinyhandTypeIdentifier.Register<int>();
        TinyhandTypeIdentifier.Register<ulong>();
        TinyhandTypeIdentifier.Register<long>();
        TinyhandTypeIdentifier.Register<float>();
        TinyhandTypeIdentifier.Register<double>();
        TinyhandTypeIdentifier.Register<bool>();
        TinyhandTypeIdentifier.Register<string>();
        TinyhandTypeIdentifier.Register<char>();
        TinyhandTypeIdentifier.Register<DateTime>();
        TinyhandTypeIdentifier.Register<byte?>();
        TinyhandTypeIdentifier.Register<sbyte?>();
        TinyhandTypeIdentifier.Register<ushort?>();
        TinyhandTypeIdentifier.Register<short?>();
        TinyhandTypeIdentifier.Register<uint?>();
        TinyhandTypeIdentifier.Register<int?>();
        TinyhandTypeIdentifier.Register<ulong?>();
        TinyhandTypeIdentifier.Register<long?>();
        TinyhandTypeIdentifier.Register<float?>();
        TinyhandTypeIdentifier.Register<double?>();
        TinyhandTypeIdentifier.Register<bool?>();
        TinyhandTypeIdentifier.Register<char?>();
        TinyhandTypeIdentifier.Register<DateTime?>();
        TinyhandTypeIdentifier.Register<Int128>();
        TinyhandTypeIdentifier.Register<UInt128>();
        TinyhandTypeIdentifier.Register<byte[]>();
        TinyhandTypeIdentifier.Register<sbyte[]>();
        TinyhandTypeIdentifier.Register<ushort[]>();
        TinyhandTypeIdentifier.Register<short[]>();
        TinyhandTypeIdentifier.Register<uint[]>();
        TinyhandTypeIdentifier.Register<int[]>();
        TinyhandTypeIdentifier.Register<ulong[]>();
        TinyhandTypeIdentifier.Register<long[]>();
        TinyhandTypeIdentifier.Register<float[]>();
        TinyhandTypeIdentifier.Register<double[]>();
        TinyhandTypeIdentifier.Register<bool[]>();
        TinyhandTypeIdentifier.Register<string[]>();
        TinyhandTypeIdentifier.Register<char[]>();
        TinyhandTypeIdentifier.Register<DateTime[]>();
        TinyhandTypeIdentifier.Register<List<byte>>();
        TinyhandTypeIdentifier.Register<List<sbyte>>();
        TinyhandTypeIdentifier.Register<List<ushort>>();
        TinyhandTypeIdentifier.Register<List<short>>();
        TinyhandTypeIdentifier.Register<List<uint>>();
        TinyhandTypeIdentifier.Register<List<int>>();
        TinyhandTypeIdentifier.Register<List<ulong>>();
        TinyhandTypeIdentifier.Register<List<long>>();
        TinyhandTypeIdentifier.Register<List<float>>();
        TinyhandTypeIdentifier.Register<List<double>>();
        TinyhandTypeIdentifier.Register<List<bool>>();
        TinyhandTypeIdentifier.Register<List<string>>();
        TinyhandTypeIdentifier.Register<List<char>>();
        TinyhandTypeIdentifier.Register<List<DateTime>>();
        TinyhandTypeIdentifier.Register<decimal>();
        TinyhandTypeIdentifier.Register<decimal?>();
        TinyhandTypeIdentifier.Register<TimeSpan>();
        TinyhandTypeIdentifier.Register<TimeSpan?>();
        TinyhandTypeIdentifier.Register<DateTimeOffset>();
        TinyhandTypeIdentifier.Register<DateTimeOffset?>();
        TinyhandTypeIdentifier.Register<Guid>();
        TinyhandTypeIdentifier.Register<Guid?>();
        TinyhandTypeIdentifier.Register<Uri>();
        TinyhandTypeIdentifier.Register<Version>();
        TinyhandTypeIdentifier.Register<StringBuilder>();
        TinyhandTypeIdentifier.Register<BitArray>();
        TinyhandTypeIdentifier.Register<System.Numerics.BigInteger>();
        TinyhandTypeIdentifier.Register<System.Numerics.BigInteger?>();
        TinyhandTypeIdentifier.Register<System.Numerics.Complex>();
        TinyhandTypeIdentifier.Register<System.Numerics.Complex?>();
        TinyhandTypeIdentifier.Register<Nil>();
        TinyhandTypeIdentifier.Register<Nil?>();
        TinyhandTypeIdentifier.Register<object[]>();
        TinyhandTypeIdentifier.Register<List<object>>();
        TinyhandTypeIdentifier.Register<Memory<byte>>();
        TinyhandTypeIdentifier.Register<Memory<byte>?>();
        TinyhandTypeIdentifier.Register<ReadOnlyMemory<byte>>();
        TinyhandTypeIdentifier.Register<ReadOnlyMemory<byte>?>();
        TinyhandTypeIdentifier.Register<ReadOnlySequence<byte>>();
        TinyhandTypeIdentifier.Register<ReadOnlySequence<byte>?>();
        TinyhandTypeIdentifier.Register<ArraySegment<byte>>();
        TinyhandTypeIdentifier.Register<ArraySegment<byte>?>();
        TinyhandTypeIdentifier.Register<Memory<char>>();
        TinyhandTypeIdentifier.Register<ReadOnlyMemory<char>>();
        TinyhandTypeIdentifier.Register<IPAddress>();
        TinyhandTypeIdentifier.Register<IPEndPoint>();
        TinyhandTypeIdentifier.Register<Struct128>();
        TinyhandTypeIdentifier.Register<Struct256>();
    }

    private static class FormatterCache<T>
    {
        internal static ITinyhandFormatter<T>? Formatter;
    }
}
