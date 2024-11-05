// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Arc.Crypto;
using Tinyhand.Formatters;

#pragma warning disable SA1509 // Opening braces should not be preceded by blank line

namespace Tinyhand.Resolvers;

/// <summary>
/// Default composited resolver.
/// </summary>
public sealed class BuiltinResolver : IFormatterResolver
{
    /// <summary>
    /// The singleton instance that can be used.
    /// </summary>
    public static readonly BuiltinResolver Instance = new BuiltinResolver();

    private static readonly Dictionary<Type, object> TypeToFormatter = new()
    {
        // Primitive
        { typeof(byte), UInt8Formatter.Instance },
        { typeof(sbyte), Int8Formatter.Instance },
        { typeof(ushort), UInt16Formatter.Instance },
        { typeof(short), Int16Formatter.Instance },
        { typeof(uint), UInt32Formatter.Instance },
        { typeof(int), Int32Formatter.Instance },
        { typeof(ulong), UInt64Formatter.Instance },
        { typeof(long), Int64Formatter.Instance },
        { typeof(float), SingleFormatter.Instance },
        { typeof(double), DoubleFormatter.Instance },
        { typeof(bool), BooleanFormatter.Instance },
        { typeof(string), StringFormatter.Instance },
        { typeof(char), CharFormatter.Instance },
        { typeof(DateTime), DateTimeFormatter.Instance },

        { typeof(byte?), NullableUInt8Formatter.Instance },
        { typeof(sbyte?), NullableInt8Formatter.Instance },
        { typeof(ushort?), NullableUInt16Formatter.Instance },
        { typeof(short?), NullableInt16Formatter.Instance },
        { typeof(uint?), NullableUInt32Formatter.Instance },
        { typeof(int?), NullableInt32Formatter.Instance },
        { typeof(ulong?), NullableUInt64Formatter.Instance },
        { typeof(long?), NullableInt64Formatter.Instance },
        { typeof(float?), NullableSingleFormatter.Instance },
        { typeof(double?), NullableDoubleFormatter.Instance },
        { typeof(bool?), NullableBooleanFormatter.Instance },
        // { typeof(string?), NullableStringFormatter.Instance },
        { typeof(char?), NullableCharFormatter.Instance },
        { typeof(DateTime?), NullableDateTimeFormatter.Instance },

        { typeof(Int128), Int128Formatter.Instance },

        // otpmitized primitive array formatter
        { typeof(byte[]), ByteArrayFormatter.Instance },
        { typeof(sbyte[]), Int8ArrayFormatter.Instance },
        { typeof(ushort[]), UInt16ArrayFormatter.Instance },
        { typeof(short[]), Int16ArrayFormatter.Instance },
        { typeof(uint[]), UInt32ArrayFormatter.Instance },
        { typeof(int[]), Int32ArrayFormatter.Instance },
        { typeof(ulong[]), UInt64ArrayFormatter.Instance },
        { typeof(long[]), Int64ArrayFormatter.Instance },
        { typeof(float[]), SingleArrayFormatter.Instance },
        { typeof(double[]), DoubleArrayFormatter.Instance },
        { typeof(bool[]), BooleanArrayFormatter.Instance },
        { typeof(string[]), StringArrayFormatter.Instance },
        { typeof(char[]), CharArrayFormatter.Instance },
        { typeof(DateTime[]), DateTimeArrayFormatter.Instance },

        { typeof(List<byte>), ByteListFormatter.Instance },
        { typeof(List<sbyte>), Int8ListFormatter.Instance },
        { typeof(List<ushort>), UInt16ListFormatter.Instance },
        { typeof(List<short>), Int16ListFormatter.Instance },
        { typeof(List<uint>), UInt32ListFormatter.Instance },
        { typeof(List<int>), Int32ListFormatter.Instance },
        { typeof(List<ulong>), UInt64ListFormatter.Instance },
        { typeof(List<long>), Int64ListFormatter.Instance },
        { typeof(List<float>), SingleListFormatter.Instance },
        { typeof(List<double>), DoubleListFormatter.Instance },
        { typeof(List<bool>), BooleanListFormatter.Instance },
        { typeof(List<string>), StringListFormatter.Instance },
        { typeof(List<char>), CharListFormatter.Instance },
        { typeof(List<DateTime>), DateTimeListFormatter.Instance },

        // StandardClassLibraryFormatter
        { typeof(decimal), DecimalFormatter.Instance },
        { typeof(decimal?), new StaticNullableFormatter<decimal>(DecimalFormatter.Instance) },
        { typeof(TimeSpan), TimeSpanFormatter.Instance },
        { typeof(TimeSpan?), new StaticNullableFormatter<TimeSpan>(TimeSpanFormatter.Instance) },
        { typeof(DateTimeOffset), DateTimeOffsetFormatter.Instance },
        { typeof(DateTimeOffset?), new StaticNullableFormatter<DateTimeOffset>(DateTimeOffsetFormatter.Instance) },
        { typeof(Guid), GuidFormatter.Instance },
        { typeof(Guid?), new StaticNullableFormatter<Guid>(GuidFormatter.Instance) },
        { typeof(Uri), UriFormatter.Instance },
        { typeof(Version), VersionFormatter.Instance },
        { typeof(StringBuilder), StringBuilderFormatter.Instance },
        { typeof(BitArray), BitArrayFormatter.Instance },
        { typeof(Type), TypeFormatter<Type>.Instance },
        { typeof(System.Numerics.BigInteger), BigIntegerFormatter.Instance },
        { typeof(System.Numerics.BigInteger?), new StaticNullableFormatter<System.Numerics.BigInteger>(BigIntegerFormatter.Instance) },
        { typeof(System.Numerics.Complex), ComplexFormatter.Instance },
        { typeof(System.Numerics.Complex?), new StaticNullableFormatter<System.Numerics.Complex>(ComplexFormatter.Instance) },

        // Nil
        { typeof(Nil), NilFormatter.Instance },
        { typeof(Nil?), NullableNilFormatter.Instance },

        { typeof(object[]), new ArrayFormatter<object>() },
        { typeof(List<object>), new ListFormatter<object>() },

        { typeof(Memory<byte>), ByteMemoryFormatter.Instance },
        { typeof(Memory<byte>?), new StaticNullableFormatter<Memory<byte>>(ByteMemoryFormatter.Instance) },
        { typeof(ReadOnlyMemory<byte>), ByteReadOnlyMemoryFormatter.Instance },
        { typeof(ReadOnlyMemory<byte>?), new StaticNullableFormatter<ReadOnlyMemory<byte>>(ByteReadOnlyMemoryFormatter.Instance) },
        { typeof(ReadOnlySequence<byte>), ByteReadOnlySequenceFormatter.Instance },
        { typeof(ReadOnlySequence<byte>?), new StaticNullableFormatter<ReadOnlySequence<byte>>(ByteReadOnlySequenceFormatter.Instance) },
        { typeof(ArraySegment<byte>), ByteArraySegmentFormatter.Instance },
        { typeof(ArraySegment<byte>?), new StaticNullableFormatter<ArraySegment<byte>>(ByteArraySegmentFormatter.Instance) },

        // Extra
        { typeof(IPAddress), IPAddressFormatter.Instance },
        { typeof(IPEndPoint), IPEndPointFormatter.Instance },
        // { typeof(BytePool.RentMemory), RentMemoryFormatter.Instance },
        // { typeof(BytePool.RentReadOnlyMemory), RentReadOnlyMemoryFormatter.Instance },
        { typeof(Struct128), Struct128Formatter.Instance },
        { typeof(Struct256), Struct256Formatter.Instance },
    };

    private BuiltinResolver()
    {
    }

    public ITinyhandFormatter<T>? TryGetFormatter<T>()
    {
        return FormatterCache<T>.Formatter;
    }

    private static class FormatterCache<T>
    {
        public static readonly ITinyhandFormatter<T>? Formatter;

        static FormatterCache()
        {
            if (BuiltinResolver.TypeToFormatter.TryGetValue(typeof(T), out var obj))
            {
                FormatterCache<T>.Formatter = (ITinyhandFormatter<T>)obj;
            }
        }
    }
}
