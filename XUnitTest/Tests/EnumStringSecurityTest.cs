// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Tinyhand.IO;
using Xunit;

namespace Tinyhand.Tests;

public class EnumStringSecurityTest
{
    [Theory]
    [InlineData(0, false)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    [InlineData(2, true)]
    [InlineData(3, false)]
    [InlineData(3, true)]
    public void UnknownNamesAreRejectedBeforeChangingTheTarget(int member, bool identifier)
    {
        var data = CreateData(member, "unknown-name", identifier);
        EnumStringSecurityModel? value = new();
        var originalArray = value.Array;
        var originalList = value.List;
        var options = TinyhandSerializerOptions.Standard with { Security = TinyhandSecurity.UntrustedData };
        Assert.Throws<TinyhandException>(() =>
        {
            var reader = new TinyhandReader(data);
            try
            {
                TinyhandSerializer.DeserializeObject(ref reader, ref value, options);
            }
            finally
            {
                Assert.Equal(0, reader.Depth);
            }
        });
        Assert.Equal(SecurityEnum.Second, value!.Scalar);
        Assert.Equal(SecurityEnum.Second, value.Nullable);
        Assert.Same(originalArray, value.Array);
        Assert.Same(originalList, value.List);
        Assert.Equal(SecurityEnum.Second, value.Array[0]);
        Assert.Equal(SecurityEnum.Second, value.List[0]);

        foreach (var security in new[] { TinyhandSecurity.TrustedData, TinyhandSecurity.UntrustedData.WithValidateStringValues(false) })
        {
            value = new();
            var reader = new TinyhandReader(data);
            TinyhandSerializer.DeserializeObject(ref reader, ref value, options with { Security = security });
            Assert.Equal(SecurityEnum.None, GetMember(value!, member));
            Assert.True(reader.End);
        }
    }

    [Theory]
    [InlineData("second", SecurityEnum.Second)]
    [InlineData("First, Second", SecurityEnum.First | SecurityEnum.Second)]
    [InlineData("3", SecurityEnum.First | SecurityEnum.Second)]
    public void ValidNamesFlagsAndNumericStringsRemainSupported(string text, SecurityEnum expected)
    {
        var options = TinyhandSerializerOptions.Standard with { Security = TinyhandSecurity.UntrustedData };
        for (var member = 0; member < 4; member++)
        {
            EnumStringSecurityModel? value = new();
            var reader = new TinyhandReader(CreateData(member, text, true));
            TinyhandSerializer.DeserializeObject(ref reader, ref value, options);
            Assert.Equal(expected, GetMember(value!, member));
            Assert.True(reader.End);
        }
    }

    private static SecurityEnum? GetMember(EnumStringSecurityModel value, int member)
        => member switch
        {
            0 => value.Scalar,
            1 => value.Nullable,
            2 => value.Array[0],
            _ => value.List[0],
        };

    private static byte[] CreateData(int member, string text, bool identifier)
    {
        var writer = TinyhandWriter.CreateFromBytePool();
        try
        {
            writer.WriteArrayHeader(member + 1);
            for (var i = 0; i < member; i++)
            {
                writer.WriteNil();
            }

            if (member >= 2)
            {
                writer.WriteArrayHeader(1);
            }

            if (identifier)
            {
                writer.WriteIdentifier(text);
            }
            else
            {
                writer.Write(text);
            }

            return writer.FlushAndGetArray();
        }
        finally
        {
            writer.Dispose();
        }
    }
}

[Flags]
public enum SecurityEnum
{
    None = 0,
    First = 1,
    Second = 2,
}

[TinyhandObject(EnumAsString = true)]
public partial class EnumStringSecurityModel
{
    [Key(0)]
    public SecurityEnum Scalar { get; set; } = SecurityEnum.Second;

    [Key(1)]
    public SecurityEnum? Nullable { get; set; } = SecurityEnum.Second;

    [Key(2)]
    public SecurityEnum[] Array { get; set; } = [SecurityEnum.Second];

    [Key(3)]
    public List<SecurityEnum> List { get; set; } = [SecurityEnum.Second];
}
