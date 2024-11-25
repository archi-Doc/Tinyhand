// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Arc.Crypto;

#pragma warning disable SA1202 // Elements should be ordered by access

namespace Arc.Visceral;

public static class VisceralHelper
{
    public const BindingFlags TargetBindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;

    public static string? Primitives_ShortenFullName(string fullName) => fullName switch
    {
        "System.Void" => "void",
        "System.Object" => "object",
        "System.Boolean" => "bool",
        "System.Boolean?" => "bool?",
        "System.Boolean[]" => "bool[]",
        "System.SByte" => "sbyte",
        "System.SByte?" => "sbyte?",
        "System.SByte[]" => "sbyte[]",
        "System.Byte" => "byte",
        "System.Byte?" => "byte?",
        "System.Byte[]" => "byte[]",
        "System.Int16" => "short",
        "System.Int16?" => "short?",
        "System.Int16[]" => "short[]",
        "System.UInt16" => "ushort",
        "System.UInt16?" => "ushort?",
        "System.UInt16[]" => "ushort[]",
        "System.Int32" => "int",
        "System.Int32?" => "int?",
        "System.Int32[]" => "int[]",
        "System.UInt32" => "uint",
        "System.UInt32?" => "uint?",
        "System.UInt32[]" => "uint[]",
        "System.Int64" => "long",
        "System.Int64?" => "long?",
        "System.Int64[]" => "long[]",
        "System.UInt64" => "ulong",
        "System.UInt64?" => "ulong?",
        "System.UInt64[]" => "ulong[]",
        "System.Single" => "float",
        "System.Single?" => "float?",
        "System.Single[]" => "float[]",
        "System.Double" => "double",
        "System.Double?" => "double?",
        "System.Double[]" => "double[]",
        "System.Decimal" => "decimal",
        "System.Decimal?" => "decimal?",
        "System.Decimal[]" => "decimal[]",
        "System.String" => "string",
        "System.String?" => "string",
        "System.String[]" => "string[]",
        "System.Char" => "char",
        "System.Char?" => "char?",
        "System.Char[]" => "char[]",
        _ => null,
    };

    public static string? Primitives_ShortenSimpleName(string fullName) => fullName switch
    {
        "Void" => "void",
        "Object" => "object",
        "Boolean" => "bool",
        "Boolean?" => "bool?",
        "Boolean[]" => "bool[]",
        "SByte" => "sbyte",
        "SByte?" => "sbyte?",
        "SByte[]" => "sbyte[]",
        "Byte" => "byte",
        "Byte?" => "byte?",
        "Byte[]" => "byte[]",
        "Int16" => "short",
        "Int16?" => "short?",
        "Int16[]" => "short[]",
        "UInt16" => "ushort",
        "UInt16?" => "ushort?",
        "UInt16[]" => "ushort[]",
        "Int32" => "int",
        "Int32?" => "int?",
        "Int32[]" => "int[]",
        "UInt32" => "uint",
        "UInt32?" => "uint?",
        "UInt32[]" => "uint[]",
        "Int64" => "long",
        "Int64?" => "long?",
        "Int64[]" => "long[]",
        "UInt64" => "ulong",
        "UInt64?" => "ulong?",
        "UInt64[]" => "ulong[]",
        "Single" => "float",
        "Single?" => "float?",
        "Single[]" => "float[]",
        "Double" => "double",
        "Double?" => "double?",
        "Double[]" => "double[]",
        "Decimal" => "decimal",
        "Decimal?" => "decimal?",
        "Decimal[]" => "decimal[]",
        "String" => "string",
        "String?" => "string?",
        "String[]" => "string[]",
        "Char" => "char",
        "Char?" => "char?",
        "Char[]" => "char[]",
        _ => null,
    };

    public static ulong TypeToFarmHash64(this Type type)
        => FarmHash.Hash64(TypeToFullName(type));

    public static string TypeToFullName(this Type type)
        => VisceralHelper.TypeToName(type, true);

    public static string TypeToLocalName(this Type type)
        => VisceralHelper.TypeToName(type, false);

    public static string GetNamespaceAndClass(this Type type)
    {
        if (type.DeclaringType == null)
        {
            return type.Namespace + "." + VisceralHelper.TypeToLocalName(type);
        }
        else
        {
            var declaringType = type.DeclaringType;
            var s = string.Empty;
            while (declaringType != null)
            {
                type = declaringType;
                s = "." + VisceralHelper.TypeToLocalName(type) + s;
                declaringType = type.DeclaringType;
            }

            return type.Namespace + s;
        }
    }

    private static string GetSimpleGenericName(string name)
    {
        var idx = name.IndexOf('`');
        if (idx < 0)
        {
            return name;
        }
        else
        {
            return name.Substring(0, idx);
        }
    }

    private static string TypeToName(Type type, bool appendNamespace)
    {
        if (type.IsArray)
        {
            if (type.GetElementType() is not { } elementType)
            {
                return string.Empty;
            }

            var sb = new StringBuilder(VisceralHelper.TypeToName(elementType, appendNamespace));
            sb.Append('[');
            for (var n = 1; n < type.GetArrayRank(); n++)
            {
                sb.Append(',');
            }

            sb.Append(']');

            return sb.ToString();
        }
        else if (type.IsGenericParameter)
        {
            return type.Name;
        }
        else if (!type.IsGenericType)
        {
            var shortName = VisceralHelper.Primitives_ShortenSimpleName(type.Name);
            if (shortName != null)
            {
                return shortName;
            }

            if (appendNamespace)
            {
                return VisceralHelper.GetNamespaceAndClass(type);
            }
            else
            {
                return type.Name;
            }
        }

        return GenericTypeToName(type, appendNamespace);
    }

    public static string GenericTypeToName(Type type, bool appendNamespace)
    {
        var definitionType = type.GetGenericTypeDefinition();
        var args = type.GetGenericArguments();
        StringBuilder sb;

        if (definitionType == typeof(Nullable<>) && args.Length == 1)
        { // int?
            var name = VisceralHelper.TypeToName(args[0], appendNamespace);
            if (appendNamespace)
            {
                var shortName = VisceralHelper.Primitives_ShortenFullName(name);
                if (shortName != null)
                {
                    return shortName + "?";
                }
                else
                {
                    return name + "?";
                }
            }
            else
            {
                return (VisceralHelper.Primitives_ShortenSimpleName(name) ?? name) + "?";
            }
        }
        else if (definitionType.IsTuple())
        { // (int, string)
            sb = new StringBuilder();

            var count = definitionType.GetGenericArguments().Length;
            sb.Append("(");
            for (int i = 0; i < args.Length; ++i)
            {
                if (i > 0)
                {
                    sb.Append(", ");
                }

                sb.Append(VisceralHelper.TypeToName(args[i], true));
            }

            sb.Append(")");

            return sb.ToString();
        }
        else
        { // Class<T1, T2>
            if (!appendNamespace)
            {
                sb = new StringBuilder();
                sb.Append(GetSimpleGenericName(type.Name));

                var declaringCount = type.DeclaringType == null ? 0 : type.DeclaringType.GetGenericArguments().Length;
                var currentCount = type.GetGenericArguments().Length;
                if (currentCount > declaringCount)
                {
                    sb.Append("<");
                    for (var i = declaringCount; i < currentCount; i++)
                    {
                        if (i > declaringCount)
                        {
                            sb.Append(", ");
                        }

                        sb.Append(VisceralHelper.TypeToName(args[i], true));
                    }

                    sb.Append(">");
                }

                return sb.ToString();
            }

            var list = new List<Type>();
            while (true)
            {
                list.Add(type);
                if (type.DeclaringType is null)
                {
                    break;
                }

                type = type.DeclaringType;
            }

            sb = new StringBuilder();
            sb.Append(list.Last().Namespace);
            sb.Append('.');

            var lastCount = 0;
            for (var i = list.Count - 1; i >= 0; i--)
            {
                type = list[i];

                sb.Append(GetSimpleGenericName(type.Name));

                var currentCount = type.GetGenericArguments().Length;
                if (currentCount > lastCount)
                {
                    sb.Append("<");
                    for (var j = lastCount; j < currentCount; j++)
                    {
                        if (j > lastCount)
                        {
                            sb.Append(", ");
                        }

                        sb.Append(VisceralHelper.TypeToName(args[j], true));
                    }

                    sb.Append(">");
                }

                lastCount = currentCount;

                if (i > 0)
                {
                    sb.Append('.');
                }
            }

            return sb.ToString();
        }
    }

    public static string TypeToSimpleName(this Type type)
    {
        if (type.IsArray)
        {
            if (type.GetElementType() is not { } elementType)
            {
                return string.Empty;
            }

            return VisceralHelper.TypeToSimpleName(elementType);
        }
        else if (type.IsGenericParameter)
        {
            return type.Name;
        }
        else if (type.IsGenericType)
        {
            var definitionType = type.GetGenericTypeDefinition();
            var args = type.GetGenericArguments();

            if (definitionType == typeof(Nullable<>) && args.Length == 1)
            { // int?
                var name = VisceralHelper.TypeToName(args[0], false);
                return VisceralHelper.Primitives_ShortenSimpleName(name) ?? name; // + "?";
            }
            else
            {
                return GetSimpleGenericName(type.Name);
            }
        }
        else if (VisceralHelper.IsTuple(type))
        {
            return "ValueTuple";
        }

        return VisceralHelper.Primitives_ShortenSimpleName(type.Name) ?? type.Name;
    }

    public static bool IsTuple(this Type type)
    {
        if (!type.IsGenericType)
        {
            return false;
        }

        var openType = type.GetGenericTypeDefinition();

        return openType == typeof(ValueTuple<>)
            || openType == typeof(ValueTuple<,>)
            || openType == typeof(ValueTuple<,,>)
            || openType == typeof(ValueTuple<,,,>)
            || openType == typeof(ValueTuple<,,,,>)
            || openType == typeof(ValueTuple<,,,,,>)
            || openType == typeof(ValueTuple<,,,,,,>)
            || (openType == typeof(ValueTuple<,,,,,,,>) && IsTuple(type.GetGenericArguments()[7]));
    }
}
