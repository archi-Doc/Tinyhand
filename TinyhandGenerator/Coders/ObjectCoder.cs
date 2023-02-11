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

    public ITinyhandCoder AddFormatter(string fullName, bool nonNullableReference = false)
    {
        if (!this.stringToCoder.TryGetValue(fullName, out var coder))
        {
            coder = new ObjectCoder(fullName, nonNullableReference);
            this.stringToCoder[fullName] = coder;
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
            var c2 = this.AddFormatter(fullName); // T?

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

internal class ObjectCoder : ITinyhandCoder
{
    public ObjectCoder(string fullName, bool nonNullableReference)
    {
        this.FullName = fullName;
        this.NonNullableReference = nonNullableReference;
    }

    public string FullName { get; }

    public bool NonNullableReference { get; }

    public void CodeSerializer(ScopingStringBuilder ssb, GeneratorInformation info)
    {
        // ssb.AppendLine($"options.Resolver.GetFormatter<{this.FullNameWithNullable}>().Serialize(ref writer, {ssb.FullObject}, options);");
        ssb.AppendLine($"TinyhandSerializer.SerializeObject(ref writer, {ssb.FullObject}, options);");
    }

    public void CodeDeserializer(ScopingStringBuilder ssb, GeneratorInformation info, bool nilChecked)
    {
        if (!this.NonNullableReference)
        {// Value type or Nullable reference type
            ssb.AppendLine($"{ssb.FullObject} = TinyhandSerializer.DeserializeObject<{this.FullName}>(ref reader, options);");
            // ssb.AppendLine($"{ssb.FullObject} = options.Resolver.GetFormatter<{this.FullNameWithNullable}>().Deserialize(ref reader, options);");
        }
        else
        {// Non-nullable reference type
            ssb.AppendLine($"{ssb.FullObject} = TinyhandSerializer.DeserializeAndReconstructObject<{this.FullName}>(ref reader, options);");
            // ssb.AppendLine($"{ssb.FullObject} = options.DeserializeAndReconstruct<{this.FullNameWithNullable}>(ref reader);");
        }
    }

    public void CodeReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
    {
        ssb.AppendLine($"{ssb.FullObject} = TinyhandSerializer.ReconstructObject<{this.FullName}>(options);");
        // ssb.AppendLine($"{ssb.FullObject} = options.Resolver.GetFormatter<{this.FullNameWithNullable}>().Reconstruct(options);");
    }

    public void CodeClone(ScopingStringBuilder ssb, GeneratorInformation info, string sourceObject)
    {
        ssb.AppendLine($"{ssb.FullObject} = TinyhandSerializer.CloneObject<{this.FullName}>({sourceObject}, options);");
        // ssb.AppendLine($"{ssb.FullObject} = options.Resolver.GetFormatter<{this.FullNameWithNullable}>().Clone({sourceObject}, options)!;");
    }
}
