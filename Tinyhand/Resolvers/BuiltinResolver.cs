// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Tinyhand.Formatters;

#pragma warning disable SA1509 // Opening braces should not be preceded by blank line

namespace Tinyhand.Resolvers
{
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
}
