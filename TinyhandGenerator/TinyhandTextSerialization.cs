// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Arc.Visceral;
using Microsoft.CodeAnalysis;

#pragma warning disable RS1024 // Compare symbols correctly

namespace Tinyhand.Generator
{
    public class TinyhandTextSerialization
    {
        public TinyhandTextSerialization(TinyhandObject obj)
        {
            this.Body = obj.Body;
            this.Object = obj;
        }

        public TinyhandBody Body { get; }
        public TinyhandObject Object { get; }

        public void CheckKey()
        {
        }
    }
}
