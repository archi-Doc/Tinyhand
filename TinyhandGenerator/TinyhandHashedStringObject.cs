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

    TinyhandGenerateMember = 1 << 10, // TinyhandGenerateMember
    TinyhandGenerateHash = 1 << 11, // TinyhandGenerateHash
}

internal class TinyhandHashedStringObject : VisceralObjectBase<TinyhandHashedStringObject>
{
    public TinyhandHashedStringObject()
    {
    }

    public new TinyhandHashedStringBody Body => (TinyhandHashedStringBody)((VisceralObjectBase<TinyhandHashedStringObject>)this).Body;

    public TinyhandHashedStringObjectFlag ObjectFlag { get; private set; }

    public List<Item>? Items { get; private set; }

    public List<TinyhandHashedStringObject>? Children { get; private set; } // The opposite of ContainingObject

    public TinyhandHashedStringGroup Group { get; private set; } = new(string.Empty);

    public void Configure()
    {
        if (this.ObjectFlag.HasFlag(TinyhandHashedStringObjectFlag.Configured))
        {
            return;
        }

        this.ObjectFlag |= TinyhandHashedStringObjectFlag.Configured;

        foreach (var objectAttribute in this.AllAttributes.Where(x => x.FullName == TinyhandGenerateMemberAttributeMock.FullName))
        {// TinyhandGenerateMember
            try
            {
                var attribute = TinyhandGenerateMemberAttributeMock.FromArray(objectAttribute.ConstructorArguments, objectAttribute.NamedArguments);
                var item = new Item(objectAttribute.Location, attribute.TinyhandPath, false);
                this.ObjectFlag |= TinyhandHashedStringObjectFlag.TinyhandGenerateMember;

                if (objectAttribute.SyntaxReference is { } syntaxReferencee)
                {
                    item.FilePath = syntaxReferencee.SyntaxTree.FilePath;
                }

                this.Items ??= new();
                this.Items.Add(item);
            }
            catch (InvalidCastException)
            {
                this.Body.AddDiagnostic(TinyhandBody.Error_AttributePropertyError, objectAttribute.Location);
            }
        }

        foreach (var x in this.AllAttributes)
        {
            if (x.FullName == TinyhandGenerateMemberAttributeMock.FullName)
            {// TinyhandGenerateMember
                try
                {
                    var attribute = TinyhandGenerateMemberAttributeMock.FromArray(x.ConstructorArguments, x.NamedArguments);
                    var item = new Item(x.Location, attribute.TinyhandPath, false);
                    this.ObjectFlag |= TinyhandHashedStringObjectFlag.TinyhandGenerateMember;

                    if (x.SyntaxReference is { } syntaxReferencee)
                    {
                        item.FilePath = syntaxReferencee.SyntaxTree.FilePath;
                    }

                    this.Items ??= new();
                    this.Items.Add(item);
                }
                catch (InvalidCastException)
                {
                    this.Body.AddDiagnostic(TinyhandBody.Error_AttributePropertyError, x.Location);
                }
            }
            else if (x.FullName == TinyhandGenerateHashAttributeMock.FullName)
            {// TinyhandGenerateMember
                try
                {
                    var attribute = TinyhandGenerateHashAttributeMock.FromArray(x.ConstructorArguments, x.NamedArguments);
                    var item = new Item(x.Location, attribute.TinyhandPath, true);
                    this.ObjectFlag |= TinyhandHashedStringObjectFlag.TinyhandGenerateHash;

                    if (x.SyntaxReference is { } syntaxReferencee)
                    {
                        item.FilePath = syntaxReferencee.SyntaxTree.FilePath;
                    }

                    this.Items ??= new();
                    this.Items.Add(item);
                }
                catch (InvalidCastException)
                {
                    this.Body.AddDiagnostic(TinyhandBody.Error_AttributePropertyError, x.Location);
                }
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

        if (this.Items != null)
        {// HashedString
            foreach (var x in this.Items)
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

    internal void LoadTinyhand(Item item)
    {
        if (!File.Exists(item.TinyhandPath))
        {
            this.Body.AddDiagnostic(TinyhandBody.Error_NoTinyhandFile, item.Location, item.TinyhandPath);
            return;
        }

        byte[] bytes;
        try
        {
            bytes = File.ReadAllBytes(item.TinyhandPath);
        }
        catch
        {
            this.Body.AddDiagnostic(TinyhandBody.Error_NoTinyhandFile, item.Location, item.TinyhandPath);
            return;
        }

        Tree.Element element;
        try
        {
            element = TinyhandParser.Parse(bytes);
        }
        catch
        {
            this.Body.AddDiagnostic(TinyhandBody.Error_ParseTinyhandFile, item.Location, item.TinyhandPath);
            return;
        }

        this.Group.Process(element, item.GenerateHash);
    }

    internal void Generate(ScopingStringBuilder ssb, GeneratorInformation info)
    {
        this.Group.Generate(this, ssb, string.Empty);
    }

    internal class Item
    {
        public Item(Location location, string tinyhandPath, bool generateHash)
        {
            this.Location = location;
            this.TinyhandPath = tinyhandPath;
            this.GenerateHash = generateHash;
        }

        public Location Location { get; set; }

        public string TinyhandPath { get; set; }

        public string FilePath { get; set; } = string.Empty;

        public bool GenerateHash { get; set; }
    }
}
