// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using Tinyhand.IO;

#pragma warning disable SA1121 // Use built-in type alias
#pragma warning disable SA1649 // File name should match first type name

namespace Tinyhand.Formatters
{
    public sealed class NativeGuidFormatter : ITinyhandFormatter<Guid>
    {
        /// <summary>
        /// Unsafe binary Guid formatter. this is only allowed on LittleEndian environment.
        /// </summary>
        public static readonly ITinyhandFormatter<Guid> Instance = new NativeGuidFormatter();

        private NativeGuidFormatter()
        {
        }

        /* Guid's underlying _a,...,_k field is sequential and same layout as .NET Framework and Mono(Unity).
         * But target machines must be same endian so restrict only for little endian. */

        public unsafe void Serialize(ref TinyhandWriter writer, ref Guid value, TinyhandSerializerOptions options)
        {
            if (!BitConverter.IsLittleEndian)
            {
                throw new InvalidOperationException("NativeGuidFormatter only allows on little endian env.");
            }

            var valueSpan = new ReadOnlySpan<byte>(Unsafe.AsPointer(ref value), sizeof(Guid));
            writer.Write(valueSpan);
        }

        public unsafe void Deserialize(ref TinyhandReader reader, ref Guid value, TinyhandSerializerOptions options)
        {
            if (!BitConverter.IsLittleEndian)
            {
                throw new InvalidOperationException("NativeGuidFormatter only allows on little endian env.");
            }

            var seq = reader.ReadBytes();
            ReadOnlySequence<byte> valueSequence = seq.HasValue ? seq.Value : default;
            if (valueSequence.Length != sizeof(Guid))
            {
                throw new TinyhandException("Invalid Guid Size.");
            }

            Guid result;
            var resultSpan = new Span<byte>(&result, sizeof(Guid));
            valueSequence.CopyTo(resultSpan);
            value = result;
        }

        public Guid Reconstruct(TinyhandSerializerOptions options)
        {
            return default;
        }
    }

    public sealed class NativeDecimalFormatter : ITinyhandFormatter<Decimal>
    {
        /// <summary>
        /// Unsafe binary Decimal formatter. this is only allows on LittleEndian environment.
        /// </summary>
        public static readonly ITinyhandFormatter<Decimal> Instance = new NativeDecimalFormatter();

        private NativeDecimalFormatter()
        {
        }

        /* decimal underlying "flags, hi, lo, mid" fields are sequential and same layuout with .NET Framework and Mono(Unity)
         * But target machines must be same endian so restrict only for little endian. */

        public unsafe void Serialize(ref TinyhandWriter writer, ref Decimal value, TinyhandSerializerOptions options)
        {
            if (!BitConverter.IsLittleEndian)
            {
                throw new InvalidOperationException("NativeDecimalFormatter only allows on little endian env.");
            }

            var valueSpan = new ReadOnlySpan<byte>(Unsafe.AsPointer(ref value), sizeof(Decimal));
            writer.Write(valueSpan);
        }

        public unsafe void Deserialize(ref TinyhandReader reader, ref Decimal value, TinyhandSerializerOptions options)
        {
            if (!BitConverter.IsLittleEndian)
            {
                throw new InvalidOperationException("NativeDecimalFormatter only allows on little endian env.");
            }

            var seq = reader.ReadBytes();
            ReadOnlySequence<byte> valueSequence = seq.HasValue ? seq.Value : default;
            if (valueSequence.Length != sizeof(decimal))
            {
                throw new TinyhandException("Invalid decimal Size.");
            }

            decimal result;
            var resultSpan = new Span<byte>(&result, sizeof(decimal));
            valueSequence.CopyTo(resultSpan);
            value = result;
        }

        public Decimal Reconstruct(TinyhandSerializerOptions options)
        {
            return default;
        }
    }
}
