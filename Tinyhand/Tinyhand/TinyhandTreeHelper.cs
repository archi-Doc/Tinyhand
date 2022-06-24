// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Tinyhand.Tree;

namespace Tinyhand;

public static class TinyhandTreeHelper
{
    /// <summary>
    /// Returns true if the element is assigned (the element's parent is TinyhandAssignment).
    /// </summary>
    /// <param name="element">Tinyhand element.</param>
    /// <returns>True if the element is assigned (the element's parent is TinyhandAssignment).</returns>
    public static bool IsAssigned(this Element element)
    {
        return element.Parent?.Type == ElementType.Assignment;
    }

    public static bool IsTrue(this Value element)
    {
        switch (element)
        {
            case Value_Bool b:
                return b.ValueBool;

            case Value_Double d:
                return d.ValueDouble > 0;

            case Value_Long l:
                return l.ValueLong > 0;

            case Value_String s:
                var c = s.ValueStringUtf8;
                if (c.Length == 4 && (c[0] == (byte)'t' || c[0] == (byte)'T') &&
                    (c[1] == (byte)'r' || c[1] == (byte)'R') &&
                    (c[2] == (byte)'u' || c[2] == (byte)'U') &&
                    (c[3] == (byte)'e' || c[3] == (byte)'E'))
                {
                    return true;
                }

                return false;

            default:
                return false;
        }
    }

    public static bool IsFalse(this Value element)
    {
        switch (element)
        {
            case Value_Bool b:
                return !b.ValueBool;

            case Value_Double d:
                return d.ValueDouble == 0;

            case Value_Long l:
                return l.ValueLong == 0;

            case Value_String s:
                var c = s.ValueStringUtf8;
                if (c.Length == 5 && (c[0] == (byte)'f' || c[0] == (byte)'F') &&
                    (c[1] == (byte)'a' || c[1] == (byte)'A') &&
                    (c[2] == (byte)'l' || c[2] == (byte)'L') &&
                    (c[3] == (byte)'s' || c[3] == (byte)'S') &&
                    (c[4] == (byte)'e' || c[4] == (byte)'E'))
                {
                    return true;
                }

                return false;

            default:
                return false;
        }
    }

    public static bool TryGetLeft_IdentifierUtf8(this Element element, [MaybeNullWhen(false)] out byte[] identifier)
    { // identifier = right : get identifier
        if (element is Assignment assignment)
        {
            var left = assignment.LeftElement;
            if (left is Value_Identifier i)
            {
                identifier = i.IdentifierUtf8;
                return true;
            }
        }

        identifier = null;
        return false;
    }

    public static bool TryGetLeft_IdentifierUtf16(this Element element, [MaybeNullWhen(false)] out string identifier)
    { // identifier = right : get identifier
        if (element is Assignment assignment)
        {
            var left = assignment.LeftElement;
            if (left is Value_Identifier i)
            {
                identifier = i.IdentifierUtf16;
                return true;
            }
        }

        identifier = null;
        return false;
    }

    public static bool TryGetRight_Value(this Element element, [MaybeNullWhen(false)] out Value value)
    { // left = value : Get value
        value = (element as Assignment)?.RightElement as Value;
        return value != null;
    }

    public static bool TryGetRight_Value_String(this Element element, [MaybeNullWhen(false)] out Value_String v)
    { // left = "valueString" : Get valueString
        v = (element as Assignment)?.RightElement as Value_String;
        return v != null;
    }

    public static bool TryGetRight_Value_Long(this Element element, [MaybeNullWhen(false)] out Value_Long v)
    { // left = "valueString" : Get valueString
        v = (element as Assignment)?.RightElement as Value_Long;
        return v != null;
    }

    public static bool TryGetRight_Value(this Element element, string identifier, [MaybeNullWhen(false)] out Value value)
    { // identifier = value : Get value if the identifiers are identical.
        if (element is Assignment assignment)
        {
            if (assignment.LeftElement is Value_Identifier i)
            {
                if (identifier == i.IdentifierUtf16)
                {
                    value = assignment.RightElement as Value;
                    return value != null;
                }
            }
        }

        value = null;
        return false;
    }

    public static bool TryGetRight_Value_String(this Element element, string identifier, [MaybeNullWhen(false)] out Value_String v)
    { // identifier = "valueString" : Get value if the identifiers are identical.
        v = null;
        if (element.TryGetRight_Value(identifier, out var v2))
        {
            v = v2 as Value_String;
        }

        return v != null;
    }

    public static bool TryGetRight_Value_Long(this Element element, string identifier, [MaybeNullWhen(false)] out Value_Long v)
    { // identifier = "valueString" : Get value if the identifiers are identical.
        v = null;
        if (element.TryGetRight_Value(identifier, out var v2))
        {
            v = v2 as Value_Long;
        }

        return v != null;
    }

    public static bool TryGetRightGroup_Value_String(this Element element, string? identifier, [MaybeNullWhen(false)] out Value_String valueString)
    { // left = { identifier = "valueString"} : Get valueString
        // left = "valueString" or left = {"valueString"} : If identifier is null.
        if (TryGetRightGroup_Value(element, identifier, out var v))
        {
            return (valueString = v as Value_String) != null;
        }
        else
        {
            valueString = null;
            return false;
        }
    }

    public static bool TryGetRightGroup_Value(this Element element, string? identifier, [MaybeNullWhen(false)] out Value value)
    { // left = { identifier = value} : Get value
        // left = value or left = {value} : If identifier is null.
        value = null;

        var assignment = element as Assignment;
        if (assignment == null)
        {
            return false;
        }

        var right = assignment.RightElement;
        if (right == null)
        {
            return false;
        }

        if (identifier == null)
        {
            value = right as Value;
            if (value != null)
            {
                return true;
            }
        }

        var group = right as Group;
        if (group == null)
        {
            return false;
        }

        if (identifier == null)
        {
            foreach (var x in group)
            {
                if (x is Value v)
                {
                    value = v;
                    return true;
                }
            }
        }
        else
        {
            foreach (var x in group)
            {
                if (x.TryGetRight_Value(identifier, out value))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
