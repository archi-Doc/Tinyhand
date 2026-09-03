// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;

namespace Tinyhand;

/// <summary>Requests static formatter registration for a closed type and its dependencies.</summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class TinyhandRegisterAttribute : Attribute
{
    public TinyhandRegisterAttribute(Type type)
    {
        this.Type = type;
    }

    public Type Type { get; }
}
