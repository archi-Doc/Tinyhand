// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net.Security;
using System.Reflection;
using Tinyhand.IO;

#pragma warning disable SA1009 // Closing parenthesis should be spaced correctly
#pragma warning disable SA1121 // Use built-in type alias

namespace Tinyhand.Formatters
{
    public class PrimitiveObjectFormatter : ITinyhandFormatter<object>
    {
        public static readonly ITinyhandFormatter<object> Instance = new PrimitiveObjectFormatter();

        private static readonly Dictionary<Type, int> TypeToJumpCode = new Dictionary<Type, int>()
        {
            // When adding types whose size exceeds 32-bits, add support in TinyhandSecurity.GetHashCollisionResistantEqualityComparer<T>()
            { typeof(Boolean), 0 },
            { typeof(Char), 1 },
            { typeof(SByte), 2 },
            { typeof(Byte), 3 },
            { typeof(Int16), 4 },
            { typeof(UInt16), 5 },
            { typeof(Int32), 6 },
            { typeof(UInt32), 7 },
            { typeof(Int64), 8 },
            { typeof(UInt64), 9 },
            { typeof(Single), 10 },
            { typeof(Double), 11 },
            { typeof(DateTime), 12 },
            { typeof(string), 13 },
            { typeof(byte[]), 14 },
        };

        protected PrimitiveObjectFormatter()
        {
        }

        public static bool IsSupportedType(Type type, TypeInfo typeInfo, object value)
        {
            if (value == null)
            {
                return true;
            }

            if (TypeToJumpCode.ContainsKey(type))
            {
                return true;
            }

            if (typeInfo.IsEnum)
            {
                return true;
            }

            if (value is System.Collections.IDictionary)
            {
                return true;
            }

            if (value is System.Collections.ICollection)
            {
                return true;
            }

            return false;
        }

        public void Serialize(ref TinyhandWriter writer, ref object? value, TinyhandSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            Type t = value.GetType();

            int code;
            if (TypeToJumpCode.TryGetValue(t, out code))
            {
                switch (code)
                {
                    case 0:
                        writer.Write((bool)value);
                        return;
                    case 1:
                        writer.Write((char)value);
                        return;
                    case 2:
                        writer.WriteInt8((sbyte)value);
                        return;
                    case 3:
                        writer.WriteUInt8((byte)value);
                        return;
                    case 4:
                        writer.WriteInt16((Int16)value);
                        return;
                    case 5:
                        writer.WriteUInt16((UInt16)value);
                        return;
                    case 6:
                        writer.WriteInt32((Int32)value);
                        return;
                    case 7:
                        writer.WriteUInt32((UInt32)value);
                        return;
                    case 8:
                        writer.WriteInt64((Int64)value);
                        return;
                    case 9:
                        writer.WriteUInt64((UInt64)value);
                        return;
                    case 10:
                        writer.Write((Single)value);
                        return;
                    case 11:
                        writer.Write((double)value);
                        return;
                    case 12:
                        writer.Write((DateTime)value);
                        return;
                    case 13:
                        writer.Write((string)value);
                        return;
                    case 14:
                        writer.Write((byte[])value);
                        return;
                    default:
                        throw new TinyhandException("Not supported primitive object resolver. type:" + t.Name);
                }
            }
            else
            {
#if UNITY_2018_3_OR_NEWER && !NETFX_CORE
                if (t.IsEnum)
#else
                if (t.GetTypeInfo().IsEnum)
#endif
                {
                    Type underlyingType = Enum.GetUnderlyingType(t);
                    var code2 = TypeToJumpCode[underlyingType];
                    switch (code2)
                    {
                        case 2:
                            writer.WriteInt8((sbyte)value);
                            return;
                        case 3:
                            writer.WriteUInt8((byte)value);
                            return;
                        case 4:
                            writer.WriteInt16((Int16)value);
                            return;
                        case 5:
                            writer.WriteUInt16((UInt16)value);
                            return;
                        case 6:
                            writer.WriteInt32((Int32)value);
                            return;
                        case 7:
                            writer.WriteUInt32((UInt32)value);
                            return;
                        case 8:
                            writer.WriteInt64((Int64)value);
                            return;
                        case 9:
                            writer.WriteUInt64((UInt64)value);
                            return;
                        default:
                            break;
                    }
                }
                else if (value is System.Collections.IDictionary d)
                {
                    // check IDictionary first
                    writer.WriteMapHeader(d.Count);
                    foreach (System.Collections.DictionaryEntry item in d)
                    {
                        var rv = item.Key;
                        var rv2 = item.Value;
                        this.Serialize(ref writer, ref rv, options);
                        this.Serialize(ref writer, ref rv2, options);
                    }

                    return;
                }
                else if (value is System.Collections.ICollection c)
                {
                    writer.WriteArrayHeader(c.Count);
                    foreach (var item in c)
                    {
                        var rv = item;
                        this.Serialize(ref writer, ref rv, options);
                    }

                    return;
                }
            }

            throw new TinyhandException("Not supported primitive object resolver. type:" + t.Name);
        }

        public void Deserialize(ref TinyhandReader reader, ref object? value, TinyhandSerializerOptions options)
        {
            MessagePackType type = reader.NextMessagePackType;
            IFormatterResolver resolver = options.Resolver;
            switch (type)
            {
                case MessagePackType.Integer:
                    var code = reader.NextCode;
                    if (code >= MessagePackCode.MinNegativeFixInt && code <= MessagePackCode.MaxNegativeFixInt)
                    {
                        value = reader.ReadInt8();
                        return;
                    }
                    else if (code >= MessagePackCode.MinFixInt && code <= MessagePackCode.MaxFixInt)
                    {
                        value = reader.ReadUInt8();
                        return;
                    }
                    else if (code == MessagePackCode.Int8)
                    {
                        value = reader.ReadInt8();
                        return;
                    }
                    else if (code == MessagePackCode.Int16)
                    {
                        value = reader.ReadInt16();
                        return;
                    }
                    else if (code == MessagePackCode.Int32)
                    {
                        value = reader.ReadInt32();
                        return;
                    }
                    else if (code == MessagePackCode.Int64)
                    {
                        value = reader.ReadInt64();
                        return;
                    }
                    else if (code == MessagePackCode.UInt8)
                    {
                        value = reader.ReadUInt8();
                        return;
                    }
                    else if (code == MessagePackCode.UInt16)
                    {
                        value = reader.ReadUInt16();
                        return;
                    }
                    else if (code == MessagePackCode.UInt32)
                    {
                        value = reader.ReadUInt32();
                        return;
                    }
                    else if (code == MessagePackCode.UInt64)
                    {
                        value = reader.ReadUInt64();
                        return;
                    }

                    throw new TinyhandException("Invalid primitive bytes.");
                case MessagePackType.Boolean:
                    value = reader.ReadBoolean();
                    return;
                case MessagePackType.Float:
                    if (reader.NextCode == MessagePackCode.Float32)
                    {
                        value = reader.ReadSingle();
                        return;
                    }
                    else
                    {
                        value = reader.ReadDouble();
                        return;
                    }

                case MessagePackType.String:
                    value = reader.ReadString();
                    return;
                case MessagePackType.Binary:
                    // We must copy the sequence returned by ReadBytes since the reader's sequence is only valid during deserialization.
                    value = reader.ReadBytes()?.ToArray();
                    return;
                case MessagePackType.Extension:
                    ExtensionHeader ext = reader.ReadExtensionFormatHeader();
                    if (ext.TypeCode == ReservedMessagePackExtensionTypeCode.DateTime)
                    {
                        value = reader.ReadDateTime(ext);
                        return;
                    }

                    throw new TinyhandException("Invalid primitive bytes.");
                case MessagePackType.Array:
                    {
                        var length = reader.ReadArrayHeader();
                        if (length == 0)
                        {
                            value = Array.Empty<object>();
                            return;
                        }

                        ITinyhandFormatter<object> objectFormatter = resolver.GetFormatter<object>();
                        var array = new object[length];
                        options.Security.DepthStep(ref reader);
                        try
                        {
                            for (int i = 0; i < length; i++)
                            {
                                objectFormatter.Deserialize(ref reader, ref array[i]!, options);
                            }
                        }
                        finally
                        {
                            reader.Depth--;
                        }

                        value = array;
                        return;
                    }

                case MessagePackType.Map:
                    {
                        var length = reader.ReadMapHeader();

                        options.Security.DepthStep(ref reader);
                        try
                        {
                            value = this.DeserializeMap(ref reader, length, options);
                            return;
                        }
                        finally
                        {
                            reader.Depth--;
                        }
                    }

                case MessagePackType.Nil:
                    reader.ReadNil();
                    value = null;
                    return;
                default:
                    throw new TinyhandException("Invalid primitive bytes.");
            }
        }

        public object Reconstruct(TinyhandSerializerOptions options)
        {
            return default!;
        }

        protected virtual object DeserializeMap(ref TinyhandReader reader, int length, TinyhandSerializerOptions options)
        {
            ITinyhandFormatter<object> objectFormatter = options.Resolver.GetFormatter<object>();
            var dictionary = new Dictionary<object, object>(length, options.Security.GetEqualityComparer<object>());
            for (int i = 0; i < length; i++)
            {
                object? key = default;
                objectFormatter.Deserialize(ref reader, ref key, options);
                object? value = default;
                objectFormatter.Deserialize(ref reader, ref value, options);
                dictionary.Add(key!, value!);
            }

            return dictionary;
        }
    }
}
