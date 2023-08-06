// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Linq;
using Arc.Visceral;
using Tinyhand.Generator;

namespace TinyhandGenerator;

internal static class JournalShared
{
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
        using (var journalScope = ssb.ScopeBrace("if (this.Journal is not null && this.Journal.TryGetJournalWriter(JournalType.Record, out var writer))"))
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
            var writeValue = obj.CodeWriter(ssb.FullObject);
            if (writeValue is not null)
            {
                ssb.AppendLine("writer.Write_Value();");
                ssb.AppendLine(writeValue);
            }

            ssb.AppendLine("this.Journal.AddJournal(writer);");
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
            name == "double")
        {
            return $"writer.Write({valueString});";
        }

        if (obj.AllAttributes.Any(x => x.FullName == TinyhandObjectAttributeMock.FullName))
        {// TinyhandObject
            return $"TinyhandSerializer.SerializeObject(ref writer, {valueString});";
        }

        return $"TinyhandSerializer.Serialize(ref writer, {valueString});";
    }
}
