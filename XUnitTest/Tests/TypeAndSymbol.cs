// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Arc.Visceral;
using Xunit;

#pragma warning disable SA1300
#pragma warning disable CS0067

namespace Tinyhand.TypeAndSymbolTests
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
    public class TestClass3
    {
        public double? Z { get; } = 1;

        private int x = 0;
        public long Y { get; set; }

        [TestAttribute("b")]
        [TestAttribute("c")]
        private string A = "test";

        public TestEnum E { get; set; } = TestEnum.B;

        public TestClass3()
        {
        }

        public TestClass3(int x, string A)
        {
            this.x = x;
        }

        public long GetY() => this.Y;

        // public TestClass4 tc2a;

        // public TestClass4? tc2b;
    }

    public class TestClass4
    {
        private int x;
    }

    public enum TestEnum
    {
        A,
        B,
        C,
    }

    public class TempClass// : INotifyPropertyChanged
    {
        public event Action testEvent;
        private event Func<int> testEvent2;
        public event PropertyChangedEventHandler? PropertyChanged;
        /*private PropertyChangedEventHandler propertyChange;// Error! symbol: Error type, type: correct type
        event PropertyChangedEventHandler? INotifyPropertyChanged.PropertyChanged
        {
            add
            {
                this.propertyChange += value;
            }

            remove
            {
                this.propertyChange -= value;
            }
        }*/

        private int? x;
        private int[] x2;
        private int?[] x3;
        private int[]? x4;
        private int?[]? x5;
        private TestClass4? tc1 = default!;
        private TestClass4[] tc = default!;
        private TestClass4[]? tc2;
        private TestClass4?[] tc3;
        private TestClass4?[]? tc5;
    }

    public class TestTypeAndSymbol
    {
        [Fact]
        public void Test1()
        {
            var roslyn = new XUnitTest.RoslynUnit();

            var typeBody = new VisceralSampleBody(null);
            var symbolBody = new VisceralSampleBody(null);

            var tempClass = symbolBody.Add(roslyn.GetTypeSymbol("TempClass"))!;
            var tempClass2 = typeBody.Add(typeof(TempClass))!;
            Assert.True(tempClass.DeepEquals(tempClass2));

            var testClass = symbolBody.Add(roslyn.GetTypeSymbol("TestClass3"))!;
            var testClass2 = typeBody.Add(typeof(TestClass3))!;
            Assert.True(testClass.DeepEquals(testClass2));
        }
    }

    public class TestClass
    {
        int memberInt;
        int? memberInt2;
        string[] memberString;
        float[][] memberFloat;
        float[,,] memberFloat2;
        (int, TestClass2) memberTuple;
        (int, TestClass2)[] memberTupleArray;
        TestStruct memberStruct;
        TestClass2 memberClass;
        GenericClass<int> memberGeneric;
        NestedClass<decimal> memberNested;
        NestedClass<string>.NestedClass2 memberNested2;

        public bool TestMethod(bool b)
        {
            return b;
        }

        public T TestMethod2<T>(bool b, T t) => t;

        public TestClass2? TestMethod3() => null;

        public class NestedClass<T>
        {
            private protected long memberLong;
            protected TestClass2 nestedClass;
            internal NestedClass2 memberNested2;
            NestedClass3<int, char> memberNested3;

            public class NestedClass2
            {
                protected internal double memberDouble;
            }

            public class NestedClass3<U, V>
            {
                private double memberDouble;
                private (U, int) memberTuple2;
                (NestedClass3<U, NestedClass3<U, byte>>, V) memberTuple3;

                public NestedClass3()
                {
                    this.memberDouble = 1.0;
                }
            }

            public static void StaticMethod()
            {
            }
        }
    }

    public class GenericClass<T>
    {
        T t;
        T[] array;
        TestClass2 testClass;
        TestClass2[] testArray;
    }

    public class GenericClass2<A, B, C>
    {
    }

    public class GenericClass2a<A, B> : GenericClass2<A, B, string>
    {
    }

    public class TestClass2
    {
        string Id { get; set; } = "test";
        public string Id2 { protected get; set; } = "test";
        public string Id3 { get; private set; } = "test";
        public string Id4 { get; } = "test";
    }

    public struct TestStruct
    {
        string Name;

        public TestStruct(string name) => this.Name = name;
    }

    public class TestName
    {
        [Fact]
        public void Test1()
        {
            var t = typeof(TestClass);
            var roslyn = new XUnitTest.RoslynUnit();
            var body = new VisceralSampleBody(null);
            var o = body.Add(roslyn.GetTypeSymbol("TestClass"))!;

            this.CheckName(t, o, ".ctor", "TestClass", "Tinyhand.TypeAndSymbolTests.TestClass.TestClass()", "void", "void");
            this.CheckName(t, o, "TestMethod", "TestMethod", "Tinyhand.TypeAndSymbolTests.TestClass.TestMethod(bool)", "bool", "bool");
            this.CheckName(t, o, "TestMethod2", "TestMethod2", "Tinyhand.TypeAndSymbolTests.TestClass.TestMethod2<T>(bool, T)", "T", "T");
            this.CheckName(t, o, "TestMethod3", "TestMethod3", "Tinyhand.TypeAndSymbolTests.TestClass.TestMethod3()", "TestClass2", "Tinyhand.TypeAndSymbolTests.TestClass2");

            this.CheckName(t, o, "memberInt", "memberInt", "Tinyhand.TypeAndSymbolTests.TestClass.memberInt", "int", "int");
            this.CheckName(t, o, "memberInt2", "memberInt2", "Tinyhand.TypeAndSymbolTests.TestClass.memberInt2", "int", "int?");
            this.CheckName(t, o, "memberString", "memberString", "Tinyhand.TypeAndSymbolTests.TestClass.memberString", "string", "string[]");
            this.CheckName(t, o, "memberFloat", "memberFloat", "Tinyhand.TypeAndSymbolTests.TestClass.memberFloat", "float", "float[][]");
            this.CheckName(t, o, "memberFloat2", "memberFloat2", "Tinyhand.TypeAndSymbolTests.TestClass.memberFloat2", "float", "float[,,]");
            this.CheckName(t, o, "memberTuple", "memberTuple", "Tinyhand.TypeAndSymbolTests.TestClass.memberTuple", "ValueTuple", "(int, Tinyhand.TypeAndSymbolTests.TestClass2)");
            this.CheckName(t, o, "memberTupleArray", "memberTupleArray", "Tinyhand.TypeAndSymbolTests.TestClass.memberTupleArray", "ValueTuple", "(int, Tinyhand.TypeAndSymbolTests.TestClass2)[]");
            this.CheckName(t, o, "memberStruct", "memberStruct", "Tinyhand.TypeAndSymbolTests.TestClass.memberStruct", "TestStruct", "Tinyhand.TypeAndSymbolTests.TestStruct");
            this.CheckName(t, o, "memberClass", "memberClass", "Tinyhand.TypeAndSymbolTests.TestClass.memberClass", "TestClass2", "Tinyhand.TypeAndSymbolTests.TestClass2");
            this.CheckName(t, o, "memberGeneric", "memberGeneric", "Tinyhand.TypeAndSymbolTests.TestClass.memberGeneric", "GenericClass", "Tinyhand.TypeAndSymbolTests.GenericClass<int>");
            this.CheckName(t, o, "memberNested", "memberNested", "Tinyhand.TypeAndSymbolTests.TestClass.memberNested", "NestedClass", "Tinyhand.TypeAndSymbolTests.TestClass.NestedClass<decimal>");
            this.CheckName(t, o, "memberNested2", "memberNested2", "Tinyhand.TypeAndSymbolTests.TestClass.memberNested2", "NestedClass2", "Tinyhand.TypeAndSymbolTests.TestClass.NestedClass<string>.NestedClass2");

            t = typeof(TestClass.NestedClass<>);
            Assert.Equal("Tinyhand.TypeAndSymbolTests.TestClass.NestedClass<T>", t.TypeToFullName());
            t = typeof(TestClass.NestedClass<int>);
            Assert.Equal("Tinyhand.TypeAndSymbolTests.TestClass.NestedClass<int>", t.TypeToFullName());
            t = typeof(TestClass.NestedClass<>.NestedClass3<,>);
            Assert.Equal("Tinyhand.TypeAndSymbolTests.TestClass.NestedClass<T>.NestedClass3<U, V>", t.TypeToFullName());
            t = typeof(TestClass.NestedClass<string>.NestedClass3<int, char>);
            Assert.Equal("Tinyhand.TypeAndSymbolTests.TestClass.NestedClass<string>.NestedClass3<int, char>", t.TypeToFullName());
            t = typeof(GenericClass2a<,>);
            Assert.Equal("Tinyhand.TypeAndSymbolTests.GenericClass2a<A, B>", t.TypeToFullName());
        }

        void CheckName(Type type, VisceralSampleObject obj, string memberName, string expectedSimpleName, string expectedFullName, string expectedTypeSimpleName, string expectedTypeFullName)
        {
            // Type
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
            var memberInfo = type.GetMember(memberName, flags).First();

            var simpleName = memberInfo.MemberInfoToSimpleName();
            Assert.Equal(expectedSimpleName, simpleName);
            var fullName = memberInfo.MemberInfoToFullName();
            Assert.Equal(expectedFullName, fullName);

            var underlyingType = memberInfo.GetUnderlyingType()!;
            Assert.NotNull(underlyingType);

            var typeSimpleName = underlyingType.TypeToSimpleName();
            Assert.Equal(expectedTypeSimpleName, typeSimpleName);
            var typeFullName = underlyingType.TypeToFullName();
            Assert.Equal(expectedTypeFullName, typeFullName);

            // VisceralObject
            if (memberName == ".ctor")
            {
                memberName = expectedSimpleName;
            }

            var memberObject = obj.GetMembers(memberName).First();

            simpleName = memberObject.SimpleName;
            Assert.Equal(expectedSimpleName, simpleName);
            fullName = memberObject.FullName;
            Assert.Equal(expectedFullName, fullName);

            var underlyingObject = memberObject.TypeObject!;
            Assert.NotNull(underlyingObject);

            typeSimpleName = underlyingObject.SimpleName;
            Assert.Equal(expectedTypeSimpleName, typeSimpleName);
            typeFullName = underlyingObject.FullName;
            Assert.Equal(expectedTypeFullName, typeFullName);
        }

        [Fact]
        public void Test2()
        {
            var roslyn = new XUnitTest.RoslynUnit();

            var typeBody = new VisceralSampleBody(null);
            var symbolBody = new VisceralSampleBody(null);

            var testClass = symbolBody.Add(roslyn.GetTypeSymbol("TestClass"))!;
            var testClass2 = typeBody.Add(typeof(TestClass))!;
            Assert.True(testClass.DeepEquals(testClass2));

            var tc = new TestClass();
        }
    }
}
