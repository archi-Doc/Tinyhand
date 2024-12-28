// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Visceral;
using Tinyhand.Generator;

namespace Tinyhand.Coders;

public sealed class NullableResolver : ICoderResolver
{
    /// <summary>
    /// The singleton instance that can be used.
    /// </summary>
    public static readonly NullableResolver Instance = new();

    public ITinyhandCoder? TryGetCoder(WithNullable<TinyhandObject> withNullable)
    {
        if (withNullable.Object == null)
        {
            return null;
        }

        if (withNullable.Object.Generics_Kind == VisceralGenericsKind.ClosedGeneric && withNullable.Object.OriginalDefinition is { } baseObject)
        {// Generics
            var arguments = withNullable.Generics_ArgumentsWithNullable;
            if (baseObject.FullName == "T?")
            {
                if (arguments.Length == 1)
                {
                    var elementCoder = CoderResolver.Instance.TryGetCoder(arguments[0]);
                    return new NullableCoder(arguments[0], elementCoder);
                }
            }
        }

        return null;
    }
}

public class NullableCoder : ITinyhandCoder
{
    public NullableCoder(WithNullable<TinyhandObject> element, ITinyhandCoder? elementCoder)
    {
        this.element = element;
        this.elementCoder = elementCoder;
    }

    public bool RequiresRefValue => true;

    public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
    {
        ssb.AppendLine($"if (!{ssb.FullObject}.HasValue) writer.WriteNil();");
        using (var scopeElse = ssb.ScopeBrace("else"))
        using (var v = ssb.ScopeObject("Value"))
        {
            if (this.elementCoder == null)
            {// use option.Resolver.GetFormatter<T>()
                ssb.AppendLine($"options.Resolver.GetFormatter<{this.element.FullNameWithNullable}>().Serialize(ref writer, {ssb.FullObject}, options);");
            }
            else
            {// use Coder
                this.elementCoder.CodeSerializer(ssb, info);
            }
        }
    }

    public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
    {
        if (nilChecked)
        {// Nil already checked.
            CodeDeserializerCore();
        }
        else
        {
            using (var b = ssb.ScopeBrace("if (reader.TryReadNil())"))
            {
                ssb.AppendLine($"{ssb.FullObject} = default;");
            }

            using (var b = ssb.ScopeBrace($"else"))
            {
                CodeDeserializerCore();
            }
        }

        void CodeDeserializerCore()
        {
            ssb.AppendLine($"options.Resolver.GetFormatter<{this.element.FullNameWithNullable}?>().Deserialize(ref reader, ref {ssb.FullObject}, options);");

            /*if (this.elementCoder == null)
            {// use option.Resolver.GetFormatter<T>()
                ssb.AppendLine($"{ssb.FullObject} = options.Resolver.GetFormatter<{this.element.FullNameWithNullable}>().Deserialize(ref reader, options)!;");
            }
            else
            {// use Coder
                this.elementCoder.CodeDeserializer(ssb, info);
            }*/
        }
    }

    public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
    {
        ssb.AppendLine($"{ssb.FullObject} = default;");
        // ssb.AppendLine($"{ssb.FullObject} = new System.Nullable<{this.element.FullName}>();");
    }

    public void CodeClone(ScopingStringBuilder ssb, GeneratorInformation info, string sourceObject)
    {
        ssb.AppendLine($"{ssb.FullObject} = options.Resolver.GetFormatter<{this.element.FullNameWithNullable}?>().Clone({sourceObject}, options)!;");
    }

    private WithNullable<TinyhandObject> element;
    private ITinyhandCoder? elementCoder;
}
