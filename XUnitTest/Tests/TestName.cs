// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Linq;
using System.Reflection;
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

namespace XUnitTest_Name
{
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

            this.CheckName(t, o, ".ctor", "TestClass", "XUnitTest_Name.TestClass.TestClass()", "void", "void");
            this.CheckName(t, o, "TestMethod", "TestMethod", "XUnitTest_Name.TestClass.TestMethod(bool)", "bool", "bool");
            this.CheckName(t, o, "TestMethod2", "TestMethod2", "XUnitTest_Name.TestClass.TestMethod2<T>(bool, T)", "T", "T");
            this.CheckName(t, o, "TestMethod3", "TestMethod3", "XUnitTest_Name.TestClass.TestMethod3()", "TestClass2", "XUnitTest_Name.TestClass2");

            this.CheckName(t, o, "memberInt", "memberInt", "XUnitTest_Name.TestClass.memberInt", "int", "int");
            this.CheckName(t, o, "memberInt2", "memberInt2", "XUnitTest_Name.TestClass.memberInt2", "int", "int?");
            this.CheckName(t, o, "memberString", "memberString", "XUnitTest_Name.TestClass.memberString", "string", "string[]");
            this.CheckName(t, o, "memberFloat", "memberFloat", "XUnitTest_Name.TestClass.memberFloat", "float", "float[][]");
            this.CheckName(t, o, "memberFloat2", "memberFloat2", "XUnitTest_Name.TestClass.memberFloat2", "float", "float[,,]");
            this.CheckName(t, o, "memberTuple", "memberTuple", "XUnitTest_Name.TestClass.memberTuple", "ValueTuple", "(int, XUnitTest_Name.TestClass2)");
            this.CheckName(t, o, "memberTupleArray", "memberTupleArray", "XUnitTest_Name.TestClass.memberTupleArray", "ValueTuple", "(int, XUnitTest_Name.TestClass2)[]");
            this.CheckName(t, o, "memberStruct", "memberStruct", "XUnitTest_Name.TestClass.memberStruct", "TestStruct", "XUnitTest_Name.TestStruct");
            this.CheckName(t, o, "memberClass", "memberClass", "XUnitTest_Name.TestClass.memberClass", "TestClass2", "XUnitTest_Name.TestClass2");
            this.CheckName(t, o, "memberGeneric", "memberGeneric", "XUnitTest_Name.TestClass.memberGeneric", "GenericClass", "XUnitTest_Name.GenericClass<int>");
            this.CheckName(t, o, "memberNested", "memberNested", "XUnitTest_Name.TestClass.memberNested", "NestedClass", "XUnitTest_Name.TestClass.NestedClass<decimal>");
            this.CheckName(t, o, "memberNested2", "memberNested2", "XUnitTest_Name.TestClass.memberNested2", "NestedClass2", "XUnitTest_Name.TestClass.NestedClass<string>.NestedClass2");

            t = typeof(TestClass.NestedClass<>);
            Assert.Equal("XUnitTest_Name.TestClass.NestedClass<T>", t.TypeToFullName());
            t = typeof(TestClass.NestedClass<int>);
            Assert.Equal("XUnitTest_Name.TestClass.NestedClass<int>", t.TypeToFullName());
            t = typeof(TestClass.NestedClass<>.NestedClass3<,>);
            Assert.Equal("XUnitTest_Name.TestClass.NestedClass<T>.NestedClass3<U, V>", t.TypeToFullName());
            t = typeof(TestClass.NestedClass<string>.NestedClass3<int, char>);
            Assert.Equal("XUnitTest_Name.TestClass.NestedClass<string>.NestedClass3<int, char>", t.TypeToFullName());
            t = typeof(GenericClass2a<,>);
            Assert.Equal("XUnitTest_Name.GenericClass2a<A, B>", t.TypeToFullName());
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
