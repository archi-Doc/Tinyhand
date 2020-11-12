﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Arc.Visceral;
using Tinyhand.Generator;
#pragma warning disable SA1401 // Fields should be private
#pragma warning disable SA1509 // Opening braces should not be preceded by blank line

namespace Tinyhand.Coders
{
    public sealed class BuiltinCoder : ICoderResolver
    {
        /// <summary>
        /// The singleton instance that can be used.
        /// </summary>
        public static readonly BuiltinCoder Instance = new BuiltinCoder();

        public readonly Dictionary<string, ITinyhandCoder> NameToCoder = new()
        {
            // Primitive
            { "bool", BooleanCoder.Instance },
            { "bool?", NullableBooleanCoder.Instance },
            { "bool[]", BooleanArrayCoder.Instance },
            { "bool[]?", NullableBooleanArrayCoder.Instance },
            { "System.Collections.Generic.List<bool>", BooleanListCoder.Instance },
            { "System.Collections.Generic.List<bool>?", NullableBooleanListCoder.Instance },

            { "sbyte", Int8Coder.Instance },
            { "sbyte?", NullableInt8Coder.Instance },
            { "sbyte[]", Int8ArrayCoder.Instance },
            { "sbyte[]?", NullableInt8ArrayCoder.Instance },
            { "System.Collections.Generic.List<sbyte>", Int8ListCoder.Instance },
            { "System.Collections.Generic.List<sbyte>?", NullableInt8ListCoder.Instance },

            { "byte", UInt8Coder.Instance },
            { "byte?", NullableUInt8Coder.Instance },
            { "byte[]", UInt8ArrayCoder.Instance },
            { "byte[]?", NullableUInt8ArrayCoder.Instance },
            { "System.Collections.Generic.List<byte>", UInt8ListCoder.Instance },
            { "System.Collections.Generic.List<byte>?", NullableUInt8ListCoder.Instance },

            { "short", Int16Coder.Instance },
            { "short?", NullableInt16Coder.Instance },
            { "short[]", Int16ArrayCoder.Instance },
            { "short[]?", NullableInt16ArrayCoder.Instance },
            { "System.Collections.Generic.List<short>", Int16ListCoder.Instance },
            { "System.Collections.Generic.List<short>?", NullableInt16ListCoder.Instance },

            { "ushort", UInt16Coder.Instance },
            { "ushort?", NullableUInt16Coder.Instance },
            { "ushort[]", UInt16ArrayCoder.Instance },
            { "ushort[]?", NullableUInt16ArrayCoder.Instance },
            { "System.Collections.Generic.List<ushort>", UInt16ListCoder.Instance },
            { "System.Collections.Generic.List<ushort>?", NullableUInt16ListCoder.Instance },

            { "int", Int32Coder.Instance },
            { "int?", NullableInt32Coder.Instance },
            { "int[]", Int32ArrayCoder.Instance },
            { "int[]?", NullableInt32ArrayCoder.Instance },
            { "System.Collections.Generic.List<int>", Int32ListCoder.Instance },
            { "System.Collections.Generic.List<int>?", NullableInt32ListCoder.Instance },

            { "uint", UInt32Coder.Instance },
            { "uint?", NullableUInt32Coder.Instance },
            { "uint[]", UInt32ArrayCoder.Instance },
            { "uint[]?", NullableUInt32ArrayCoder.Instance },
            { "System.Collections.Generic.List<uint>", UInt32ListCoder.Instance },
            { "System.Collections.Generic.List<uint>?", NullableUInt32ListCoder.Instance },

            { "long", Int64Coder.Instance },
            { "long?", NullableInt64Coder.Instance },
            { "long[]", Int64ArrayCoder.Instance },
            { "long[]?", NullableInt64ArrayCoder.Instance },
            { "System.Collections.Generic.List<long>", Int64ListCoder.Instance },
            { "System.Collections.Generic.List<long>?", NullableInt64ListCoder.Instance },

            { "ulong", UInt64Coder.Instance },
            { "ulong?", NullableUInt64Coder.Instance },
            { "ulong[]", UInt64ArrayCoder.Instance },
            { "ulong[]?", NullableUInt64ArrayCoder.Instance },
            { "System.Collections.Generic.List<ulong>", UInt64ListCoder.Instance },
            { "System.Collections.Generic.List<ulong>?", NullableUInt64ListCoder.Instance },

            { "float", SingleCoder.Instance },
            { "float?", NullableSingleCoder.Instance },
            { "float[]", SingleArrayCoder.Instance },
            { "float[]?", NullableSingleArrayCoder.Instance },
            { "System.Collections.Generic.List<float>", SingleListCoder.Instance },
            { "System.Collections.Generic.List<float>?", NullableSingleListCoder.Instance },

            { "double", DoubleCoder.Instance },
            { "double?", NullableDoubleCoder.Instance },
            { "double[]", DoubleArrayCoder.Instance },
            { "double[]?", NullableDoubleArrayCoder.Instance },
            { "System.Collections.Generic.List<double>", DoubleListCoder.Instance },
            { "System.Collections.Generic.List<double>?", NullableDoubleListCoder.Instance },

            { "char", CharCoder.Instance },
            { "char?", NullableCharCoder.Instance },
            { "char[]", CharArrayCoder.Instance },
            { "char[]?", NullableCharArrayCoder.Instance },
            { "System.Collections.Generic.List<char>", CharListCoder.Instance },
            { "System.Collections.Generic.List<char>?", NullableCharListCoder.Instance },

            { "string", StringCoder.Instance },
            { "string?", NullableStringCoder.Instance },
            { "string[]", StringArrayCoder.Instance },
            { "string?[]", NullableStringArrayCoder.Instance },
            { "System.Collections.Generic.List<string>", StringListCoder.Instance },
            { "System.Collections.Generic.List<string>?", NullableStringListCoder.Instance },

            { "System.DateTime", DateTimeCoder.Instance },
            { "System.DateTime?", NullableDateTimeCoder.Instance },
            { "System.DateTime[]", DateTimeArrayCoder.Instance },
            { "System.DateTime[]?", NullableDateTimeArrayCoder.Instance },
            { "System.Collections.Generic.List<System.DateTime>", DateTimeListCoder.Instance },
            { "System.Collections.Generic.List<System.DateTime>?", NullableDateTimeListCoder.Instance },
        };

        public ITinyhandCoder? TryGetCoder(WithNullable<TinyhandObject> withNullable)
        {
            /*var nameWithNullable = withNullable.Object.FullName;
            if (withNullable.Object.Kind.IsReferenceType() && withNullable.Nullable != NullableAnnotation.NotAnnotated)
            {
                nameWithNullable += "?";
            }*/

            this.NameToCoder.TryGetValue(withNullable.FullNameWithNullable, out var coder);
            return coder;
        }
    }

    public sealed class StringCoder : ITinyhandCoder
    {
        public static readonly StringCoder Instance = new();

        private StringCoder()
        {
        }

        public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"writer.Write({ssb.FullObject});");
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
        {
            ssb.AppendLine($"{ssb.FullObject} = reader.ReadString() ?? string.Empty;");
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = string.Empty;");
        }
    }

    public sealed class NullableStringCoder : ITinyhandCoder
    {
        public static readonly NullableStringCoder Instance = new();

        private NullableStringCoder()
        {
        }

        public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"writer.Write({ssb.FullObject});");
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
        {
            ssb.AppendLine($"{ssb.FullObject} = reader.ReadString();");
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = string.Empty;");
        }
    }

    public sealed class StringArrayCoder : ITinyhandCoder
    {
        public static readonly StringArrayCoder Instance = new();

        private StringArrayCoder()
        {
        }

        public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"global::Tinyhand.Formatters.Builtin.SerializeStringArray(ref writer, {ssb.FullObject});");
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
        {
            ssb.AppendLine($"{ssb.FullObject} = global::Tinyhand.Formatters.Builtin.DeserializeStringArray(ref reader) ?? new string[0];");
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = new string[0];");
        }
    }

    public sealed class NullableStringArrayCoder : ITinyhandCoder
    {
        public static readonly NullableStringArrayCoder Instance = new();

        private NullableStringArrayCoder()
        {
        }

        public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"global::Tinyhand.Formatters.Builtin.SerializeNullableStringArray(ref writer, {ssb.FullObject});");
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
        {
            ssb.AppendLine($"{ssb.FullObject} = global::Tinyhand.Formatters.Builtin.DeserializeNullableStringArray(ref reader) ?? new string[0];");
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = new string[0];");
        }
    }

    public sealed class StringListCoder : ITinyhandCoder
    {
        public static readonly StringListCoder Instance = new();

        private StringListCoder()
        {
        }

        public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"global::Tinyhand.Formatters.Builtin.SerializeStringList(ref writer, {ssb.FullObject});");
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
        {
            ssb.AppendLine($"{ssb.FullObject} = global::Tinyhand.Formatters.Builtin.DeserializeStringList(ref reader) ?? new List<string>();");
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = new List<string>();");
        }
    }

    public sealed class NullableStringListCoder : ITinyhandCoder
    {
        public static readonly NullableStringListCoder Instance = new();

        private NullableStringListCoder()
        {
        }

        public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"global::Tinyhand.Formatters.Builtin.SerializeStringList(ref writer, {ssb.FullObject});");
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
        {
            ssb.AppendLine($"{ssb.FullObject} = global::Tinyhand.Formatters.Builtin.DeserializeStringList(ref reader);");
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = new List<string>();");
        }
    }

    public sealed class UInt8ArrayCoder : ITinyhandCoder
    {
        public static readonly UInt8ArrayCoder Instance = new ();

        private UInt8ArrayCoder()
        {
        }

        public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"writer.Write({ssb.FullObject});");
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
        {
            ssb.AppendLine($"{ssb.FullObject} = reader.ReadBytesToArray();");
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = Array.Empty<byte>();");
        }
    }

    public sealed class NullableUInt8ArrayCoder : ITinyhandCoder
    {
        public static readonly NullableUInt8ArrayCoder Instance = new ();

        private NullableUInt8ArrayCoder()
        {
        }

        public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"writer.Write({ssb.FullObject});");
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
        {
            ssb.AppendLine($"{ssb.FullObject} = reader.ReadBytes()?.ToArray();");
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = Array.Empty<byte>();");
        }
    }

    public sealed class UInt8ListCoder : ITinyhandCoder
    {
        public static readonly UInt8ListCoder Instance = new ();

        private UInt8ListCoder()
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
                ssb.AppendLine($"writer.Write({ssb.FullObject}.ToArray().AsSpan());");
            }
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
        {
            if (nilChecked)
            {
                ssb.AppendLine($"{ssb.FullObject} = new List<byte>(reader.ReadBytes()?.ToArray());");
            }
            else
            {
                using (var b = ssb.ScopeBrace("if (reader.TryReadNil())"))
                {
                    ssb.AppendLine($"{ssb.FullObject} = new List<byte>();");
                }

                using (var b = ssb.ScopeBrace($"else"))
                {
                    ssb.AppendLine($"{ssb.FullObject} = new List<byte>(reader.ReadBytes()?.ToArray());");
                }
            }
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = new List<byte>();");
        }
    }

    public sealed class NullableUInt8ListCoder : ITinyhandCoder
    {
        public static readonly NullableUInt8ListCoder Instance = new();

        private NullableUInt8ListCoder()
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
                ssb.AppendLine($"writer.Write({ssb.FullObject}.ToArray().AsSpan());");
            }
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
        {
            if (nilChecked)
            {
                ssb.AppendLine($"{ssb.FullObject} = new List<byte>(reader.ReadBytes()?.ToArray());");
            }
            else
            {
                using (var b = ssb.ScopeBrace("if (reader.TryReadNil())"))
                {
                    ssb.AppendLine($"{ssb.FullObject} = default;");
                }

                using (var b = ssb.ScopeBrace($"else"))
                {
                    ssb.AppendLine($"{ssb.FullObject} = new List<byte>(reader.ReadBytes()?.ToArray());");
                }
            }
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = new List<byte>();");
        }
    }
}
