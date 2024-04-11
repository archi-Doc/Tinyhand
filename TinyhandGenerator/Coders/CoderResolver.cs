// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Generic;
using Arc.Visceral;
using Tinyhand.Generator;

namespace Tinyhand.Coders;

public sealed class CoderResolver : ICoderResolver
{
    /// <summary>
    /// The singleton instance that can be used.
    /// </summary>
    public static readonly CoderResolver Instance = new CoderResolver();

    private static readonly ICoderResolver[] Resolvers = new ICoderResolver[]
    {
        BuiltinCoder.Instance,
        NullableResolver.Instance,
        ArrayResolver.Instance,
        ListResolver.Instance,
        EnumResolver.Instance,
        ObjectResolver.Instance,
        FormatterResolver.Instance,
    };

    private CoderResolver()
    {
    }

    public bool IsCoderOrFormatterAvailable(WithNullable<TinyhandObject> withNullable)
    {
        if (withNullable.Object == null)
        {
            return false;
        }

        this.objectToCoder.TryGetValue(withNullable, out var coder);
        if (coder != null)
        {
            return true;
        }

        if (BuiltinCoder.Instance.TryGetCoder(withNullable) != null)
        {
            return true;
        }

        if (ObjectResolver.Instance.IsCoderOrFormatterAvailable(withNullable))
        {
            return true;
        }

        if (FormatterResolver.Instance.IsCoderOrFormatterAvailable(withNullable))
        {
            return true;
        }

        // Several types which have formatters AND coders.
        if (withNullable.Object.Array_Rank == 1)
        {// Array
            var elementWithNullable = withNullable.Array_ElementWithNullable;
            if (elementWithNullable != null)
            {
                return this.IsCoderOrFormatterAvailable(elementWithNullable);
            }
        }
        else if (withNullable.Object.Generics_Kind == VisceralGenericsKind.ClosedGeneric && withNullable.Object.OriginalDefinition is { } baseObject)
        {// Generics (List, Nullable)
            var arguments = withNullable.Generics_ArgumentsWithNullable;
            var ret = baseObject.FullName switch
            {
                "System.Collections.Generic.List<T>" => true,
                "T?" => true,
                _ => false,
            };

            if (ret)
            {
                return this.IsCoderOrFormatterAvailable(arguments[0]);
            }

            /*if (ret == false)
            {
                return ret;
            }

            if (arguments.Length == 0)
            {
                return false;
            }*/
        }
        else if (withNullable.Object.Kind == VisceralObjectKind.Enum)
        {// Enum
            return true;
        }

        var obj = withNullable.Object;
        if (obj is null)
        {
            return false;
        }

        obj.Configure();
        return obj.ObjectAttribute is not null;
    }

    public ITinyhandCoder? TryGetCoder(WithNullable<TinyhandObject> withNullable)
    {
        this.objectToCoder.TryGetValue(withNullable, out var coder);
        if (coder != null)
        {
            return coder;
        }

        foreach (var x in Resolvers)
        {
            var c = x.TryGetCoder(withNullable);
            if (c != null)
            {
                this.objectToCoder[withNullable] = c;
                coder = c;
                break;
            }
        }

        return coder;
    }

    private Dictionary<WithNullable<TinyhandObject>, ITinyhandCoder> objectToCoder = new();
}
