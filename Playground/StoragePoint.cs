// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Tinyhand;

namespace CrystalData;

#pragma warning disable SA1204 // Static elements should appear before instance elements

[TinyhandObject(ExplicitKeyOnly = true)]
public partial class StoragePoint<TData>
    where TData : notnull
{
}
