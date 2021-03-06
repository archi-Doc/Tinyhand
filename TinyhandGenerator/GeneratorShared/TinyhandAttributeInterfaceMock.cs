﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

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
            else if (name != null)
            {// Named Argument.
                var pair = namedArguments.FirstOrDefault(x => x.Key == name);
                if (pair.Equals(default(KeyValuePair<string, object?>)))
                {
                    return null;
                }

                return pair.Value;
            }
            else
            {
                return null;
            }
        }
    }

    public sealed class TinyhandObjectAttributeMock
    {
        public static readonly string SimpleName = "TinyhandObject";
        public static readonly string Name = SimpleName + "Attribute";
        public static readonly string FullName = "Tinyhand." + Name;

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
        /// Gets or sets a value indicating whether or not to skip a serialization if the value is the same as the default value [the default is false].
        /// </summary>
        public bool SkipSerializingDefaultValue { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether or not to use <seealso cref="IServiceProvider"/> to create an instance [the default is false].
        /// </summary>
        public bool UseServiceProvider { get; set; } = false;

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
            val = AttributeHelper.GetValue(-1, nameof(ImplicitKeyAsName), constructorArguments, namedArguments);
            if (val != null)
            {
                attribute.ImplicitKeyAsName = (bool)val;
            }

            val = AttributeHelper.GetValue(-1, nameof(IncludePrivateMembers), constructorArguments, namedArguments);
            if (val != null)
            {
                attribute.IncludePrivateMembers = (bool)val;
            }

            val = AttributeHelper.GetValue(-1, nameof(ExplicitKeyOnly), constructorArguments, namedArguments);
            if (val != null)
            {
                attribute.ExplicitKeyOnly = (bool)val;
            }

            val = AttributeHelper.GetValue(-1, nameof(ReconstructMember), constructorArguments, namedArguments);
            if (val != null)
            {
                attribute.ReconstructMember = (bool)val;
            }

            val = AttributeHelper.GetValue(-1, nameof(ReuseMember), constructorArguments, namedArguments);
            if (val != null)
            {
                attribute.ReuseMember = (bool)val;
            }

            val = AttributeHelper.GetValue(-1, nameof(SkipSerializingDefaultValue), constructorArguments, namedArguments);
            if (val != null)
            {
                attribute.SkipSerializingDefaultValue = (bool)val;
            }

            val = AttributeHelper.GetValue(-1, nameof(UseServiceProvider), constructorArguments, namedArguments);
            if (val != null)
            {
                attribute.UseServiceProvider = (bool)val;
            }

            return attribute;
        }
    }

    public class KeyAttributeMock
    {
        public static readonly string SimpleName = "Key";
        public static readonly string Name = SimpleName + "Attribute";
        public static readonly string FullName = "Tinyhand." + Name;

        public int? IntKey { get; private set; }

        public string? StringKey { get; private set; }

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

            return attribute;
        }

        public void SetKey(string x)
        {
            this.IntKey = null;
            this.StringKey = x;
        }
    }

    public class KeyAsNameAttributeMock
    {
        public static readonly string SimpleName = "KeyAsName";
        public static readonly string Name = SimpleName + "Attribute";
        public static readonly string FullName = "Tinyhand." + Name;

        public static KeyAsNameAttributeMock FromArray(object?[] constructorArguments, KeyValuePair<string, object?>[] namedArguments)
        {
            var attribute = new KeyAsNameAttributeMock();

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
            val = AttributeHelper.GetValue(0, nameof(Reconstruct), constructorArguments, namedArguments);
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
            val = AttributeHelper.GetValue(0, nameof(ReuseInstance), constructorArguments, namedArguments);
            if (val != null)
            {
                attribute.ReuseInstance = (bool)val;
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

    public class TinyhandUnionAttributeMock
    {
        public static readonly string SimpleName = "TinyhandUnion";
        public static readonly string Name = SimpleName + "Attribute";
        public static readonly string FullName = "Tinyhand." + Name;

        public Location Location { get; }

        /// <summary>
        /// Gets the distinguishing value that identifies a particular subtype.
        /// </summary>
        public int Key { get; private set; }

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
            this.Key = key;
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
                    attribute.Key = intKey;
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
}
