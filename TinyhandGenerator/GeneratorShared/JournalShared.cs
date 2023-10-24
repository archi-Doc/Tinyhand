// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Linq;
using Arc.Visceral;
using Tinyhand.Generator;

namespace TinyhandGenerator;

internal static class JournalShared
{
    public static void GenerateValue_MaxLength(ScopingStringBuilder ssb, TinyhandObject x, MaxLengthAttributeMock attribute)
    {
        if (x.TypeObject is not { } typeObject)
        {
            return;
        }

        if (typeObject.FullName == "string")
        {// string
            if (attribute.MaxLength >= 0)
            {
                using (var scopeIf = ssb.ScopeBrace($"if (value.Length > {attribute.MaxLength})"))
                {// text = text.Substring(0, MaxLength);
                    ssb.AppendLine($"value = value.Substring(0, {attribute.MaxLength});");
                }
            }
        }
        else if (typeObject.Array_Rank == 1)
        {// T[]
            if (attribute.MaxLength >= 0)
            {
                using (var scopeIf = ssb.ScopeBrace($"if (value.Length > {attribute.MaxLength})"))
                {// array = array[..MaxLength];
                    ssb.AppendLine($"value = value[..{attribute.MaxLength}];");
                }
            }

            if (attribute.MaxChildLength >= 0)
            {
                if (typeObject.Array_Element?.FullName == "string")
                {// string[]
                    using (var scopeFor = ssb.ScopeBrace($"for (var i = 0; i < value.Length; i++)"))
                    {
                        using (var scopeIf = ssb.ScopeBrace($"if (value[i].Length > {attribute.MaxChildLength})"))
                        {// text = text.Substring(0, MaxLength);
                            ssb.AppendLine($"value[i] = value[i].Substring(0, {attribute.MaxChildLength});");
                        }
                    }
                }
                else if (typeObject.Array_Element?.Array_Rank == 1)
                {// T[][]
                    using (var scopeFor = ssb.ScopeBrace($"for (var i = 0; i < value.Length; i++)"))
                    {
                        using (var scopeIf = ssb.ScopeBrace($"if (value[i].Length > {attribute.MaxChildLength})"))
                        {
                            ssb.AppendLine($"value[i] = value[i][..{attribute.MaxChildLength}];");
                        }
                    }
                }
            }
        }
        else if (typeObject.Generics_Kind == VisceralGenericsKind.ClosedGeneric &&
            typeObject.OriginalDefinition is { } baseObject &&
            baseObject.FullName == "System.Collections.Generic.List<T>" &&
            typeObject.Generics_Arguments.Length == 1)
        {// List<T>
            if (attribute.MaxLength >= 0)
            {
                using (var scopeIf = ssb.ScopeBrace($"if (value.Count > {attribute.MaxLength})"))
                {// list = list.GetRange(0, MaxLength);
                    ssb.AppendLine($"value = value.GetRange(0, {attribute.MaxLength});");
                }
            }

            if (attribute.MaxChildLength >= 0)
            {
                if (typeObject.Generics_Arguments[0].FullName == "string")
                {// List<string>
                    using (var scopeFor = ssb.ScopeBrace($"for (var i = 0; i < value.Count; i++)"))
                    {
                        using (var scopeIf = ssb.ScopeBrace($"if (value[i].Length > {attribute.MaxChildLength})"))
                        {// text = text.Substring(0, MaxLength);
                            ssb.AppendLine($"value[i] = value[i].Substring(0, {attribute.MaxChildLength});");
                        }
                    }
                }
                else if (typeObject.Generics_Arguments[0].Array_Rank == 1)
                {// List<T[]>
                    using (var scopeFor = ssb.ScopeBrace($"for (var i = 0; i < value.Count; i++)"))
                    {
                        using (var scopeIf = ssb.ScopeBrace($"if (value[i].Length > {attribute.MaxChildLength})"))
                        {
                            ssb.AppendLine($"value[i] = value[i][..{attribute.MaxChildLength}];");
                        }
                    }
                }
            }
        }
    }

    public static string? CodeWriteKey(this TinyhandObject obj)
    {
        int intKey = -1;
        string? stringKey = null;

        foreach (var x in obj.AllAttributes)
        {
            if (x.FullName == KeyAttributeMock.FullName)
            {// KeyAttribute
                var val = x.ConstructorArguments[0];
                if (val is int i)
                {
                    intKey = i;
                    break;
                }
                else if (val is string s)
                {
                    stringKey = s;
                    break;
                }
            }
            else if (x.FullName == KeyAsNameAttributeMock.FullName)
            {// KeyAsNameAttribute
                stringKey = obj.SimpleName;
                break;
            }
        }

        if (intKey >= 0)
        {
            return $"writer.Write({intKey.ToString()});";
        }
        else if (stringKey is not null)
        {
            return $"writer.WriteString(\"{stringKey}\"u8);";
        }
        else
        {
            return null;
        }
    }

    public static void CodeJournal(this TinyhandObject obj, ScopingStringBuilder ssb, TinyhandObject? locator)
    {
        using (var journalScope = ssb.ScopeBrace($"if ((({TinyhandBody.ITreeObject})this).TryGetJournalWriter(out var root, out var writer, true))"))
        {
            // Custom locator
            using (var customScope = ssb.ScopeBrace($"if (this is Tinyhand.ITinyhandCustomJournal custom)"))
            {
                ssb.AppendLine("custom.WriteCustomLocator(ref writer);");
            }

            // Locator
            if (locator is not null &&
                obj.CodeWriter($"this.{locator.SimpleName}") is { } writeLocator)
            {
                ssb.AppendLine("writer.Write_Locator();");
                ssb.AppendLine(writeLocator);
            }

            // Key
            var writeKey = obj.CodeWriteKey();
            if (writeKey is not null)
            {
                ssb.AppendLine("writer.Write_Key();");
                ssb.AppendLine(writeKey);
            }

            // Value
            var writeValue = obj.CodeWriter("value"); // ssb.FullObject "this.id" -> "value"
            if (writeValue is not null)
            {
                ssb.AppendLine("writer.Write_Value();");
                ssb.AppendLine(writeValue);
            }

            ssb.AppendLine("root.AddJournal(writer);");
        }
    }

    public static string? CodeReader(this TinyhandObject obj)
    {
        var coder = obj.FullName switch
        {
            "bool" => "reader.ReadBoolean()",
            "byte" => "reader.ReadUInt8()",
            "sbyte" => "reader.ReadInt8()",
            "ushort" => "reader.ReadUInt16()",
            "short" => "reader.ReadInt16()",
            "uint" => "reader.ReadUInt32()",
            "int" => "reader.ReadInt32()",
            "ulong" => "reader.ReadUInt64()",
            "long" => "reader.ReadInt64()",

            "char" => "reader.ReadChar()",
            "string" => "reader.ReadString()",
            "float" => "reader.ReadSingle()",
            "double" => "reader.ReadDouble()",
            "System.DateTime" => "reader.ReadDateTime()",

            _ => null,
        };

        if (coder is not null)
        {
            return coder;
        }

        if (obj.AllAttributes.Any(x => x.FullName == TinyhandObjectAttributeMock.FullName))
        {// TinyhandObject
            return $"TinyhandSerializer.DeserializeAndReconstructObject<{obj.FullName}>(ref reader)";
        }

        return $"TinyhandSerializer.Deserialize<{obj.FullName}>(ref reader)";
    }

    public static string? CodeWriter(this TinyhandObject obj, string valueString)
    {
        var name = obj.TypeObject?.FullName;
        if (name is null)
        {
            return null;
        }
        else if (name == "bool" || name == "byte" || name == "sbyte" || name == "ushort" ||
            name == "short" || name == "uint" || name == "int" || name == "ulong" ||
            name == "long" || name == "char" || name == "string" || name == "float" ||
            name == "double" || name == "System.DateTime")
        {
            return $"writer.Write({valueString});";
        }
        else if (obj.TypeObject?.Kind == VisceralObjectKind.Enum)
        {
            var coder = obj.TypeObject.Enum_UnderlyingTypeObject?.FullName switch
            {
                "sbyte" => $"var ev = {valueString}; writer.Write(Unsafe.As<{name}, sbyte>(ref ev));",
                "byte" => $"var ev = {valueString}; writer.Write(Unsafe.As<{name}, byte>(ref ev));",
                "short" => $"var ev = {valueString}; writer.Write(Unsafe.As<{name}, short>(ref ev));",
                "ushort" => $"var ev = {valueString}; writer.Write(Unsafe.As<{name}, ushort>(ref ev));",
                "int" => $"var ev = {valueString}; writer.Write(Unsafe.As<{name}, int>(ref ev));",
                "uint" => $"var ev = {valueString}; writer.Write(Unsafe.As<{name}, uint>(ref ev));",
                "long" => $"var ev = {valueString}; writer.Write(Unsafe.As<{name}, long>(ref ev));",
                "ulong" => $"var ev = {valueString}; writer.Write(Unsafe.As<{name}, ulong>(ref ev));",
                _ => null,
            };

            if (coder is not null)
            {
                return coder;
            }
        }

        if (obj.AllAttributes.Any(x => x.FullName == TinyhandObjectAttributeMock.FullName))
        {// TinyhandObject
            return $"TinyhandSerializer.SerializeObject(ref writer, {valueString});";
        }

        return $"TinyhandSerializer.Serialize(ref writer, {valueString});";
    }
}
