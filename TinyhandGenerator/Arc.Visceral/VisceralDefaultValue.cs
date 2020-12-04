// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;

namespace Arc.Visceral
{
    public static class VisceralDefaultValue
    {
        public static object? ConvertDefaultValue(object defaultValue, string typeName)
        {
            try
            {
                var defaultValueTypeName = VisceralHelper.Primitives_ShortenName(defaultValue.GetType().FullName);
                if (defaultValueTypeName == typeName)
                {// Type matched
                    return defaultValue;
                }

                if (typeName == "sbyte")
                {
                    return Convert.ToSByte(defaultValue);
                }
                else if (typeName == "byte")
                {
                    return Convert.ToByte(defaultValue);
                }
                else if (typeName == "short")
                {
                    return Convert.ToInt16(defaultValue);
                }
                else if (typeName == "ushort")
                {
                    return Convert.ToUInt16(defaultValue);
                }
                else if (typeName == "int")
                {
                    return Convert.ToInt32(defaultValue);
                }
                else if (typeName == "uint")
                {
                    return Convert.ToUInt32(defaultValue);
                }
                else if (typeName == "long")
                {
                    return Convert.ToInt64(defaultValue);
                }
                else if (typeName == "ulong")
                {
                    return Convert.ToUInt64(defaultValue);
                }
                else if (typeName == "float")
                {
                    return Convert.ToSingle(defaultValue);
                }
                else if (typeName == "double")
                {
                    return Convert.ToDouble(defaultValue);
                }
                else if (typeName == "decimal")
                {
                    // return decimal.Parse(ds);
                    return Convert.ToDecimal(defaultValue);
                }
                else
                {
                    return null;
                }
            }
            catch
            {
                return null;
            }
        }

        public static bool IsDefaultableType(Type type)
        {
            if (type.IsGenericType)
            {
                var definitionType = type.GetGenericTypeDefinition();
                var argumentType = type.GetGenericArguments()[0];
                if (definitionType == typeof(Nullable<>))
                {
                    return IsDefaultableType(argumentType);
                }
                else
                {
                    return false;
                }
            }

            if (type == typeof(bool) || type == typeof(sbyte) || type == typeof(byte) ||
                type == typeof(short) || type == typeof(ushort) || type == typeof(int) ||
                type == typeof(uint) || type == typeof(long) || type == typeof(ulong) ||
                type == typeof(float) || type == typeof(double) || type == typeof(decimal) ||
                type == typeof(string) || type == typeof(char))
            {
                return true;
            }

            return false;
        }

        public static bool IsDefaultableType(string fullName) => fullName switch
        {
            "bool" => true,
            "bool?" => true,
            "sbyte" => true,
            "sbyte?" => true,
            "byte" => true,
            "byte?" => true,
            "short" => true,
            "short?" => true,
            "ushort" => true,
            "ushort?" => true,
            "int" => true,
            "int?" => true,
            "uint" => true,
            "uint?" => true,
            "long" => true,
            "long?" => true,
            "ulong" => true,
            "ulong?" => true,
            "float" => true,
            "float?" => true,
            "double" => true,
            "double?" => true,
            "decimal" => true,
            "decimal?" => true,
            "string" => true,
            "string?" => true,
            "char" => true,
            "char?" => true,
            _ => false,
        };

        public static bool IsEnumUnderlyingType(string fullName) => fullName switch
        {
            "sbyte" => true,
            "byte" => true,
            "short" => true,
            "ushort" => true,
            "int" => true,
            "uint" => true,
            "long" => true,
            "ulong" => true,
            _ => false,
        };

        public static string? DefaultValueToString(object? obj)
        {
            if (obj == null)
            {
                return "null";
            }

            var type = obj.GetType();
            if (type.IsGenericType)
            {
                var definition = type.GetGenericTypeDefinition();
                var arg = type.GetGenericArguments()[0];
                if (definition == typeof(Nullable<>))
                {
                    return DefaultValueToString(Convert.ChangeType(obj, arg));
                }
                else
                {
                    return null;
                }
            }

            if (type == typeof(sbyte) || type == typeof(byte) ||
                type == typeof(short) || type == typeof(ushort) || type == typeof(int) ||
                type == typeof(EnumString))
            {
                return obj.ToString();
            }
            else if (type == typeof(bool))
            {
                if ((bool)obj == true)
                {
                    return "true";
                }
                else
                {
                    return "false";
                }
            }
            else if (type == typeof(uint))
            {
                return obj.ToString() + "u";
            }
            else if (type == typeof(long))
            {
                return obj.ToString() + "L";
            }
            else if (type == typeof(ulong))
            {
                return obj.ToString() + "ul";
            }
            else if (type == typeof(float))
            {
                return obj.ToString() + "f";
            }
            else if (type == typeof(double))
            {
                return obj.ToString() + "d";
            }
            else if (type == typeof(decimal))
            {
                return obj.ToString() + "m";
            }
            else if (type == typeof(string))
            {
                return "\"" + obj.ToString() + "\"";
            }
            else if (type == typeof(char))
            {
                return "'" + obj.ToString() + "'";
            }
            else
            {
                return null;
            }
        }
    }

    public class EnumString
    {
        public EnumString(string name)
        {
            this.Name = name;
        }

        public string Name { get; }

        public override string ToString() => this.Name;
    }
}
