// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

#pragma warning disable SA1602 // Enumeration items should be documented

namespace Tinyhand.Generator;

// To add chain : Add ChainType, GeneratorHelper.ChainTypeToName
// If necessary: Generate_AddLink(), GenerateGoshujin_Chain()
public enum ChainType
{
    None,
    List,
    LinkedList,
    StackList,
    QueueList,
    Ordered,
    ReverseOrdered,
    Unordered,
    Observable,
}

public enum IsolationLevel
{
    None,
    Serializable,
    RepeatableRead,
}

public enum ValueLinkAccessibility
{
    PublicGetter,
    Public,
    Inherit,
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
public sealed class ValueLinkObjectAttributeMock : Attribute
{
    public static readonly string SimpleName = "ValueLinkObject";
    public static readonly string StandardName = SimpleName + "Attribute";
    public static readonly string FullName = "ValueLink." + StandardName;

    public string GoshujinClass { get; set; } = string.Empty;

    public string GoshujinInstance { get; set; } = string.Empty;

    public string ExplicitPropertyChanged { get; set; } = string.Empty;

    public IsolationLevel Isolation { get; set; } = IsolationLevel.None;

    public ValueLinkObjectAttributeMock()
    {
    }

    public static ValueLinkObjectAttributeMock FromArray(object?[] constructorArguments, KeyValuePair<string, object?>[] namedArguments)
    {
        var attribute = new ValueLinkObjectAttributeMock();
        object? val;

        val = AttributeHelper.GetValue(-1, nameof(GoshujinClass), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.GoshujinClass = (string)val;
        }

        val = AttributeHelper.GetValue(-1, nameof(GoshujinInstance), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.GoshujinInstance = (string)val;
        }

        val = AttributeHelper.GetValue(-1, nameof(ExplicitPropertyChanged), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.ExplicitPropertyChanged = (string)val;
        }

        val = AttributeHelper.GetValue(-1, nameof(Isolation), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.Isolation = (IsolationLevel)val;
        }

        return attribute;
    }
}
