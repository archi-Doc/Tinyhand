// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Tinyhand.Tree;

namespace Tinyhand
{
    public static class TreeSerialize
    {
        public static bool IsNull(Element element) => element is Value_Null;

        public static Group GetGroup(Element element)
        {
            if (element is Group group)
            {
                return group;
            }

            throw new TinyhandTreeTypeException(element, typeof(Group));
        }

        public static void GetGroupItem(Element element, out Value_Identifier identifier, out Element e)
        {
            if (element is Assignment assignment)
            {
                if (assignment.LeftElement is Value_Identifier left)
                {
                    if (assignment.RightElement is { } right)
                    {
                        identifier = left;
                        e = right;
                        return;
                    }

                    throw new TinyhandTreeException(element, "Right value is null.");
                }

                throw new TinyhandTreeTypeException(element, typeof(Value_Identifier));
            }

            throw new TinyhandTreeTypeException(element, typeof(Assignment));
        }
    }

    public class TinyhandTreeException : TinyhandException
    {
        public TinyhandTreeException(Element element, string message)
            : base(message + $" (Line:{element.LineNumber} BytePosition:{element.BytePositionInLine})")
        {
        }

        public TinyhandTreeException(Element element, string message, Exception innerException)
            : base(message + $" (Line:{element.LineNumber} BytePosition:{element.BytePositionInLine})", innerException)
        {
        }
    }

    public class TinyhandTreeTypeException : TinyhandTreeException
    {
        public TinyhandTreeTypeException(Element element, Type typeExpected)
            : base(element, $"The expected type is {typeExpected.Name}, but the actual type is {element.ToString()}.")
        {
        }

        public TinyhandTreeTypeException(Element element, Type typeExpected, Exception innerException)
            : base(element, $"The expected type is {typeExpected.Name}, but the actual type is {element.ToString()}.", innerException)
        {
        }
    }
}
