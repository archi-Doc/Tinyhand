﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Visceral;
using Tinyhand.Generator;

namespace Tinyhand.Coders
{
    public sealed class TextArrayResolver : ITextCoderResolver
    {
        /// <summary>
        /// The singleton instance that can be used.
        /// </summary>
        public static readonly TextArrayResolver Instance = new();

        public ITinyhandTextCoder? TryGetCoder(WithNullable<TinyhandObject> withNullable)
        {
            if (withNullable.Object.Array_Rank == 1)
            {// T[]
                var element = withNullable.Array_ElementWithNullable;
                if (element == null)
                {
                    return null;
                }

                var elementCoder = TextCoderResolver.Instance.TryGetCoder(element);
                return new TextArrayCoder(element, elementCoder, withNullable.Nullable);
            }

            return null;
        }
    }

    public class TextArrayCoder : ITinyhandTextCoder
    {
        public TextArrayCoder(WithNullable<TinyhandObject> element, ITinyhandTextCoder? elementCoder, NullableAnnotation nullableAnnotation)
        {
            this.element = element;
            this.elementCoder = elementCoder;
            this.nullableAnnotation = nullableAnnotation;
        }

        public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            // if (this.block == null) // XUnitTest static issue
            {
                this.GenerateMethod(info);
            }

            ssb.AppendLine($"{GeneratorInformation.GeneratedMethod}.SerializeArray_{this.block!.SerialNumber:0000}(out {ssb.SecondaryObject}, {ssb.FullObject}, options);");
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            // if (this.block == null) // XUnitTest static issue
            {
                this.GenerateMethod(info);
            }

            if (this.nullableAnnotation != NullableAnnotation.NotAnnotated)
            {// Nullable
                ssb.AppendLine($"{ssb.FullObject} = {GeneratorInformation.GeneratedMethod}.DeserializeArray_{this.block!.SerialNumber:0000}({ssb.SecondaryObject}, options);");
            }
            else
            {// Non-nullable
                ssb.AppendLine($"{ssb.FullObject} = {GeneratorInformation.GeneratedMethod}.DeserializeArray_{this.block!.SerialNumber:0000}({ssb.SecondaryObject}, options) ?? System.Array.Empty<{this.element.FullNameWithNullable}>();");
            }
        }

        private void GenerateMethod(GeneratorInformation info)
        {
            var key = this.element.FullNameWithNullable;
            if (!info.CreateBlock($"TextArray::{key}", out this.block))
            {// Already exists.
                return;
            }

            this.GenerateSerializer(this.block.SSB, info);
            this.GenerateDeserializer(this.block.SSB, info);
            this.block.SSB.AppendLine();
        }

        private void GenerateSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            using (var m = ssb.ScopeBrace($"internal static void SerializeArray_{this.block!.SerialNumber:0000}(out Element element, {this.element.FullNameWithNullable}[]? value, TinyhandSerializerOptions options)"))
            using (var v = ssb.ScopeObject("value"))
            {
                ssb.AppendLine($"if ({ssb.FullObject} == null) element = new Value_Null();");
                using (var e = ssb.ScopeBrace("else"))
                {
                    if (this.elementCoder == null)
                    {// use option.Resolver.GetFormatter<T>()
                        ssb.AppendLine($"var textFormatter = options.TextResolver.GetFormatter<{this.element.FullName}>();");
                    }

                    ssb.AppendLine($"element = new Group({ssb.FullObject}.Length);");
                    using (var b = ssb.ScopeBrace($"for (int i = 0; i < {ssb.FullObject}.Length; i++)"))
                    {
                        using (var element = ssb.ScopeObject("[i]", false))
                        {
                            ssb.SetSecondaryObject("element[i]");
                            if (this.elementCoder == null)
                            {// use option.Resolver.GetFormatter<T>()
                                ssb.AppendLine($"formatter.Serialize(out {ssb.SecondaryObject}, {ssb.FullObject}, options);");
                            }
                            else
                            {// use coder
                                this.elementCoder.CodeSerializer(ssb, info);
                            }
                        }
                    }
                }
            }
        }

        private void GenerateDeserializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            using (var m = ssb.ScopeBrace($"internal static {this.element.FullNameWithNullable}[]? DeserializeArray_{this.block!.SerialNumber:0000}(Element element, TinyhandSerializerOptions options)"))
            {
                if (this.elementCoder == null)
                {// use option.Resolver.GetFormatter<T>()
                    ssb.AppendLine($"var formatter = options.Resolver.GetFormatter<{this.element.FullName}>();");
                    ssb.AppendLine($"var textFormatter = options.TextResolver.GetFormatter<{this.element.FullName}>();");
                }

                ssb.AppendLine("var group = TreeSerialize.GetGroup(element);");
                ssb.AppendLine("var len = group.ElementList.Count");
                var idx = this.element.FullName.IndexOf('[');
                if (idx < 0)
                { // int
                    ssb.AppendLine($"var array = new {this.element.FullNameWithNullable}[len];");
                }
                else
                { // int[][]
                    ssb.AppendLine($"var array = new {this.element.FullNameWithNullable.Substring(0, idx)}[len]{this.element.FullNameWithNullable.Substring(idx)};");
                }

                ssb.AppendLine("options.Security.DepthStep(ref reader);");
                using (var scopeSecurityTry = ssb.ScopeBrace("try"))
                {
                    using (var c2 = ssb.ScopeBrace("for (int i = 0; i < array.Length; i++)"))
                    {
                        using (var element = ssb.ScopeObject("array[i]", false))
                        {
                            if (this.elementCoder == null)
                            {// use option.Resolver.GetFormatter<T>()
                                if (this.element.Nullable == NullableAnnotation.NotAnnotated)
                                {
                                    ssb.AppendLine($"{element.FullObject} = textFormatter.Deserialize(group.ElementList[i], options)!;");
                                }
                                else
                                {
                                    ssb.AppendLine($"{element.FullObject} = textFormatter.Deserialize(group.ElementList[i], options) ?? formatter.Reconstruct(options);");
                                }
                            }
                            else
                            {// use coder
                                this.elementCoder.CodeDeserializer(ssb, info);
                            }
                        }
                    }
                }

                using (var scopeSecurityFinally = ssb.ScopeBrace("finally"))
                {
                    ssb.AppendLine("reader.Depth--;");
                }

                ssb.AppendLine($"return array;");
            }
        }

        private GeneratorBlock? block;
        private WithNullable<TinyhandObject> element;
        private ITinyhandTextCoder? elementCoder;
        private NullableAnnotation nullableAnnotation;
    }
}
