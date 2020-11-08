// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Arc.Visceral;
using Tinyhand.Generator;

namespace Tinyhand.Coders
{
    public sealed class EnumResolver : ICoderResolver
    {
        /// <summary>
        /// The singleton instance that can be used.
        /// </summary>
        public static readonly EnumResolver Instance = new EnumResolver();

        public ITinyhandCoder? TryGetCoder(WithNullable<TinyhandObject> withNullable)
        {
            if (withNullable.Object?.Kind == VisceralObjectKind.Enum)
            {// Enum
                return new EnumCoder(withNullable.Object);
            }

            return null;
        }
    }

    public class EnumCoder : ITinyhandCoder
    {
        public EnumCoder(TinyhandObject enumType)
        {
            this.SerializeFormat = "writer.WriteNil();";
            this.DeserializeFormat = "reader.Skip();";

            var underlying = enumType.Enum_UnderlyingTypeObject;
            if (underlying == null)
            {
                return;
            }

            switch (underlying.FullName)
            {
                case "sbyte":
                    this.SerializeFormat = $"var ev = {{0}}; writer.Write(Unsafe.As<{enumType.FullName}, sbyte>(ref ev));";
                    this.DeserializeFormat = $"var ev = reader.ReadInt8(); {{0}} = Unsafe.As<sbyte, {enumType.FullName}>(ref ev);";
                    break;
                case "byte":
                    this.SerializeFormat = $"var ev = {{0}}; writer.Write(Unsafe.As<{enumType.FullName}, byte>(ref ev));";
                    this.DeserializeFormat = $"var ev = reader.ReadUInt8(); {{0}} = Unsafe.As<byte, {enumType.FullName}>(ref ev);";
                    break;
                case "short":
                    this.SerializeFormat = $"var ev = {{0}}; writer.Write(Unsafe.As<{enumType.FullName}, short>(ref ev));";
                    this.DeserializeFormat = $"var ev = reader.ReadInt16(); {{0}} = Unsafe.As<short, {enumType.FullName}>(ref ev);";
                    break;
                case "ushort":
                    this.SerializeFormat = $"var ev = {{0}}; writer.Write(Unsafe.As<{enumType.FullName}, ushort>(ref ev));";
                    this.DeserializeFormat = $"var ev = reader.ReadUInt16(); {{0}} = Unsafe.As<ushort, {enumType.FullName}>(ref ev);";
                    break;
                case "int":
                    this.SerializeFormat = $"var ev = {{0}}; writer.Write(Unsafe.As<{enumType.FullName}, int>(ref ev));";
                    this.DeserializeFormat = $"var ev = reader.ReadInt32(); {{0}} = Unsafe.As<int, {enumType.FullName}>(ref ev);";
                    break;
                case "uint":
                    this.SerializeFormat = $"var ev = {{0}}; writer.Write(Unsafe.As<{enumType.FullName}, uint>(ref ev));";
                    this.DeserializeFormat = $"var ev = reader.ReadUInt32(); {{0}} = Unsafe.As<uint, {enumType.FullName}>(ref ev);";
                    break;
                case "long":
                    this.SerializeFormat = $"var ev = {{0}}; writer.Write(Unsafe.As<{enumType.FullName}, long>(ref ev));";
                    this.DeserializeFormat = $"var ev = reader.ReadInt64(); {{0}} = Unsafe.As<long, {enumType.FullName}>(ref ev);";
                    break;
                case "ulong":
                    this.SerializeFormat = $"var ev = {{0}}; writer.Write(Unsafe.As<{enumType.FullName}, ulong>(ref ev));";
                    this.DeserializeFormat = $"var ev = reader.ReadUInt64(); {{0}} = Unsafe.As<ulong, {enumType.FullName}>(ref ev);";
                    break;

                default:
                    break;
            }
        }

        public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine(string.Format(this.SerializeFormat, ssb.FullObject));
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
        {
            ssb.AppendLine(string.Format(this.DeserializeFormat, ssb.FullObject));
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = default;");
        }

        public string SerializeFormat { get; }

        public string DeserializeFormat { get; }
    }
}
