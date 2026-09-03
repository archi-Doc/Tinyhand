// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Tinyhand.IO;

namespace Tinyhand;

public delegate void ByRefAction<T1, T2>(in T1 arg1, T2 arg2); // For struct setter.

public delegate void ByRefFunc<T1, T2>(in T1 arg1, T2 arg2); // For struct getter.

/// <summary>
/// Specifies the accessibility of the generated property.
/// </summary>
public enum PropertyAccessibility
{
    /// <summary>
    /// The generated property has both public getter and setter [default].
    /// </summary>
    PublicSetter,

    /// <summary>
    /// The generated property has a public getter and a protected setter.
    /// </summary>
    ProtectedSetter,

    /// <summary>
    /// The generated property has a getter, but does not have a setter.
    /// </summary>
    GetterOnly,
}

/// <summary>
/// Marks a callback to run before serialization.
/// </summary>
/// <remarks>
/// Callbacks are not inherited by derived classes. Serialization and deserialization callbacks run under the configured object lock.
/// </remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class TinyhandOnSerializingAttribute : Attribute;

/// <summary>
/// Marks a callback to run after serialization.
/// </summary>
/// <remarks>
/// Callbacks are not inherited by derived classes. Serialization and deserialization callbacks run under the configured object lock.
/// </remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class TinyhandOnSerializedAttribute : Attribute;

/// <summary>
/// Marks a callback to run before deserialization.
/// </summary>
/// <remarks>
/// Callbacks are not inherited by derived classes. Serialization and deserialization callbacks run under the configured object lock.
/// </remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class TinyhandOnDeserializingAttribute : Attribute;

/// <summary>
/// Marks a callback to run after deserialization.
/// </summary>
/// <remarks>
/// Callbacks are not inherited by derived classes. Serialization and deserialization callbacks run under the configured object lock.
/// </remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class TinyhandOnDeserializedAttribute : Attribute;

/* AbandonReconstructCode /// <summary>
/// Attribute to specify a method to be called before reconstruction.<br/>
/// Callbacks are not inherited by derived classes.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class TinyhandOnReconstructingAttribute : Attribute;*/

/// <summary>
/// Marks a callback to run after reconstruction.
/// </summary>
/// <remarks>
/// Callbacks are not inherited by derived classes.
/// </remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class TinyhandOnReconstructedAttribute : Attribute;

/// <summary>
/// Enables generated Tinyhand serialization for a partial class, struct, record, or union interface.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, AllowMultiple = false, Inherited = true)]
public sealed class TinyhandObjectAttribute : Attribute
{
    /// <summary>
    /// Gets or sets a value indicating whether private and protected members are serialization targets. The default is false.
    /// </summary>
    public bool IncludePrivateMembers { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether eligible members use their names as string keys. The default is false.
    /// </summary>
    public bool ImplicitMemberNameAsKey { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether only explicitly keyed members are serialized. The default is false.
    /// </summary>
    public bool ExplicitKeysOnly { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether members are automatically selected for reconstruction. The default is true.
    /// </summary>
    public bool ReconstructMembers { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether deserialization reuses existing Tinyhand member instances. The default is true.
    /// </summary>
    public bool ReuseMembers { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether generated serialization omits recognized default values. The default is true.
    /// </summary>
    public bool SkipDefaultValues { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether instances are obtained from <see cref="TinyhandSerializer.ServiceProvider"/>. The default is false.
    /// </summary>
    public bool UseServiceProvider { get; set; } = false;

    /// <summary>
    /// Gets or sets the number of integer keys reserved by this type, starting at zero, for use by its own members.
    /// </summary>
    public int ReservedKeyCount { get; set; } = 0;

    /// <summary>
    /// Gets or sets the name of the member used to lock serialization and deserialization. An empty string disables locking.
    /// </summary>
    public string LockObject { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether enum members are serialized by name. The default is false.
    /// </summary>
    public bool EnumAsString { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether generated callers use a formatter resolver for this type instead of its static methods. The default is false.
    /// </summary>
    public bool UseResolver { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to generate object-tree and journaling support. The default is false.
    /// </summary>
    public bool Structural { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether serialization code for this type is supplied externally. The default is false.
    /// </summary>
    public bool External { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether signature serialization includes the type signature identifier. The default is true.
    /// </summary>
    public bool AddSignatureId { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to generate a read-only wrapper, ToImmutable(), and CloneAndToImmutable(). The default is false.
    /// </summary>
    public bool AddImmutable { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether integer-key objects also support alternate string keys for text serialization. The default is false.
    /// </summary>
    public bool AddAlternateKey { get; set; } = false;

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
/// Includes a field or property in serialization with an integer or string key.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public class KeyAttribute : Attribute
{
    private const int DefaultLevel = int.MinValue;

    /// <summary>
    /// Gets the unique integer key used for serialization.
    /// </summary>
    public int? IntKey { get; private set; }

    /// <summary>
    /// Gets the unique string key used for serialization.
    /// </summary>
    public string? StringKey { get; private set; }

    /// <summary>
    /// Gets or sets the minimum writer level for including this member in signature mode. The default includes it at all levels.
    /// </summary>
    public int Level { get; set; } = DefaultLevel;

    /// <summary>
    /// Gets or sets a value indicating whether this member is omitted in exclude mode. The default is false.
    /// </summary>
    public bool Exclude { get; set; } = false;

    /// <summary>
    /// Gets or sets the name of the property generated for this field. An empty string disables property generation.
    /// </summary>
    public string AddProperty { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the alternate string key when AddAlternateKey is enabled. An empty string uses the member name.
    /// </summary>
    public string Alternate { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the accessor visibility of the property generated for this field.
    /// </summary>
    public PropertyAccessibility PropertyAccessibility { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to suppress reserved-key diagnostics for this member. The default is false.
    /// </summary>
    public bool IgnoreKeyReservation { get; set; } = false;

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
/// Includes a field or property in serialization using its name as a string key.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public class MemberNameAsKeyAttribute : Attribute
{
    public MemberNameAsKeyAttribute()
    {
    }
}

/// <summary>
/// Excludes a field or property from generated serialization.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public class IgnoreMemberAttribute : Attribute
{
}

/// <summary>
/// Controls whether a member is selected for generated reconstruction.
/// </summary>
/// <remarks>
/// Class reconstruction relies on constructors and initializers. Deserialization can still initialize missing non-nullable map members.
/// </remarks>
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
/// Controls whether deserialization reuses an existing member instance of a Tinyhand object type.
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
/// Limits string, array, or list length during deserialization and in generated property setters.
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

/// <summary>
/// Specifies the options for the Tinyhand generator.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
public sealed class TinyhandGeneratorOptionAttribute : Attribute
{
    /// <summary>
    /// Gets or sets a value indicating whether to attach a debugger during code generation.
    /// </summary>
    public bool AttachDebugger { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to generate the code to a file.
    /// </summary>
    public bool GenerateToFile { get; set; } = false;

    /// <summary>
    /// Gets or sets the custom namespace for the generated code.
    /// </summary>
    public string? CustomNamespace { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TinyhandGeneratorOptionAttribute"/> class.
    /// </summary>
    public TinyhandGeneratorOptionAttribute()
    {
    }
}

/// <summary>
/// Marks a type as serializable using a single Tinyhand layout, either map or array.
/// </summary>
public interface ITinyhandSingleLayoutSerializable;

/// <summary>
/// Defines serialization and deserialization methods for a Tinyhand object.
/// </summary>
public interface ITinyhandSerializable
{
    /// <summary>
    /// Serializes the object to the specified writer.
    /// </summary>
    /// <param name="writer">The writer to serialize the object to.</param>
    /// <param name="options">The serialization options to use.</param>
    void Serialize(ref TinyhandWriter writer, TinyhandSerializerOptions options);

    /// <summary>
    /// Deserializes the object from the specified reader.
    /// </summary>
    /// <param name="reader">The reader to deserialize the object from.</param>
    /// <param name="options">The deserialization options to use.</param>
    void Deserialize(ref TinyhandReader reader, TinyhandSerializerOptions options);

    /*/// <summary>
    /// Gets the type identifier (FarmHash.Hash64(Type.FullName)) for the object.
    /// </summary>
    /// <returns>The type identifier.</returns>
    ulong GetTypeIdentifier(); // GetTypeIdentifierCode */
}

/// <summary>
/// Defines static serialization methods that generated code can provide or a type can implement.
/// </summary>
/// <typeparam name="T">The type to be serialized.</typeparam>
public interface ITinyhandSerializable<T>
{
    /// <summary>
    /// Serializes the object to the specified writer.
    /// </summary>
    /// <param name="writer">The writer to serialize the object to.</param>
    /// <param name="value">The value to be serialized.</param>
    /// <param name="options">The serialization options to use.</param>
    static abstract void Serialize(ref TinyhandWriter writer, scoped ref T? value, TinyhandSerializerOptions options);

    /// <summary>
    /// Deserializes the object from the specified reader.
    /// </summary>
    /// <param name="reader">The reader to deserialize the object from.</param>
    /// <param name="value">The value to be deserialized.</param>
    /// <param name="options">The deserialization options to use.</param>
    static abstract void Deserialize(ref TinyhandReader reader, scoped ref T? value, TinyhandSerializerOptions options);

    /* /// <summary>
    /// Gets the type identifier (FarmHash.Hash64(Type.FullName)) for the object.
    /// </summary>
    /// <returns>The type identifier.</returns>
    static abstract ulong GetTypeIdentifier(); // GetTypeIdentifierCode */
}

/// <summary>
/// Defines a static reconstruction method that generated code can provide or a type can implement.
/// </summary>
/// <typeparam name="T">The type to be reconstructed.</typeparam>
public interface ITinyhandReconstructable<T>
{
    static abstract void Reconstruct([NotNull] scoped ref T? value, TinyhandSerializerOptions options);
}

/// <summary>
/// Defines a static cloning method that generated code can provide or a type can implement.
/// </summary>
/// <typeparam name="T">The type to be cloned.</typeparam>
public interface ITinyhandCloneable<T>
{
    static abstract T? Clone(scoped ref T? value, TinyhandSerializerOptions options);
}

/// <summary>
/// Allows a type to indicate that its current value can be omitted from serialization.
/// </summary>
public interface ITinyhandDefault
{
    /// <summary>
    /// Determines if serialization of this object can be omitted.
    /// </summary>
    /// <returns><see langword="true"/> if serialization can omit this value; otherwise, <see langword="false"/>.</returns>
    bool CanSkipSerialization();
}

/// <summary>
/// Registers a derived type under an integer or string key on an abstract base class or interface.
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
/// Generates initialized members and nested classes from a Tinyhand text file.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = true)]
public sealed class TinyhandGenerateMemberAttribute : Attribute
{
    public TinyhandGenerateMemberAttribute(string tinyhandPath)
    {
    }
}

/// <summary>
/// Generates identifier hash constants and nested classes from a Tinyhand text file.
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
