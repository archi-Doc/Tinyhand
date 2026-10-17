// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Generic;
using System.Text;
using Tinyhand.IO;
using Xunit;

namespace Tinyhand.Tests;

public class StringKeySecurityTest
{
    private static readonly string[] Keys =
    [
        "a", "bb", "ccc", "dddd", "eeeee", "ffffff", "ggggggg", "hhhhhhhh",
        "prefix__", "prefix__a", "prefix__abcdefg", "prefix__abcdefgh", "prefix__abcdefghi", "日本語",
    ];

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ExactKeysMatchAcrossFragmentBoundaries(bool untrusted)
    {
        foreach (var key in Keys)
        {
            var data = TinyhandSerializer.Serialize(new Dictionary<string, int> { [key] = 123 });
            var value = Deserialize(data, untrusted);
            Assert.Equal(123, value.Sum);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NulPaddedKeysCannotOverwriteKnownMembers(bool untrusted)
    {
        foreach (var key in Keys)
        {
            // Exercise every padding length through the next eight-byte fragment boundary.
            var paddingLimit = 9 - (Encoding.UTF8.GetByteCount(key) % 8);
            for (var padding = 1; padding <= paddingLimit; padding++)
            {
                var writer = TinyhandWriter.CreateFromBytePool();
                try
                {
                    writer.WriteMapHeader(3);
                    writer.Write(key);
                    writer.Write(123);
                    writer.Write(key + new string('\0', padding));
                    writer.Write(456);
                    writer.Write("sentinel");
                    writer.Write(789);

                    var value = Deserialize(writer.FlushAndGetArray(), untrusted);
                    Assert.Equal(123, value.Sum);
                    Assert.Equal(789, value.Sentinel);
                }
                finally
                {
                    writer.Dispose();
                }
            }
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UnknownKeysSkipNestedValuesWithoutLosingAlignment(bool untrusted)
    {
        var writer = TinyhandWriter.CreateFromBytePool();
        try
        {
            writer.WriteMapHeader(5);
            writer.WriteNil();
            writer.WriteArrayHeader(1);
            writer.Write(1);
            writer.Write(string.Empty);
            writer.WriteMapHeader(1);
            writer.Write("unknown");
            writer.Write(2);
            writer.Write("prefix__a\0");
            writer.WriteArrayHeader(1);
            writer.Write(3);
            writer.Write("pre\0fix__a");
            writer.Write(4);
            writer.Write("sentinel");
            writer.Write(789);

            var value = Deserialize(writer.FlushAndGetArray(), untrusted);
            Assert.Equal(0, value.Sum);
            Assert.Equal(789, value.Sentinel);
        }
        finally
        {
            writer.Dispose();
        }
    }

    private static StringKeySecurityModel Deserialize(byte[] data, bool untrusted)
    {
        var options = TinyhandSerializerOptions.Standard with
        {
            Security = untrusted ? TinyhandSecurity.UntrustedData : TinyhandSecurity.TrustedData,
        };
        var reader = new TinyhandReader(data);
        StringKeySecurityModel? value = null;
        TinyhandSerializer.DeserializeObject(ref reader, ref value, options);
        Assert.True(reader.End);
        return Assert.IsType<StringKeySecurityModel>(value);
    }
}

[TinyhandObject]
public partial class StringKeySecurityModel
{
    [Key("a")]
    public int A { get; set; }

    [Key("bb")]
    public int B { get; set; }

    [Key("ccc")]
    public int C { get; set; }

    [Key("dddd")]
    public int D { get; set; }

    [Key("eeeee")]
    public int E { get; set; }

    [Key("ffffff")]
    public int F { get; set; }

    [Key("ggggggg")]
    public int G { get; set; }

    [Key("hhhhhhhh")]
    public int H { get; set; }

    [Key("prefix__")]
    public int Prefix { get; set; }

    [Key("prefix__a")]
    public int PrefixA { get; set; }

    [Key("prefix__abcdefg")]
    public int PrefixSeven { get; set; }

    [Key("prefix__abcdefgh")]
    public int PrefixEight { get; set; }

    [Key("prefix__abcdefghi")]
    public int PrefixNine { get; set; }

    [Key("日本語")]
    public int Unicode { get; set; }

    [Key("sentinel")]
    public int Sentinel { get; set; }

    [IgnoreMember]
    public int Sum => this.A + this.B + this.C + this.D + this.E + this.F + this.G + this.H
        + this.Prefix + this.PrefixA + this.PrefixSeven + this.PrefixEight + this.PrefixNine + this.Unicode;
}
