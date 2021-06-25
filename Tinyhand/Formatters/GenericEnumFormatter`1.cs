// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Tinyhand.IO;

#pragma warning disable SA1107 // Code should not contain multiple statements on one line
#pragma warning disable SA1121 // Use built-in type alias

namespace Tinyhand.Formatters
{
    public sealed class GenericEnumFormatter<T> : ITinyhandFormatter<T>
        where T : Enum
    {
        private delegate void EnumSerialize(ref TinyhandWriter writer, ref T value);

        private delegate T EnumDeserialize(ref TinyhandReader reader);

        private readonly EnumSerialize serializer;
        private readonly EnumDeserialize deserializer;

        public GenericEnumFormatter()
        {
            var underlyingType = typeof(T).GetEnumUnderlyingType();
            switch (Type.GetTypeCode(underlyingType))
            {
                case TypeCode.Byte:
                    this.serializer = (ref TinyhandWriter writer, ref T value) => writer.Write(Unsafe.As<T, Byte>(ref value));
                    this.deserializer = (ref TinyhandReader reader) => { var v = reader.ReadUInt8(); return Unsafe.As<Byte, T>(ref v); };
                    break;
                case TypeCode.Int16:
                    this.serializer = (ref TinyhandWriter writer, ref T value) => writer.Write(Unsafe.As<T, Int16>(ref value));
                    this.deserializer = (ref TinyhandReader reader) => { var v = reader.ReadInt16(); return Unsafe.As<Int16, T>(ref v); };
                    break;
                case TypeCode.Int32:
                    this.serializer = (ref TinyhandWriter writer, ref T value) => writer.Write(Unsafe.As<T, Int32>(ref value));
                    this.deserializer = (ref TinyhandReader reader) => { var v = reader.ReadInt32(); return Unsafe.As<Int32, T>(ref v); };
                    break;
                case TypeCode.Int64:
                    this.serializer = (ref TinyhandWriter writer, ref T value) => writer.Write(Unsafe.As<T, Int64>(ref value));
                    this.deserializer = (ref TinyhandReader reader) => { var v = reader.ReadInt64(); return Unsafe.As<Int64, T>(ref v); };
                    break;
                case TypeCode.SByte:
                    this.serializer = (ref TinyhandWriter writer, ref T value) => writer.Write(Unsafe.As<T, SByte>(ref value));
                    this.deserializer = (ref TinyhandReader reader) => { var v = reader.ReadInt8(); return Unsafe.As<SByte, T>(ref v); };
                    break;
                case TypeCode.UInt16:
                    this.serializer = (ref TinyhandWriter writer, ref T value) => writer.Write(Unsafe.As<T, UInt16>(ref value));
                    this.deserializer = (ref TinyhandReader reader) => { var v = reader.ReadUInt16(); return Unsafe.As<UInt16, T>(ref v); };
                    break;
                case TypeCode.UInt32:
                    this.serializer = (ref TinyhandWriter writer, ref T value) => writer.Write(Unsafe.As<T, UInt32>(ref value));
                    this.deserializer = (ref TinyhandReader reader) => { var v = reader.ReadUInt32(); return Unsafe.As<UInt32, T>(ref v); };
                    break;
                case TypeCode.UInt64:
                    this.serializer = (ref TinyhandWriter writer, ref T value) => writer.Write(Unsafe.As<T, UInt64>(ref value));
                    this.deserializer = (ref TinyhandReader reader) => { var v = reader.ReadUInt64(); return Unsafe.As<UInt64, T>(ref v); };
                    break;
                default:
                    this.serializer = (ref TinyhandWriter writer, ref T value) => { };
                    this.deserializer = (ref TinyhandReader reader) => default!;
                    break;
            }
        }

        public void Serialize(ref TinyhandWriter writer, T? value, TinyhandSerializerOptions options)
        {
            this.serializer(ref writer, ref value!);
        }

        public T? Deserialize(ref TinyhandReader reader, TinyhandSerializerOptions options)
        {
            return this.deserializer(ref reader);
        }

        public T Reconstruct(TinyhandSerializerOptions options) => default!;

        public T? Clone(T? value, TinyhandSerializerOptions options) => value;
    }
}
