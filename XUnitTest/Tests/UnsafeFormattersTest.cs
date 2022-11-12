// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tinyhand.Formatters;
using Tinyhand.IO;
using Xunit;

namespace Tinyhand.Tests;

public class UnsafeFormattersTest
{
    [Fact]
    public void GuidTest()
    {
        var guid = Guid.NewGuid();
        var sequenceWriter = new TinyhandWriter();
        NativeGuidFormatter.Instance.Serialize(ref sequenceWriter, guid, null);
        var sequence = sequenceWriter.FlushAndGetReadOnlySequence();
        sequence.Length.Is(18);

        var sequenceReader = new TinyhandReader(sequence.ToArray());
        Guid nguid = NativeGuidFormatter.Instance.Deserialize(ref sequenceReader, null);
        Assert.True(sequenceReader.End);

        guid.Is(nguid);
    }

    [Fact]
    public void DecimalTest()
    {
        var d = new Decimal(1341, 53156, 61, true, 3);
        var sequenceWriter = new TinyhandWriter();
        NativeDecimalFormatter.Instance.Serialize(ref sequenceWriter, d, null);
        var sequence = sequenceWriter.FlushAndGetReadOnlySequence();
        sequence.Length.Is(18);

        var sequenceReader = new TinyhandReader(sequence.ToArray());
        var nd = NativeDecimalFormatter.Instance.Deserialize(ref sequenceReader, null);
        Assert.True(sequenceReader.End);

        d.Is(nd);
    }
}
