// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Arc.Visceral;
using Tinyhand.Generator;

namespace Tinyhand.Coders
{
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

            if (withNullable.Object.Array_Rank > 0)
            {// Array
                var elementWithNullable = withNullable.Array_ElementWithNullable;
                if (elementWithNullable != null)
                {
                    return this.IsCoderOrFormatterAvailable(elementWithNullable);
                }
            }
            else if (withNullable.Object.Generics_Kind == VisceralGenericsKind.CloseGeneric && withNullable.Object.ConstructedFrom is { } baseObject)
            {// Generics
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
            {
                return true;
            }

            return false;
        }

        public void AddTinyhandObjectCoder(TinyhandObject obj)
        {
            if (obj.ObjectAttribute == null || !obj.Kind.IsType())
            {
                return;
            }

            var withNullable = obj.TypeObjectWithNullable;
            if (withNullable != null && !this.objectToCoder.ContainsKey(withNullable))
            {// T
                this.objectToCoder[withNullable] = new TinyhandObjectCoder(obj.FullName, false);
            }

            if (obj.Kind.IsReferenceType())
            {// T?
                withNullable = obj.CreateWithNullable(NullableAnnotation.Annotated);
                if (withNullable != null && !this.objectToCoder.ContainsKey(withNullable))
                {
                    this.objectToCoder[withNullable] = new TinyhandObjectCoder(obj.FullName, true);
                }
            }
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

        private class TinyhandObjectCoder : ITinyhandCoder
        {
            public TinyhandObjectCoder(string fullName, bool nullable)
            {
                this.FullName = fullName;
                this.Nullable = nullable;
            }

            public string FullName { get; }

            public bool Nullable { get; }

            public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
            {
                ssb.AppendLine($"options.Resolver.GetFormatter<{this.FullName}>().Serialize(ref writer, {ssb.FullObject}, options);");
            }

            public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
            {
                if (this.Nullable)
                {
                    ssb.AppendLine($"{ssb.FullObject} = options.Resolver.GetFormatter<{this.FullName}>().Deserialize(ref reader, options);");
                }
                else
                {
                    ssb.AppendLine($"{ssb.FullObject} = options.ResolveAndDeserializeReconstruct<{this.FullName}>(ref reader);");
                }
            }

            public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
            {
                ssb.AppendLine($"{ssb.FullObject} = options.Resolver.GetFormatter<{this.FullName}>().Reconstruct(options);");
            }
        }
    }
}
