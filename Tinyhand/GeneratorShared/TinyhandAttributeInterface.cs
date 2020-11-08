// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Tinyhand
{
    public static class AttributeHelper
    {
        public static object? GetValue(int constructorIndex, string? name, object?[] constructorArguments, KeyValuePair<string, object?>[] namedArguments)
        {
            if (constructorIndex >= 0 && constructorIndex < constructorArguments.Length)
            {// Constructor Argument.
                return constructorArguments[constructorIndex];
            }
            else if (name != null && namedArguments.FirstOrDefault(x => x.Key == name) is { } pair)
            {// Named Argument.
                return pair.Value;
            }
            else
            {
                return null;
            }
        }
    }

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

        /*public TinyhandObjectAttribute(
            bool includePrivateMembers = true,
            bool keyAsPropertyName = false,
            bool reconstructMember = true,
            bool skipSerializingDefaultValue = false)
        {
            this.IncludePrivateMembers = includePrivateMembers;
            this.KeyAsPropertyName = keyAsPropertyName;
            this.ReconstructMember = reconstructMember;
            this.SkipSerializingDefaultValue = skipSerializingDefaultValue;
        }*/

        /// <summary>
        /// Create an attribute instance from constructor arguments and named arguments.
        /// </summary>
        /// <param name="constructorArguments">Constructor arguments.</param>
        /// <param name="namedArguments">Named arguments.</param>
        /// <returns>A new attribute instance.</returns>
        public static TinyhandObjectAttribute FromArray(object?[] constructorArguments, KeyValuePair<string, object?>[] namedArguments)
        {
            var attribute = new TinyhandObjectAttribute();

            object? val;
            val = AttributeHelper.GetValue(0, nameof(IncludePrivateMembers), constructorArguments, namedArguments);
            if (val != null)
            {
                attribute.IncludePrivateMembers = (bool)val;
            }

            val = AttributeHelper.GetValue(1, nameof(KeyAsPropertyName), constructorArguments, namedArguments);
            if (val != null)
            {
                attribute.KeyAsPropertyName = (bool)val;
            }

            val = AttributeHelper.GetValue(2, nameof(ReconstructMember), constructorArguments, namedArguments);
            if (val != null)
            {
                attribute.ReconstructMember = (bool)val;
            }

            val = AttributeHelper.GetValue(3, nameof(SkipSerializingDefaultValue), constructorArguments, namedArguments);
            if (val != null)
            {
                attribute.SkipSerializingDefaultValue = (bool)val;
            }

            return attribute;
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

        public static KeyAttribute FromArray(object?[] constructorArguments, KeyValuePair<string, object?>[] namedArguments)
        {
            var attribute = new KeyAttribute(null!);

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

            return attribute;
        }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class IgnoreMemberAttribute : Attribute
    {
        public static IgnoreMemberAttribute FromArray(object?[] constructorArguments, KeyValuePair<string, object?>[] namedArguments)
        {
            var attribute = new IgnoreMemberAttribute();

            return attribute;
        }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class ReconstructAttribute : Attribute
    {
        public bool Reconstruct { get; set; }

        public ReconstructAttribute(bool reconstruct)
        {
            this.Reconstruct = reconstruct;
        }

        public static ReconstructAttribute FromArray(object?[] constructorArguments, KeyValuePair<string, object?>[] namedArguments)
        {
            var attribute = new ReconstructAttribute(true);

            object? val;
            val = AttributeHelper.GetValue(0, nameof(Reconstruct), constructorArguments, namedArguments);
            if (val != null)
            {
                attribute.Reconstruct = (bool)val;
            }

            return attribute;
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
    public sealed class TinyhandGeneratorTestAttribute : Attribute
    {
        public bool AttachDebugger { get; set; }

        public bool GenerateToFile { get; set; }

        public TinyhandGeneratorTestAttribute(bool attachDebugger, bool generateToFile)
        {
            this.AttachDebugger = attachDebugger;
            this.GenerateToFile = generateToFile;
        }

        public static TinyhandGeneratorTestAttribute FromArray(object?[] constructorArguments, KeyValuePair<string, object?>[] namedArguments)
        {
            var attribute = new TinyhandGeneratorTestAttribute(false, false);

            object? val;
            val = AttributeHelper.GetValue(0, nameof(AttachDebugger), constructorArguments, namedArguments);
            if (val != null)
            {
                attribute.AttachDebugger = (bool)val;
            }

            val = AttributeHelper.GetValue(1, nameof(GenerateToFile), constructorArguments, namedArguments);
            if (val != null)
            {
                attribute.GenerateToFile = (bool)val;
            }

            return attribute;
        }
    }

    public interface ITinyhandSerializationCallback
    {
        public void OnBeforeSerialize();

        public void OnAfterDeserialize();
    }
}
