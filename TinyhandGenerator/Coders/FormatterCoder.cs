// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Arc.Visceral;
using Tinyhand.Generator;

namespace Tinyhand.Coders
{
    public sealed class FormatterResolver : ICoderResolver
    {
        /// <summary>
        /// The singleton instance that can be used.
        /// </summary>
        public static readonly FormatterResolver Instance = new();

        public FormatterResolver()
        {
            // Several non-generic types which have formatters but not coders.
            this.AddFormatter("decimal");
            this.AddFormatter("decimal?");
            this.AddFormatter("System.TimeSpan");
            this.AddFormatter("System.DateTimeOffset");
            this.AddFormatter("System.Guid");
            this.AddFormatter("System.Uri");
            this.AddFormatter("System.Version");
            this.AddFormatter("System.Text.StringBuilder");
            this.AddFormatter("System.Collections.BitArray");
            this.AddFormatter("System.Numerics.BigInteger");
            this.AddFormatter("System.Numerics.Complex");
        }

        public bool IsCoderOrFormatterAvailable(WithNullable<TinyhandObject> withNullable)
        {
            if (this.stringToCoder.ContainsKey(withNullable.FullNameWithNullable))
            {// Found
                return true;
            }

            // Several generic types which have formatters but not coders.
            else if (withNullable.Object.Generics_Kind == VisceralGenericsKind.CloseGeneric && withNullable.Object.ConstructedFrom is { } baseObject)
            {// Generics
                var arguments = withNullable.Generics_ArgumentsWithNullable;
                var ret = baseObject.FullName switch
                {
                    "System.Lazy<T>" => true,
                    "System.Collections.Generic.KeyValuePair<TKey, TValue>" => true,
                    _ => false,
                };

                if (ret == false)
                {
                    return ret;
                }

                foreach (var x in arguments)
                {// Check all the arguments.
                    if (!CoderResolver.Instance.IsCoderOrFormatterAvailable(x))
                    {
                        return false;
                    }
                }

                return true;
            }

            return false;
        }

        public ITinyhandCoder? TryGetCoder(WithNullable<TinyhandObject> withNullable)
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

        public ITinyhandCoder AddFormatter(string fullNameWithNullable, bool nonNullableReference = false)
        {
            if (!this.stringToCoder.TryGetValue(fullNameWithNullable, out var coder))
            {
                coder = new FormatterCoder(fullNameWithNullable, nonNullableReference);
                this.stringToCoder[fullNameWithNullable] = coder;
            }

            return coder;
        }

        public ITinyhandCoder? AddFormatter(WithNullable<TinyhandObject> withNullable)
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

        private Dictionary<string, ITinyhandCoder> stringToCoder = new();
    }

    internal class FormatterCoder : ITinyhandCoder
    {
        public FormatterCoder(string fullNameWithNullable, bool nonNullableReference)
        {
            this.FullNameWithNullable = fullNameWithNullable;
            this.NonNullableReference = nonNullableReference;
        }

        public string FullNameWithNullable { get; }

        public bool NonNullableReference { get; }

        public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"options.Resolver.GetFormatter<{this.FullNameWithNullable}>().Serialize(ref writer, {ssb.FullObject}, options);");
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
        {
            if (!this.NonNullableReference)
            {// Value type or Nullable reference type
                ssb.AppendLine($"{ssb.FullObject} = options.Resolver.GetFormatter<{this.FullNameWithNullable}>().Deserialize(ref reader, options);");
            }
            else
            {
                ssb.AppendLine($"{ssb.FullObject} = options.ResolveAndDeserializeReconstruct<{this.FullNameWithNullable}>(ref reader);");
            }
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = options.Resolver.GetFormatter<{this.FullNameWithNullable}>().Reconstruct(options);");
        }
    }
}
