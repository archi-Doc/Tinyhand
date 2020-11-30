﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

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
            this.AddFormatter(typeof(System.TimeSpan));
            this.AddFormatter(typeof(System.DateTimeOffset));
            this.AddFormatter(typeof(System.Guid));
            this.AddFormatter(typeof(System.Uri));
            this.AddFormatter(typeof(System.Version));
            this.AddFormatter(typeof(System.Text.StringBuilder));
            this.AddFormatter(typeof(System.Collections.BitArray));
            this.AddFormatter(typeof(System.Numerics.BigInteger));
            this.AddFormatter(typeof(System.Numerics.Complex));
            this.AddFormatter(typeof(Type));

            this.AddFormatter("MessagePack.Nil");
            this.AddFormatter("MessagePack.Nil?");

            this.AddFormatter(typeof(object[]));
            this.AddFormatter(typeof(List<object>));

            this.AddFormatter(typeof(Memory<byte>));
            this.AddFormatter(typeof(Memory<byte>?));
            this.AddFormatter(typeof(ReadOnlyMemory<byte>));
            this.AddFormatter(typeof(ReadOnlyMemory<byte>?));
            this.AddFormatter(typeof(System.Buffers.ReadOnlySequence<byte>));
            this.AddFormatter(typeof(System.Buffers.ReadOnlySequence<byte>?));
            this.AddFormatter(typeof(ArraySegment<byte>));
            this.AddFormatter(typeof(ArraySegment<byte>?));
        }

        public bool IsCoderOrFormatterAvailable(WithNullable<TinyhandObject> withNullable)
        {
            if (this.stringToCoder.ContainsKey(withNullable.FullNameWithNullable))
            {// Found
                return true;
            }

            // Several generic types which have formatters but not coders.
            if (withNullable.Object.Array_Rank >= 1 && withNullable.Object.Array_Rank <= 4)
            {// Array 1-4
                var elementWithNullable = withNullable.Array_ElementWithNullable;
                if (elementWithNullable != null)
                {
                    return CoderResolver.Instance.IsCoderOrFormatterAvailable(elementWithNullable);
                }
            }
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

        public ITinyhandCoder AddFormatterForReferenceType(string fullNameWithNullable)
        {
            var coder = this.AddFormatter(fullNameWithNullable, true);
            this.AddFormatter(fullNameWithNullable + "?");
            return coder;
        }

        public ITinyhandCoder? AddFormatter(Type type)
        {
            if (type.IsValueType)
            {// Value type
                return this.AddFormatter(type.FullName);
            }
            else
            {// Reference type
                var coder = this.AddFormatter(type.FullName, true);
                this.AddFormatter(type.FullName + "?");
                return coder;
            }
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
                ssb.AppendLine($"{ssb.FullObject} = options.Resolver.GetFormatter<{this.FullNameWithNullable}>().Deserialize(ref reader, overwriteFlag, options);");
            }
            else
            {// Non-nullable reference type
                ssb.AppendLine($"{ssb.FullObject} = options.DeserializeAndReconstruct<{this.FullNameWithNullable}>(ref reader, overwriteFlag);");
            }
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = options.Resolver.GetFormatter<{this.FullNameWithNullable}>().Reconstruct(options);");
        }
    }
}
