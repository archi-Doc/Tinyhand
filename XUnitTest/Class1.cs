// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tinyhand;

#pragma warning disable SA1401 // Fields should be private

namespace XUnitTest
{
    [TinyhandObject]
    public partial class PrimitiveClass
    {
        [Key(0)]
        public int IntField;
    }
}
