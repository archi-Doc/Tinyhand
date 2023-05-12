// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Arc.Visceral;
using Microsoft.CodeAnalysis;

#pragma warning disable SA1602 // Enumeration items should be documented

namespace Tinyhand.Generator;

[Flags]
public enum TinyhandGenerateMemberObjectFlag
{
    Configured = 1 << 0,
    RelationConfigured = 1 << 1,
    Checked = 1 << 2,

    TinyhandGenerateMember = 1 << 10, // TinyhandGenerateMember
    TinyhandGenerateHash = 1 << 11, // TinyhandGenerateHash
}

internal class TinyhandGenerateMemberObject : VisceralObjectBase<TinyhandGenerateMemberObject>
{
    public TinyhandGenerateMemberObject()
    {
    }

    public new TinyhandGenerateMemberBody Body => (TinyhandGenerateMemberBody)((VisceralObjectBase<TinyhandGenerateMemberObject>)this).Body;

    public TinyhandGenerateMemberObjectFlag ObjectFlag { get; private set; }

    public List<Item>? Items { get; private set; }

    public List<TinyhandGenerateMemberObject>? Children { get; private set; } // The opposite of ContainingObject

    public TinyhandGenerateMemberGroup Group { get; private set; } = new(string.Empty);

    public void Configure()
    {
        if (this.ObjectFlag.HasFlag(TinyhandGenerateMemberObjectFlag.Configured))
        {
            return;
        }

        this.ObjectFlag |= TinyhandGenerateMemberObjectFlag.Configured;

        foreach (var objectAttribute in this.AllAttributes.Where(x => x.FullName == TinyhandGenerateMemberAttributeMock.FullName))
        {// TinyhandGenerateMember
            try
            {
                var attribute = TinyhandGenerateMemberAttributeMock.FromArray(objectAttribute.ConstructorArguments, objectAttribute.NamedArguments);
                var item = new Item(objectAttribute.Location, attribute.TinyhandPath, false);
                this.ObjectFlag |= TinyhandGenerateMemberObjectFlag.TinyhandGenerateMember;

                if (objectAttribute.SyntaxReference is { } syntaxReferencee)
                {
                    item.SourcePath = syntaxReferencee.SyntaxTree.FilePath;
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
                    this.ObjectFlag |= TinyhandGenerateMemberObjectFlag.TinyhandGenerateMember;

                    if (x.SyntaxReference is { } syntaxReferencee)
                    {
                        item.SourcePath = syntaxReferencee.SyntaxTree.FilePath;
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
                    this.ObjectFlag |= TinyhandGenerateMemberObjectFlag.TinyhandGenerateHash;

                    if (x.SyntaxReference is { } syntaxReferencee)
                    {
                        item.SourcePath = syntaxReferencee.SyntaxTree.FilePath;
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
        if (this.ObjectFlag.HasFlag(TinyhandGenerateMemberObjectFlag.RelationConfigured))
        {
            return;
        }

        this.ObjectFlag |= TinyhandGenerateMemberObjectFlag.RelationConfigured;

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
            List<TinyhandGenerateMemberObject>? list;
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
        if (this.ObjectFlag.HasFlag(TinyhandGenerateMemberObjectFlag.Checked))
        {
            return;
        }

        this.ObjectFlag |= TinyhandGenerateMemberObjectFlag.Checked;

        if (this.Items != null)
        {// Generate member
            foreach (var x in this.Items)
            {
                if (!Path.IsPathRooted(x.TinyhandPath) &&
                !string.IsNullOrEmpty(x.SourcePath))
                {
                    x.TinyhandPath = Path.Combine(Path.GetDirectoryName(x.SourcePath), x.TinyhandPath);
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

        this.Group.Process(this.Body, item.Location, element, item.GenerateHash);
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
            this.TinyhandPath = tinyhandPath.Replace('\\', '/'); // backslash -> forward slash.
            this.GenerateHash = generateHash;
        }

        public Location Location { get; set; }

        public string TinyhandPath { get; set; }

        public string SourcePath { get; set; } = string.Empty;

        public bool GenerateHash { get; set; }
    }
}
