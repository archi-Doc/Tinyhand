﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Tinyhand.Tests;

[TinyhandObject]
public partial class GlobalNamespaceTestClass
{
    public GlobalNamespaceTestClass()
    {
    }

    [Key(0)]
    public int Default { get; set; }
}
