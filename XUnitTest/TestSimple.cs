// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using Arc.Visceral;
using Xunit;

#pragma warning disable 0169, 0649, 0414
#pragma warning disable SA1009 // Closing parenthesis should be spaced correctly
#pragma warning disable SA1202 // Elements should be ordered by access
#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
#pragma warning disable SA1306 // Field names should begin with lower-case letter
#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
#pragma warning disable SA1400 // Access modifier should be declared
#pragma warning disable SA1401 // Fields should be private
#pragma warning disable SA1314 // Type parameter names should begin with T
#pragma warning disable SA1516 // Elements should be separated by blank line
#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable SA1602 // Enumeration items should be documented

namespace XUnitTest_Simple
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = true)]
    public sealed class TestAttribute : Attribute
    {
        public TestAttribute(string text)
        {
            this.Text = text;
        }

        public string Text { get; private set; }
    }

    [TestAttribute("a")]
    public class TestClass
    {
        public double? Z { get; } = 1;

        private int x = 0;
        public long Y { get; set; }

        [TestAttribute("b")]
        [TestAttribute("c")]
        private string A = "test";

        public TestEnum E { get; set; } = TestEnum.B;

        public TestClass()
        {
        }

        public TestClass(int x, string A)
        {
            this.x = x;
        }

        public long GetY() => this.Y;

        // public TestClass2 tc2a;

        // public TestClass2? tc2b;
    }

    public class TestClass2
    {
        private int x;
    }

    public enum TestEnum
    {
        A,
        B,
        C,
    }

    public class TempClass
    {
        private int? x;
        private int[] x2;
        private int?[] x3;
        private int[]? x4;
        private int?[]? x5;
        private TestClass2? tc1 = default!;
        private TestClass2[] tc = default!;
        private TestClass2[]? tc2;
        private TestClass2?[] tc3;
        private TestClass2?[]? tc5;
    }

    public class TestSimple
    {
        [Fact]
        public void Test1()
        {
            var roslyn = new XUnitTest.RoslynUnit();

            var tc = new TempClass();

            var typeBody = new VisceralSampleBody(null);
            var symbolBody = new VisceralSampleBody(null);

            var tempClass = symbolBody.Add(roslyn.GetTypeSymbol("TempClass"))!;
            var tempClass2 = typeBody.Add(typeof(TempClass))!;
            Assert.True(tempClass.DeepEquals(tempClass2));

            var testClass = symbolBody.Add(roslyn.GetTypeSymbol("TestClass"))!;
            var testClass2 = typeBody.Add(typeof(TestClass))!;
            Assert.True(testClass.DeepEquals(testClass2));
        }
    }
}
