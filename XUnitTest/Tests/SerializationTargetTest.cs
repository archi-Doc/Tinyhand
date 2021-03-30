// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tinyhand;
using Xunit;

namespace Tinyhand.Tests
{
    [TinyhandObject] // Integer key
    public partial class SerializationTargetClass1
    {
        [Key(0)]
        [DefaultValue(1)]
        public int A; // Serialize

        [IgnoreMember]
        [DefaultValue(1)]
        public int B; // Not

        [DefaultValue(1)]
        private int C; // Not

        public void Test(SerializationTargetClass1 target)
        {
            this.A.Is(target.A);
            this.B.IsNot(target.B);
            this.C.IsNot(target.C);
        }
    }

    [TinyhandObject] // String key
    public partial class SerializationTargetClass2
    {
        [Key("A")]
        [DefaultValue(1)]
        public int A; // Serialize

        [IgnoreMember]
        [DefaultValue(1)]
        public int B; // Not

        [DefaultValue(1)]
        private int C; // Not

        [KeyAsName]
        [DefaultValue(1)]
        public int D; // Serialize

        public void Test(SerializationTargetClass2 target)
        {
            this.A.Is(target.A);
            this.B.IsNot(target.B);
            this.C.IsNot(target.C);
            this.D.Is(target.D);
        }
    }

    [TinyhandObject(ExplicitKeyOnly = true)] // ExplicitKeyOnly
    public partial class SerializationTargetClass3
    {
        [Key(0)]
        [DefaultValue(1)]
        public int A; // Serialize

        [DefaultValue(1)]
        public int B; // Not

        [DefaultValue(1)]
        private int C; // Not

        [Key(1)]
        [DefaultValue(1)]
        private int D; // Serialize

        public void Test(SerializationTargetClass3 target)
        {
            this.A.Is(target.A);
            this.B.IsNot(target.B);
            this.C.IsNot(target.C);
            this.D.Is(target.D);
        }
    }

    [TinyhandObject(ImplicitKeyAsName = true)] // ImplicitKeyAsName
    public partial class SerializationTargetClass4
    {
        [Key("A")]
        [DefaultValue(1)]
        public int A; // Serialize

        [DefaultValue(1)]
        public int B; // Serialize

        [KeyAsName]
        [DefaultValue(1)]
        private int C; // Serialize

        [DefaultValue(1)]
        private int D; // Not

        public void Test(SerializationTargetClass4 target)
        {
            this.A.Is(target.A);
            this.B.Is(target.B);
            this.C.Is(target.C);
            this.D.IsNot(target.D);
        }
    }

    public class SerializationTargetTest
    {
        [Fact]
        public void Test1()
        {
            var c = TinyhandSerializer.Deserialize<SerializationTargetClass1>(TinyhandSerializer.Serialize(TinyhandSerializer.Reconstruct<SerializationTargetClass1>()));
            var target = TinyhandSerializer.Reconstruct<SerializationTargetClass1>();
            c.Test(target);
        }

        [Fact]
        public void Test2()
        {
            var c = TinyhandSerializer.Deserialize<SerializationTargetClass2>(TinyhandSerializer.Serialize(TinyhandSerializer.Reconstruct<SerializationTargetClass2>()));
            var target = TinyhandSerializer.Reconstruct<SerializationTargetClass2>();
            c.Test(target);
        }

        [Fact]
        public void Test3()
        {
            var c = TinyhandSerializer.Deserialize<SerializationTargetClass3>(TinyhandSerializer.Serialize(TinyhandSerializer.Reconstruct<SerializationTargetClass3>()));
            var target = TinyhandSerializer.Reconstruct<SerializationTargetClass3>();
            c.Test(target);
        }

        [Fact]
        public void Test4()
        {
            var c = TinyhandSerializer.Deserialize<SerializationTargetClass4>(TinyhandSerializer.Serialize(TinyhandSerializer.Reconstruct<SerializationTargetClass4>()));
            var target = TinyhandSerializer.Reconstruct<SerializationTargetClass4>();
            c.Test(target);
        }
    }
}
