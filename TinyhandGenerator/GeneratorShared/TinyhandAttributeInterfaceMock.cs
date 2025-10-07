// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Arc.Visceral;
using Microsoft.CodeAnalysis;

#pragma warning disable SA1602

namespace Tinyhand.Generator;

public enum PropertyAccessibility
{
    PublicSetter,
    ProtectedSetter,
    GetterOnly,
}

public enum LockObjectType
{
    // No lock object.
    NoLock,

    /// <summary>
    /// Object.
    /// </summary>
    Object,

    /// <summary>
    /// System.Threading.Lock.
    /// </summary>
    Lock,

    /// <summary>
    /// Arc.Threading.SemaphoreLock.
    /// </summary>
    SemaphoreLock,
}

public sealed class TinyhandOnSerializingAttributeMock
{
    public static readonly string SimpleName = "TinyhandOnSerializing";
    public static readonly string Name = SimpleName + "Attribute";
    public static readonly string FullName = "Tinyhand." + Name;
}

public sealed class TinyhandOnSerializedAttributeMock
{
    public static readonly string SimpleName = "TinyhandOnSerialized";
    public static readonly string Name = SimpleName + "Attribute";
    public static readonly string FullName = "Tinyhand." + Name;
}

public sealed class TinyhandOnDeserializingAttributeMock
{
    public static readonly string SimpleName = "TinyhandOnDeserializing";
    public static readonly string Name = SimpleName + "Attribute";
    public static readonly string FullName = "Tinyhand." + Name;
}

public sealed class TinyhandOnDeserializedAttributeMock
{
    public static readonly string SimpleName = "TinyhandOnDeserialized";
    public static readonly string Name = SimpleName + "Attribute";
    public static readonly string FullName = "Tinyhand." + Name;
}

public sealed class TinyhandOnReconstructedAttributeMock
{
    public static readonly string SimpleName = "TinyhandOnReconstructed";
    public static readonly string Name = SimpleName + "Attribute";
    public static readonly string FullName = "Tinyhand." + Name;
}

public sealed class TinyhandObjectAttributeMock
{
    public static readonly string SimpleName = "TinyhandObject";
    public static readonly string Name = SimpleName + "Attribute";
    public static readonly string FullName = "Tinyhand." + Name;

    public static TinyhandObjectAttributeMock ExternalObject { get; }

    static TinyhandObjectAttributeMock()
    {
        ExternalObject = new();
        ExternalObject.External = true;
    }

    public bool IncludePrivateMembers { get; set; } = false;

    public bool ImplicitMemberNameAsKey { get; set; } = false;

    public bool ExplicitKeysOnly { get; set; } = false;

    public bool ReconstructMembers { get; set; } = true;

    public bool ReuseMembers { get; set; } = true;

    public bool SkipDefaultValues { get; set; } = true;

    public bool UseServiceProvider { get; set; } = false;

    public int ReservedKeyCount { get; set; } = 0;

    public string LockObject { get; set; } = string.Empty;

    public bool EnumAsString { get; set; } = false;

    public bool UseResolver { get; set; } = false;

    public bool Structural { get; set; } = false;

    public bool External { get; set; } = false;

    public bool AddSignatureId { get; set; } = true;

    public bool AddImmutable { get; set; } = false;

    public bool AddAlternateKey { get; set; } = false;

    public TinyhandObjectAttributeMock()
    {
    }

    /// <summary>
    /// Create an attribute instance from constructor arguments and named arguments.
    /// </summary>
    /// <param name="constructorArguments">Constructor arguments.</param>
    /// <param name="namedArguments">Named arguments.</param>
    /// <returns>A new attribute instance.</returns>
    public static TinyhandObjectAttributeMock FromArray(object?[] constructorArguments, KeyValuePair<string, object?>[] namedArguments)
    {
        var attribute = new TinyhandObjectAttributeMock();

        object? val;
        val = VisceralHelper.GetValue(-1, nameof(ImplicitMemberNameAsKey), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.ImplicitMemberNameAsKey = (bool)val;
        }

        val = VisceralHelper.GetValue(-1, nameof(IncludePrivateMembers), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.IncludePrivateMembers = (bool)val;
        }

        val = VisceralHelper.GetValue(-1, nameof(ExplicitKeysOnly), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.ExplicitKeysOnly = (bool)val;
        }

        val = VisceralHelper.GetValue(-1, nameof(ReconstructMembers), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.ReconstructMembers = (bool)val;
        }

        val = VisceralHelper.GetValue(-1, nameof(ReuseMembers), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.ReuseMembers = (bool)val;
        }

        val = VisceralHelper.GetValue(-1, nameof(SkipDefaultValues), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.SkipDefaultValues = (bool)val;
        }

        val = VisceralHelper.GetValue(-1, nameof(UseServiceProvider), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.UseServiceProvider = (bool)val;
        }

        val = VisceralHelper.GetValue(-1, nameof(ReservedKeyCount), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.ReservedKeyCount = (int)val;
        }

        val = VisceralHelper.GetValue(-1, nameof(LockObject), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.LockObject = (string)val;
        }

        val = VisceralHelper.GetValue(-1, nameof(EnumAsString), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.EnumAsString = (bool)val;
        }

        val = VisceralHelper.GetValue(-1, nameof(UseResolver), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.UseResolver = (bool)val;
        }

        val = VisceralHelper.GetValue(-1, nameof(Structural), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.Structural = (bool)val;
        }

        val = VisceralHelper.GetValue(-1, nameof(External), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.External = (bool)val;
        }

        val = VisceralHelper.GetValue(-1, nameof(AddSignatureId), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.AddSignatureId = (bool)val;
        }

        val = VisceralHelper.GetValue(-1, nameof(AddImmutable), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.AddImmutable = (bool)val;
        }

        val = VisceralHelper.GetValue(-1, nameof(AddAlternateKey), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.AddAlternateKey = (bool)val;
        }

        return attribute;
    }

    public LockObjectType LockObjectType { get; set; }
}

public class KeyAttributeMock
{
    public const int DefaultLevel = int.MinValue;
    public static readonly string SimpleName = "Key";
    public static readonly string Name = SimpleName + "Attribute";
    public static readonly string FullName = "Tinyhand." + Name;

    public int? IntKey { get; private set; }

    public string? StringKey { get; private set; }

    public int Level { get; set; } = DefaultLevel;

    public bool Exclude { get; set; } = false;

    public string AddProperty { get; set; } = string.Empty;

    public PropertyAccessibility PropertyAccessibility { get; set; } = PropertyAccessibility.PublicSetter;

    public bool IgnoreKeyReservation { get; set; } = false;

    public string Alternate { get; set; } = string.Empty;

    // public bool ConvertToString { get; set; } = false;

    // public bool Utf8String { get; set; } = false;

    public KeyAttributeMock(int x)
    {
        this.IntKey = x;
    }

    public KeyAttributeMock(string x)
    {
        this.StringKey = x;
    }

    public static KeyAttributeMock FromArray(object?[] constructorArguments, KeyValuePair<string, object?>[] namedArguments)
    {
        var attribute = new KeyAttributeMock(null!);

        if (constructorArguments.Length > 0)
        {
            var val = constructorArguments[0];
            if (val is int intKey)
            {
                attribute.IntKey = intKey;
            }
            else if (val is string stringKey)
            {
                attribute.StringKey = stringKey;
            }
        }

        if (attribute.IntKey == null && attribute.StringKey == null)
        {// Exception: KeyAttribute requires a valid int key or string key.
            throw new ArgumentNullException();
        }

        var v = VisceralHelper.GetValue(-1, nameof(Level), constructorArguments, namedArguments);
        if (v != null)
        {
            attribute.Level = (int)v;
        }

        v = VisceralHelper.GetValue(-1, nameof(Exclude), constructorArguments, namedArguments);
        if (v != null)
        {
            attribute.Exclude = (bool)v;
        }

        v = VisceralHelper.GetValue(-1, nameof(AddProperty), constructorArguments, namedArguments);
        if (v != null)
        {
            attribute.AddProperty = (string)v;
        }

        v = VisceralHelper.GetValue(-1, nameof(PropertyAccessibility), constructorArguments, namedArguments);
        if (v != null)
        {
            attribute.PropertyAccessibility = (PropertyAccessibility)v;
        }

        v = VisceralHelper.GetValue(-1, nameof(IgnoreKeyReservation), constructorArguments, namedArguments);
        if (v != null)
        {
            attribute.IgnoreKeyReservation = (bool)v;
        }

        v = VisceralHelper.GetValue(-1, nameof(Alternate), constructorArguments, namedArguments);
        if (v != null)
        {
            attribute.Alternate = (string)v;
        }

        /*v = VisceralHelper.GetValue(-1, nameof(ConvertToString), constructorArguments, namedArguments);
        if (v != null)
        {
            attribute.ConvertToString = (bool)v;
        }*/

        /*v = VisceralHelper.GetValue(-1, nameof(Utf8String), constructorArguments, namedArguments);
        if (v != null)
        {
            attribute.Utf8String = (bool)v;
        }*/

        return attribute;
    }

    public void SetKey(string x)
    {
        this.IntKey = null;
        this.StringKey = x;
    }
}

public class MemberNameAsKeyAttributeMock
{
    public static readonly string SimpleName = "MemberNameAsKey";
    public static readonly string Name = SimpleName + "Attribute";
    public static readonly string FullName = "Tinyhand." + Name;

    // public bool ConvertToString { get; set; } = false;

    public static MemberNameAsKeyAttributeMock FromArray(object?[] constructorArguments, KeyValuePair<string, object?>[] namedArguments)
    {
        var attribute = new MemberNameAsKeyAttributeMock();

        /*var v = VisceralHelper.GetValue(-1, nameof(ConvertToString), constructorArguments, namedArguments);
        if (v != null)
        {
            attribute.ConvertToString = (bool)v;
        }*/

        return attribute;
    }
}

public class IgnoreMemberAttributeMock
{
    public static readonly string SimpleName = "IgnoreMember";
    public static readonly string Name = SimpleName + "Attribute";
    public static readonly string FullName = "Tinyhand." + Name;

    public static IgnoreMemberAttributeMock FromArray(object?[] constructorArguments, KeyValuePair<string, object?>[] namedArguments)
    {
        var attribute = new IgnoreMemberAttributeMock();

        return attribute;
    }
}

public class ReconstructAttributeMock
{
    public static readonly string SimpleName = "Reconstruct";
    public static readonly string Name = SimpleName + "Attribute";
    public static readonly string FullName = "Tinyhand." + Name;

    public bool Reconstruct { get; set; }

    public ReconstructAttributeMock(bool reconstruct)
    {
        this.Reconstruct = reconstruct;
    }

    public static ReconstructAttributeMock FromArray(object?[] constructorArguments, KeyValuePair<string, object?>[] namedArguments)
    {
        var attribute = new ReconstructAttributeMock(true);

        object? val;
        val = VisceralHelper.GetValue(0, nameof(Reconstruct), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.Reconstruct = (bool)val;
        }

        return attribute;
    }
}

public class ReuseAttributeMock
{
    public static readonly string SimpleName = "Reuse";
    public static readonly string Name = SimpleName + "Attribute";
    public static readonly string FullName = "Tinyhand." + Name;

    public bool ReuseInstance { get; set; }

    public ReuseAttributeMock(bool reuseInstance)
    {
        this.ReuseInstance = reuseInstance;
    }

    public static ReuseAttributeMock FromArray(object?[] constructorArguments, KeyValuePair<string, object?>[] namedArguments)
    {
        var attribute = new ReuseAttributeMock(false);

        object? val;
        val = VisceralHelper.GetValue(0, nameof(ReuseInstance), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.ReuseInstance = (bool)val;
        }

        return attribute;
    }
}

public class MaxLengthAttributeMock
{
    public static readonly string SimpleName = "MaxLength";
    public static readonly string Name = SimpleName + "Attribute";
    public static readonly string FullName = "Tinyhand." + Name;

    public int MaxLength { get; private set; } = -1;

    public int MaxChildLength { get; private set; } = -1;

    public MaxLengthAttributeMock()
    {
    }

    public static MaxLengthAttributeMock FromArray(object?[] constructorArguments, KeyValuePair<string, object?>[] namedArguments)
    {
        var attribute = new MaxLengthAttributeMock();

        object? val;
        val = VisceralHelper.GetValue(0, nameof(MaxLength), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.MaxLength = (int)val;
        }

        val = VisceralHelper.GetValue(1, nameof(MaxChildLength), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.MaxChildLength = (int)val;
        }

        return attribute;
    }
}

public sealed class TinyhandGeneratorOptionAttributeMock
{
    public static readonly string SimpleName = "TinyhandGeneratorOption";
    public static readonly string Name = SimpleName + "Attribute";
    public static readonly string FullName = "Tinyhand." + Name;

    public bool AttachDebugger { get; set; } = false;

    public bool GenerateToFile { get; set; } = false;

    public string? CustomNamespace { get; set; }

    public static TinyhandGeneratorOptionAttributeMock FromArray(object?[] constructorArguments, KeyValuePair<string, object?>[] namedArguments)
    {
        var attribute = new TinyhandGeneratorOptionAttributeMock();

        object? val;
        val = VisceralHelper.GetValue(-1, nameof(AttachDebugger), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.AttachDebugger = (bool)val;
        }

        val = VisceralHelper.GetValue(-1, nameof(GenerateToFile), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.GenerateToFile = (bool)val;
        }

        val = VisceralHelper.GetValue(-1, nameof(CustomNamespace), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.CustomNamespace = (string)val;
        }

        return attribute;
    }
}

public class TinyhandUnionAttributeMock
{
    public static readonly string SimpleName = "TinyhandUnion";
    public static readonly string Name = SimpleName + "Attribute";
    public static readonly string FullName = "Tinyhand." + Name;

    public Location Location { get; }

    public int IntKey { get; private set; }

    public string? StringKey { get; private set; }

    public bool HasStringKey => this.StringKey is not null;

    /// <summary>
    /// Gets the derived or implementing type.
    /// </summary>
    public ISymbol? SubType { get; private set; }

    public TinyhandUnionAttributeMock(Microsoft.CodeAnalysis.Location location)
    {
        this.Location = location;
    }

    public TinyhandUnionAttributeMock(int key, ISymbol subType, Microsoft.CodeAnalysis.Location location)
    {
        this.IntKey = key;
        this.SubType = subType;
        this.Location = location;
    }

    public static TinyhandUnionAttributeMock FromArray(object?[] constructorArguments, KeyValuePair<string, object?>[] namedArguments, Microsoft.CodeAnalysis.Location location)
    {
        var attribute = new TinyhandUnionAttributeMock(location);

        if (constructorArguments.Length > 0)
        {
            var val = constructorArguments[0];
            if (val is int intKey)
            {
                attribute.IntKey = intKey;
            }
            else if (val is string stringKey)
            {
                attribute.StringKey = stringKey;
            }
        }

        if (constructorArguments.Length > 1)
        {
            var val = constructorArguments[1];
            if (val is ISymbol subType)
            {
                attribute.SubType = subType;
            }
        }

        return attribute;
    }
}

/*public class TinyhandUnionToAttributeMock
{
    public static readonly string SimpleName = "TinyhandUnionTo";
    public static readonly string Name = SimpleName + "Attribute";
    public static readonly string FullName = "Tinyhand." + Name;

    public Location Location { get; }

    /// <summary>
    /// Gets the distinguishing value that identifies a particular subtype.
    /// </summary>
    public int Key { get; private set; }

    /// <summary>
    /// Gets the base type.
    /// </summary>
    public ISymbol? BaseType { get; private set; }

    /// <summary>
    /// Gets the derived or implementing type.
    /// </summary>
    public ISymbol? SubType { get; private set; }

    public TinyhandUnionToAttributeMock(Microsoft.CodeAnalysis.Location location)
    {
        this.Location = location;
    }

    public static TinyhandUnionToAttributeMock FromArray(object?[] constructorArguments, KeyValuePair<string, object?>[] namedArguments, Microsoft.CodeAnalysis.Location location)
    {
        var attribute = new TinyhandUnionToAttributeMock(location);

        if (constructorArguments.Length > 2)
        {
            var val = constructorArguments[0];
            if (val is int intKey)
            {
                attribute.Key = intKey;
            }

            val = constructorArguments[1];
            if (val is ISymbol baseType)
            {
                attribute.BaseType = baseType;
            }

            val = constructorArguments[2];
            if (val is ISymbol subType)
            {
                attribute.SubType = subType;
            }
        }

        return attribute;
    }
}*/

public sealed class TinyhandGenerateMemberAttributeMock
{
    public static readonly string SimpleName = "TinyhandGenerateMember";
    public static readonly string Name = SimpleName + "Attribute";
    public static readonly string FullName = "Tinyhand." + Name;

    public Location Location { get; set; } = Location.None;

    public string TinyhandPath { get; set; } = string.Empty;

    public static TinyhandGenerateMemberAttributeMock FromArray(object?[] constructorArguments, KeyValuePair<string, object?>[] namedArguments)
    {
        var attribute = new TinyhandGenerateMemberAttributeMock();

        object? val;
        val = VisceralHelper.GetValue(0, nameof(TinyhandPath), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.TinyhandPath = (string)val;
        }

        return attribute;
    }
}

public sealed class TinyhandGenerateHashAttributeMock
{
    public static readonly string SimpleName = "TinyhandGenerateHash";
    public static readonly string Name = SimpleName + "Attribute";
    public static readonly string FullName = "Tinyhand." + Name;

    public Location Location { get; set; } = Location.None;

    public string TinyhandPath { get; set; } = string.Empty;

    public static TinyhandGenerateHashAttributeMock FromArray(object?[] constructorArguments, KeyValuePair<string, object?>[] namedArguments)
    {
        var attribute = new TinyhandGenerateHashAttributeMock();

        object? val;
        val = VisceralHelper.GetValue(0, nameof(TinyhandPath), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.TinyhandPath = (string)val;
        }

        return attribute;
    }
}
