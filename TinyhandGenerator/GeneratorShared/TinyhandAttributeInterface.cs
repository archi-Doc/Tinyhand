// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Tinyhand.Generator
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

    public sealed class TinyhandObjectAttributeFake
    {
        public static readonly string SimpleName = "TinyhandObject";
        public static readonly string Name = SimpleName + "Attribute";
        public static readonly string FullName = "Tinyhand." + Name;

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

        public TinyhandObjectAttributeFake()
        {
        }

        /// <summary>
        /// Create an attribute instance from constructor arguments and named arguments.
        /// </summary>
        /// <param name="constructorArguments">Constructor arguments.</param>
        /// <param name="namedArguments">Named arguments.</param>
        /// <returns>A new attribute instance.</returns>
        public static TinyhandObjectAttributeFake FromArray(object?[] constructorArguments, KeyValuePair<string, object?>[] namedArguments)
        {
            var attribute = new TinyhandObjectAttributeFake();

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

    public class KeyAttributeFake
    {
        public static readonly string SimpleName = "Key";
        public static readonly string Name = SimpleName + "Attribute";
        public static readonly string FullName = "Tinyhand." + Name;

        public int? IntKey { get; private set; }

        public string? StringKey { get; private set; }

        public KeyAttributeFake(int x)
        {
            this.IntKey = x;
        }

        public KeyAttributeFake(string x)
        {
            this.StringKey = x;
        }

        public static KeyAttributeFake FromArray(object?[] constructorArguments, KeyValuePair<string, object?>[] namedArguments)
        {
            var attribute = new KeyAttributeFake(null!);

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

    public class IgnoreMemberAttributeFake
    {
        public static readonly string SimpleName = "IgnoreMember";
        public static readonly string Name = SimpleName + "Attribute";
        public static readonly string FullName = "Tinyhand." + Name;

        public static IgnoreMemberAttributeFake FromArray(object?[] constructorArguments, KeyValuePair<string, object?>[] namedArguments)
        {
            var attribute = new IgnoreMemberAttributeFake();

            return attribute;
        }
    }

    public class ReconstructAttributeFake
    {
        public static readonly string SimpleName = "Reconstruct";
        public static readonly string Name = SimpleName + "Attribute";
        public static readonly string FullName = "Tinyhand." + Name;

        public bool Reconstruct { get; set; }

        public ReconstructAttributeFake(bool reconstruct)
        {
            this.Reconstruct = reconstruct;
        }

        public static ReconstructAttributeFake FromArray(object?[] constructorArguments, KeyValuePair<string, object?>[] namedArguments)
        {
            var attribute = new ReconstructAttributeFake(true);

            object? val;
            val = AttributeHelper.GetValue(0, nameof(Reconstruct), constructorArguments, namedArguments);
            if (val != null)
            {
                attribute.Reconstruct = (bool)val;
            }

            return attribute;
        }
    }

    public sealed class TinyhandGeneratorOptionAttributeFake : Attribute
    {
        public static readonly string SimpleName = "TinyhandGeneratorOption";
        public static readonly string Name = SimpleName + "Attribute";
        public static readonly string FullName = "Tinyhand." + Name;

        public bool AttachDebugger { get; set; } = false;

        public bool GenerateToFile { get; set; } = false;

        public string? CustomNamespace { get; set; }

        public static TinyhandGeneratorOptionAttributeFake FromArray(object?[] constructorArguments, KeyValuePair<string, object?>[] namedArguments)
        {
            var attribute = new TinyhandGeneratorOptionAttributeFake();

            object? val;
            val = AttributeHelper.GetValue(-1, nameof(AttachDebugger), constructorArguments, namedArguments);
            if (val != null)
            {
                attribute.AttachDebugger = (bool)val;
            }

            val = AttributeHelper.GetValue(-1, nameof(GenerateToFile), constructorArguments, namedArguments);
            if (val != null)
            {
                attribute.GenerateToFile = (bool)val;
            }

            val = AttributeHelper.GetValue(-1, nameof(CustomNamespace), constructorArguments, namedArguments);
            if (val != null)
            {
                attribute.CustomNamespace = (string)val;
            }

            return attribute;
        }
    }
}
