// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using Tinyhand.IO;

namespace Tinyhand
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
    public sealed class TinyhandObjectAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets a value indicating whether or not to include private members as serialization targets [Default value is false].
        /// </summary>
        public bool IncludePrivateMembers { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether or not to use property names as string keys. String key and Int key are exclusive [Default value is false].
        /// </summary>
        public bool KeyAsPropertyName { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether or not to create an instance of a member variable even if there is no matching data (default constructor required) [Default value is true].
        /// </summary>
        public bool ReconstructMember { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether or not to skip a serialization if the value is the same as the default value [Default value is false].
        /// </summary>
        public bool SkipSerializingDefaultValue { get; set; } = false;

        public TinyhandObjectAttribute()
        {
        }
    }

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

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class IgnoreMemberAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class ReconstructAttribute : Attribute
    {
        public bool Reconstruct { get; set; }

        public ReconstructAttribute(bool reconstruct)
        {
            this.Reconstruct = reconstruct;
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
    /// Interface for custom serialize/deserialize/reconstruct methods.
    /// Tinyhand call those methods instead of the generated code.
    /// </summary>
    public interface ITinyhandCustom
    {
        void Serialize(ref TinyhandWriter writer, TinyhandSerializerOptions options);

        void Deserialize(ref TinyhandReader reader, TinyhandSerializerOptions options);

        void Reconstruct(TinyhandSerializerOptions options);
    }
}
