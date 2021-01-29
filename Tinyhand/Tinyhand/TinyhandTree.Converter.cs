// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Arc.Crypto;
using Arc.IO;
using Tinyhand.IO;
using Tinyhand.Tree;

#pragma warning disable SA1011 // Closing square brackets should be spaced correctly
#pragma warning disable SA1201 // Elements should appear in the correct order
#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
#pragma warning disable SA1401 // Fields should be private
#pragma warning disable SA1602 // Enumeration items should be documented
#pragma warning disable SA1649 // File name should match first type name

namespace Tinyhand
{
    public static class TinyhandTreeConverter
    {
        public static void FromBinary(byte[] byteArray, out Element element)
        {
            var reader = new TinyhandReader(byteArray);
            var byteSequence = new ByteSequence();
            try
            {
                if (TinyhandSerializer.TryDecompress(ref reader, byteSequence))
                {
                    var r = reader.Clone(byteSequence.GetReadOnlySequence());
                    FromReader(ref r, out element);
                }
                else
                {
                    FromReader(ref reader, out element);
                }
            }
            finally
            {
                byteSequence.Dispose();
            }
        }

        public static void ToBinary(Element element, out byte[] binary, out TreeConverterInfo info)
        {

        }

        internal static void FromReader(ref TinyhandReader reader, out Element element)
        {

        }
    }
}
