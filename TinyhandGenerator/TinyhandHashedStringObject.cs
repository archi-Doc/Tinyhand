// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Arc.Visceral;
using Microsoft.CodeAnalysis;

#pragma warning disable SA1202 // Elements should be ordered by access
#pragma warning disable SA1204 // Static elements should appear before instance elements
#pragma warning disable SA1602 // Enumeration items should be documented

namespace Tinyhand.Generator;

[Flags]
public enum TinyhandHashedStringObjectFlag
{
    Configured = 1 << 0,
    RelationConfigured = 1 << 1,
    Checked = 1 << 2,

    TinyhandHashedString = 1 << 10, // TinyhandHashedString
}

internal class TinyhandHashedStringObject : VisceralObjectBase<TinyhandHashedStringObject>
{
    public TinyhandHashedStringObject()
    {
    }

    public new TinyhandHashedStringBody Body => (TinyhandHashedStringBody)((VisceralObjectBase<TinyhandHashedStringObject>)this).Body;

    public TinyhandHashedStringObjectFlag ObjectFlag { get; private set; }

    public List<TinyhandGenerateFromAttributeMock>? HashedStringList { get; private set; }

    public List<TinyhandHashedStringObject>? Children { get; private set; } // The opposite of ContainingObject

    public TinyhandHashedStringGroup Group { get; private set; } = new(string.Empty);

    public void Configure()
    {
        if (this.ObjectFlag.HasFlag(TinyhandHashedStringObjectFlag.Configured))
        {
            return;
        }

        this.ObjectFlag |= TinyhandHashedStringObjectFlag.Configured;

        foreach (var objectAttribute in this.AllAttributes.Where(x => x.FullName == TinyhandGenerateFromAttributeMock.FullName))
        {// TinyhandHashedStringAttribute
            try
            {
                var attribute = TinyhandGenerateFromAttributeMock.FromArray(objectAttribute.ConstructorArguments, objectAttribute.NamedArguments);
                attribute.Location = objectAttribute.Location;
                this.ObjectFlag |= TinyhandHashedStringObjectFlag.TinyhandHashedString;

                if (objectAttribute.SyntaxReference is { } syntaxReferencee)
                {
                    attribute.FilePath = syntaxReferencee.SyntaxTree.FilePath;
                }

                this.HashedStringList ??= new();
                this.HashedStringList.Add(attribute);
            }
            catch (InvalidCastException)
            {
                this.Body.AddDiagnostic(TinyhandBody.Error_AttributePropertyError, objectAttribute.Location);
            }
        }

        // Generic type is not supported.
        if (this.Generics_Kind != VisceralGenericsKind.NotGeneric)
        {
            this.Body.AddDiagnostic(TinyhandBody.Error_GenericType, this.Location);
            return;
        }
    }

    public void ConfigureRelation()
    {// Create an object tree.
        if (this.ObjectFlag.HasFlag(TinyhandHashedStringObjectFlag.RelationConfigured))
        {
            return;
        }

        this.ObjectFlag |= TinyhandHashedStringObjectFlag.RelationConfigured;

        if (!this.Kind.IsType())
        {// Not type
            return;
        }

        var cf = this.OriginalDefinition;
        if (cf == null)
        {
            return;
        }
        else if (cf != this)
        {
            cf.ConfigureRelation();
        }

        if (cf.ContainingObject == null)
        {// Root object
            List<TinyhandHashedStringObject>? list;
            if (!this.Body.Namespaces.TryGetValue(this.Namespace, out list))
            {// Create a new namespace.
                list = new();
                this.Body.Namespaces[this.Namespace] = list;
            }

            if (!list.Contains(cf))
            {
                list.Add(cf);
            }
        }
        else
        {// Child object
            var parent = cf.ContainingObject;
            parent.ConfigureRelation();
            if (parent.Children == null)
            {
                parent.Children = new();
            }

            if (!parent.Children.Contains(cf))
            {
                parent.Children.Add(cf);
            }
        }
    }

    public void Check()
    {
        if (this.ObjectFlag.HasFlag(TinyhandHashedStringObjectFlag.Checked))
        {
            return;
        }

        this.ObjectFlag |= TinyhandHashedStringObjectFlag.Checked;

        if (this.HashedStringList != null)
        {// HashedString
            foreach (var x in this.HashedStringList)
            {
                if (!Path.IsPathRooted(x.TinyhandPath) &&
                !string.IsNullOrEmpty(x.FilePath))
                {
                    x.TinyhandPath = Path.Combine(Path.GetDirectoryName(x.FilePath), x.TinyhandPath);
                }

                this.LoadTinyhand(x);
            }
        }
    }

    internal void LoadTinyhand(TinyhandGenerateFromAttributeMock attribute)
    {
        if (!File.Exists(attribute.TinyhandPath))
        {
            this.Body.AddDiagnostic(TinyhandBody.Error_NoTinyhandFile, attribute.Location, attribute.TinyhandPath);
            return;
        }

        byte[] bytes;
        try
        {
            bytes = File.ReadAllBytes(attribute.TinyhandPath);
        }
        catch
        {
            this.Body.AddDiagnostic(TinyhandBody.Error_NoTinyhandFile, attribute.Location, attribute.TinyhandPath);
            return;
        }

        Tree.Element element;
        try
        {
            element = TinyhandParser.Parse(bytes);
        }
        catch
        {
            this.Body.AddDiagnostic(TinyhandBody.Error_ParseTinyhandFile, attribute.Location, attribute.TinyhandPath);
            return;
        }

        this.Group.Process(element, attribute.HashedString);
    }

    internal void Generate(ScopingStringBuilder ssb, GeneratorInformation info)
    {
        this.Group.Generate(this, ssb, string.Empty);
    }
}
