// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Generic;
using Arc.Visceral;
using Tinyhand.Generator;

namespace Tinyhand.Coders;

public sealed class CoderResolver : ICoderResolver
{
    private readonly ICoderResolver[] resolvers;

    public CoderResolver()
    {
        this.resolvers = new ICoderResolver[]
        {
            BuiltinCoder.Instance,
            NullableResolver.Instance,
            ArrayResolver.Instance,
            ListResolver.Instance,
            EnumResolver.Instance,
            this.ObjectResolver,
            this.FormatterResolver,
        };
    }

    public ObjectResolver ObjectResolver { get; } = new();

    public FormatterResolver FormatterResolver { get; } = new();

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

        if (this.ObjectResolver.IsCoderOrFormatterAvailable(withNullable))
        {
            return true;
        }

        if (this.FormatterResolver.IsCoderOrFormatterAvailable(withNullable))
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

        foreach (var x in this.resolvers)
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

    private readonly Dictionary<WithNullable<TinyhandObject>, ITinyhandCoder> objectToCoder = new();
}
