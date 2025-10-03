// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Generic;
using Arc.Visceral;
using Tinyhand.Generator;

namespace Tinyhand.Coders;

public sealed class ObjectResolver : ICoderResolver
{
    /// <summary>
    /// The singleton instance that can be used.
    /// </summary>
    public static readonly ObjectResolver Instance = new();

    public ObjectResolver()
    {
    }

    public bool IsCoderOrFormatterAvailable(WithNullable<TinyhandObject> withNullable)
    {
        if (this.stringToCoder.ContainsKey(withNullable.FullNameWithNullable))
        {// Found
            return true;
        }

        return false;
    }

    public ITinyhandCoder? TryGetCoder(WithNullable<TinyhandObject> withNullable)
    {
        this.stringToCoder.TryGetValue(withNullable.FullNameWithNullable, out var value);
        return value;
    }

    public ITinyhandCoder AddFormatter(string fullNameWithNullable, bool nonNullableReference, bool isStringConvertible)
    {
        if (!this.stringToCoder.TryGetValue(fullNameWithNullable, out var coder))
        {
            coder = new ObjectCoder(fullNameWithNullable, nonNullableReference, isStringConvertible);
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

        var isStringConvertible = withNullable.Object.ObjectFlag.HasFlag(TinyhandObjectFlag.HasIStringConvertible);
        if (withNullable.Object.Kind.IsReferenceType())
        {// Reference type
            var fullName = withNullable.FullNameWithNullable.TrimEnd('?');
            var c = this.AddFormatter(fullName, true, isStringConvertible); // T (non-nullable)
            var c2 = this.AddFormatter(fullName + "?", false, isStringConvertible); // T?

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
            return this.AddFormatter(withNullable.FullNameWithNullable, false, isStringConvertible); // T
        }
    }

    private Dictionary<string, ITinyhandCoder> stringToCoder = new();
}

internal class ObjectCoder : ITinyhandCoder
{
    public ObjectCoder(string fullNameWithNullable, bool nonNullableReference, bool isStringConvertible)
    {
        this.FullNameWithNullable = fullNameWithNullable;
        this.FullName = fullNameWithNullable.TrimEnd('?');
        this.NonNullableReference = nonNullableReference;
        this.IsStringConvertible = isStringConvertible;
    }

    public bool RequiresRefValue => true;

    public string FullName { get; }

    public string FullNameWithNullable { get; }

    public bool NonNullableReference { get; }

    public bool IsStringConvertible { get; }

    public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
    {
        if (this.IsStringConvertible)
        {
            ssb.AppendLine($"if (options.HasConvertToStringFlag) writer.WriteStringConvertible({ssb.FullObject});");
            ssb.AppendLine($"else TinyhandSerializer.SerializeObject(ref writer, {ssb.FullObject}, options);");
        }
        else
        {
            ssb.AppendLine($"TinyhandSerializer.SerializeObject(ref writer, {ssb.FullObject}, options);");
        }
    }

    public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
    {
        if (this.IsStringConvertible)
        {
            if (!this.NonNullableReference)
            {// Value type or Nullable reference type
                ssb.AppendLine($"TinyhandSerializer.ReadStringConvertibleOrDeserializeObject(ref reader, ref {ssb.FullObject}, options);");
            }
            else
            {// Non-nullable reference type
                ssb.AppendLine($"TinyhandSerializer.ReadStringConvertibleOrDeserializeObject2(ref reader, ref {ssb.FullObject}, options);");
            }
        }
        else
        {
            if (!this.NonNullableReference)
            {// Value type or Nullable reference type
                ssb.AppendLine($"TinyhandSerializer.DeserializeObject<{this.FullName}>(ref reader, ref {ssb.FullObject}, options);");
            }
            else
            {// Non-nullable reference type
                ssb.AppendLine($"TinyhandSerializer.DeserializeAndReconstructObject<{this.FullName}>(ref reader, ref {ssb.FullObject}, options);");
            }
        }
    }

    public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
    {
        ssb.AppendLine($"{ssb.FullObject} ??= TinyhandSerializer.ReconstructObject<{this.FullName}>(options);");
    }

    public void CodeClone(ScopingStringBuilder ssb, GeneratorInformation info, string sourceObject)
    {
        ssb.AppendLine($"{ssb.FullObject} = TinyhandSerializer.CloneObject<{this.FullName}>({sourceObject}, options)!;");
        // ssb.AppendLine($"{ssb.FullObject} = options.Resolver.GetFormatter<{this.FullNameWithNullable}>().Clone({sourceObject}, options)!;");
    }
}
