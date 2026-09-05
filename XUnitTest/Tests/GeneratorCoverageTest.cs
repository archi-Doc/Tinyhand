// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Microsoft.CodeAnalysis;
using Tinyhand.Generator;
using Xunit;

namespace XUnitTest;

public partial class StaticRegistrationGeneratorTest
{
    [Theory]
    [InlineData("typeof(System.Collections.Generic.List<int>), typeof(int[])")]
    [InlineData("")]
    [InlineData("null")]
    [InlineData("null, More = null")]
    public void AttributeTypeArraysDoNotCrashRegistration(string arguments)
    {
        var result = Generate($$"""
            using System;
            using Tinyhand;
            public sealed class KnownTypesAttribute(params Type[] types) : Attribute { public Type[]? More { get; set; } }
            [TinyhandObject, KnownTypes({{arguments}})]
            public partial class Model { }
            """, out var output, new TinyhandGeneratorV2().AsSourceGenerator());
        AssertSuccessfulCompilation(result, output);
        if (arguments.StartsWith("typeof", System.StringComparison.Ordinal))
        {
            Assert.Contains("RegisterListFormatter<int>()", RegistrationSource(result));
        }
    }

    [Theory]
    [InlineData("byte")]
    [InlineData("sbyte")]
    [InlineData("short")]
    [InlineData("ushort")]
    [InlineData("int")]
    [InlineData("uint")]
    [InlineData("long")]
    [InlineData("ulong")]
    [InlineData("float")]
    [InlineData("double")]
    [InlineData("bool")]
    [InlineData("char")]
    [InlineData("System.DateTime")]
    [InlineData("System.Int128")]
    [InlineData("System.UInt128")]
    public void PrimitiveScalarNullableArrayAndListCodersCompile(string type)
    {
        var result = Generate($$"""
            using Tinyhand;
            [TinyhandObject]
            public partial class Model
            {
                [Key(0)] public {{type}} Value { get; set; }
                [Key(1)] public {{type}}? Optional { get; set; }
                [Key(2)] public {{type}}[] Values { get; set; } = [];
                [Key(3)] public System.Collections.Generic.List<{{type}}> Items { get; set; } = new();
            }
            """, out var output, new TinyhandGeneratorV2().AsSourceGenerator());
        AssertSuccessfulCompilation(result, output);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EnumCallbacksLockAndLengthLimitsCompile(bool enumAsString)
    {
        var result = Generate($$"""
            using Tinyhand;
            public enum State { First, Second }
            [TinyhandObject(EnumAsString = {{enumAsString.ToString().ToLowerInvariant()}}, LockObject = nameof(sync))]
            public partial class Model
            {
                private readonly object sync = new();
                [Key(0)] public State Value { get; set; } = State.Second;
                [Key(1)] public State? Optional { get; set; }
                [Key(2)] public State[] Values { get; set; } = [];
                [Key(3, AddProperty = "Name"), MaxLength(10)] private string name = "default";
                [TinyhandOnSerializing] private void Serializing() { }
                [TinyhandOnSerialized] private void Serialized() { }
                [TinyhandOnDeserializing] private void Deserializing() { }
                [TinyhandOnDeserialized] private void Deserialized() { }
                [TinyhandOnReconstructed] private void Reconstructed() { }
            }
            """, out var output, new TinyhandGeneratorV2().AsSourceGenerator());
        AssertSuccessfulCompilation(result, output);
    }
}
