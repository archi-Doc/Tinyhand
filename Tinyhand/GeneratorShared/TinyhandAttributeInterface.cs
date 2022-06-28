// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using Tinyhand.IO;

namespace Tinyhand;

public delegate void ByRefAction<T1, T2>(in T1 arg1, T2 arg2); // For struct setter.

/// <summary>
/// Enables serialization/deserialization by TinyhandSerializer. The class or struct must be a partial type.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, AllowMultiple = false, Inherited = true)]
public sealed class TinyhandObjectAttribute : Attribute
{
    /// <summary>
    /// Gets or sets a value indicating whether or not to include private/protected members as serialization targets [the default is false].
    /// </summary>
    public bool IncludePrivateMembers { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether or not to use member names as string keys. String key and Int key are exclusive [the default is false].
    /// </summary>
    public bool ImplicitKeyAsName { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether the serialization target should be limited to members with the Key attribute [the default is false].
    /// </summary>
    public bool ExplicitKeyOnly { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether or not to create an instance of a member variable even if there is no matching data (default constructor required) [the default is true].
    /// </summary>
    public bool ReconstructMember { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether or not to reuse an instance of a member variable when deserializing/reconstructing [the default is true].
    /// </summary>
    public bool ReuseMember { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether or not to skip a serialization if the value is the same as the the default value [the default is false].
    /// </summary>
    public bool SkipSerializingDefaultValue { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether or not to use <seealso cref="IServiceProvider"/> to create an instance [the default is false]. Set <see cref="TinyhandSerializer.ServiceProvider"/>.
    /// </summary>
    public bool UseServiceProvider { get; set; } = false;

    /// <summary>
    /// Gets or sets the number of reserved keys for the future use.<br/>
    /// Derived classes cannot use reserved keys (from 0 to ReservedKeys).
    /// </summary>
    public int ReservedKeys { get; set; } = -1;

    public TinyhandObjectAttribute()
    {
    }
}

/*/// <summary>
/// Reserves keys (from 0 to numberOfKeys) for the future use.<br/>
/// Derives classes cannot use reserved keys.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, AllowMultiple = false, Inherited = true)]
public sealed class ReserveKeyAttribute : Attribute
{
    public ReserveKeyAttribute(int numberOfKeys)
    {
        this.NumberOfKeys = numberOfKeys;
    }

    public int NumberOfKeys { get; private set; }
}*/

/// <summary>
/// Adds the member to the serialization target and specify the Key (integer or string).
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public class KeyAttribute : Attribute
{
    /// <summary>
    /// Gets the unique integer key used for serialization.
    /// </summary>
    public int? IntKey { get; private set; }

    /// <summary>
    /// Gets the unique string key used for serialization.
    /// </summary>
    public string? StringKey { get; private set; }

    /// <summary>
    /// Gets or sets a value indicating whether or not to put a marker to get the location.
    /// </summary>
    public bool Marker { get; set; }

    public KeyAttribute(int x)
    {
        this.IntKey = x;
    }

    public KeyAttribute(string x)
    {
        this.StringKey = x;
    }
}

/// <summary>
/// Adds the member to the serialization target and specify the Key as the member's name.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public class KeyAsNameAttribute : Attribute
{
    public KeyAsNameAttribute()
    {
    }
}

/// <summary>
/// Removes the member from the serialization target.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public class IgnoreMemberAttribute : Attribute
{
}

/// <summary>
/// Adds the member to the reconstruct target if reconstruct is true.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class ReconstructAttribute : Attribute
{
    public bool Reconstruct { get; set; }

    public ReconstructAttribute(bool reconstruct = true)
    {
        this.Reconstruct = reconstruct;
    }
}

/// <summary>
/// Reuse the member instance when deserializing. The member must have TinyhandObject attribute.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class ReuseAttribute : Attribute
{
    public bool ReuseInstance { get; set; }

    public ReuseAttribute(bool reuseInstance)
    {
        this.ReuseInstance = reuseInstance;
    }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
public sealed class TinyhandGeneratorOptionAttribute : Attribute
{
    public bool AttachDebugger { get; set; } = false;

    public bool GenerateToFile { get; set; } = false;

    public string? CustomNamespace { get; set; }

    public TinyhandGeneratorOptionAttribute()
    {
    }
}

public interface ITinyhandSerializationCallback
{
    public void OnBeforeSerialize();

    public void OnAfterDeserialize();
}

/// <summary>
/// Interface for custom serialize/deserialize methods.
/// If this interface is implemented, Tinyhand use it instead of the generated code.
/// </summary>
public interface ITinyhandSerialize
{
    void Serialize(ref TinyhandWriter writer, TinyhandSerializerOptions options);

    void Deserialize(ref TinyhandReader reader, TinyhandSerializerOptions options);
}

/// <summary>
/// Interface for custom reconstruct methods.
/// If this interface is implemented, Tinyhand use it instead of the generated code.
/// </summary>
public interface ITinyhandReconstruct
{
    void Reconstruct(TinyhandSerializerOptions options);
}

/// <summary>
/// Interface for custom clone methods.
/// If this interface is implemented, Tinyhand use it instead of the generated code.
/// </summary>
/// <typeparam name="T">The type to be cloned.</typeparam>
public interface ITinyhandClone<T>
{
    T DeepClone(TinyhandSerializerOptions options);
}

/// <summary>
/// You can serialize/deserialize derived types via the base type by adding TinyhandUnionAttribute to the base type.<br/>
/// The base type must be an abstract class or interface.<br/>
/// Specify Key (an identifier of the subtype) and SubType (the derived or implementing type).
/// </summary>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public class TinyhandUnionAttribute : Attribute
{
    /// <summary>
    /// Gets the distinguishing value that identifies a particular subtype.
    /// </summary>
    public int Key { get; private set; }

    /// <summary>
    /// Gets the derived or implementing type.
    /// </summary>
    public Type SubType { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TinyhandUnionAttribute"/> class.
    /// </summary>
    /// <param name="key">The distinguishing value that identifies a particular subtype.</param>
    /// <param name="subType">The derived or implementing type.</param>
    public TinyhandUnionAttribute(int key, Type subType)
    {
        this.Key = key;
        this.SubType = subType;
    }
}

/// <summary>
/// Adds members or child classes generated from tinyhand file to the class/struct.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = true)]
public sealed class TinyhandGenerateFromAttribute : Attribute
{
    public TinyhandGenerateFromAttribute(string tinyhandPath, bool hashedString)
    {
    }
}

/*/// <summary>
/// TinyhandUnionToAttribute is derived-side version of TinyhandUnionAttribute.
/// You can serialize/deserialize derived types via the base type by adding TinyhandUnionAttribute to the derived type.<br/>
/// The base type must be an abstract class or interface.<br/>
/// Specify Key (an identifier of the subtype) and BaseType and SubType.
/// </summary>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public class TinyhandUnionToAttribute : Attribute
{
    /// <summary>
    /// Gets the distinguishing value that identifies a particular subtype.
    /// </summary>
    public int Key { get; private set; }

    /// <summary>
    /// Gets the base type.
    /// </summary>
    public Type BaseType { get; private set; }

    /// <summary>
    /// Gets the derived or implementing type.
    /// </summary>
    public Type SubType { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TinyhandUnionToAttribute"/> class.
    /// </summary>
    /// <param name="key">The distinguishing value that identifies a particular subtype.</param>
    /// <param name="baseType">The base type.</param>
    /// <param name="subType">The derived or implementing type.</param>
    public TinyhandUnionToAttribute(int key, Type baseType, Type subType)
    {
        this.Key = key;
        this.BaseType = baseType;
        this.SubType = subType;
    }
}*/
