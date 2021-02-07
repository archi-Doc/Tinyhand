// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tinyhand;
using Xunit;

namespace Tinyhand.Tests
{
    public class TextSerializeTest
    {
        [Fact]
        public void Test1()
        {// Requires visual assessment.
            string st;
            var simple = TinyhandSerializerOptions.Standard.WithCompose(TinyhandComposeOption.Simple);

            var c1 = TinyhandSerializer.Reconstruct<SimpleIntKeyData>();
            st = TinyhandSerializer.SerializeToString(c1);
            c1.IsStructuralEqual(TinyhandSerializer.DeserializeFromString<SimpleIntKeyData>(st));
            st = TinyhandSerializer.SerializeToString(c1, simple);
            c1.IsStructuralEqual(TinyhandSerializer.DeserializeFromString<SimpleIntKeyData>(st));

            var c2 = TinyhandSerializer.Reconstruct<EmptyClass>();
            st = TinyhandSerializer.SerializeToString(c2);
            c2.IsStructuralEqual(TinyhandSerializer.DeserializeFromString<EmptyClass>(st));
            st = TinyhandSerializer.SerializeToString(c2, simple);
            c2.IsStructuralEqual(TinyhandSerializer.DeserializeFromString<EmptyClass>(st));

            var c3 = TinyhandSerializer.Reconstruct<EmptyClass2>();
            st = TinyhandSerializer.SerializeToString(c3);
            c3.IsStructuralEqual(TinyhandSerializer.DeserializeFromString<EmptyClass2>(st));
            st = TinyhandSerializer.SerializeToString(c3, simple);
            c3.IsStructuralEqual(TinyhandSerializer.DeserializeFromString<EmptyClass2>(st));

            /* var c4 = TinyhandSerializer.Reconstruct<FormatterResolverClass>();
            st = TinyhandSerializer.SerializeToString(c4);
            c4.IsStructuralEqual(TinyhandSerializer.DeserializeFromString<FormatterResolverClass>(st));
            st = TinyhandSerializer.SerializeToString(c4, simple);
            c4.IsStructuralEqual(TinyhandSerializer.DeserializeFromString<FormatterResolverClass>(st));*/
        }
    }
}
