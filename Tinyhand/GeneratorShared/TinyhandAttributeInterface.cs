// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using Tinyhand.IO;

namespace Tinyhand
{
    /// <summary>
    /// Enables serialization/deserialization by TinyhandSerializer. The class or struct must be a partial type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
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

        public TinyhandObjectAttribute()
        {
        }
    }

    /// <summary>
    /// Adds the member to the serialization target and specify the Key (integer or string).
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class KeyAttribute : Attribute
    {
        public int? IntKey { get; private set; }

        public string? StringKey { get; private set; }

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
    public interface ITinyhandClone
    {
        void DeepClone(TinyhandSerializerOptions options);
    }

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
}
