// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Visceral;
using Tinyhand.Generator;

namespace Tinyhand.Coders
{
    public sealed class ArrayResolver : ICoderResolver
    {
        /// <summary>
        /// The singleton instance that can be used.
        /// </summary>
        public static readonly ArrayResolver Instance = new();

        public ITinyhandCoder? TryGetCoder(WithNullable<TinyhandObject> withNullable)
        {
            if (withNullable.Object.Array_Rank == 1)
            {// T[]
                var element = withNullable.Array_ElementWithNullable;
                if (element == null)
                {
                    return null;
                }
                else if (element.Object.Kind == VisceralObjectKind.TypeParameter)
                {
                    return new GenericArrayCoder(element, withNullable.Nullable);
                }

                var elementCoder = CoderResolver.Instance.TryGetCoder(element);
                return new ArrayCoder(element, elementCoder, withNullable.Nullable);
            }

            return null;
        }
    }

    public class ArrayCoder : ITinyhandCoder
    {
        public ArrayCoder(WithNullable<TinyhandObject> element, ITinyhandCoder? elementCoder, NullableAnnotation nullableAnnotation)
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

            ssb.AppendLine($"{GeneratorInformation.GeneratedMethod}.SerializeArray_{this.block!.SerialNumber:0000}(ref writer, {ssb.FullObject}, options);");
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
        {
            // if (this.block == null) // XUnitTest static issue
            {
                this.GenerateMethod(info);
            }

            if (this.nullableAnnotation != NullableAnnotation.NotAnnotated)
            {// Nullable
                if (nilChecked)
                {// Nil already checked.
                    CodeDeserializerNullable();
                }
                else
                {
                    using (var b = ssb.ScopeBrace("if (reader.TryReadNil())"))
                    {
                        ssb.AppendLine($"{ssb.FullObject} = default;");
                    }

                    using (var b = ssb.ScopeBrace($"else"))
                    {
                        CodeDeserializerNullable();
                    }
                }
            }
            else
            {// Non-nullable
                if (nilChecked)
                {// Nil already checked.
                    CodeDeserializerNonNullable();
                }
                else
                {
                    using (var b = ssb.ScopeBrace("if (reader.TryReadNil())"))
                    {
                        ssb.AppendLine($"{ssb.FullObject} = System.Array.Empty<{this.element.FullNameWithNullable}>();");
                    }

                    using (var b = ssb.ScopeBrace($"else"))
                    {
                        CodeDeserializerNonNullable();
                    }
                }
            }

            void CodeDeserializerNullable()
            {
                ssb.AppendLine($"{ssb.FullObject} = {GeneratorInformation.GeneratedMethod}.DeserializeArray_{this.block!.SerialNumber:0000}(ref reader, options);");
            }

            void CodeDeserializerNonNullable()
            {
                ssb.AppendLine($"{ssb.FullObject} = {GeneratorInformation.GeneratedMethod}.DeserializeArray_{this.block!.SerialNumber:0000}(ref reader, options) ?? System.Array.Empty<{this.element.FullNameWithNullable}>();");
            }
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = System.Array.Empty<{this.element.FullName}>();");
        }

        public void CodeClone(ScopingStringBuilder ssb, GeneratorInformation info, string sourceObject)
        {
            ssb.AppendLine($"{ssb.FullObject} = options.Resolver.GetFormatter<{this.element.FullNameWithNullable}[]>().Clone({sourceObject}, options)!;");
        }

        private void GenerateMethod(GeneratorInformation info)
        {
            var key = this.element.FullNameWithNullable;
            /*if (this.element.Object.Kind.IsReferenceType())
            {
                key = key.TrimEnd('?');
            }*/

            if (!info.CreateBlock($"Array::{key}", out this.block))
            {// Already exists.
                return;
            }

            this.GenerateSerializer(this.block.SSB, info);
            this.GenerateDeserializer(this.block.SSB, info);
            this.block.SSB.AppendLine();
        }

        private void GenerateSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            using (var m = ssb.ScopeBrace($"internal static void SerializeArray_{this.block!.SerialNumber:0000}(ref TinyhandWriter writer, {this.element.FullNameWithNullable}[]? value, TinyhandSerializerOptions options)"))
            using (var v = ssb.ScopeObject("value"))
            {
                ssb.AppendLine($"if ({ssb.FullObject} == null) writer.WriteNil();");
                using (var e = ssb.ScopeBrace("else"))
                {
                    if (this.elementCoder == null)
                    {// use option.Resolver.GetFormatter<T>()
                        ssb.AppendLine($"var formatter = options.Resolver.GetFormatter<{this.element.FullName}>();");
                    }

                    ssb.AppendLine($"writer.WriteArrayHeader({ssb.FullObject}.Length);");
                    using (var b = ssb.ScopeBrace($"for (int i = 0; i < {ssb.FullObject}.Length; i++)"))
                    {
                        using (var element = ssb.ScopeObject("[i]", false))
                        {
                            if (this.elementCoder == null)
                            {// use option.Resolver.GetFormatter<T>()
                                ssb.AppendLine($"formatter.Serialize(ref writer, {ssb.FullObject}, options);");
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
            using (var m = ssb.ScopeBrace($"internal static {this.element.FullNameWithNullable}[]? DeserializeArray_{this.block!.SerialNumber:0000}(ref TinyhandReader reader, TinyhandSerializerOptions options)"))
            {
                if (this.elementCoder == null)
                {// use option.Resolver.GetFormatter<T>()
                    ssb.AppendLine($"var formatter = options.Resolver.GetFormatter<{this.element.FullName}>();");
                }

                ssb.AppendLine("var len = reader.ReadArrayHeader();");
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
                        // ssb.AppendLine("#nullable disable");
                        using (var element = ssb.ScopeObject("array[i]", false))
                        {
                            if (this.elementCoder == null)
                            {// use option.Resolver.GetFormatter<T>()
                                if (this.element.Nullable == NullableAnnotation.NotAnnotated)
                                {
                                    ssb.AppendLine($"{element.FullObject} = formatter.Deserialize(ref reader, options)!;");
                                }
                                else
                                {
                                    ssb.AppendLine($"{element.FullObject} = formatter.Deserialize(ref reader, options) ?? formatter.Reconstruct(options);");
                                }
                            }
                            else
                            {// use coder
                                this.elementCoder.CodeDeserializer(ssb, info);
                            }
                        }

                        // ssb.AppendLine("#nullable restore");
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
        private ITinyhandCoder? elementCoder;
        private NullableAnnotation nullableAnnotation;
    }

    public class GenericArrayCoder : ITinyhandCoder
    {
        public GenericArrayCoder(WithNullable<TinyhandObject> element, NullableAnnotation nullableAnnotation)
        {
            this.element = element;
            this.nullableAnnotation = nullableAnnotation;
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked = false)
        {
            if (this.nullableAnnotation != NullableAnnotation.NotAnnotated)
            {// Nullable
                ssb.AppendLine($"{ssb.FullObject} = options.Resolver.GetFormatter<{this.element.FullName}[]>().Deserialize(ref reader, options);");
            }
            else
            {// Non-nullable
                ssb.AppendLine($"{ssb.FullObject} = options.DeserializeAndReconstruct<{this.element.FullName}[]>(ref reader);");
            }
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = System.Array.Empty<{this.element.FullName}>();");
        }

        public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"options.Resolver.GetFormatter<{this.element.FullName}[]>().Serialize(ref writer, {ssb.FullObject}, options);");
        }

        public void CodeClone(ScopingStringBuilder ssb, GeneratorInformation info, string sourceObject)
        {
            ssb.AppendLine($"{ssb.FullObject} = options.Resolver.GetFormatter<{this.element.FullName}[]>().Clone({sourceObject}, options)!;");
        }

        private WithNullable<TinyhandObject> element;
        private NullableAnnotation nullableAnnotation;
    }
}
