// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

/* THIS (.cs) FILE IS GENERATED. DO NOT CHANGE IT.
 * CHANGE THE .tt FILE INSTEAD. */

using System;
using Arc.Visceral;
using Tinyhand.Generator;

#pragma warning disable SA1649 // File name should match first type name

namespace Tinyhand.Coders
{
    public sealed class UInt8Coder : ITinyhandCoder
    {
        public static readonly UInt8Coder Instance = new ();

        private UInt8Coder()
        {
        }

        public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"writer.Write({ssb.FullObject});");
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
        {
            ssb.AppendLine($"{ssb.FullObject} = reader.ReadUInt8();");
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = 0;");
        }
    }

    public sealed class NullableUInt8Coder : ITinyhandCoder
    {
        public static readonly NullableUInt8Coder Instance = new ();

        private NullableUInt8Coder()
        {
        }

        public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            using (var b = ssb.ScopeBrace($"if ({ssb.FullObject} == null)"))
            {
                ssb.AppendLine("writer.WriteNil();");
            }

            using (var b = ssb.ScopeBrace($"else"))
            {
                ssb.AppendLine($"writer.Write({ssb.FullObject}.Value);");
            }
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
        {
            if (nilChecked)
            {
                ssb.AppendLine($"{ssb.FullObject} = reader.ReadUInt8();");
            }
            else
            {
                using (var b = ssb.ScopeBrace("if (reader.TryReadNil())"))
                {
                    ssb.AppendLine($"{ssb.FullObject} = default;");
                }

                using (var b = ssb.ScopeBrace($"else"))
                {
                    ssb.AppendLine($"{ssb.FullObject} = reader.ReadUInt8();");
                }
            }
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = 0;");
        }
    }

    public sealed class Int8Coder : ITinyhandCoder
    {
        public static readonly Int8Coder Instance = new ();

        private Int8Coder()
        {
        }

        public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"writer.Write({ssb.FullObject});");
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
        {
            ssb.AppendLine($"{ssb.FullObject} = reader.ReadInt8();");
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = 0;");
        }
    }

    public sealed class NullableInt8Coder : ITinyhandCoder
    {
        public static readonly NullableInt8Coder Instance = new ();

        private NullableInt8Coder()
        {
        }

        public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            using (var b = ssb.ScopeBrace($"if ({ssb.FullObject} == null)"))
            {
                ssb.AppendLine("writer.WriteNil();");
            }

            using (var b = ssb.ScopeBrace($"else"))
            {
                ssb.AppendLine($"writer.Write({ssb.FullObject}.Value);");
            }
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
        {
            if (nilChecked)
            {
                ssb.AppendLine($"{ssb.FullObject} = reader.ReadInt8();");
            }
            else
            {
                using (var b = ssb.ScopeBrace("if (reader.TryReadNil())"))
                {
                    ssb.AppendLine($"{ssb.FullObject} = default;");
                }

                using (var b = ssb.ScopeBrace($"else"))
                {
                    ssb.AppendLine($"{ssb.FullObject} = reader.ReadInt8();");
                }
            }
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = 0;");
        }
    }

    public sealed class Int8ArrayCoder : ITinyhandCoder
    {
        public static readonly Int8ArrayCoder Instance = new ();

        private Int8ArrayCoder()
        {
        }

        public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"global::Tinyhand.Formatters.Builtin.SerializeInt8Array(ref writer, {ssb.FullObject});");
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
        {
            ssb.AppendLine($"{ssb.FullObject} = global::Tinyhand.Formatters.Builtin.DeserializeInt8Array(ref reader) ?? new sbyte[0];");
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = new sbyte[0];");
        }
    }

    public sealed class NullableInt8ArrayCoder : ITinyhandCoder
    {
        public static readonly NullableInt8ArrayCoder Instance = new ();

        private NullableInt8ArrayCoder()
        {
        }

        public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"global::Tinyhand.Formatters.Builtin.SerializeInt8Array(ref writer, {ssb.FullObject});");
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
        {
            ssb.AppendLine($"{ssb.FullObject} = global::Tinyhand.Formatters.Builtin.DeserializeInt8Array(ref reader);");
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = new sbyte[0];");
        }
    }

    public sealed class Int8ListCoder : ITinyhandCoder
    {
        public static readonly Int8ListCoder Instance = new ();

        private Int8ListCoder()
        {
        }

        public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"global::Tinyhand.Formatters.Builtin.SerializeInt8List(ref writer, {ssb.FullObject});");
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
        {
            ssb.AppendLine($"{ssb.FullObject} = global::Tinyhand.Formatters.Builtin.DeserializeInt8List(ref reader) ?? new List<sbyte>();");
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = new List<sbyte>();");
        }
    }

    public sealed class NullableInt8ListCoder : ITinyhandCoder
    {
        public static readonly NullableInt8ListCoder Instance = new ();

        private NullableInt8ListCoder()
        {
        }

        public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"global::Tinyhand.Formatters.Builtin.SerializeInt8List(ref writer, {ssb.FullObject});");
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
        {
            ssb.AppendLine($"{ssb.FullObject} = global::Tinyhand.Formatters.Builtin.DeserializeInt8List(ref reader);");
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = new List<sbyte>();");
        }
    }

    public sealed class UInt16Coder : ITinyhandCoder
    {
        public static readonly UInt16Coder Instance = new ();

        private UInt16Coder()
        {
        }

        public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"writer.Write({ssb.FullObject});");
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
        {
            ssb.AppendLine($"{ssb.FullObject} = reader.ReadUInt16();");
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = 0;");
        }
    }

    public sealed class NullableUInt16Coder : ITinyhandCoder
    {
        public static readonly NullableUInt16Coder Instance = new ();

        private NullableUInt16Coder()
        {
        }

        public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            using (var b = ssb.ScopeBrace($"if ({ssb.FullObject} == null)"))
            {
                ssb.AppendLine("writer.WriteNil();");
            }

            using (var b = ssb.ScopeBrace($"else"))
            {
                ssb.AppendLine($"writer.Write({ssb.FullObject}.Value);");
            }
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
        {
            if (nilChecked)
            {
                ssb.AppendLine($"{ssb.FullObject} = reader.ReadUInt16();");
            }
            else
            {
                using (var b = ssb.ScopeBrace("if (reader.TryReadNil())"))
                {
                    ssb.AppendLine($"{ssb.FullObject} = default;");
                }

                using (var b = ssb.ScopeBrace($"else"))
                {
                    ssb.AppendLine($"{ssb.FullObject} = reader.ReadUInt16();");
                }
            }
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = 0;");
        }
    }

    public sealed class UInt16ArrayCoder : ITinyhandCoder
    {
        public static readonly UInt16ArrayCoder Instance = new ();

        private UInt16ArrayCoder()
        {
        }

        public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"global::Tinyhand.Formatters.Builtin.SerializeUInt16Array(ref writer, {ssb.FullObject});");
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
        {
            ssb.AppendLine($"{ssb.FullObject} = global::Tinyhand.Formatters.Builtin.DeserializeUInt16Array(ref reader) ?? new ushort[0];");
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = new ushort[0];");
        }
    }

    public sealed class NullableUInt16ArrayCoder : ITinyhandCoder
    {
        public static readonly NullableUInt16ArrayCoder Instance = new ();

        private NullableUInt16ArrayCoder()
        {
        }

        public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"global::Tinyhand.Formatters.Builtin.SerializeUInt16Array(ref writer, {ssb.FullObject});");
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
        {
            ssb.AppendLine($"{ssb.FullObject} = global::Tinyhand.Formatters.Builtin.DeserializeUInt16Array(ref reader);");
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = new ushort[0];");
        }
    }

    public sealed class UInt16ListCoder : ITinyhandCoder
    {
        public static readonly UInt16ListCoder Instance = new ();

        private UInt16ListCoder()
        {
        }

        public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"global::Tinyhand.Formatters.Builtin.SerializeUInt16List(ref writer, {ssb.FullObject});");
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
        {
            ssb.AppendLine($"{ssb.FullObject} = global::Tinyhand.Formatters.Builtin.DeserializeUInt16List(ref reader) ?? new List<ushort>();");
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = new List<ushort>();");
        }
    }

    public sealed class NullableUInt16ListCoder : ITinyhandCoder
    {
        public static readonly NullableUInt16ListCoder Instance = new ();

        private NullableUInt16ListCoder()
        {
        }

        public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"global::Tinyhand.Formatters.Builtin.SerializeUInt16List(ref writer, {ssb.FullObject});");
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
        {
            ssb.AppendLine($"{ssb.FullObject} = global::Tinyhand.Formatters.Builtin.DeserializeUInt16List(ref reader);");
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = new List<ushort>();");
        }
    }

    public sealed class Int16Coder : ITinyhandCoder
    {
        public static readonly Int16Coder Instance = new ();

        private Int16Coder()
        {
        }

        public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"writer.Write({ssb.FullObject});");
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
        {
            ssb.AppendLine($"{ssb.FullObject} = reader.ReadInt16();");
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = 0;");
        }
    }

    public sealed class NullableInt16Coder : ITinyhandCoder
    {
        public static readonly NullableInt16Coder Instance = new ();

        private NullableInt16Coder()
        {
        }

        public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            using (var b = ssb.ScopeBrace($"if ({ssb.FullObject} == null)"))
            {
                ssb.AppendLine("writer.WriteNil();");
            }

            using (var b = ssb.ScopeBrace($"else"))
            {
                ssb.AppendLine($"writer.Write({ssb.FullObject}.Value);");
            }
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
        {
            if (nilChecked)
            {
                ssb.AppendLine($"{ssb.FullObject} = reader.ReadInt16();");
            }
            else
            {
                using (var b = ssb.ScopeBrace("if (reader.TryReadNil())"))
                {
                    ssb.AppendLine($"{ssb.FullObject} = default;");
                }

                using (var b = ssb.ScopeBrace($"else"))
                {
                    ssb.AppendLine($"{ssb.FullObject} = reader.ReadInt16();");
                }
            }
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = 0;");
        }
    }

    public sealed class Int16ArrayCoder : ITinyhandCoder
    {
        public static readonly Int16ArrayCoder Instance = new ();

        private Int16ArrayCoder()
        {
        }

        public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"global::Tinyhand.Formatters.Builtin.SerializeInt16Array(ref writer, {ssb.FullObject});");
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
        {
            ssb.AppendLine($"{ssb.FullObject} = global::Tinyhand.Formatters.Builtin.DeserializeInt16Array(ref reader) ?? new short[0];");
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = new short[0];");
        }
    }

    public sealed class NullableInt16ArrayCoder : ITinyhandCoder
    {
        public static readonly NullableInt16ArrayCoder Instance = new ();

        private NullableInt16ArrayCoder()
        {
        }

        public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"global::Tinyhand.Formatters.Builtin.SerializeInt16Array(ref writer, {ssb.FullObject});");
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
        {
            ssb.AppendLine($"{ssb.FullObject} = global::Tinyhand.Formatters.Builtin.DeserializeInt16Array(ref reader);");
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = new short[0];");
        }
    }

    public sealed class Int16ListCoder : ITinyhandCoder
    {
        public static readonly Int16ListCoder Instance = new ();

        private Int16ListCoder()
        {
        }

        public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"global::Tinyhand.Formatters.Builtin.SerializeInt16List(ref writer, {ssb.FullObject});");
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
        {
            ssb.AppendLine($"{ssb.FullObject} = global::Tinyhand.Formatters.Builtin.DeserializeInt16List(ref reader) ?? new List<short>();");
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = new List<short>();");
        }
    }

    public sealed class NullableInt16ListCoder : ITinyhandCoder
    {
        public static readonly NullableInt16ListCoder Instance = new ();

        private NullableInt16ListCoder()
        {
        }

        public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"global::Tinyhand.Formatters.Builtin.SerializeInt16List(ref writer, {ssb.FullObject});");
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
        {
            ssb.AppendLine($"{ssb.FullObject} = global::Tinyhand.Formatters.Builtin.DeserializeInt16List(ref reader);");
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = new List<short>();");
        }
    }

    public sealed class UInt32Coder : ITinyhandCoder
    {
        public static readonly UInt32Coder Instance = new ();

        private UInt32Coder()
        {
        }

        public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"writer.Write({ssb.FullObject});");
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
        {
            ssb.AppendLine($"{ssb.FullObject} = reader.ReadUInt32();");
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = 0;");
        }
    }

    public sealed class NullableUInt32Coder : ITinyhandCoder
    {
        public static readonly NullableUInt32Coder Instance = new ();

        private NullableUInt32Coder()
        {
        }

        public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            using (var b = ssb.ScopeBrace($"if ({ssb.FullObject} == null)"))
            {
                ssb.AppendLine("writer.WriteNil();");
            }

            using (var b = ssb.ScopeBrace($"else"))
            {
                ssb.AppendLine($"writer.Write({ssb.FullObject}.Value);");
            }
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
        {
            if (nilChecked)
            {
                ssb.AppendLine($"{ssb.FullObject} = reader.ReadUInt32();");
            }
            else
            {
                using (var b = ssb.ScopeBrace("if (reader.TryReadNil())"))
                {
                    ssb.AppendLine($"{ssb.FullObject} = default;");
                }

                using (var b = ssb.ScopeBrace($"else"))
                {
                    ssb.AppendLine($"{ssb.FullObject} = reader.ReadUInt32();");
                }
            }
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = 0;");
        }
    }

    public sealed class UInt32ArrayCoder : ITinyhandCoder
    {
        public static readonly UInt32ArrayCoder Instance = new ();

        private UInt32ArrayCoder()
        {
        }

        public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"global::Tinyhand.Formatters.Builtin.SerializeUInt32Array(ref writer, {ssb.FullObject});");
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
        {
            ssb.AppendLine($"{ssb.FullObject} = global::Tinyhand.Formatters.Builtin.DeserializeUInt32Array(ref reader) ?? new uint[0];");
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = new uint[0];");
        }
    }

    public sealed class NullableUInt32ArrayCoder : ITinyhandCoder
    {
        public static readonly NullableUInt32ArrayCoder Instance = new ();

        private NullableUInt32ArrayCoder()
        {
        }

        public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"global::Tinyhand.Formatters.Builtin.SerializeUInt32Array(ref writer, {ssb.FullObject});");
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
        {
            ssb.AppendLine($"{ssb.FullObject} = global::Tinyhand.Formatters.Builtin.DeserializeUInt32Array(ref reader);");
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = new uint[0];");
        }
    }

    public sealed class UInt32ListCoder : ITinyhandCoder
    {
        public static readonly UInt32ListCoder Instance = new ();

        private UInt32ListCoder()
        {
        }

        public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"global::Tinyhand.Formatters.Builtin.SerializeUInt32List(ref writer, {ssb.FullObject});");
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
        {
            ssb.AppendLine($"{ssb.FullObject} = global::Tinyhand.Formatters.Builtin.DeserializeUInt32List(ref reader) ?? new List<uint>();");
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = new List<uint>();");
        }
    }

    public sealed class NullableUInt32ListCoder : ITinyhandCoder
    {
        public static readonly NullableUInt32ListCoder Instance = new ();

        private NullableUInt32ListCoder()
        {
        }

        public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"global::Tinyhand.Formatters.Builtin.SerializeUInt32List(ref writer, {ssb.FullObject});");
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
        {
            ssb.AppendLine($"{ssb.FullObject} = global::Tinyhand.Formatters.Builtin.DeserializeUInt32List(ref reader);");
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = new List<uint>();");
        }
    }

    public sealed class Int32Coder : ITinyhandCoder
    {
        public static readonly Int32Coder Instance = new ();

        private Int32Coder()
        {
        }

        public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"writer.Write({ssb.FullObject});");
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
        {
            ssb.AppendLine($"{ssb.FullObject} = reader.ReadInt32();");
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = 0;");
        }
    }

    public sealed class NullableInt32Coder : ITinyhandCoder
    {
        public static readonly NullableInt32Coder Instance = new ();

        private NullableInt32Coder()
        {
        }

        public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            using (var b = ssb.ScopeBrace($"if ({ssb.FullObject} == null)"))
            {
                ssb.AppendLine("writer.WriteNil();");
            }

            using (var b = ssb.ScopeBrace($"else"))
            {
                ssb.AppendLine($"writer.Write({ssb.FullObject}.Value);");
            }
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
        {
            if (nilChecked)
            {
                ssb.AppendLine($"{ssb.FullObject} = reader.ReadInt32();");
            }
            else
            {
                using (var b = ssb.ScopeBrace("if (reader.TryReadNil())"))
                {
                    ssb.AppendLine($"{ssb.FullObject} = default;");
                }

                using (var b = ssb.ScopeBrace($"else"))
                {
                    ssb.AppendLine($"{ssb.FullObject} = reader.ReadInt32();");
                }
            }
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = 0;");
        }
    }

    public sealed class Int32ArrayCoder : ITinyhandCoder
    {
        public static readonly Int32ArrayCoder Instance = new ();

        private Int32ArrayCoder()
        {
        }

        public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"global::Tinyhand.Formatters.Builtin.SerializeInt32Array(ref writer, {ssb.FullObject});");
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
        {
            ssb.AppendLine($"{ssb.FullObject} = global::Tinyhand.Formatters.Builtin.DeserializeInt32Array(ref reader) ?? new int[0];");
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = new int[0];");
        }
    }

    public sealed class NullableInt32ArrayCoder : ITinyhandCoder
    {
        public static readonly NullableInt32ArrayCoder Instance = new ();

        private NullableInt32ArrayCoder()
        {
        }

        public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"global::Tinyhand.Formatters.Builtin.SerializeInt32Array(ref writer, {ssb.FullObject});");
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
        {
            ssb.AppendLine($"{ssb.FullObject} = global::Tinyhand.Formatters.Builtin.DeserializeInt32Array(ref reader);");
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = new int[0];");
        }
    }

    public sealed class Int32ListCoder : ITinyhandCoder
    {
        public static readonly Int32ListCoder Instance = new ();

        private Int32ListCoder()
        {
        }

        public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"global::Tinyhand.Formatters.Builtin.SerializeInt32List(ref writer, {ssb.FullObject});");
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
        {
            ssb.AppendLine($"{ssb.FullObject} = global::Tinyhand.Formatters.Builtin.DeserializeInt32List(ref reader) ?? new List<int>();");
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = new List<int>();");
        }
    }

    public sealed class NullableInt32ListCoder : ITinyhandCoder
    {
        public static readonly NullableInt32ListCoder Instance = new ();

        private NullableInt32ListCoder()
        {
        }

        public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"global::Tinyhand.Formatters.Builtin.SerializeInt32List(ref writer, {ssb.FullObject});");
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
        {
            ssb.AppendLine($"{ssb.FullObject} = global::Tinyhand.Formatters.Builtin.DeserializeInt32List(ref reader);");
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = new List<int>();");
        }
    }

    public sealed class UInt64Coder : ITinyhandCoder
    {
        public static readonly UInt64Coder Instance = new ();

        private UInt64Coder()
        {
        }

        public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"writer.Write({ssb.FullObject});");
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
        {
            ssb.AppendLine($"{ssb.FullObject} = reader.ReadUInt64();");
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = 0;");
        }
    }

    public sealed class NullableUInt64Coder : ITinyhandCoder
    {
        public static readonly NullableUInt64Coder Instance = new ();

        private NullableUInt64Coder()
        {
        }

        public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            using (var b = ssb.ScopeBrace($"if ({ssb.FullObject} == null)"))
            {
                ssb.AppendLine("writer.WriteNil();");
            }

            using (var b = ssb.ScopeBrace($"else"))
            {
                ssb.AppendLine($"writer.Write({ssb.FullObject}.Value);");
            }
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
        {
            if (nilChecked)
            {
                ssb.AppendLine($"{ssb.FullObject} = reader.ReadUInt64();");
            }
            else
            {
                using (var b = ssb.ScopeBrace("if (reader.TryReadNil())"))
                {
                    ssb.AppendLine($"{ssb.FullObject} = default;");
                }

                using (var b = ssb.ScopeBrace($"else"))
                {
                    ssb.AppendLine($"{ssb.FullObject} = reader.ReadUInt64();");
                }
            }
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = 0;");
        }
    }

    public sealed class UInt64ArrayCoder : ITinyhandCoder
    {
        public static readonly UInt64ArrayCoder Instance = new ();

        private UInt64ArrayCoder()
        {
        }

        public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"global::Tinyhand.Formatters.Builtin.SerializeUInt64Array(ref writer, {ssb.FullObject});");
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
        {
            ssb.AppendLine($"{ssb.FullObject} = global::Tinyhand.Formatters.Builtin.DeserializeUInt64Array(ref reader) ?? new ulong[0];");
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = new ulong[0];");
        }
    }

    public sealed class NullableUInt64ArrayCoder : ITinyhandCoder
    {
        public static readonly NullableUInt64ArrayCoder Instance = new ();

        private NullableUInt64ArrayCoder()
        {
        }

        public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"global::Tinyhand.Formatters.Builtin.SerializeUInt64Array(ref writer, {ssb.FullObject});");
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
        {
            ssb.AppendLine($"{ssb.FullObject} = global::Tinyhand.Formatters.Builtin.DeserializeUInt64Array(ref reader);");
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = new ulong[0];");
        }
    }

    public sealed class UInt64ListCoder : ITinyhandCoder
    {
        public static readonly UInt64ListCoder Instance = new ();

        private UInt64ListCoder()
        {
        }

        public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"global::Tinyhand.Formatters.Builtin.SerializeUInt64List(ref writer, {ssb.FullObject});");
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
        {
            ssb.AppendLine($"{ssb.FullObject} = global::Tinyhand.Formatters.Builtin.DeserializeUInt64List(ref reader) ?? new List<ulong>();");
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = new List<ulong>();");
        }
    }

    public sealed class NullableUInt64ListCoder : ITinyhandCoder
    {
        public static readonly NullableUInt64ListCoder Instance = new ();

        private NullableUInt64ListCoder()
        {
        }

        public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"global::Tinyhand.Formatters.Builtin.SerializeUInt64List(ref writer, {ssb.FullObject});");
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
        {
            ssb.AppendLine($"{ssb.FullObject} = global::Tinyhand.Formatters.Builtin.DeserializeUInt64List(ref reader);");
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = new List<ulong>();");
        }
    }

    public sealed class Int64Coder : ITinyhandCoder
    {
        public static readonly Int64Coder Instance = new ();

        private Int64Coder()
        {
        }

        public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"writer.Write({ssb.FullObject});");
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
        {
            ssb.AppendLine($"{ssb.FullObject} = reader.ReadInt64();");
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = 0;");
        }
    }

    public sealed class NullableInt64Coder : ITinyhandCoder
    {
        public static readonly NullableInt64Coder Instance = new ();

        private NullableInt64Coder()
        {
        }

        public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            using (var b = ssb.ScopeBrace($"if ({ssb.FullObject} == null)"))
            {
                ssb.AppendLine("writer.WriteNil();");
            }

            using (var b = ssb.ScopeBrace($"else"))
            {
                ssb.AppendLine($"writer.Write({ssb.FullObject}.Value);");
            }
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
        {
            if (nilChecked)
            {
                ssb.AppendLine($"{ssb.FullObject} = reader.ReadInt64();");
            }
            else
            {
                using (var b = ssb.ScopeBrace("if (reader.TryReadNil())"))
                {
                    ssb.AppendLine($"{ssb.FullObject} = default;");
                }

                using (var b = ssb.ScopeBrace($"else"))
                {
                    ssb.AppendLine($"{ssb.FullObject} = reader.ReadInt64();");
                }
            }
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = 0;");
        }
    }

    public sealed class Int64ArrayCoder : ITinyhandCoder
    {
        public static readonly Int64ArrayCoder Instance = new ();

        private Int64ArrayCoder()
        {
        }

        public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"global::Tinyhand.Formatters.Builtin.SerializeInt64Array(ref writer, {ssb.FullObject});");
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
        {
            ssb.AppendLine($"{ssb.FullObject} = global::Tinyhand.Formatters.Builtin.DeserializeInt64Array(ref reader) ?? new long[0];");
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = new long[0];");
        }
    }

    public sealed class NullableInt64ArrayCoder : ITinyhandCoder
    {
        public static readonly NullableInt64ArrayCoder Instance = new ();

        private NullableInt64ArrayCoder()
        {
        }

        public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"global::Tinyhand.Formatters.Builtin.SerializeInt64Array(ref writer, {ssb.FullObject});");
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
        {
            ssb.AppendLine($"{ssb.FullObject} = global::Tinyhand.Formatters.Builtin.DeserializeInt64Array(ref reader);");
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = new long[0];");
        }
    }

    public sealed class Int64ListCoder : ITinyhandCoder
    {
        public static readonly Int64ListCoder Instance = new ();

        private Int64ListCoder()
        {
        }

        public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"global::Tinyhand.Formatters.Builtin.SerializeInt64List(ref writer, {ssb.FullObject});");
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
        {
            ssb.AppendLine($"{ssb.FullObject} = global::Tinyhand.Formatters.Builtin.DeserializeInt64List(ref reader) ?? new List<long>();");
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = new List<long>();");
        }
    }

    public sealed class NullableInt64ListCoder : ITinyhandCoder
    {
        public static readonly NullableInt64ListCoder Instance = new ();

        private NullableInt64ListCoder()
        {
        }

        public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"global::Tinyhand.Formatters.Builtin.SerializeInt64List(ref writer, {ssb.FullObject});");
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
        {
            ssb.AppendLine($"{ssb.FullObject} = global::Tinyhand.Formatters.Builtin.DeserializeInt64List(ref reader);");
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = new List<long>();");
        }
    }

    public sealed class SingleCoder : ITinyhandCoder
    {
        public static readonly SingleCoder Instance = new ();

        private SingleCoder()
        {
        }

        public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"writer.Write({ssb.FullObject});");
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
        {
            ssb.AppendLine($"{ssb.FullObject} = reader.ReadSingle();");
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = 0;");
        }
    }

    public sealed class NullableSingleCoder : ITinyhandCoder
    {
        public static readonly NullableSingleCoder Instance = new ();

        private NullableSingleCoder()
        {
        }

        public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            using (var b = ssb.ScopeBrace($"if ({ssb.FullObject} == null)"))
            {
                ssb.AppendLine("writer.WriteNil();");
            }

            using (var b = ssb.ScopeBrace($"else"))
            {
                ssb.AppendLine($"writer.Write({ssb.FullObject}.Value);");
            }
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
        {
            if (nilChecked)
            {
                ssb.AppendLine($"{ssb.FullObject} = reader.ReadSingle();");
            }
            else
            {
                using (var b = ssb.ScopeBrace("if (reader.TryReadNil())"))
                {
                    ssb.AppendLine($"{ssb.FullObject} = default;");
                }

                using (var b = ssb.ScopeBrace($"else"))
                {
                    ssb.AppendLine($"{ssb.FullObject} = reader.ReadSingle();");
                }
            }
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = 0;");
        }
    }

    public sealed class SingleArrayCoder : ITinyhandCoder
    {
        public static readonly SingleArrayCoder Instance = new ();

        private SingleArrayCoder()
        {
        }

        public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"global::Tinyhand.Formatters.Builtin.SerializeSingleArray(ref writer, {ssb.FullObject});");
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
        {
            ssb.AppendLine($"{ssb.FullObject} = global::Tinyhand.Formatters.Builtin.DeserializeSingleArray(ref reader) ?? new float[0];");
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = new float[0];");
        }
    }

    public sealed class NullableSingleArrayCoder : ITinyhandCoder
    {
        public static readonly NullableSingleArrayCoder Instance = new ();

        private NullableSingleArrayCoder()
        {
        }

        public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"global::Tinyhand.Formatters.Builtin.SerializeSingleArray(ref writer, {ssb.FullObject});");
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
        {
            ssb.AppendLine($"{ssb.FullObject} = global::Tinyhand.Formatters.Builtin.DeserializeSingleArray(ref reader);");
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = new float[0];");
        }
    }

    public sealed class SingleListCoder : ITinyhandCoder
    {
        public static readonly SingleListCoder Instance = new ();

        private SingleListCoder()
        {
        }

        public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"global::Tinyhand.Formatters.Builtin.SerializeSingleList(ref writer, {ssb.FullObject});");
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
        {
            ssb.AppendLine($"{ssb.FullObject} = global::Tinyhand.Formatters.Builtin.DeserializeSingleList(ref reader) ?? new List<float>();");
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = new List<float>();");
        }
    }

    public sealed class NullableSingleListCoder : ITinyhandCoder
    {
        public static readonly NullableSingleListCoder Instance = new ();

        private NullableSingleListCoder()
        {
        }

        public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"global::Tinyhand.Formatters.Builtin.SerializeSingleList(ref writer, {ssb.FullObject});");
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
        {
            ssb.AppendLine($"{ssb.FullObject} = global::Tinyhand.Formatters.Builtin.DeserializeSingleList(ref reader);");
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = new List<float>();");
        }
    }

    public sealed class DoubleCoder : ITinyhandCoder
    {
        public static readonly DoubleCoder Instance = new ();

        private DoubleCoder()
        {
        }

        public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"writer.Write({ssb.FullObject});");
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
        {
            ssb.AppendLine($"{ssb.FullObject} = reader.ReadDouble();");
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = 0;");
        }
    }

    public sealed class NullableDoubleCoder : ITinyhandCoder
    {
        public static readonly NullableDoubleCoder Instance = new ();

        private NullableDoubleCoder()
        {
        }

        public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            using (var b = ssb.ScopeBrace($"if ({ssb.FullObject} == null)"))
            {
                ssb.AppendLine("writer.WriteNil();");
            }

            using (var b = ssb.ScopeBrace($"else"))
            {
                ssb.AppendLine($"writer.Write({ssb.FullObject}.Value);");
            }
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
        {
            if (nilChecked)
            {
                ssb.AppendLine($"{ssb.FullObject} = reader.ReadDouble();");
            }
            else
            {
                using (var b = ssb.ScopeBrace("if (reader.TryReadNil())"))
                {
                    ssb.AppendLine($"{ssb.FullObject} = default;");
                }

                using (var b = ssb.ScopeBrace($"else"))
                {
                    ssb.AppendLine($"{ssb.FullObject} = reader.ReadDouble();");
                }
            }
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = 0;");
        }
    }

    public sealed class DoubleArrayCoder : ITinyhandCoder
    {
        public static readonly DoubleArrayCoder Instance = new ();

        private DoubleArrayCoder()
        {
        }

        public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"global::Tinyhand.Formatters.Builtin.SerializeDoubleArray(ref writer, {ssb.FullObject});");
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
        {
            ssb.AppendLine($"{ssb.FullObject} = global::Tinyhand.Formatters.Builtin.DeserializeDoubleArray(ref reader) ?? new double[0];");
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = new double[0];");
        }
    }

    public sealed class NullableDoubleArrayCoder : ITinyhandCoder
    {
        public static readonly NullableDoubleArrayCoder Instance = new ();

        private NullableDoubleArrayCoder()
        {
        }

        public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"global::Tinyhand.Formatters.Builtin.SerializeDoubleArray(ref writer, {ssb.FullObject});");
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
        {
            ssb.AppendLine($"{ssb.FullObject} = global::Tinyhand.Formatters.Builtin.DeserializeDoubleArray(ref reader);");
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = new double[0];");
        }
    }

    public sealed class DoubleListCoder : ITinyhandCoder
    {
        public static readonly DoubleListCoder Instance = new ();

        private DoubleListCoder()
        {
        }

        public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"global::Tinyhand.Formatters.Builtin.SerializeDoubleList(ref writer, {ssb.FullObject});");
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
        {
            ssb.AppendLine($"{ssb.FullObject} = global::Tinyhand.Formatters.Builtin.DeserializeDoubleList(ref reader) ?? new List<double>();");
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = new List<double>();");
        }
    }

    public sealed class NullableDoubleListCoder : ITinyhandCoder
    {
        public static readonly NullableDoubleListCoder Instance = new ();

        private NullableDoubleListCoder()
        {
        }

        public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"global::Tinyhand.Formatters.Builtin.SerializeDoubleList(ref writer, {ssb.FullObject});");
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
        {
            ssb.AppendLine($"{ssb.FullObject} = global::Tinyhand.Formatters.Builtin.DeserializeDoubleList(ref reader);");
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = new List<double>();");
        }
    }

    public sealed class BooleanCoder : ITinyhandCoder
    {
        public static readonly BooleanCoder Instance = new ();

        private BooleanCoder()
        {
        }

        public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"writer.Write({ssb.FullObject});");
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
        {
            ssb.AppendLine($"{ssb.FullObject} = reader.ReadBoolean();");
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = false;");
        }
    }

    public sealed class NullableBooleanCoder : ITinyhandCoder
    {
        public static readonly NullableBooleanCoder Instance = new ();

        private NullableBooleanCoder()
        {
        }

        public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            using (var b = ssb.ScopeBrace($"if ({ssb.FullObject} == null)"))
            {
                ssb.AppendLine("writer.WriteNil();");
            }

            using (var b = ssb.ScopeBrace($"else"))
            {
                ssb.AppendLine($"writer.Write({ssb.FullObject}.Value);");
            }
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
        {
            if (nilChecked)
            {
                ssb.AppendLine($"{ssb.FullObject} = reader.ReadBoolean();");
            }
            else
            {
                using (var b = ssb.ScopeBrace("if (reader.TryReadNil())"))
                {
                    ssb.AppendLine($"{ssb.FullObject} = default;");
                }

                using (var b = ssb.ScopeBrace($"else"))
                {
                    ssb.AppendLine($"{ssb.FullObject} = reader.ReadBoolean();");
                }
            }
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = false;");
        }
    }

    public sealed class BooleanArrayCoder : ITinyhandCoder
    {
        public static readonly BooleanArrayCoder Instance = new ();

        private BooleanArrayCoder()
        {
        }

        public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"global::Tinyhand.Formatters.Builtin.SerializeBooleanArray(ref writer, {ssb.FullObject});");
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
        {
            ssb.AppendLine($"{ssb.FullObject} = global::Tinyhand.Formatters.Builtin.DeserializeBooleanArray(ref reader) ?? new bool[0];");
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = new bool[0];");
        }
    }

    public sealed class NullableBooleanArrayCoder : ITinyhandCoder
    {
        public static readonly NullableBooleanArrayCoder Instance = new ();

        private NullableBooleanArrayCoder()
        {
        }

        public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"global::Tinyhand.Formatters.Builtin.SerializeBooleanArray(ref writer, {ssb.FullObject});");
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
        {
            ssb.AppendLine($"{ssb.FullObject} = global::Tinyhand.Formatters.Builtin.DeserializeBooleanArray(ref reader);");
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = new bool[0];");
        }
    }

    public sealed class BooleanListCoder : ITinyhandCoder
    {
        public static readonly BooleanListCoder Instance = new ();

        private BooleanListCoder()
        {
        }

        public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"global::Tinyhand.Formatters.Builtin.SerializeBooleanList(ref writer, {ssb.FullObject});");
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
        {
            ssb.AppendLine($"{ssb.FullObject} = global::Tinyhand.Formatters.Builtin.DeserializeBooleanList(ref reader) ?? new List<bool>();");
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = new List<bool>();");
        }
    }

    public sealed class NullableBooleanListCoder : ITinyhandCoder
    {
        public static readonly NullableBooleanListCoder Instance = new ();

        private NullableBooleanListCoder()
        {
        }

        public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"global::Tinyhand.Formatters.Builtin.SerializeBooleanList(ref writer, {ssb.FullObject});");
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
        {
            ssb.AppendLine($"{ssb.FullObject} = global::Tinyhand.Formatters.Builtin.DeserializeBooleanList(ref reader);");
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = new List<bool>();");
        }
    }

    public sealed class CharCoder : ITinyhandCoder
    {
        public static readonly CharCoder Instance = new ();

        private CharCoder()
        {
        }

        public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"writer.Write({ssb.FullObject});");
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
        {
            ssb.AppendLine($"{ssb.FullObject} = reader.ReadChar();");
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = 0;");
        }
    }

    public sealed class NullableCharCoder : ITinyhandCoder
    {
        public static readonly NullableCharCoder Instance = new ();

        private NullableCharCoder()
        {
        }

        public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            using (var b = ssb.ScopeBrace($"if ({ssb.FullObject} == null)"))
            {
                ssb.AppendLine("writer.WriteNil();");
            }

            using (var b = ssb.ScopeBrace($"else"))
            {
                ssb.AppendLine($"writer.Write({ssb.FullObject}.Value);");
            }
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
        {
            if (nilChecked)
            {
                ssb.AppendLine($"{ssb.FullObject} = reader.ReadChar();");
            }
            else
            {
                using (var b = ssb.ScopeBrace("if (reader.TryReadNil())"))
                {
                    ssb.AppendLine($"{ssb.FullObject} = default;");
                }

                using (var b = ssb.ScopeBrace($"else"))
                {
                    ssb.AppendLine($"{ssb.FullObject} = reader.ReadChar();");
                }
            }
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = 0;");
        }
    }

    public sealed class CharArrayCoder : ITinyhandCoder
    {
        public static readonly CharArrayCoder Instance = new ();

        private CharArrayCoder()
        {
        }

        public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"global::Tinyhand.Formatters.Builtin.SerializeCharArray(ref writer, {ssb.FullObject});");
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
        {
            ssb.AppendLine($"{ssb.FullObject} = global::Tinyhand.Formatters.Builtin.DeserializeCharArray(ref reader) ?? new char[0];");
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = new char[0];");
        }
    }

    public sealed class NullableCharArrayCoder : ITinyhandCoder
    {
        public static readonly NullableCharArrayCoder Instance = new ();

        private NullableCharArrayCoder()
        {
        }

        public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"global::Tinyhand.Formatters.Builtin.SerializeCharArray(ref writer, {ssb.FullObject});");
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
        {
            ssb.AppendLine($"{ssb.FullObject} = global::Tinyhand.Formatters.Builtin.DeserializeCharArray(ref reader);");
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = new char[0];");
        }
    }

    public sealed class CharListCoder : ITinyhandCoder
    {
        public static readonly CharListCoder Instance = new ();

        private CharListCoder()
        {
        }

        public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"global::Tinyhand.Formatters.Builtin.SerializeCharList(ref writer, {ssb.FullObject});");
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
        {
            ssb.AppendLine($"{ssb.FullObject} = global::Tinyhand.Formatters.Builtin.DeserializeCharList(ref reader) ?? new List<char>();");
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = new List<char>();");
        }
    }

    public sealed class NullableCharListCoder : ITinyhandCoder
    {
        public static readonly NullableCharListCoder Instance = new ();

        private NullableCharListCoder()
        {
        }

        public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"global::Tinyhand.Formatters.Builtin.SerializeCharList(ref writer, {ssb.FullObject});");
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
        {
            ssb.AppendLine($"{ssb.FullObject} = global::Tinyhand.Formatters.Builtin.DeserializeCharList(ref reader);");
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = new List<char>();");
        }
    }

    public sealed class DateTimeCoder : ITinyhandCoder
    {
        public static readonly DateTimeCoder Instance = new ();

        private DateTimeCoder()
        {
        }

        public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"writer.Write({ssb.FullObject});");
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
        {
            ssb.AppendLine($"{ssb.FullObject} = reader.ReadDateTime();");
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = default(DateTime);");
        }
    }

    public sealed class NullableDateTimeCoder : ITinyhandCoder
    {
        public static readonly NullableDateTimeCoder Instance = new ();

        private NullableDateTimeCoder()
        {
        }

        public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            using (var b = ssb.ScopeBrace($"if ({ssb.FullObject} == null)"))
            {
                ssb.AppendLine("writer.WriteNil();");
            }

            using (var b = ssb.ScopeBrace($"else"))
            {
                ssb.AppendLine($"writer.Write({ssb.FullObject}.Value);");
            }
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
        {
            if (nilChecked)
            {
                ssb.AppendLine($"{ssb.FullObject} = reader.ReadDateTime();");
            }
            else
            {
                using (var b = ssb.ScopeBrace("if (reader.TryReadNil())"))
                {
                    ssb.AppendLine($"{ssb.FullObject} = default;");
                }

                using (var b = ssb.ScopeBrace($"else"))
                {
                    ssb.AppendLine($"{ssb.FullObject} = reader.ReadDateTime();");
                }
            }
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = default(DateTime);");
        }
    }

    public sealed class DateTimeArrayCoder : ITinyhandCoder
    {
        public static readonly DateTimeArrayCoder Instance = new ();

        private DateTimeArrayCoder()
        {
        }

        public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"global::Tinyhand.Formatters.Builtin.SerializeDateTimeArray(ref writer, {ssb.FullObject});");
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
        {
            ssb.AppendLine($"{ssb.FullObject} = global::Tinyhand.Formatters.Builtin.DeserializeDateTimeArray(ref reader) ?? new DateTime[0];");
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = new DateTime[0];");
        }
    }

    public sealed class NullableDateTimeArrayCoder : ITinyhandCoder
    {
        public static readonly NullableDateTimeArrayCoder Instance = new ();

        private NullableDateTimeArrayCoder()
        {
        }

        public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"global::Tinyhand.Formatters.Builtin.SerializeDateTimeArray(ref writer, {ssb.FullObject});");
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
        {
            ssb.AppendLine($"{ssb.FullObject} = global::Tinyhand.Formatters.Builtin.DeserializeDateTimeArray(ref reader);");
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = new DateTime[0];");
        }
    }

    public sealed class DateTimeListCoder : ITinyhandCoder
    {
        public static readonly DateTimeListCoder Instance = new ();

        private DateTimeListCoder()
        {
        }

        public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"global::Tinyhand.Formatters.Builtin.SerializeDateTimeList(ref writer, {ssb.FullObject});");
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
        {
            ssb.AppendLine($"{ssb.FullObject} = global::Tinyhand.Formatters.Builtin.DeserializeDateTimeList(ref reader) ?? new List<DateTime>();");
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = new List<DateTime>();");
        }
    }

    public sealed class NullableDateTimeListCoder : ITinyhandCoder
    {
        public static readonly NullableDateTimeListCoder Instance = new ();

        private NullableDateTimeListCoder()
        {
        }

        public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"global::Tinyhand.Formatters.Builtin.SerializeDateTimeList(ref writer, {ssb.FullObject});");
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
        {
            ssb.AppendLine($"{ssb.FullObject} = global::Tinyhand.Formatters.Builtin.DeserializeDateTimeList(ref reader);");
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = new List<DateTime>();");
        }
    }
}
