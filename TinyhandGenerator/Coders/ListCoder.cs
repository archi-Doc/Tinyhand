// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Visceral;
using Tinyhand.Generator;

namespace Tinyhand.Coders
{
    public sealed class ListResolver : ICoderResolver
    {
        /// <summary>
        /// The singleton instance that can be used.
        /// </summary>
        public static readonly ListResolver Instance = new();

        public ITinyhandCoder? TryGetCoder(WithNullable<TinyhandObject> withNullable)
        {
            if (withNullable.Object == null)
            {
                return null;
            }

            if (withNullable.Object.Generics_Kind == VisceralGenericsKind.CloseGeneric && withNullable.Object.OriginalDefinition is { } baseObject)
            {// Generics
                var arguments = withNullable.Generics_ArgumentsWithNullable;
                if (baseObject.FullName == "System.Collections.Generic.List<T>")
                {
                    if (arguments.Length == 1)
                    {
                        if (arguments[0].Object.Kind == VisceralObjectKind.TypeParameter)
                        {
                            return null;
                        }

                        var elementCoder = CoderResolver.Instance.TryGetCoder(arguments[0]);
                        return new ListCoder(arguments[0], elementCoder, withNullable.Nullable);
                    }
                }
            }

            return null;
        }
    }

    public class ListCoder : ITinyhandCoder
    {
        public ListCoder(WithNullable<TinyhandObject> withNullable, ITinyhandCoder? elementCoder, NullableAnnotation nullableAnnotation)
        {
            this.element = withNullable;
            this.elementCoder = elementCoder;
            this.nullableAnnotation = nullableAnnotation;
        }

        public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            this.GenerateMethod(info);

            ssb.AppendLine($"{GeneratorInformation.GeneratedMethod}.SerializeList_{this.block!.SerialNumber:0000}(ref writer, {ssb.FullObject}, options);");
        }

        public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
        {
            this.GenerateMethod(info);

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
                        ssb.AppendLine($"{ssb.FullObject} = new System.Collections.Generic.List<{this.element.FullNameWithNullable}>();");
                    }

                    using (var b = ssb.ScopeBrace($"else"))
                    {
                        CodeDeserializerNonNullable();
                    }
                }
            }

            void CodeDeserializerNullable()
            {
                ssb.AppendLine($"{ssb.FullObject} = {GeneratorInformation.GeneratedMethod}.DeserializeList_{this.block!.SerialNumber:0000}(ref reader, options);");
            }

            void CodeDeserializerNonNullable()
            {
                ssb.AppendLine($"{ssb.FullObject} = {GeneratorInformation.GeneratedMethod}.DeserializeList_{this.block!.SerialNumber:0000}(ref reader, options) ?? new System.Collections.Generic.List<{this.element.FullNameWithNullable}>();");
            }
        }

        public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"{ssb.FullObject} = new System.Collections.Generic.List<{this.element.FullNameWithNullable}>();");
        }

        private void GenerateMethod(GeneratorInformation info)
        {
            var key = this.element.FullNameWithNullable;
            /*if (this.element.Object.Kind.IsReferenceType())
            {
                key = key.TrimEnd('?');
            }*/

            if (!info.CreateBlock($"List::{key}", out this.block))
            {// Already exists.
                return;
            }

            this.GenerateSerializer(this.block.SSB, info);
            this.GenerateDeserializer(this.block.SSB, info);
            this.block.SSB.AppendLine();
        }

        private void GenerateSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            using (var m = ssb.ScopeBrace($"internal static void SerializeList_{this.block!.SerialNumber:0000}(ref TinyhandWriter writer, System.Collections.Generic.List<{this.element.FullNameWithNullable}>? value, TinyhandSerializerOptions options)"))
            using (var v = ssb.ScopeObject("value"))
            {
                ssb.AppendLine($"if ({ssb.FullObject} == null) writer.WriteNil();");
                using (var e = ssb.ScopeBrace("else"))
                {
                    if (this.elementCoder == null)
                    {// use option.Resolver.GetFormatter<T>()
                        ssb.AppendLine($"var formatter = options.Resolver.GetFormatter<{this.element.FullName}>();");
                    }

                    ssb.AppendLine($"writer.WriteArrayHeader({ssb.FullObject}.Count);");
                    using (var b = ssb.ScopeBrace($"for (int i = 0; i < {ssb.FullObject}.Count; i++)"))
                    {
                        // ssb.AppendLine("#nullable disable", false);
                        using (var element = ssb.ScopeObject("[i]", false))
                        {
                            ssb.AppendLine("writer.CancellationToken.ThrowIfCancellationRequested();");
                            if (this.elementCoder == null)
                            {// use option.Resolver.GetFormatter<T>()
                                ssb.AppendLine($"formatter.Serialize(ref writer, {ssb.FullObject}, options);");
                            }
                            else
                            {// use coder
                                this.elementCoder.CodeSerializer(ssb, info);
                            }
                        }

                        // ssb.AppendLine("#nullable restore");
                    }
                }
            }
        }

        private void GenerateDeserializer(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            using (var m = ssb.ScopeBrace($"internal static System.Collections.Generic.List<{this.element.FullNameWithNullable}>? DeserializeList_{this.block!.SerialNumber:0000}(ref TinyhandReader reader, TinyhandSerializerOptions options)"))
            {
                if (this.elementCoder == null)
                {// use option.Resolver.GetFormatter<T>()
                    ssb.AppendLine($"var formatter = options.Resolver.GetFormatter<{this.element.FullName}>();");
                }

                ssb.AppendLine("var len = reader.ReadArrayHeader();");
                ssb.AppendLine($"var list = new System.Collections.Generic.List<{this.element.FullNameWithNullable}>(len);");
                ssb.AppendLine("options.Security.DepthStep(ref reader);");
                using (var v = ssb.ScopeObject("list"))
                using (var scopeSecurityTry = ssb.ScopeBrace("try"))
                {
                    using (var c2 = ssb.ScopeBrace("for (int i = 0; i < len; i++)"))
                    {
                        using (var element = ssb.ScopeObject("[i]", false))
                        {
                            ssb.AppendLine("reader.CancellationToken.ThrowIfCancellationRequested();");
                            if (this.elementCoder == null)
                            {// use option.Resolver.GetFormatter<T>()
                                ssb.AppendLine($"list.Add(formatter.Deserialize(ref reader, options)!);");
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

                ssb.AppendLine($"return list;");
            }
        }

        private GeneratorBlock? block;
        private WithNullable<TinyhandObject> element;
        private ITinyhandCoder? elementCoder;
        private NullableAnnotation nullableAnnotation;
    }
}
