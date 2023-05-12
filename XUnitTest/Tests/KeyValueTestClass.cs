// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace Tinyhand.Tests;

public class KeyValueTestClass
{
    [Fact]
    public void Test1()
    {
        var array = new KeyValuePair<string, int>[3];
        array[0] = new("a", 0);
        array[1] = new("bb", 1);
        array[2] = new("ccc", 10);

        var list = new KeyValueList<string, int>(array);
        var dictionary = new Dictionary<string, int>(array);

        var st = TinyhandSerializer.SerializeToString(array);
        st = TinyhandSerializer.SerializeToString(list);
        var st2 = TinyhandSerializer.SerializeToString(dictionary);

        st.Is(st2);

        var list2 = TinyhandSerializer.DeserializeFromString<KeyValueList<string, int>>(st);
        list2.IsStructuralEqual(list);

        var list3 = TinyhandSerializer.Clone(list);
        list3.IsStructuralEqual(list);
    }
}
