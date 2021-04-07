// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Tinyhand;
using Xunit;

namespace Tinyhand.Tests
{
    [TinyhandObject(ImplicitKeyAsName = true, IncludePrivateMembers = true)]
    public partial record TestRecord
    {
        public int X { get; set; }

        public int Y { get; init; }

        private string A { get; set; } = string.Empty;

        private string B = string.Empty;

        public TestRecord(int x, int y, string a, string b)
        {
            this.X = x;
            this.Y = y;
            this.A = a;
            this.B = b;
        }

        public TestRecord()
        {
        }
    }

    public class RecordTest
    {
        [Fact]
        public void Test1()
        {
            var r = new TestRecord(1, 2, "a", "b");
            var r2 = r with { X = 3, };
            r.Equals(r2).IsFalse();

            var st = TinyhandSerializer.SerializeToString(r);
            var r3 = TinyhandSerializer.DeserializeFromString<TestRecord>(st);
            r3.Equals(r).IsFalse();
            r3 = TinyhandSerializer.Deserialize<TestRecord>(TinyhandSerializer.Serialize(r));
            r.Equals(r3).IsFalse();

            r = r with { Y = 0, };

            st = TinyhandSerializer.SerializeToString(r);
            r3 = TinyhandSerializer.DeserializeFromString<TestRecord>(st);
            r3.Equals(r).IsTrue();
            r3 = TinyhandSerializer.Deserialize<TestRecord>(TinyhandSerializer.Serialize(r));
            r.Equals(r3).IsTrue();
        }
    }
}
