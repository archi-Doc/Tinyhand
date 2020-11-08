// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Tinyhand.Generator
{
    internal class ByteArrayComparer : EqualityComparer<byte[]>
    {
        public override bool Equals(byte[] first, byte[] second)
        {
            if (first == null || second == null)
            {
                return first == second;
            }
            else if (ReferenceEquals(first, second))
            {
                return true;
            }
            else if (first.Length != second.Length)
            {
                return false;
            }

            return first.SequenceEqual(second);
        }

        public override int GetHashCode(byte[] obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            return obj.Length;
        }
    }
}
