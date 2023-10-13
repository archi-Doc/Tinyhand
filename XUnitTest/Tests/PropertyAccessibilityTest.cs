// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Xunit;

namespace Tinyhand.Tests;

[TinyhandObject]
public partial class PropertyAccessibilityClass
{
    [Key(0, AddProperty = "A", PropertyAccessibility = PropertyAccessibility.PublicSetter)]
    private int _a;

    [Key(1, AddProperty = "B", PropertyAccessibility = PropertyAccessibility.ProtectedSetter)]
    private int _b;

    [Key(2, AddProperty = "C", PropertyAccessibility = PropertyAccessibility.GetterOnly)]
    private int _c = 3;

    [Key(3, AddProperty = "X")]
    [MaxLength(10)]
    private string _x = string.Empty;

    [Key(4, AddProperty = "Y", PropertyAccessibility = PropertyAccessibility.GetterOnly)]
    [MaxLength(10)] // This attribute is invalid because the property is getter-only.
    private string _y = string.Empty;

    public void SetB(int b) => this.B = b;
}

public class PropertyAccessibilityTest
{
    [Fact]
    public void Test1()
    {
        var c = new PropertyAccessibilityClass();
        c.A = 1;
        c.SetB(2);
        c.X = "Test";

        var b = TinyhandSerializer.Serialize(c);
        var c2 = TinyhandSerializer.Deserialize<PropertyAccessibilityClass>(b);
        c.IsStructuralEqual(c2);
    }
}
