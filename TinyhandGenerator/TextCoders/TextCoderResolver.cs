// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Arc.Visceral;
using Tinyhand.Generator;

namespace Tinyhand.Coders
{
    public sealed class TextCoderResolver : ITextCoderResolver
    {
        /// <summary>
        /// The singleton instance that can be used.
        /// </summary>
        public static readonly TextCoderResolver Instance = new TextCoderResolver();

        private static readonly ITextCoderResolver[] Resolvers = new ITextCoderResolver[]
        {
            TextBuiltinCoder.Instance,
            TextNullableResolver.Instance,
            TextArrayResolver.Instance,
            TextListResolver.Instance,
            TextEnumResolver.Instance,
            TextFormatterResolver.Instance,
        };

        private TextCoderResolver()
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

            if (TextBuiltinCoder.Instance.TryGetCoder(withNullable) != null)
            {
                return true;
            }

            if (TextFormatterResolver.Instance.IsCoderOrFormatterAvailable(withNullable))
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
            else if (withNullable.Object.Generics_Kind == VisceralGenericsKind.CloseGeneric && withNullable.Object.OriginalDefinition is { } baseObject)
            {// Generics (List, Nullable)
                var arguments = withNullable.Generics_ArgumentsWithNullable;
                var ret = baseObject.FullName switch
                {
                    "System.Collections.Generic.List<T>" => true,
                    "T?" => true,
                    _ => false,
                };

                if (ret == false)
                {
                    return ret;
                }

                if (arguments.Length == 0)
                {
                    return false;
                }

                return this.IsCoderOrFormatterAvailable(arguments[0]);
            }
            else if (withNullable.Object.Kind == VisceralObjectKind.Enum)
            {// Enum
                return true;
            }

            return false;
        }

        public ITinyhandTextCoder? TryGetCoder(WithNullable<TinyhandObject> withNullable)
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

        private Dictionary<WithNullable<TinyhandObject>, ITinyhandTextCoder> objectToCoder = new();
    }
}
