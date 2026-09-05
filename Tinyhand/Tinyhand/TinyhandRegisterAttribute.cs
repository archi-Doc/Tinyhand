// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;

namespace Tinyhand;

/// <summary>Requests static formatter registration for a closed type and its dependencies.</summary>
/// <remarks>
/// For External types without visible implementations of all three Tinyhand self-type interfaces,
/// only dependencies are explored. The implementation provider must register the type or supply a custom formatter.
/// </remarks>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class TinyhandRegisterAttribute : Attribute
{
    public TinyhandRegisterAttribute(Type type)
    {
        this.Type = type;
    }

    public Type Type { get; }
}
