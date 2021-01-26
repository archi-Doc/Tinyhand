// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using Arc.Visceral;
using Tinyhand.Generator;

namespace Tinyhand.Coders
{
    public sealed class TextFormatterResolver : ITextCoderResolver
    {
        /// <summary>
        /// The singleton instance that can be used.
        /// </summary>
        public static readonly TextFormatterResolver Instance = new();

        public TextFormatterResolver()
        {
            // Several non-generic types which have formatters but not coders.
        }

        public bool IsCoderOrFormatterAvailable(WithNullable<TinyhandObject> withNullable)
        {
            if (this.stringToCoder.ContainsKey(withNullable.FullNameWithNullable))
            {// Found
                return true;
            }

            // Several generic types which have formatters but not coders.
            /* if (withNullable.Object.Array_Rank >= 1 && withNullable.Object.Array_Rank <= 4)
            {// Array 1-4
                var elementWithNullable = withNullable.Array_ElementWithNullable;
                if (elementWithNullable != null)
                {
                    return CoderResolver.Instance.IsCoderOrFormatterAvailable(elementWithNullable);
                }
            }
            else if (withNullable.Object.Generics_Kind == VisceralGenericsKind.CloseGeneric && withNullable.Object.OriginalDefinition is { } baseObject)
            {// Generics
                var arguments = withNullable.Generics_ArgumentsWithNullable;
                if (this.genericsType.Contains(baseObject.FullName))
                {
                    goto Check_GenericsArguments;
                }
                else if (withNullable.Object.SimpleName == "Tuple")
                {// Tuple
                    goto Check_GenericsArguments;
                }
                else if (withNullable.Object.SimpleName == "ValueTuple")
                {// ValueTuple
                    goto Check_GenericsArguments;
                }

                // Not supported generics type.
                return false;

Check_GenericsArguments:
                foreach (var x in arguments)
                {// Check all the arguments.
                    if (!CoderResolver.Instance.IsCoderOrFormatterAvailable(x))
                    {
                        return false;
                    }
                }

                return true;
            }
            */

            return false;
        }

        public ITinyhandTextCoder? TryGetCoder(WithNullable<TinyhandObject> withNullable)
        {
            this.stringToCoder.TryGetValue(withNullable.FullNameWithNullable, out var value);
            if (value != null)
            {
                return value;
            }

            if (this.IsCoderOrFormatterAvailable(withNullable))
            {
                value = this.AddFormatter(withNullable);
            }

            return value;
        }

        public ITinyhandTextCoder AddFormatter(string fullNameWithNullable, bool nonNullableReference = false)
        {
            if (!this.stringToCoder.TryGetValue(fullNameWithNullable, out var coder))
            {
                coder = new TextFormatterCoder(fullNameWithNullable, nonNullableReference);
                this.stringToCoder[fullNameWithNullable] = coder;
            }

            return coder;
        }

        public ITinyhandTextCoder AddFormatterForReferenceType(string fullNameWithNullable)
        {
            var coder = this.AddFormatter(fullNameWithNullable, true);
            this.AddFormatter(fullNameWithNullable + "?");
            return coder;
        }

        public void AddGenericsType(Type genericsType)
        {
            this.genericsType.Add(VisceralHelper.TypeToFullName(genericsType));
        }

        public ITinyhandTextCoder? AddFormatter(Type type)
        {
            var fullName = VisceralHelper.TypeToFullName(type);

            if (type.IsValueType)
            {// Value type
                return this.AddFormatter(fullName);
            }
            else
            {// Reference type
                var coder = this.AddFormatter(fullName, true);
                this.AddFormatter(fullName + "?");
                return coder;
            }
        }

        public ITinyhandTextCoder? AddFormatter(WithNullable<TinyhandObject> withNullable)
        {
            if (!withNullable.Object.Kind.IsType())
            {
                return null;
            }

            if (withNullable.Object.Kind.IsReferenceType())
            {// Reference type
                var fullName = withNullable.FullNameWithNullable.TrimEnd('?');
                var c = this.AddFormatter(fullName, true); // T (non-nullable)
                var c2 = this.AddFormatter(fullName + "?"); // T?

                if (withNullable.Nullable == NullableAnnotation.NotAnnotated)
                {// T
                    return c;
                }
                else
                {// T?, None
                    return c2;
                }
            }
            else
            {// Value type
                return this.AddFormatter(withNullable.FullNameWithNullable); // T
            }
        }

        private Dictionary<string, ITinyhandTextCoder> stringToCoder = new();

        private HashSet<string> genericsType = new();
    }

    internal class TextFormatterCoder : ITinyhandTextCoder
    {
        public TextFormatterCoder(string fullNameWithNullable, bool nonNullableReference)
        {
            this.FullNameWithNullable = fullNameWithNullable;
            this.NonNullableReference = nonNullableReference;
        }

        public string FullNameWithNullable { get; }

        public bool NonNullableReference { get; }

        public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"options.TextResolver.GetFormatter<{this.FullNameWithNullable}>().Serialize(out {ssb.SecondaryObject}, {ssb.FullObject}, options);");
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            if (!this.NonNullableReference)
            {// Value type or Nullable reference type
                ssb.AppendLine($"{ssb.FullObject} = options.TextResolver.GetFormatter<{this.FullNameWithNullable}>().Deserialize({ssb.SecondaryObject}, options);");
            }
            else
            {// Non-nullable reference type
                ssb.AppendLine($"{ssb.FullObject} = options.TextDeserializeAndReconstruct<{this.FullNameWithNullable}>({ssb.SecondaryObject});");
            }
        }
    }
}
