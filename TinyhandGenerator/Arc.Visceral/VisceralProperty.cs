// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Arc.Visceral;

public struct VisceralProperty
{
    public Accessibility Getter;
    public Accessibility Setter;
    public bool InitOnly;

    public VisceralProperty(PropertyInfo pi)
    {
        this.Getter = VisceralHelper.MethodBaseToAccessibility(pi.GetMethod);
        this.Setter = VisceralHelper.MethodBaseToAccessibility(pi.SetMethod);
        this.InitOnly = false;
    }

    public VisceralProperty(IPropertySymbol ps)
    {
        this.Getter = ps.GetMethod == null ? Accessibility.NotApplicable : ps.GetMethod.DeclaredAccessibility;
        this.Setter = ps.SetMethod == null ? Accessibility.NotApplicable : ps.SetMethod.DeclaredAccessibility;
        this.InitOnly = ps.SetMethod?.IsInitOnly == true;
    }

    public VisceralProperty(Accessibility getterAccessibility, Accessibility setterAccessibility, bool initOnly)
    {
        this.Getter = getterAccessibility;
        this.Setter = setterAccessibility;
        this.InitOnly = initOnly;
    }

    public string GetterName
        => $"{this.GetterAccessibility.AccessibilityToStringPlusSpace()}get";

    public string SetterName
        => this.InitOnly ? "init" : $"{this.SetterAccessibility.AccessibilityToStringPlusSpace()}set";

    public Accessibility DeclarationAccessibility
        => this.Getter > this.Setter ? this.Getter : this.Setter;

    public Accessibility GetterAccessibility
        => this.Getter >= this.Setter ? Accessibility.NotApplicable : this.Getter;

    public Accessibility SetterAccessibility
        => this.Setter >= this.Getter ? Accessibility.NotApplicable : this.Setter;
}
