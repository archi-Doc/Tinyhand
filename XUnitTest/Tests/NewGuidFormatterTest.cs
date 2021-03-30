// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tinyhand.Formatters;
using Tinyhand.IO;
using Xunit;

namespace Tinyhand.Tests
{
    public partial class NewGuidFormatterTest
    {
        // GuidBits is internal...

        ////[Fact]
        ////public void GuidBitsTest()
        ////{
        ////    var original = Guid.NewGuid();

        ////    var patternA = Encoding.UTF8.GetBytes(original.ToString().ToUpper());
        ////    var patternB = Encoding.UTF8.GetBytes(original.ToString().ToLower());
        ////    var patternC = Encoding.UTF8.GetBytes(original.ToString().ToUpper().Replace("-", ""));
        ////    var patternD = Encoding.UTF8.GetBytes(original.ToString().ToLower().Replace("-", ""));

        ////    new MessagePack.Internal.GuidBits(new ArraySegment<byte>(patternA, 0, patternA.Length)).Value.Is(original);
        ////    new MessagePack.Internal.GuidBits(new ArraySegment<byte>(patternB, 0, patternB.Length)).Value.Is(original);
        ////    new MessagePack.Internal.GuidBits(new ArraySegment<byte>(patternC, 0, patternC.Length)).Value.Is(original);
        ////    new MessagePack.Internal.GuidBits(new ArraySegment<byte>(patternD, 0, patternD.Length)).Value.Is(original);
        ////}

        [TinyhandObject(ImplicitKeyAsName = true)]
        public partial class InClass
        {
            public int MyProperty { get; set; }

            public Guid Guid { get; set; }
        }

        [Fact]
        public void FastGuid()
        {
            {
                var original = Guid.NewGuid();
                var sequenceWriter = new TinyhandWriter();
                GuidFormatter.Instance.Serialize(ref sequenceWriter, original, null);
                var sequence = sequenceWriter.FlushAndGetReadOnlySequence();
                sequence.Length.Is(38);

                var sequenceReader = new TinyhandReader(sequence);
                GuidFormatter.Instance.Deserialize(ref sequenceReader, null).Is(original);
                sequenceReader.End.IsTrue();
            }

            {
                var c = new InClass() { MyProperty = 3414141, Guid = Guid.NewGuid() };
                InClass c2 = TinyhandSerializer.Deserialize<InClass>(TinyhandSerializer.Serialize(c));
                c.MyProperty.Is(c2.MyProperty);
                c.Guid.Is(c2.Guid);
            }
        }
    }
}
