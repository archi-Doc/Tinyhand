// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Xunit;

namespace XUnitTest.Tests;

public class GeneratorTest
{
    [Fact]
    public void GeneratedMethodIncludesAssemblyName()
    {
        var assembly = typeof(GeneratorTest).Assembly;
        var assemblyName = assembly.GetName().Name;

        Assert.NotNull(assembly.GetType($"Tinyhand.Formatters.Generated_{assemblyName}"));
        Assert.Null(assembly.GetType("Tinyhand.Formatters.Generated"));
    }
}
