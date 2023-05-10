// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Tinyhand.IO;

namespace Tinyhand;

public delegate void ByRefAction<T1, T2>(in T1 arg1, T2 arg2); // For struct setter.

/// <summary>
/// Specifies the accessibility of the generated property.
/// </summary>
public enum PropertyAccessibility
{
    /// <summary>
    /// [Default] Generated properties have public getter and setter.
    /// </summary>
    PublicSetter,

    /// <summary>
    /// Generated properties have public getter and protected setter.
    /// </summary>
    ProtectedSetter,
}

/// <summary>
/// Enables serialization/deserialization by TinyhandSerializer. The class or struct must be a partial type.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, AllowMultiple = false, Inherited = true)]
public sealed class TinyhandObjectAttribute : Attribute
{
    /// <summary>
    /// Gets or sets a value indicating whether or not to include private/protected members as serialization targets [default is <see langword="false"/>].
    /// </summary>
    public bool IncludePrivateMembers { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether or not to use member names as string keys. String key and Int key are exclusive [default is <see langword="false"/>].
    /// </summary>
    public bool ImplicitKeyAsName { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether the serialization target should be limited to members with the Key attribute [default is <see langword="false"/>].
    /// </summary>
    public bool ExplicitKeyOnly { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether or not to create an instance of a member variable even if there is no matching data (default constructor required) [default is <see langword="true"/>].
    /// </summary>
    public bool ReconstructMember { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether or not to reuse an instance of a member variable when deserializing/reconstructing [default is <see langword="true"/>].
    /// </summary>
    public bool ReuseMember { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether or not to skip a serialization if the value is the same as the the default value [default is <see langword="true"/>].
    /// </summary>
    public bool SkipSerializingDefaultValue { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether or not to use <seealso cref="IServiceProvider"/> to create an instance [default is <see langword="false"/>]. Set <see cref="TinyhandSerializer.ServiceProvider"/>.
    /// </summary>
    public bool UseServiceProvider { get; set; } = false;

    /// <summary>
    /// Gets or sets the number of reserved keys for the future use.<br/>
    /// Derived classes cannot use reserved keys (from 0 to ReservedKeys).
    /// </summary>
    public int ReservedKeys { get; set; } = -1;

    /// <summary>
    /// Gets or sets the name of the member used for lock statememt during serialization and deserialization [default is <see cref="string.Empty"/>].
    /// </summary>
    public string LockObject { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether or not to serialize/deserialize objects of Enum type as strings [default is <see langword="false"/>].
    /// </summary>
    public bool EnumAsString { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether or not to use formatter resolvers instead of static abstract methods [default is <see langword="false"/>].
    /// </summary>
    public bool UseResolver { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether or not to add a journaling feature (<see cref="ITinyhandJournal"/>) to the object [default is <see langword="false"/>].
    /// </summary>
    public bool Journaling { get; set; } = false;

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
    /// Gets or sets a value indicating whether or not to serialize this member during conditional serialization [default is <see langword="true"/>].
    /// </summary>
    public bool Condition { get; set; } = true;

    /// <summary>
    /// Gets or sets a name of a property that will be created from the field.<br/>
    /// <b>Valid for fields only.</b>
    /// </summary>
    public string PropertyName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets an accessibility of a property that will be created from the field.<br/>
    /// </summary>
    public PropertyAccessibility PropertyAccessibility { get; set; }

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

/// <summary>
/// Sets the maximum length of the member (<see cref="string"/>, <see cref="Array"/>, <see cref="List{T}"/>).<br/>
/// Valid only when <b>deserializing</b>.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class MaxLengthAttribute : Attribute
{
    public int MaxLength { get; private set; } = -1;

    public int MaxChildLength { get; private set; } = -1;

    public MaxLengthAttribute(int maxLength, int maxChildLength = -1)
    {
        this.MaxLength = maxLength;
        this.MaxChildLength = maxChildLength;
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
/// Interface for serialize/deserialize methods.
/// </summary>
public interface ITinyhandSerialize
{
    void Serialize(ref TinyhandWriter writer, TinyhandSerializerOptions options);

    void Deserialize(ref TinyhandReader reader, TinyhandSerializerOptions options);
}

/// <summary>
/// Interface for serialize/deserialize methods.
/// If this interface is implemented, Tinyhand use it instead of the generated code.
/// </summary>
/// <typeparam name="T">The type to be serialized.</typeparam>
public interface ITinyhandSerialize<T>
{
    static abstract void Serialize(ref TinyhandWriter writer, scoped ref T? value, TinyhandSerializerOptions options);

    static abstract void Deserialize(ref TinyhandReader reader, scoped ref T? value, TinyhandSerializerOptions options);
}

/// <summary>
/// Interface for custom reconstruct methods.
/// If this interface is implemented, Tinyhand use it instead of the generated code.
/// </summary>
/// <typeparam name="T">The type to be reconstructed.</typeparam>
public interface ITinyhandReconstruct<T>
{
    static abstract void Reconstruct([NotNull] scoped ref T? value, TinyhandSerializerOptions options);
}

/// <summary>
/// Interface for custom clone methods.
/// If this interface is implemented, Tinyhand use it instead of the generated code.
/// </summary>
/// <typeparam name="T">The type to be cloned.</typeparam>
public interface ITinyhandClone<T>
{
    static abstract T? Clone(scoped ref T? value, TinyhandSerializerOptions options);
}

/// <summary>
/// Interface for handling default value.
/// </summary>
/// <typeparam name="TDefault">The type of the default value.</typeparam>
public interface ITinyhandDefault<TDefault>
{
    /// <summary>
    /// Sets the default value (<see cref="DefaultValueAttribute"/>).
    /// </summary>
    /// <param name="defaultValue">The default value.</param>
    void SetDefaultValue(TDefault defaultValue);

    /// <summary>
    /// Determines if serialization of this object can be omitted.
    /// </summary>
    /// <returns><see langword="true"/>; Skip serializing this object.</returns>
    bool CanSkipSerialization();
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
    /// Gets the distinguishing value(<see cref="int"/>) that identifies a particular subtype.
    /// </summary>
    public int IntKey { get; private set; }

    /// <summary>
    /// Gets the distinguishing value(<see cref="string"/>) that identifies a particular subtype.
    /// </summary>
    public string? StringKey { get; private set; } = null;

    /// <summary>
    /// Gets the derived or implementing type.
    /// </summary>
    public Type SubType { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TinyhandUnionAttribute"/> class.
    /// </summary>
    /// <param name="key">The distinguishing value(<see cref="int"/>) that identifies a particular subtype.</param>
    /// <param name="subType">The derived or implementing type.</param>
    public TinyhandUnionAttribute(int key, Type subType)
    {
        this.IntKey = key;
        this.SubType = subType;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TinyhandUnionAttribute"/> class.
    /// </summary>
    /// <param name="key">The distinguishing value(<see cref="string"/>) that identifies a particular subtype.</param>
    /// <param name="subType">The derived or implementing type.</param>
    public TinyhandUnionAttribute(string key, Type subType)
    {
        this.StringKey = key;
        this.SubType = subType;
    }
}

/// <summary>
/// Generates members or child classes from tinyhand file and set values to the members.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = true)]
public sealed class TinyhandGenerateMemberAttribute : Attribute
{
    public TinyhandGenerateMemberAttribute(string tinyhandPath)
    {
    }
}

/// <summary>
/// Generates members or child classes from tinyhand file, and sets the hash of the identifier to the value.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = true)]
public sealed class TinyhandGenerateHashAttribute : Attribute
{
    public TinyhandGenerateHashAttribute(string tinyhandPath)
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
