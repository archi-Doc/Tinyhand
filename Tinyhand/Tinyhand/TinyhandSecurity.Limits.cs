// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;

namespace Tinyhand;

public partial class TinyhandSecurity
{
    /// <summary>Gets the size and work limits applied before deserialization allocations.</summary>
    public TinyhandDeserializationLimits Limits { get; private set; } = TinyhandDeserializationLimits.Unrestricted;

    /// <summary>Creates a security policy with the specified immutable deserialization limits.</summary>
    /// <param name="limits">The size and work limits.</param>
    /// <returns>The updated policy, or this instance if the limits are unchanged.</returns>
    public TinyhandSecurity WithLimits(TinyhandDeserializationLimits limits)
    {
        ArgumentNullException.ThrowIfNull(limits);
        if (this.Limits == limits)
        {
            return this;
        }

        var clone = this.Clone();
        clone.Limits = limits;
        return clone;
    }
}
