// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

#pragma warning disable CS8605

namespace Tinyhand.Tests
{
    public class PrimitiveResolverTest
    {
#if !ENABLE_IL2CPP

        [Fact]
        public void PrimitiveTest2()
        {
            {
                DateTime x = DateTime.UtcNow;
                var bin = TinyhandSerializer.Serialize<object>(x);
                var re1 = TinyhandSerializer.Deserialize<object>(bin);
                re1.Is(x);
            }

            {
                var x = 'あ';
                var bin = TinyhandSerializer.Serialize<object>(x);
                var re1 = TinyhandSerializer.Deserialize<object>(bin);
                ((char)(ushort)re1).Is(x);
            }

            {
                IntEnum x = IntEnum.C;
                var bin = TinyhandSerializer.Serialize<object>(x);
                var re1 = TinyhandSerializer.Deserialize<object>(bin);
                ((IntEnum)(int)re1).Is(x);
            }

            {
                var x = new object[] { 1, 10, 1000, new[] { 999, 424 }, new Dictionary<string, int> { { "hoge", 100 }, { "foo", 999 } }, true };

                var bin = TinyhandSerializer.Serialize<object>(x);
                var re1 = (object[])TinyhandSerializer.Deserialize<object>(bin);

                x[0].Is((int)re1[0]);
                x[1].Is((int)re1[1]);
                x[2].Is((int)re1[2]);
                x[5].Is(re1[5]);

                ((int[])x[3])[0].Is((ushort)((object[])re1[3])[0]);
                ((int[])x[3])[1].Is((ushort)((object[])re1[3])[1]);

                (x[4] as Dictionary<string, int>)["hoge"].Is((int)(byte)(re1[4] as Dictionary<object, object>)["hoge"]);
                (x[4] as Dictionary<string, int>)["foo"].Is((ushort)(re1[4] as Dictionary<object, object>)["foo"]);
            }
        }

#endif
    }
}
