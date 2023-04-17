// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Arc.Visceral;
using Microsoft.CodeAnalysis;
using Tinyhand.Coders;
using static Arc.Visceral.ScopingStringBuilder;

#pragma warning disable SA1202 // Elements should be ordered by access
#pragma warning disable SA1204 // Static elements should appear before instance elements
#pragma warning disable SA1310 // Field names should not contain underscore
#pragma warning disable SA1401 // Fields should be private
#pragma warning disable SA1602 // Enumeration items should be documented

namespace Tinyhand.Generator;

public enum ReconstructState
{
    IfPreferable, // Reconstruct a member unless no default constructor is available or members depends on each other (cricular dependency).
    Do, // An exception is thrown if a member doesn't have a default counsturcotr or members depends on each other.
    Dont, // Don't reconstruct.
}

public enum MethodCondition
{
    MemberMethod, // member method (generated)
    StaticMethod, // static method (generated, for generic class)
    Declared, // declared (user-declared)
    ExplicitlyDeclared, // declared (explicit interface)
}

[Flags]
public enum TinyhandObjectFlag
{
    Configured = 1 << 0,
    RelationConfigured = 1 << 1,
    Checked = 1 << 2,

    StringKeyObject = 1 << 3,
    IntKeyConflicted = 1 << 4,

    Target = 1 << 5,
    SerializeTarget = 1 << 6,
    ReconstructTarget = 1 << 7,
    ReuseInstanceTarget = 1 << 8,
    CloneTarget = 1 << 9,
    HiddenMember = 1 << 10,

    HasITinyhandSerializationCallback = 1 << 20, // Has ITinyhandSerializationCallback interface
    HasExplicitOnBeforeSerialize = 1 << 21, // ITinyhandSerializationCallback.OnBeforeSerialize()
    HasExplicitOnAfterDeserialize = 1 << 22, // ITinyhandSerializationCallback.OnAfterDeserialize()
    HasITinyhandSerialize = 1 << 23, // Has ITinyhandSerialize interface
    HasITinyhandReconstruct = 1 << 24, // Has ITinyhandReconstruct interface
    HasITinyhandClone = 1 << 26, // Has ITinyhandClone interface
    CanCreateInstance = 1 << 27, // Can create an instance
    InterfaceImplemented = 1 << 28, // ITinyhandSerialize, ITinyhandReconstruct, ITinyhandClone
}

public class TinyhandObject : VisceralObjectBase<TinyhandObject>
{
    public TinyhandObject()
    {
    }

    public new TinyhandBody Body => (TinyhandBody)((VisceralObjectBase<TinyhandObject>)this).Body;

    public TinyhandObjectFlag ObjectFlag { get; private set; }

    public TinyhandObjectAttributeMock? ObjectAttribute { get; private set; }

    public TinyhandUnion? Union { get; set; }

    public KeyAttributeMock? KeyAttribute { get; private set; }

    public VisceralAttribute? KeyVisceralAttribute { get; private set; }

    public IgnoreMemberAttributeMock? IgnoreMemberAttribute { get; private set; }

    public ReconstructAttributeMock? ReconstructAttribute { get; private set; }

    public ReconstructState ReconstructState { get; private set; }

    public ReuseAttributeMock? ReuseAttribute { get; private set; }

    public MaxLengthAttributeMock? MaxLengthAttribute { get; private set; }

    public TinyhandObject? MinimumConstructor { get; private set; }

    public TinyhandObject[] Members { get; private set; } = Array.Empty<TinyhandObject>(); // Members is not static && property or field

    public IEnumerable<TinyhandObject> MembersWithFlag(TinyhandObjectFlag flag) => this.Members.Where(x => x.ObjectFlag.HasFlag(flag));

    public object? DefaultValue { get; private set; }

    public string? DefaultValueTypeName { get; private set; }

    public Location? DefaultValueLocation { get; private set; }

    public TinyhandObject? DefaultInterface { get; private set; }

    public bool IsDefaultable { get; private set; }

    public bool IsAbstractOrInterface => this.Kind == VisceralObjectKind.Interface || (this.symbol is INamedTypeSymbol nts && nts.IsAbstract);

    public List<TinyhandObject>? Children { get; private set; } // The opposite of ContainingObject

    public List<TinyhandObject>? ConstructedObjects { get; private set; } // The opposite of ConstructedFrom

    public VisceralIdentifier Identifier { get; private set; } = VisceralIdentifier.Default;

    public TinyhandObject? ClosedGenericHint { get; private set; }

    public string? SetterDelegateIdentifier { get; private set; }

    public string? GetterDelegateIdentifier { get; private set; }

    public bool RequiresUnsafeDeserialize { get; private set; }

    public string UnsafeDeserializeString => this.RequiresUnsafeDeserialize ? "unsafe " : string.Empty;

    public string InIfStruct => this.Kind == VisceralObjectKind.Struct ? "in " : string.Empty;

    internal Automata? Automata { get; private set; }

    public TinyhandObject[]? IntKey_Array;

    public int IntKey_Min { get; private set; } = -1;

    public int IntKey_Max { get; private set; } = -1;

    public int IntKey_Number { get; private set; } = 0;

    public MethodCondition MethodCondition_Serialize { get; private set; }

    public MethodCondition MethodCondition_Deserialize { get; private set; }

    public MethodCondition MethodCondition_Reconstruct { get; private set; }

    public MethodCondition MethodCondition_CanSkipSerialization { get; private set; }

    public MethodCondition MethodCondition_SetDefaultValue { get; private set; }

    public MethodCondition MethodCondition_Clone { get; private set; }

    public bool RequiresGetter { get; private set; }

    public bool RequiresSetter { get; private set; }

    private ReconstructCondition reconstructCondition;

    public ReconstructCondition ReconstructCondition
    {
        get
        {
            if (this.reconstructCondition == ReconstructCondition.None)
            {
                this.reconstructCondition = TinyhandReconstruct.GetReconstructCondition(this);
            }

            return this.reconstructCondition;
        }

        protected set
        {
            this.reconstructCondition = value;
        }
    }

    public bool IsOptimizedType => this.FullName switch
    {
        "bool" => true,
        "sbyte" => true,
        "byte" => true,
        "short" => true,
        "ushort" => true,
        "int" => true,
        "uint" => true,
        "long" => true,
        "ulong" => true,
        "float" => true,
        "double" => true,
        "decimal" => true,
        "string" => true,
        "char" => true,
        _ => false,
    };

    public bool HasNullableAnnotation
    {
        get
        {
            if (this.symbol is ITypeSymbol ts)
            {
                return ts.NullableAnnotation == Microsoft.CodeAnalysis.NullableAnnotation.Annotated;
            }
            else if (this.symbol is IFieldSymbol fs)
            {
                return fs.NullableAnnotation == Microsoft.CodeAnalysis.NullableAnnotation.Annotated;
            }
            else if (this.symbol is IPropertySymbol ps)
            {
                return ps.NullableAnnotation == Microsoft.CodeAnalysis.NullableAnnotation.Annotated;
            }

            return false;
        }
    }

    public Arc.Visceral.NullableAnnotation NullableAnnotationIfReferenceType
    {
        get
        {
            if (this.TypeObject is { } typeObject)
            {
                if (typeObject.Kind.IsReferenceType() ||
                    typeObject.Kind == VisceralObjectKind.Error)
                {
                    if (this.symbol is IFieldSymbol fs)
                    {
                        return (Arc.Visceral.NullableAnnotation)fs.NullableAnnotation;
                    }
                    else if (this.symbol is IPropertySymbol ps)
                    {
                        return (Arc.Visceral.NullableAnnotation)ps.NullableAnnotation;
                    }
                }
            }

            return Arc.Visceral.NullableAnnotation.None;
        }
    }

    public string QuestionMarkIfReferenceType
    {
        get
        {
            if (this.Kind.IsReferenceType())
            {
                return "?";
            }
            else
            {
                return string.Empty;
            }
        }
    }

    public bool IsTypeParameterWithValueTypeConstraint()
    {
        if (this.symbol is ITypeParameterSymbol tps)
        {
            return tps.HasValueTypeConstraint;
        }

        return false;
    }

    public bool ContainsTypeParameter
    {
        get
        {
            if (this.Kind == VisceralObjectKind.TypeParameter)
            {
                return true;
            }
            else if (this.Array_Element is { } element &&
                element.Kind == VisceralObjectKind.TypeParameter)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    public string SimpleNameOrAddedProperty
    {
        get
        {
            if (!string.IsNullOrEmpty(this.KeyAttribute?.PropertyName))
            {
                return this.KeyAttribute!.PropertyName;
            }
            else
            {
                return this.SimpleName;
            }
        }
    }

    public void TryConfigure()
    {
        if (this.ObjectFlag.HasFlag(TinyhandObjectFlag.Configured))
        {
            return;
        }

        if (this.IsSystem)
        {
            this.ObjectFlag |= TinyhandObjectFlag.Configured;
            return;
        }

        foreach (var x in this.AllAttributes)
        {
            if (x.FullName == TinyhandObjectAttributeMock.FullName)
            {
                this.Configure();
                break;
            }
            else if (x.FullName == TinyhandUnionAttributeMock.FullName)
            {
                this.Configure();
                break;
            }
        }

        this.ObjectFlag |= TinyhandObjectFlag.Configured;
    }

    public void Configure()
    {
        if (this.ObjectFlag.HasFlag(TinyhandObjectFlag.Configured))
        {
            return;
        }

        this.ObjectFlag |= TinyhandObjectFlag.Configured;

        // Open generic type is not supported.
        /* var genericsType = this.Generics_Kind;
        if (genericsType == VisceralGenericsKind.OpenGeneric)
        {
            return;
        }*/

        if (this.Generics_Kind == VisceralGenericsKind.ClosedGeneric)
        {
            if (this.OriginalDefinition != null && this.OriginalDefinition.ClosedGenericHint == null)
            {
                this.OriginalDefinition.ClosedGenericHint = this;
            }
        }

        // ObjectAttribute
        if (this.AllAttributes.FirstOrDefault(x => x.FullName == TinyhandObjectAttributeMock.FullName) is { } objectAttribute)
        {
            try
            {
                this.ObjectAttribute = TinyhandObjectAttributeMock.FromArray(objectAttribute.ConstructorArguments, objectAttribute.NamedArguments);
            }
            catch (InvalidCastException)
            {
                this.Body.ReportDiagnostic(TinyhandBody.Error_AttributePropertyError, objectAttribute.Location);
            }
        }

        // UnionAttribute
        this.Union = TinyhandUnion.CreateFromObject(this);
        if (this.Union != null && this.ObjectAttribute == null)
        {// Add ObjectAttribute
            this.ObjectAttribute = new TinyhandObjectAttributeMock();
        }

        // UnionToAttribute
        /*if (this.Generics_Kind != VisceralGenericsKind.ClosedGeneric)
        {// Avoid duplication
            foreach (var x in this.AllAttributes.Where(a => a.FullName == TinyhandUnionToAttributeMock.FullName))
            {
                TinyhandUnionToAttributeMock unionTo;
                try
                {
                    unionTo = TinyhandUnionToAttributeMock.FromArray(x.ConstructorArguments, x.NamedArguments, x.Location);
                    if (unionTo.BaseType == null)
                    {
                        continue;
                    }
                }
                catch (InvalidCastException)
                {
                    this.Body.ReportDiagnostic(TinyhandBody.Error_AttributePropertyError, x.Location);
                    continue;
                }

                var originalBaseType = unionTo.BaseType.OriginalDefinition;
                if (!this.Body.TryGet(originalBaseType, out var baseObj))
                { // no base type
                    this.Body.AddDiagnostic(TinyhandBody.Error_UnionToError, x.Location);
                    continue;
                }
                else if (baseObj.Generics_Kind == VisceralGenericsKind.ClosedGeneric)
                {
                    continue;
                }

                this.Body.UnionToList.Add(new UnionToItem(unionTo, baseObj));
            }
        }*/

        foreach (var x in this.AllAttributes)
        {
            if (x.FullName == KeyAttributeMock.FullName)
            {// KeyAttribute
                this.KeyVisceralAttribute = x;
                try
                {
                    this.KeyAttribute = KeyAttributeMock.FromArray(x.ConstructorArguments, x.NamedArguments);
                }
                catch (ArgumentNullException)
                {
                    this.Body.ReportDiagnostic(TinyhandBody.Error_KeyAttributeError, x.Location);
                }
            }
            else if (x.FullName == KeyAsNameAttributeMock.FullName)
            {// KeyAsNameAttribute
                if (this.KeyAttribute != null)
                {// KeyAttribute and KeyAsNameAttribute are exclusive.
                    this.Body.ReportDiagnostic(TinyhandBody.Warning_KeyAsNameExclusive, x.Location);
                }
                else
                {// KeyAsNameAttribute to KeyAttribute.
                    this.KeyVisceralAttribute = x;
                    this.KeyAttribute = new KeyAttributeMock(this.SimpleName);
                }
            }
            else if (x.FullName == IgnoreMemberAttributeMock.FullName)
            {// IgnoreMemberAttribute
                try
                {
                    this.IgnoreMemberAttribute = IgnoreMemberAttributeMock.FromArray(x.ConstructorArguments, x.NamedArguments);
                }
                catch (InvalidCastException)
                {
                    this.Body.ReportDiagnostic(TinyhandBody.Error_AttributePropertyError, x.Location);
                }
            }
            else if (x.FullName == ReconstructAttributeMock.FullName)
            {// ReconstructAttribute
                try
                {
                    this.ReconstructAttribute = ReconstructAttributeMock.FromArray(x.ConstructorArguments, x.NamedArguments);
                }
                catch (InvalidCastException)
                {
                    this.Body.ReportDiagnostic(TinyhandBody.Error_AttributePropertyError, x.Location);
                }
            }
            else if (x.FullName == ReuseAttributeMock.FullName)
            {// ReuseAttribute
                try
                {
                    this.ReuseAttribute = ReuseAttributeMock.FromArray(x.ConstructorArguments, x.NamedArguments);
                }
                catch (InvalidCastException)
                {
                    this.Body.ReportDiagnostic(TinyhandBody.Error_AttributePropertyError, x.Location);
                }
            }
            else if (x.FullName == typeof(DefaultValueAttribute).FullName)
            {// DefaultValueAttribute
                this.DefaultValueLocation = x.Location;
                if (x.ConstructorArguments.Length > 0)
                {
                    this.DefaultValue = x.ConstructorArguments[0] ?? "null";
                }
            }
            else if (x.FullName == MaxLengthAttributeMock.FullName)
            {// MaxLengthAttribute
                try
                {
                    this.MaxLengthAttribute = MaxLengthAttributeMock.FromArray(x.ConstructorArguments, x.NamedArguments);
                }
                catch (InvalidCastException)
                {
                    this.Body.ReportDiagnostic(TinyhandBody.Error_AttributePropertyError, x.Location);
                }
            }
        }

        if (this.ReconstructAttribute != null)
        {
            if (this.ReconstructAttribute.Reconstruct == true)
            {
                this.ReconstructState = ReconstructState.Do;
            }
            else
            {
                this.ReconstructState = ReconstructState.Dont;
            }
        }

        if (this.ObjectAttribute != null)
        {// TinyhandObject
            this.ConfigureObject();
        }
    }

    private void ConfigureObject()
    {
        // Method condition (Serialize/Deserialize)
        this.MethodCondition_Serialize = MethodCondition.StaticMethod;
        this.MethodCondition_Deserialize = MethodCondition.StaticMethod;
        var serializeInterface = $"Tinyhand.ITinyhandSerialize<{this.FullName}>";
        if (this.Interfaces.Any(x => x.FullName == serializeInterface))
        {// ITinyhandSerialize implemented
            this.ObjectFlag |= TinyhandObjectFlag.HasITinyhandSerialize;

            if (this.GetMembers(VisceralTarget.Method).Any(x => x.SimpleName == $"{serializeInterface}.Serialize"))
            {
                this.MethodCondition_Serialize = MethodCondition.ExplicitlyDeclared;
            }
            else
            {
                this.MethodCondition_Serialize = MethodCondition.Declared;
            }

            if (this.GetMembers(VisceralTarget.Method).Any(x => x.SimpleName == $"{serializeInterface}.Deserialize"))
            {
                this.MethodCondition_Deserialize = MethodCondition.ExplicitlyDeclared;
            }
            else
            {
                this.MethodCondition_Deserialize = MethodCondition.Declared;
            }
        }

        // Method condition (Reconstruct)
        this.MethodCondition_Reconstruct = MethodCondition.StaticMethod;
        if (this.Interfaces.Any(x => x.FullName == $"Tinyhand.ITinyhandReconstruct<{this.FullName}>"))
        {// ITinyhandReconstruct implemented
            this.ObjectFlag |= TinyhandObjectFlag.HasITinyhandReconstruct;

            if (this.GetMembers(VisceralTarget.Method).Any(x => x.SimpleName == $"Tinyhand.ITinyhandReconstruct<{this.FullName}>.Reconstruct"))
            {
                this.MethodCondition_Reconstruct = MethodCondition.ExplicitlyDeclared;
            }
            else
            {
                this.MethodCondition_Reconstruct = MethodCondition.Declared;
            }
        }

        // Method condition (Default)
        var defaultInterface = this.Interfaces.FirstOrDefault(x => x.FullName.StartsWith($"{TinyhandBody.Namespace}.{TinyhandBody.ITinyhandDefault}") &&
        x.SimpleName == TinyhandBody.ITinyhandDefault);
        if (defaultInterface != null)
        {// ITinyhandDefault implemented
            this.DefaultInterface = defaultInterface;

            if (this.GetMembers(VisceralTarget.Method).Any(x => x.SimpleName == $"{this.DefaultInterface.FullName}.{TinyhandBody.CanSkipSerializationMethod}"))
            {
                this.MethodCondition_CanSkipSerialization = MethodCondition.ExplicitlyDeclared;
            }
            else
            {
                this.MethodCondition_CanSkipSerialization = MethodCondition.Declared;
            }

            if (this.GetMembers(VisceralTarget.Method).Any(x => x.SimpleName == $"{this.DefaultInterface.FullName}.{TinyhandBody.SetDefaultValueMethod}"))
            {
                this.MethodCondition_SetDefaultValue = MethodCondition.ExplicitlyDeclared;
            }
            else
            {
                this.MethodCondition_SetDefaultValue = MethodCondition.Declared;
            }
        }

        // Method condition (Clone)
        var cloneInterface = $"Tinyhand.ITinyhandClone<{this.FullName}>";
        this.MethodCondition_Clone = MethodCondition.StaticMethod;
        if (this.Interfaces.Any(x => x.FullName == cloneInterface))
        {// ITinyhandClone implemented
            this.ObjectFlag |= TinyhandObjectFlag.HasITinyhandClone;

            cloneInterface += ".Clone";
            if (this.GetMembers(VisceralTarget.Method).Any(x => x.SimpleName == cloneInterface))
            {
                this.MethodCondition_Clone = MethodCondition.ExplicitlyDeclared;
            }
            else
            {
                this.MethodCondition_Clone = MethodCondition.Declared;
            }
        }

        // ITinyhandSerializationCallback
        if (this.Interfaces.Any(x => x.FullName == "Tinyhand.ITinyhandSerializationCallback"))
        {
            this.ObjectFlag |= TinyhandObjectFlag.HasITinyhandSerializationCallback;
            if (this.GetMembers(VisceralTarget.Method).Any(x => x.SimpleName == "Tinyhand.ITinyhandSerializationCallback.OnBeforeSerialize"))
            {
                this.ObjectFlag |= TinyhandObjectFlag.HasExplicitOnBeforeSerialize;
            }

            if (this.GetMembers(VisceralTarget.Method).Any(x => x.SimpleName == "Tinyhand.ITinyhandSerializationCallback.OnAfterDeserialize"))
            {
                this.ObjectFlag |= TinyhandObjectFlag.HasExplicitOnAfterDeserialize;
            }
        }

        // Members: Property
        var list = new List<TinyhandObject>();
        foreach (var x in this.AllMembers.Where(x => x.Kind == VisceralObjectKind.Property))
        {
            if (x.TypeObject != null && !x.IsStatic && (!x.IsInternal || this.IsSameAssembly(x)))
            {// Valid TypeObject && not static
                x.Configure();
                list.Add(x);
            }
        }

        // Members: Field
        foreach (var x in this.AllMembers.Where(x => x.Kind == VisceralObjectKind.Field))
        {
            if (x.TypeObject != null && !x.IsStatic && (!x.IsInternal || this.IsSameAssembly(x)))
            {// Valid TypeObject && not static
                x.Configure();
                list.Add(x);
            }
        }

        this.Members = list.ToArray();
    }

    public void ConfigureRelation()
    {// Create an object tree.
        if (this.ObjectFlag.HasFlag(TinyhandObjectFlag.RelationConfigured))
        {
            return;
        }

        this.ObjectFlag |= TinyhandObjectFlag.RelationConfigured;

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
            List<TinyhandObject>? list;
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

        // Add default coder (options.Resolver.GetFormatter<T>()...)
        if (this.TypeObjectWithNullable != null)
        {
            if (this.TypeObjectWithNullable.Object.ObjectAttribute?.UseResolver == false)
            {
                ObjectResolver.Instance.AddFormatter(this.TypeObjectWithNullable);
            }
            else
            {
                FormatterResolver.Instance.AddFormatter(this.TypeObjectWithNullable);
            }

            /*if (this.Generics_Kind == VisceralGenericsKind.ClosedGeneric &&
                this.ContainingObject != null &&
                this.ContainingObject.OriginalDefinition is { } od)
            {// Requires Class<T>.NestedClass<int> formatter.
                var typeName = od.FullName + "." + this.LocalName;
                FormatterResolver.Instance.AddFormatter(this.Kind, typeName);
            }*/
        }

        if (cf.ConstructedObjects == null)
        {
            cf.ConstructedObjects = new();
        }

        if (!cf.ConstructedObjects.Contains(this))
        {
            cf.ConstructedObjects.Add(this);
            // this.GenericsNumber = cf.ConstructedObjects.Count;
        }
    }

    public void CheckObject()
    {
        // Identifier
        this.Identifier = new VisceralIdentifier("__gen_th_identifier_");
        foreach (var x in this.AllMembers)
        {
            this.Identifier.Add(x.SimpleName);
        }

        var partialRequired = false;
        if (!this.IsAbstractOrInterface)
        {// Class/Struct
            partialRequired = true;
            this.ObjectFlag |= TinyhandObjectFlag.CanCreateInstance;
        }
        else
        {// Interface/Abstract
            if (this.Union != null)
            {
                partialRequired = true;
            }
        }

        // partial class required.
        if (partialRequired && !this.IsPartial)
        {
            this.Body.ReportDiagnostic(TinyhandBody.Error_NotPartial, this.Location, this.FullName);
        }

        if (this.ObjectFlag.HasFlag(TinyhandObjectFlag.CanCreateInstance))
        {// Type which can create an instance
            // default constructor required.
            if (this.Kind.IsReferenceType())
            {
                if (this.IsRecord)
                {
                    this.MinimumConstructor = this.GetMembers(VisceralTarget.Method).Where(a => a.Method_IsConstructor && a.IsPublic).MinBy(a => a.Method_Parameters.Length).First();
                }
                else if (this.ObjectAttribute?.UseServiceProvider == false &&
                    this.GetMembers(VisceralTarget.Method).Any(a => a.Method_IsConstructor && a.Method_Parameters.Length == 0) != true)
                {
                    this.Body.ReportDiagnostic(TinyhandBody.Error_NoDefaultConstructor, this.Location, this.FullName);
                }
            }

            // Parent class also needs to be a partial class.
            var parent = this.ContainingObject;
            while (parent != null)
            {
                if (!parent.IsPartial)
                {
                    this.Body.ReportDiagnostic(TinyhandBody.Error_NotPartialParent, parent.Location, parent.FullName);
                }

                parent = parent.ContainingObject;
            }
        }

        if (this.ObjectAttribute?.ImplicitKeyAsName == true && this.ObjectAttribute?.ExplicitKeyOnly == true)
        {
            this.Body.ReportDiagnostic(TinyhandBody.Error_ImplicitExplicitKey, this.Location, this.FullName);
        }

        // Union
        this.Union?.CheckAndPrepare();

        // Target
        foreach (var x in this.Members)
        {
            if (!x.IsSerializable || x.IsReadOnly)
            {// Not serializable
                continue;
            }

            if (this.ObjectAttribute?.ExplicitKeyOnly == true)
            {// Explicit key only
                if (x.KeyAttribute == null)
                {
                    continue;
                }
            }
            else if (!x.IsPublic && this.ObjectAttribute?.IncludePrivateMembers != true)
            {// Skip protected or private members if IncludePrivateMembers is false.
                continue;
            }

            x.ObjectFlag |= TinyhandObjectFlag.Target | TinyhandObjectFlag.CloneTarget;
        }

        // Key, SerializeTarget
        this.CheckObject_Key();

        // ReconstructTarget
        if (this.ObjectFlag.HasFlag(TinyhandObjectFlag.HasITinyhandSerialize))
        {// ITinyhandSerialize is implemented.
            foreach (var x in this.MembersWithFlag(TinyhandObjectFlag.Target))
            {
                if (x.TypeObject?.Kind.IsReferenceType() == true)
                {
                    x.ObjectFlag |= TinyhandObjectFlag.ReconstructTarget;
                }
            }
        }
        else
        {
            foreach (var x in this.MembersWithFlag(TinyhandObjectFlag.SerializeTarget))
            {
                if (x.TypeObject?.Kind.IsReferenceType() == true)
                {
                    x.ObjectFlag |= TinyhandObjectFlag.ReconstructTarget;
                }
            }
        }

        // LockObject
        var lockObjectName = this.ObjectAttribute?.LockObject;
        if (string.IsNullOrEmpty(lockObjectName) && this.ObjectAttribute is not null)
        {// Try to get the lock object of base objects.
            var baseObject = this.BaseObject?.OriginalDefinition;
            while (baseObject != null)
            {
                baseObject.TryConfigure();
                if (!string.IsNullOrEmpty(baseObject.ObjectAttribute?.LockObject))
                {
                    lockObjectName = baseObject.ObjectAttribute!.LockObject!;
                    this.ObjectAttribute.LockObject = baseObject.ObjectAttribute!.LockObject!;
                    break;
                }

                baseObject = baseObject.BaseObject?.OriginalDefinition;
            }
        }

        if (!string.IsNullOrEmpty(lockObjectName))
        {
            var lockObject = this.AllMembers.FirstOrDefault(x => x.SimpleName == lockObjectName);
            if (lockObject == null)
            {// Not found
                this.Body.ReportDiagnostic(TinyhandBody.Error_LockObject, this.Location);
            }
            else if (lockObject.TypeObject is { } typeObject)
            {
                if (!lockObject.IsReadableFrom(this))
                {// Not accessible
                    this.Body.ReportDiagnostic(TinyhandBody.Error_LockObject3, this.Location);
                }
                else if (!typeObject.Kind.IsReferenceType())
                {// Not reference type
                    this.Body.ReportDiagnostic(TinyhandBody.Error_LockObject2, this.Location);
                }
                else if (typeObject.FullName == TinyhandBody.ILockable ||
                    typeObject.AllInterfaces.Any(x => x == TinyhandBody.ILockable))
                {// ILockable
                    this.ObjectAttribute!.LockObjectIsLockable = true;
                }
            }
        }

        // Check members.
        if (this.Kind != VisceralObjectKind.Interface)
        {
            foreach (var x in this.Members)
            {
                x.CheckMember(this);
            }
        }
    }

    private void CheckObject_Key()
    {
        if (this.ObjectFlag.HasFlag(TinyhandObjectFlag.HasITinyhandSerialize))
        {// ITinyhandSerialize is implemented. KeyAttribute is ignored.
            foreach (var x in this.MembersWithFlag(TinyhandObjectFlag.Target))
            {
                if (x.KeyAttribute != null)
                {
                    this.Body.ReportDiagnostic(TinyhandBody.Warning_KeyIgnored, x.KeyVisceralAttribute?.Location);
                    x.KeyAttribute = null;
                }
            }
        }
        else
        {
            // SerializeTarget
            foreach (var x in this.Members)
            {
                if (x.IgnoreMemberAttribute != null)
                {// [IgnoreMember]
                    if (x.KeyAttribute != null)
                    {// KeyAttribute and IgnoreMemberAttribute are exclusive.
                        this.Body.ReportDiagnostic(TinyhandBody.Warning_KeyAndIgnoreAttribute, x.KeyVisceralAttribute?.Location);
                        x.KeyAttribute = null;
                    }
                }
                else
                {// No [IgnoreMember]
                    if (x.ObjectFlag.HasFlag(TinyhandObjectFlag.Target) || x.KeyAttribute != null)
                    {// Target or has [Key]
                        x.ObjectFlag |= TinyhandObjectFlag.SerializeTarget | TinyhandObjectFlag.CloneTarget;
                    }
                }
            }

            // Search keys
            var intKeyExists = false;
            var stringKeyExists = false;
            foreach (var x in this.MembersWithFlag(TinyhandObjectFlag.SerializeTarget))
            {
                if (x.KeyAttribute == null)
                {// No KeyAttribute
                    if (this.ObjectAttribute!.ImplicitKeyAsName)
                    {// ImplicitKeyAsName
                        x.KeyAttribute = new KeyAttributeMock(x.SimpleName);
                        stringKeyExists = true;
                    }
                }
                else if (x.KeyAttribute.StringKey != null)
                {// String key
                    stringKeyExists = true;
                }
                else if (x.KeyAttribute.IntKey != null)
                {// Integer key
                    intKeyExists = true;
                }
            }

            if (stringKeyExists || (!intKeyExists && this.ObjectAttribute!.ImplicitKeyAsName == true))
            {// String key
                this.ObjectFlag |= TinyhandObjectFlag.StringKeyObject;
                this.CheckObject_StringKey();
            }
            else
            {// Int key
                this.CheckObject_IntKey();
            }
        }
    }

    private void CheckObject_StringKey()
    {
        this.Automata = new Automata(this);

        foreach (var x in this.MembersWithFlag(TinyhandObjectFlag.SerializeTarget))
        {
            if (x.KeyAttribute?.IntKey is int i)
            {
                this.Body.ReportDiagnostic(TinyhandBody.Error_IntStringKeyConflict, x.KeyVisceralAttribute?.Location);
            }
            else if (x.KeyAttribute?.StringKey is string s)
            {
                if (s == string.Empty)
                {
                    this.Body.ReportDiagnostic(TinyhandBody.Warning_InvalidIdentifier, x.KeyVisceralAttribute?.Location, s, "_");
                    s = "_";
                }

                /*if (x.KeyAttribute?.Marker == true)
                {// Key marker is only valid for integer keys.
                    this.Body.AddDiagnostic(TinyhandBody.Warning_InvalidKeyMarker, x.KeyVisceralAttribute?.Location);
                }*/

                this.Automata.AddNode(s, x);
            }
        }
    }

    private void CheckObject_IntKey()
    {
        // Reserved keys
        var reservedKeys = -1;
        var baseObject = this.BaseObject;
        while (baseObject != null)
        {
            baseObject.TryConfigure();
            if (baseObject.ObjectAttribute?.ReservedKeys is int key && key >= 0)
            {
                reservedKeys = Math.Max(reservedKeys, key);
            }

            baseObject = baseObject.BaseObject;
        }

        // Integer key
        foreach (var x in this.MembersWithFlag(TinyhandObjectFlag.SerializeTarget))
        {
            if (x.KeyAttribute?.IntKey is int i)
            {
                if (i < 0 || i > TinyhandBody.MaxIntegerKey)
                {
                    this.Body.ReportDiagnostic(TinyhandBody.Error_IntKeyOutOfRange, x.KeyVisceralAttribute?.Location);
                }
                else
                {
                    this.IntKey_Number++;
                    this.IntKey_Max = Math.Max(this.IntKey_Max, i);
                    this.IntKey_Min = Math.Min(this.IntKey_Min, i);
                }
            }
        }

        this.IntKey_Array = new TinyhandObject[this.IntKey_Max + 1];

        foreach (var x in this.MembersWithFlag(TinyhandObjectFlag.SerializeTarget))
        {
            if (x.KeyAttribute?.IntKey is int i && i >= 0 && i <= TinyhandBody.MaxIntegerKey)
            {
                if (i <= reservedKeys && x.ContainingObject == this)
                {// Reserved
                    this.Body.ReportDiagnostic(TinyhandBody.Error_IntKeyReserved, x.KeyVisceralAttribute?.Location, reservedKeys);
                }
                else if (this.IntKey_Array[i] != null)
                {// Conflict
                    this.IntKey_Array[i].ObjectFlag |= TinyhandObjectFlag.IntKeyConflicted;
                    x.ObjectFlag |= TinyhandObjectFlag.IntKeyConflicted;
                }
                else
                {
                    this.IntKey_Array[i] = x;
                }
            }
        }

        foreach (var x in this.MembersWithFlag(TinyhandObjectFlag.IntKeyConflicted))
        {
            this.Body.ReportDiagnostic(TinyhandBody.Error_IntKeyConflicted, x.KeyVisceralAttribute?.Location);
        }

        var unusedKeys = this.IntKey_Max - (reservedKeys + 1);
        if (unusedKeys >= 10 && unusedKeys > (this.IntKey_Number * 2))
        {// Too many unused key.
            this.Body.ReportDiagnostic(TinyhandBody.Warning_IntKeyUnused, this.Location);
        }

        return;
    }

    public void CheckMember(TinyhandObject parent)
    {
        // Avoid this.TypeObject!
        if (this.TypeObject == null)
        {
            return;
        }

        if (!this.IsSerializable || this.IsReadOnly)
        {// Not serializable (before)
            if (this.KeyAttribute != null || this.ReconstructAttribute != null)
            {
                if (this.Kind == VisceralObjectKind.Field)
                {// Requires unsafe deserialize method
                    parent.RequiresUnsafeDeserialize = true;
                }
                else if (this.Kind == VisceralObjectKind.Property)
                {// Getter-only property is not supported.
                    this.Body.ReportDiagnostic(TinyhandBody.Error_NotSerializableMember, this.Location, this.SimpleName);
                }
            }
        }

        if (this.KeyAttribute != null)
        {// Has KeyAttribute
            this.Body.DebugAssert(this.ObjectFlag.HasFlag(TinyhandObjectFlag.SerializeTarget), $"{this.FullName}: KeyAttribute and SerializeTarget are inconsistent.");

            if (// parent.Generics_Kind != VisceralGenericsKind.OpenGeneric &&
                this.TypeObjectWithNullable != null &&
                !this.TypeObject.ContainsTypeParameter &&
                this.TypeObjectWithNullable.Object.ObjectAttribute == null &&
                CoderResolver.Instance.IsCoderOrFormatterAvailable(this.TypeObjectWithNullable) == false)
            {// No Coder or Formatter
                var obj = this.TypeObjectWithNullable.Object;
                obj.Configure();
                if (obj.ObjectAttribute == null)
                {
                    this.Body.ReportDiagnostic(TinyhandBody.Error_ObjectAttributeRequired, this.Location, this.TypeObject.FullName);
                }
            }
        }
        else
        {// No KeyAttribute
            if (this.ObjectFlag.HasFlag(TinyhandObjectFlag.SerializeTarget))
            {
                this.Body.ReportDiagnostic(TinyhandBody.Error_KeyAttributeRequired, this.Location);
            }

            if (this.ObjectFlag.HasFlag(TinyhandObjectFlag.CloneTarget))
            {// Exclude clone target
                if (/*parent.Generics_Kind != VisceralGenericsKind.OpenGeneric &&*/
                this.TypeObjectWithNullable != null &&
                this.TypeObjectWithNullable.Object.ObjectAttribute == null &&
CoderResolver.Instance.IsCoderOrFormatterAvailable(this.TypeObjectWithNullable) == false)
                {// No Coder or Formatter
                    this.ObjectFlag &= ~TinyhandObjectFlag.CloneTarget;
                }
                else if (this.IgnoreMemberAttribute != null)
                {// [IgnoreMember]
                    this.ObjectFlag &= ~TinyhandObjectFlag.CloneTarget;
                }
            }
        }

        if (this.DefaultValue != null)
        {
            if (this.TypeObject.Array_Rank > 0)
            {
                if (this.DefaultValue is not "null")
                {// Only null is supported.
                    this.Body.ReportDiagnostic(TinyhandBody.Warning_DefaultValueType, this.DefaultValueLocation ?? this.Location);
                }

                this.IsDefaultable = true;
                this.DefaultValue = null;
            }
            else if (VisceralDefaultValue.IsDefaultableType(this.TypeObject.SimpleName))
            {// Memeber is defaultable
                this.DefaultValue = VisceralDefaultValue.ConvertDefaultValue(this.DefaultValue, this.TypeObject.SimpleName);
                if (this.DefaultValue != null)
                {// Set default value
                    this.DefaultValueTypeName = VisceralHelper.Primitives_ShortenName(this.DefaultValue.GetType().FullName);
                    this.IsDefaultable = true;
                }
                else
                {// Type does not match.
                    this.Body.ReportDiagnostic(TinyhandBody.Warning_DefaultValueType, this.DefaultValueLocation ?? this.Location);
                }
            }
            else if (this.TypeObject.Array_Rank > 0)
            {
                this.Body.ReportDiagnostic(TinyhandBody.Warning_DefaultValueType, this.DefaultValueLocation ?? this.Location);
            }
            else if (this.TypeObject.Kind == VisceralObjectKind.Enum)
            {// Enum
                this.DefaultValueTypeName = VisceralHelper.Primitives_ShortenName(this.DefaultValue.GetType().FullName);
                if (this.DefaultValueTypeName != null && VisceralDefaultValue.IsEnumUnderlyingType(this.DefaultValueTypeName))
                {
                    /* var idx = (int)this.DefaultValue;
                    if (idx >= 0 && idx < this.TypeObject.AllMembers.Length)
                    {
                        this.DefaultValue = new EnumString(this.TypeObject.AllMembers[idx].FullName);
                    }*/
                    if (this.TypeObject.Enum_GetEnumObjectFromObject(this.DefaultValue) is { } enumObject)
                    {
                        this.IsDefaultable = true;
                        this.DefaultValue = new EnumString(enumObject.FullName);
                    }
                    else
                    { // (this.DefaultValueTypeName != this.TypeObject.Enum_UnderlyingTypeObject?.FullName)
                        this.IsDefaultable = false;
                        this.DefaultValue = null;
                        this.Body.ReportDiagnostic(TinyhandBody.Warning_DefaultValueType, this.DefaultValueLocation ?? this.Location);
                    }
                }
                else
                {// Type does not match.
                    this.IsDefaultable = false;
                    this.DefaultValue = null;
                    this.Body.ReportDiagnostic(TinyhandBody.Warning_DefaultValueType, this.DefaultValueLocation ?? this.Location);
                }
            }
            else
            {// Other (ITinyhandDefault is required)
                this.IsDefaultable = false;
                this.DefaultValueTypeName = VisceralHelper.Primitives_ShortenName(this.DefaultValue.GetType().FullName);
                if (this.TypeObject.DefaultInterface is { } defaultInterface &&
                    defaultInterface.Generics_Arguments.Length > 0)
                {
                    if (VisceralDefaultValue.ConvertDefaultValue(this.DefaultValue, defaultInterface.Generics_Arguments[0].SimpleName) == null)
                    {// Type unmatched
                        this.DefaultValue = null;
                        this.Body.ReportDiagnostic(TinyhandBody.Warning_DefaultValueType, this.DefaultValueLocation ?? this.Location);
                    }
                }
                else
                {// ITinyhandDefault is required.
                    this.DefaultValue = null;
                    this.Body.ReportDiagnostic(TinyhandBody.Warning_DefaultInterface, this.DefaultValueLocation ?? this.Location, this.DefaultValueTypeName);
                }
            }
        }

        // ReconstructTarget
        if (parent.ObjectFlag.HasFlag(TinyhandObjectFlag.HasITinyhandSerialize))
        {// ITinyhandSerialize is implemented.
            if (this.ObjectFlag.HasFlag(TinyhandObjectFlag.Target) && this.TypeObject.Kind.IsReferenceType())
            { // Target && Reference type
                this.ObjectFlag |= TinyhandObjectFlag.ReconstructTarget;
            }
        }
        else
        {
            if (this.ObjectFlag.HasFlag(TinyhandObjectFlag.SerializeTarget) &&
                (this.TypeObject.Kind.IsReferenceType() ||
                this.TypeObject.ObjectAttribute != null))
            { // SerializeTarget && (Reference type || TinyhandObject)
                this.ObjectFlag |= TinyhandObjectFlag.ReconstructTarget;
            }
        }

        if (this.TypeObject.Kind == VisceralObjectKind.Error && this.ObjectFlag.HasFlag(TinyhandObjectFlag.SerializeTarget))
        {// Error type
            this.ObjectFlag |= TinyhandObjectFlag.ReconstructTarget;
        }

        if (this.ReconstructAttribute?.Reconstruct == true)
        {// Reconstruct(true)
            this.ObjectFlag |= TinyhandObjectFlag.ReconstructTarget;
            this.ReconstructState = ReconstructState.Do;
        }
        else if (this.ReconstructAttribute?.Reconstruct == false)
        {// Reconstruct(false)
            this.ObjectFlag &= ~TinyhandObjectFlag.ReconstructTarget;
            this.ReconstructState = ReconstructState.Dont;
        }

        if (!this.ObjectFlag.HasFlag(TinyhandObjectFlag.ReconstructTarget))
        {// Not ReconstructTarget
            this.ReconstructState = ReconstructState.Dont;
        }
        else
        {// ReconstructTarget
            this.CheckMember_Reconstruct(parent);
        }

        // Check ReconstructTarget
        if (!this.ObjectFlag.HasFlag(TinyhandObjectFlag.ReconstructTarget))
        {// Not ReconstructTarget
            this.Body.DebugAssert(this.ReconstructState == ReconstructState.Dont, "this.ReconstructState == ReconstructState.Dont");
        }
        else
        {// ReconstructTarget
            this.Body.DebugAssert(this.ReconstructState == ReconstructState.Do, "this.ReconstructState == ReconstructState.Do");
        }

        // ReuseInstanceTarget
        var reuseInstanceFlag = parent.ObjectAttribute?.ReuseMember == true;
        if (this.ReuseAttribute?.ReuseInstance == true)
        {
            if (this.TypeObject.ObjectAttribute != null)
            {// Has TinyhandObject attribute
                reuseInstanceFlag = true;
            }
            else
            {
                this.Body.ReportDiagnostic(TinyhandBody.Warning_TinyhandObjectRequiredToReuse, this.Location);
            }
        }
        else if (this.ReuseAttribute?.ReuseInstance == false)
        {
            reuseInstanceFlag = false;
        }

        if (reuseInstanceFlag && this.TypeObject.ObjectAttribute != null)
        {
            this.ObjectFlag |= TinyhandObjectFlag.ReuseInstanceTarget;
        }

        // Hidden members
        var parentObject = parent;
        while (parentObject != null && parentObject != this.ContainingObject)
        {
            if (parentObject.AllMembers.Any(x =>
            (x.Kind == VisceralObjectKind.Field || x.Kind == VisceralObjectKind.Property) &&
            x.ContainingObject == parentObject &&
            x.SimpleName == this.SimpleName))
            {
                this.ObjectFlag |= TinyhandObjectFlag.HiddenMember;
                break;
            }

            parentObject = parentObject.BaseObject;
        }

        // Requires getter/setter
        if (this.IsInitOnly)
        {
            this.RequiresSetter = true;
        }
        else if (this.Kind == VisceralObjectKind.Property)
        {
            if (this.IsReadOnly)
            {
                this.RequiresSetter = true;
            }
        }

        if (this.ContainingObject != parent)
        {
            if (this.ObjectFlag.HasFlag(TinyhandObjectFlag.HiddenMember))
            {// Hidden members
                if (this.Kind == VisceralObjectKind.Field)
                {
                    if (!this.Field_IsPublic)
                    {
                        this.RequiresGetter = true;
                        this.RequiresSetter = true;
                    }
                }
                else if (this.Kind == VisceralObjectKind.Property)
                {
                    if (!this.Property_IsPublicGetter)
                    {
                        this.RequiresGetter = true;
                    }

                    if (!this.Property_IsPublicSetter)
                    {
                        this.RequiresSetter = true;
                    }
                }
            }
            else
            {// Other
                if (this.Kind == VisceralObjectKind.Field)
                {
                    if (this.Field_IsPrivate)
                    {
                        this.RequiresGetter = true;
                        this.RequiresSetter = true;
                    }
                }
                else if (this.Kind == VisceralObjectKind.Property)
                {
                    if (this.Property_IsPrivateGetter)
                    {
                        this.RequiresGetter = true;
                    }

                    if (this.Property_IsPrivateSetter)
                    {
                        this.RequiresSetter = true;
                    }
                }
            }
        }

        // Add property
        if (this.KeyAttribute != null && !string.IsNullOrEmpty(this.KeyAttribute.PropertyName))
        {
            if (this.Kind != VisceralObjectKind.Field)
            {
                this.Body.ReportDiagnostic(TinyhandBody.Error_AddProperty, this.KeyVisceralAttribute?.Location);
            }

            if (!parent.Identifier.Add(this.KeyAttribute.PropertyName))
            {
                this.Body.ReportDiagnostic(TinyhandBody.Error_DuplicateKeyword, this.KeyVisceralAttribute?.Location, parent.SimpleName, this.KeyAttribute.PropertyName);
            }

            this.RequiresGetter = false;
            this.RequiresSetter = false;
        }

        // MaxLength
        if (this.MaxLengthAttribute != null)
        {
            var typeObject = this.TypeObject;
            if (typeObject.FullName == "string")
            {// string
            }
            else if (typeObject.Array_Rank == 1)
            {// T[]
            }
            else if (typeObject.Generics_Kind == VisceralGenericsKind.ClosedGeneric &&
                typeObject.OriginalDefinition is { } baseObject &&
                baseObject.FullName == "System.Collections.Generic.List<T>" &&
                typeObject.Generics_Arguments.Length == 1)
            {// List<T>
            }
            else
            {
                this.Body.ReportDiagnostic(TinyhandBody.Warning_MaxLengthAttribute, this.Location);
            }

            if (string.IsNullOrEmpty(this.KeyAttribute?.PropertyName))
            {
                this.Body.ReportDiagnostic(TinyhandBody.Warning_MaxLengthAttribute2, this.Location);
            }
        }
    }

    private void CheckMember_Reconstruct(TinyhandObject parent)
    {
        if (this.ReconstructState == ReconstructState.IfPreferable)
        {
            // Parent's ReconstructMember is false
            if (parent.ObjectAttribute?.ReconstructMember == false)
            {
                this.ReconstructState = ReconstructState.Dont;
            }

            // Avoid reconstruct "T?"
            if (this.NullableAnnotationIfReferenceType == Arc.Visceral.NullableAnnotation.Annotated)
            {
                this.ReconstructState = ReconstructState.Dont;
            }
        }
        else if (this.ReconstructState == ReconstructState.Do)
        {
            if (this.IsReadOnly)
            {
                this.Body.ReportDiagnostic(TinyhandBody.Error_ReadonlyMember, this.Location, this.SimpleName);
                this.ReconstructState = ReconstructState.Dont;
            }
        }

        if (this.ReconstructState != ReconstructState.Dont)
        {// ReconstructState.IfPreferable or ReconstructState.Do
            var condition = this.ReconstructCondition;

            if (condition == ReconstructCondition.Can)
            {// Can reconstruct.
                this.ReconstructState = ReconstructState.Do;
            }
            else
            {// Cannot reconstruct.
                if (this.ReconstructState == ReconstructState.IfPreferable)
                {
                    this.ReconstructState = ReconstructState.Dont;
                }
                else
                {// Warning
                    this.ReconstructState = ReconstructState.Dont;
                    if (condition == ReconstructCondition.CircularDependency)
                    {
                        this.Body.ReportDiagnostic(TinyhandBody.Warning_Circular, this.Location, this.TypeObject!.FullName);
                    }
                    else if (condition == ReconstructCondition.NoDefaultConstructor)
                    {
                        this.Body.ReportDiagnostic(TinyhandBody.Warning_NoDefaultConstructor, this.Location, this.TypeObject!.FullName);
                    }
                    else if (condition == ReconstructCondition.NotReferenceType)
                    {
                        this.Body.ReportDiagnostic(TinyhandBody.Warning_NotReferenceType, this.Location, this.TypeObject!.FullName);
                    }
                }
            }
        }

        if (this.ReconstructState != ReconstructState.Do)
        {
            this.ObjectFlag &= ~TinyhandObjectFlag.ReconstructTarget;
        }
    }

    public void Check()
    {
        if (this.ObjectFlag.HasFlag(TinyhandObjectFlag.Checked))
        {
            return;
        }

        this.ObjectFlag |= TinyhandObjectFlag.Checked;

        this.Body.DebugAssert(this.ObjectAttribute != null, "this.ObjectAttribute != null");
        this.CheckObject();
    }

    public static void GenerateLoader(ScopingStringBuilder ssb, GeneratorInformation info, List<TinyhandObject> list)
    {// list: Primary TinyhandObject
        var list2 = list.SelectMany(x => x.ConstructedObjects).Where(x => x.ObjectAttribute != null).ToArray();

        if (list2.Length > 0 && list2[0].ContainingObject is { } containingObject)
        {// Add ModuleInitializerClass
            string? initializerClassName = null;

            if (containingObject.ClosedGenericHint != null)
            {// ClosedGenericHint
                initializerClassName = containingObject.ClosedGenericHint.FullName;
                goto ModuleInitializerClass_Added;
            }

            var constructedList = containingObject.ConstructedObjects;
            if (constructedList != null)
            {// Closed generic
                for (var n = 0; n < constructedList.Count; n++)
                {
                    if (constructedList[n].Generics_Kind != VisceralGenericsKind.OpenGeneric)
                    {
                        initializerClassName = constructedList[n].FullName;
                        goto ModuleInitializerClass_Added;
                    }
                }
            }

            // Open generic
            var nameList = containingObject.GetSafeGenericNameList();
            (initializerClassName, _) = containingObject.GetClosedGenericName(nameList);

ModuleInitializerClass_Added:
            if (initializerClassName != null)
            {
                info.ModuleInitializerClass.Add(initializerClassName);
            }
        }

        using (var m = ssb.ScopeBrace("internal static void __gen__th()"))
        {
            foreach (var x in list2.Where(x => x.ObjectFlag.HasFlag(TinyhandObjectFlag.InterfaceImplemented)))
            {
                x.GenerateLoaderCore(ssb, info, false);
            }
        }
    }

    internal void GenerateLoaderCore(ScopingStringBuilder ssb, GeneratorInformation info, bool checkAccessibility)
    {
        var isAccessible = true;
        if (checkAccessibility && this.ContainsNonPublicObject())
        {
            isAccessible = false;
        }

        if (this.Generics_Kind != VisceralGenericsKind.OpenGeneric)
        {// FormatterContainsNonPublic
            if (isAccessible)
            {
                ssb.AppendLine($"GeneratedResolver.Instance.SetFormatter(new Tinyhand.Formatters.TinyhandObjectFormatter<{this.FullName}>());");
            }
            else
            {
                var fullName = this.GetGenericsName();
                ssb.AppendLine($"GeneratedResolver.Instance.SetFormatterGenerator(Type.GetType(\"{fullName}\")!, static (x, y) =>");
                ssb.AppendLine("{");
                ssb.IncrementIndent();

                ssb.AppendLine($"var formatter = Activator.CreateInstance(typeof(Tinyhand.Formatters.TinyhandObjectFormatter<>).MakeGenericType(x));");
                ssb.AppendLine("return (ITinyhandFormatter)formatter!;");

                ssb.DecrementIndent();
                ssb.AppendLine("});");
            }
        }
        else
        {// Formatter generator
            string typeName;
            if (isAccessible)
            {
                var generic = this.GetClosedGenericName(null);
                typeName = $"typeof({generic.Name})";
            }
            else
            {
                var fullName = this.GetGenericsName();
                typeName = $"Type.GetType(\"{fullName}\")!";
            }

            ssb.AppendLine($"GeneratedResolver.Instance.SetFormatterGenerator({typeName}, static (x, y) =>");
            ssb.AppendLine("{");
            ssb.IncrementIndent();

            ssb.AppendLine($"var ft = x.MakeGenericType(y);");
            ssb.AppendLine($"var formatter = Activator.CreateInstance(typeof(Tinyhand.Formatters.TinyhandObjectFormatter<>).MakeGenericType(ft));");
            ssb.AppendLine("return (ITinyhandFormatter)formatter!;");

            ssb.DecrementIndent();
            ssb.AppendLine("});");
        }
    }

    internal void GenerateFlatLoader(ScopingStringBuilder ssb, GeneratorInformation info)
    {
        if (this.ObjectAttribute == null)
        {
        }
        else if (this.ConstructedObjects == null)
        {
        }
        else if (this.IsAbstractOrInterface && this.Union == null)
        {
        }
        else if (!this.ObjectFlag.HasFlag(TinyhandObjectFlag.InterfaceImplemented))
        {
        }
        else
        {
            this.GenerateLoaderCore(ssb, info, true);
        }

        if (this.Children?.Count > 0)
        {
            foreach (var x in this.Children)
            {
                x.GenerateFlatLoader(ssb, info);
            }
        }
    }

    internal void Generate(ScopingStringBuilder ssb, GeneratorInformation info)
    { // Primary TinyhandObject
        if (this.ConstructedObjects == null)
        {
            return;
        }
        else if (this.IsAbstractOrInterface && this.Union == null)
        {
            if (this.Children?.Count > 0)
            {// Generate children and loader.
                using (var cls = ssb.ScopeBrace($"{this.AccessibilityName} partial {this.KindName} {this.LocalName}"))
                {
                    foreach (var x in this.Children)
                    {
                        x.Generate(ssb, info);
                    }

                    if (!info.FlatLoader)
                    {
                        ssb.AppendLine();
                        GenerateLoader(ssb, info, this.Children);
                    }
                }
            }

            return;
        }

        this.ObjectFlag |= TinyhandObjectFlag.InterfaceImplemented;
        var interfaceString = string.Empty;
        if (this.ObjectAttribute != null)
        {
            if (this.MethodCondition_Serialize == MethodCondition.StaticMethod)
            {
                interfaceString = $" : ITinyhandSerialize<{this.RegionalName}>";
            }

            if (this.MethodCondition_Reconstruct == MethodCondition.StaticMethod)
            {
                if (interfaceString == string.Empty)
                {
                    interfaceString = $" : ITinyhandReconstruct<{this.RegionalName}>";
                }
                else
                {
                    interfaceString += $", ITinyhandReconstruct<{this.RegionalName}>";
                }
            }

            if (this.MethodCondition_Clone == MethodCondition.StaticMethod)
            {
                if (interfaceString == string.Empty)
                {
                    interfaceString = $" : ITinyhandClone<{this.RegionalName}>";
                }
                else
                {
                    interfaceString += $", ITinyhandClone<{this.RegionalName}>";
                }
            }

            if (interfaceString == string.Empty)
            {
                interfaceString = $" : ITinyhandSerialize";
            }
            else
            {
                interfaceString += $", ITinyhandSerialize";
            }
        }

        // Prepare generator information
        info.EnumAsString = this.ObjectAttribute?.EnumAsString == true;

        using (var cls = ssb.ScopeBrace($"{this.AccessibilityName} partial {this.KindName} {this.LocalName}{interfaceString}"))
        {
            // Prepare Primary
            this.Generate_PreparePrimary();

            if (this.ObjectAttribute != null)
            {// Constructor/SetMembers
             // this.GenerateConstructor_Method(ssb, info);
             // this.GenerateSetMembers_Method(ssb, info);
            }

            foreach (var x in this.ConstructedObjects)
            {
                if (x.ObjectAttribute == null)
                {
                    continue;
                }

                if (x.Generics_Kind == VisceralGenericsKind.ClosedGeneric)
                {// Use Class<T> for not optimized type.
                    /* if (!x.Generics_Arguments.All(a => a.IsOptimizedType))
                    {
                        continue;
                    }*/

                    var optimizedType = true;
                    var c = x;
                    while (c != null)
                    {
                        if (!c.Generics_Arguments.All(a => a.IsOptimizedType))
                        {
                            optimizedType = false;
                            break;
                        }

                        c = c.ContainingObject;
                    }

                    if (!optimizedType)
                    {
                        continue;
                    }
                }

                /*if (x.GenericsNumber > 1)
                {
                    ssb.AppendLine();
                }*/

                // Prepare Secondary
                x.Generate_PrepareSecondary();

                x.GenerateMethod(ssb, info);
            }

            /*if (this.ObjectAttribute != null && info.UseMemberNotNull)
            {// MemberNotNull
                if (this.MethodCondition_Reconstruct == MethodCondition.MemberMethod)
                {
                    this.GenerateMemberNotNull_MemberMethod(ssb, info);
                }
                else if (this.MethodCondition_Serialize == MethodCondition.StaticMethod)
                {
                    this.GenerateMemberNotNull_StaticMethod(ssb, info);
                }
            }*/

            // StringKey fields
            this.GenerateStringKeyFields(ssb, info);

            // Init-only property delegates
            this.GenerateInitSetters(ssb, info);

            if (this.Children?.Count > 0)
            {// Generate children and loader.
                ssb.AppendLine();
                foreach (var x in this.Children)
                {
                    x.Generate(ssb, info);
                }

                if (!info.FlatLoader)
                {
                    ssb.AppendLine();
                    GenerateLoader(ssb, info, this.Children);
                }
            }
        }
    }

    internal void Generate_PreparePrimary()
    {// Prepare Primary TinyhandObject
        this.PrepareAutomata();
        if (this.Automata != null)
        {
            foreach (var x in this.Automata.NodeList)
            {
                x.Identifier = this.Identifier.GetIdentifier(); // Exclusive to Primary
            }
        }

        // Init setter delegates
        var array = this.MembersWithFlag(TinyhandObjectFlag.SerializeTarget).Where(x => x.RequiresSetter).ToArray();
        if (array.Length != 0)
        {
            foreach (var x in array)
            {
                x.SetterDelegateIdentifier = this.Identifier.GetIdentifier(); // Exclusive to Primary
            }
        }

        // Init getter delegates
        array = this.MembersWithFlag(TinyhandObjectFlag.SerializeTarget).Where(x => x.RequiresGetter).ToArray();
        if (array.Length != 0)
        {
            foreach (var x in array)
            {
                x.GetterDelegateIdentifier = this.Identifier.GetIdentifier(); // Exclusive to Primary
            }
        }
    }

    internal void Generate_PrepareSecondary()
    {// Prepare Secondary TinyhandObject
        this.PrepareAutomata();

        var od = this.OriginalDefinition;
        if (od == null)
        {
            return;
        }

        // Automata
        if (od.Automata != null && this.Automata != null)
        {
            for (var n = 0; n < this.Automata.NodeList.Count; n++)
            {
                this.Automata.NodeList[n].Identifier = od.Automata.NodeList[n].Identifier;
            }
        }

        // Init setter delegates
        for (var n = 0; n < this.Members.Length; n++)
        {
            this.Members[n].SetterDelegateIdentifier = od.Members[n].SetterDelegateIdentifier;
            this.Members[n].GetterDelegateIdentifier = od.Members[n].GetterDelegateIdentifier;
        }
    }

    internal void GenerateInitSetters(ScopingStringBuilder ssb, GeneratorInformation info)
    {
        // Array
        var array = this.MembersWithFlag(TinyhandObjectFlag.SerializeTarget).Where(x => x.RequiresSetter || x.RequiresGetter).ToArray();
        if (array.Length == 0)
        {
            return;
        }

        // Identifier
        var initializeFlag = this.Identifier.GetIdentifier(); // Exclusive to Primary
        var initializeMethod = this.Identifier.GetIdentifier(); // Exclusive to Primary

        // initializeFlag / initializeMethod
        ssb.AppendLine();
        ssb.AppendLine($"private static bool {initializeFlag} = {initializeMethod}();");
        using (var scopeMethod = ssb.ScopeBrace($"private static bool {initializeMethod}()"))
        {
            ssb.AppendLine($"var type = typeof({this.LocalName});");
            ssb.AppendLine("var expType = Expression.Parameter(type);");
            // ssb.AppendLine("System.Reflection.MethodInfo mi;");
            ssb.AppendLine("ParameterExpression exp;");
            ssb.AppendLine("ParameterExpression exp2;");
            // ssb.AppendLine("FieldInfo fi;");
            foreach (var x in array)
            {
                if (x.RequiresSetter)
                {
                    ssb.AppendLine($"exp2 = Expression.Parameter(typeof({x.TypeObject!.FullName}));");
                    if (this.Kind == VisceralObjectKind.Struct)
                    {// Struct
                        ssb.AppendLine($"exp = Expression.Parameter(typeof({x.ContainingObject!.FullName}).MakeByRefType());");
                        ssb.AppendLine($"{x.SetterDelegateIdentifier} = Expression.Lambda<ByRefAction<{this.LocalName}, {x.TypeObject!.FullName}{x.TypeObject!.QuestionMarkIfReferenceType}>>(Expression.Assign(Expression.PropertyOrField(exp, \"{x.SimpleName}\"), exp2), exp, exp2).CompileFast();");
                    }
                    else
                    {// Class
                        ssb.AppendLine($"exp = Expression.Parameter(typeof({x.ContainingObject!.FullName}));");
                        ssb.AppendLine($"{x.SetterDelegateIdentifier} = Expression.Lambda<Action<{this.LocalName}, {x.TypeObject!.FullName}{x.TypeObject!.QuestionMarkIfReferenceType}>>(Expression.Assign(Expression.PropertyOrField(exp, \"{x.SimpleName}\"), exp2), exp, exp2).CompileFast();");
                    }

                    /*if (x.Kind == VisceralObjectKind.Property)
                    {// Property
                        ssb.AppendLine($"mi = type.GetMethod(\"set_{x.SimpleName}\")!;");
                        ssb.AppendLine($"exp = Expression.Parameter(typeof({x.TypeObject!.FullName}));");
                        ssb.AppendLine($"{x.SetterDelegateIdentifier} = Expression.Lambda<Action<{this.LocalName}, {x.TypeObject!.FullName}>>(Expression.Call(expType, mi!, exp), expType, exp).CompileFast();");
                    }
                    else
                    {// Field
                        // ssb.AppendLine($"fi = typeof({x.ContainingObject!.FullName}).GetField(\"{x.SimpleName}\", BindingFlags.NonPublic | BindingFlags.Instance)!;");
                        ssb.AppendLine($"exp = Expression.Parameter(typeof({x.ContainingObject!.FullName}));");
                        ssb.AppendLine($"exp2 = Expression.Parameter(typeof({x.TypeObject!.FullName}));");
                        ssb.AppendLine($"{x.SetterDelegateIdentifier} = Expression.Lambda<Action<{this.LocalName}, {x.TypeObject!.FullName}>>(Expression.Assign(Expression.Field(exp, \"{x.SimpleName}\"), exp2), exp, exp2).CompileFast();");
                    }*/
                }

                if (x.RequiresGetter)
                {
                    if (this == x.ContainingObject)
                    {
                        ssb.AppendLine($"{x.GetterDelegateIdentifier} = Expression.Lambda<Func<{this.LocalName}, {x.TypeObject!.FullName}>>(Expression.PropertyOrField(expType, \"{x.SimpleName}\"), expType).CompileFast();");
                    }
                    else
                    {
                        ssb.AppendLine($"{x.GetterDelegateIdentifier} = Expression.Lambda<Func<{this.LocalName}, {x.TypeObject!.FullName}>>(Expression.PropertyOrField(Expression.Convert(expType, typeof({x.ContainingObject!.FullName})), \"{x.SimpleName}\"), expType).CompileFast();");
                    }

                    /*if (x.Kind == VisceralObjectKind.Property)
                    {// Property
                        ssb.AppendLine($"mi = type.GetMethod(\"get_{x.SimpleName}\")!;");
                        ssb.AppendLine($"exp = Expression.Parameter(typeof({x.TypeObject!.FullName}));");
                        ssb.AppendLine($"{x.GetterDelegateIdentifier} = Expression.Lambda<Func<{this.LocalName}, {x.TypeObject!.FullName}>>(Expression.Call(expType, mi!" +
                            $"), expType).CompileFast();");
                    }
                    else
                    {// Field
                        // ssb.AppendLine($"exp = Expression.Parameter(typeof({x.ContainingObject!.FullName}));");
                        // ssb.AppendLine($"{x.GetterDelegateIdentifier} = Expression.Lambda<Func<{this.LocalName}, {x.TypeObject!.FullName}>>(Expression.Field(exp, \"{x.SimpleName}\")).CompileFast();");

                        ssb.AppendLine($"{x.GetterDelegateIdentifier} = Expression.Lambda<Func<{this.LocalName}, {x.TypeObject!.FullName}>>(Expression.Field(Expression.Convert(expType, typeof({x.ContainingObject!.FullName})), \"{x.SimpleName}\"), expType).CompileFast();");
                    }*/
                }
            }

            ssb.AppendLine("return true;");
        }

        // Delegates
        ssb.AppendLine();
        foreach (var x in array)
        {
            if (x.RequiresSetter)
            {
                if (this.Kind == VisceralObjectKind.Struct)
                {
                    ssb.AppendLine($"private static ByRefAction<{this.LocalName}, {x.TypeObject!.FullName}{x.TypeObject!.QuestionMarkIfReferenceType}>? {x.SetterDelegateIdentifier};");
                }
                else
                {
                    ssb.AppendLine($"private static Action<{this.LocalName}, {x.TypeObject!.FullName}{x.TypeObject!.QuestionMarkIfReferenceType}>? {x.SetterDelegateIdentifier};");
                }
            }

            if (x.RequiresGetter)
            {
                ssb.AppendLine($"private static Func<{this.LocalName}, {x.TypeObject!.FullName}>? {x.GetterDelegateIdentifier};");
            }
        }

        ssb.AppendLine();
    }

    internal void Generate_OnBeforeSerialize(ScopingStringBuilder ssb, GeneratorInformation info)
    {
        if (this.ObjectFlag.HasFlag(TinyhandObjectFlag.HasITinyhandSerializationCallback))
        {
            if (this.ObjectFlag.HasFlag(TinyhandObjectFlag.HasExplicitOnBeforeSerialize))
            {
                ssb.AppendLine($"((Tinyhand.ITinyhandSerializationCallback){ssb.FullObject}).OnBeforeSerialize();");
            }
            else
            {
                ssb.AppendLine($"{ssb.FullObject}.OnBeforeSerialize();");
            }
        }
    }

    internal void Generate_OnAfterDeserialize(ScopingStringBuilder ssb, GeneratorInformation info)
    {
        if (this.ObjectFlag.HasFlag(TinyhandObjectFlag.HasITinyhandSerializationCallback))
        {
            if (this.ObjectFlag.HasFlag(TinyhandObjectFlag.HasExplicitOnAfterDeserialize))
            {
                ssb.AppendLine($"((Tinyhand.ITinyhandSerializationCallback){ssb.FullObject}).OnAfterDeserialize();");
            }
            else
            {
                ssb.AppendLine($"{ssb.FullObject}.OnAfterDeserialize();");
            }
        }
    }

    internal void GenerateSerialize_Method(ScopingStringBuilder ssb, GeneratorInformation info)
    {
        string methodCode;
        string objectCode;

        if (this.MethodCondition_Serialize == MethodCondition.MemberMethod)
        {
            info.GeneratingStaticMethod = false;
            methodCode = "public void Serialize(ref TinyhandWriter writer, TinyhandSerializerOptions options)";
            objectCode = "this";
        }
        else if (this.MethodCondition_Serialize == MethodCondition.StaticMethod)
        {
            info.GeneratingStaticMethod = true;
            methodCode = $"public static void Serialize(ref TinyhandWriter writer, {this.RegionalName} v, TinyhandSerializerOptions options)";
            objectCode = "v";
        }
        else
        {
            return;
        }

        using (var m = ssb.ScopeBrace(methodCode))
        using (var v = ssb.ScopeObject(objectCode))
        {
            // ITinyhandSerializationCallback.OnBeforeSerialize
            this.Generate_OnBeforeSerialize(ssb, info);

            if (this.ObjectFlag.HasFlag(TinyhandObjectFlag.StringKeyObject))
            {// String Key
                this.GenerateSerializerStringKey(ssb, info);
            }
            else
            {// Int Key
                this.GenerateSerializerIntKey(ssb, info);
            }
        }
    }

    internal void GenerateSerialize_Method2(ScopingStringBuilder ssb, GeneratorInformation info)
    {// static abstract
        info.GeneratingStaticMethod = true;
        var methodCode = $"static void ITinyhandSerialize<{this.RegionalName}>.Serialize(ref TinyhandWriter writer, scoped ref {this.RegionalName}{this.QuestionMarkIfReferenceType} v, TinyhandSerializerOptions options)";
        var objectCode = "v";

        using (var m = ssb.ScopeBrace(methodCode))
        using (var v = ssb.ScopeObject(objectCode))
        {
            if (this.Union != null)
            {
                this.Union.GenerateFormatter_Serialize2(ssb, info);
                return;
            }

            if (this.Kind.IsReferenceType())
            {
                using (var scopeNullCheck = ssb.ScopeBrace($"if ({ssb.FullObject} == null)"))
                {
                    ssb.AppendLine("writer.WriteNil();");
                    ssb.AppendLine("return;");
                }

                ssb.AppendLine();
            }

            // LockObject
            IScope? lockScope = null;
            if (!string.IsNullOrEmpty(this.ObjectAttribute?.LockObject))
            {
                ssb.AppendLine($"var {TinyhandBody.LockTaken} = false;");
                lockScope = ssb.ScopeBrace("try");

                if (this.ObjectAttribute!.LockObjectIsLockable)
                {
                    ssb.AppendLine($"{TinyhandBody.LockTaken} = {ssb.FullObject}.{this.ObjectAttribute!.LockObject}!.Enter();");
                }
                else
                {
                    ssb.AppendLine($"System.Threading.Monitor.Enter({ssb.FullObject}.{this.ObjectAttribute!.LockObject}!, ref {TinyhandBody.LockTaken});");
                }
            }

            // ITinyhandSerializationCallback.OnBeforeSerialize
            this.Generate_OnBeforeSerialize(ssb, info);

            if (this.ObjectFlag.HasFlag(TinyhandObjectFlag.StringKeyObject))
            {// String Key
                this.GenerateSerializerStringKey(ssb, info);
            }
            else
            {// Int Key
                this.GenerateSerializerIntKey(ssb, info);
            }

            if (lockScope != null)
            {
                lockScope.Dispose();
                using (var finallyScope = ssb.ScopeBrace("finally"))
                {
                    if (this.ObjectAttribute!.LockObjectIsLockable)
                    {
                        ssb.AppendLine($"if ({TinyhandBody.LockTaken}) {ssb.FullObject}.{this.ObjectAttribute!.LockObject}!.Exit();");
                    }
                    else
                    {
                        ssb.AppendLine($"if ({TinyhandBody.LockTaken}) System.Threading.Monitor.Exit({ssb.FullObject}.{this.ObjectAttribute!.LockObject}!);");
                    }
                }
            }
        }
    }

    /*internal void GenerateFormatter_Serialize(ScopingStringBuilder ssb, GeneratorInformation info)
    {
        if (this.Kind.IsReferenceType())
        {// Reference type
            ssb.AppendLine($"if ({ssb.FullObject} == null) {{ writer.WriteNil(); return; }}");
        }

        if (this.MethodCondition_Serialize == MethodCondition.StaticMethod)
        {// Static method
            ssb.AppendLine($"{this.FullName}.Serialize(ref writer, {ssb.FullObject}, options);");
        }
        else if (this.MethodCondition_Serialize == MethodCondition.ExplicitlyDeclared)
        {// Explicitly declared (Interface.Method())
            ssb.AppendLine($"((ITinyhandSerialize){ssb.FullObject}).Serialize(ref writer, options);");
        }
        else
        {// Member method
            ssb.AppendLine($"{ssb.FullObject}.Serialize(ref writer, options);");
        }
    }*/

    /*internal void GenerateFormatter_DeserializeCore(ScopingStringBuilder ssb, GeneratorInformation info, string name)
    {
        if (this.MethodCondition_Deserialize == MethodCondition.StaticMethod)
        {// Static method
            // ssb.AppendLine($"{this.FullName}.Deserialize{this.GenericsNumberString}(ref {name}, ref reader, options);");
            ssb.AppendLine($"{name}.Deserialize(ref reader, options);");
        }
        else if (this.MethodCondition_Deserialize == MethodCondition.ExplicitlyDeclared)
        {// Explicitly declared (Interface.Method())
            ssb.AppendLine($"((ITinyhandSerialize){name}).Deserialize(ref reader, options);");
        }
        else
        {// Member method
            ssb.AppendLine($"{name}.Deserialize(ref reader, options);");
        }
    }*/

    internal string NewInstanceCode()
    {
        if (this.IsAbstractOrInterface)
        {
            return $"default({this.FullName})";
        }
        else if (this.MinimumConstructor == null)
        {
            if (this.ObjectAttribute?.UseServiceProvider == true)
            {// Service Provider
                return $"({this.FullName})TinyhandSerializer.GetService(typeof({this.FullName}))";
            }
            else
            {// Default constructor. new()
                return "new " + this.FullName + "()";
            }
        }
        else
        {// new(default!, ..., default!)
            var sb = new StringBuilder();
            sb.Append("new ");
            sb.Append(this.FullName);
            sb.Append("(");
            for (var i = 0; i < this.MinimumConstructor.Method_Parameters.Length; i++)
            {
                sb.Append($"({this.MinimumConstructor.Method_Parameters[i]})default!");
                if (i != (this.MinimumConstructor.Method_Parameters.Length - 1))
                {
                    sb.Append($", ");
                }
            }

            sb.Append(")");
            return sb.ToString();
        }
    }

    internal void GenerateFormatter_Deserialize2(ScopingStringBuilder ssb, GeneratorInformation info, string originalName, object? defaultValue, bool reuseInstance)
    {// Called by GenerateDeserializeCore, GenerateDeserializeCore2
        /*if (this.Kind == VisceralObjectKind.Interface)
        {
            if (!reuseInstance)
            {// New Instance
            }
            else
            {// Reuse Instance
            }

            return;
        }*/

        if (this.Kind == VisceralObjectKind.TypeParameter)
        {
            if (!reuseInstance)
            {// New Instance
                ssb.AppendLine($"{this.FullName} v2 = default!;");
            }
            else
            {// Reuse Instance
                ssb.AppendLine($"var v2 = {originalName};");
            }
        }
        else
        {
            if (!reuseInstance)
            {// New Instance
                ssb.AppendLine($"var v2 = {this.NewInstanceCode()};");
            }
            else
            {// Reuse Instance
                if (this.Kind.IsReferenceType())
                {// Reference type
                    ssb.AppendLine($"var v2 = {originalName} ?? {this.NewInstanceCode()};");
                }
                else
                {// Value type
                    ssb.AppendLine($"var v2 = {originalName};");
                }
            }
        }

        if (defaultValue != null)
        {
            if (this.MethodCondition_SetDefaultValue == MethodCondition.Declared)
            {
                ssb.AppendLine($"v2.{TinyhandBody.SetDefaultValueMethod}({VisceralDefaultValue.DefaultValueToString(defaultValue)});");
            }
            else if (this.MethodCondition_SetDefaultValue == MethodCondition.ExplicitlyDeclared)
            {
                ssb.AppendLine($"(({TinyhandBody.ITinyhandDefault})v2).{TinyhandBody.SetDefaultValueMethod}({VisceralDefaultValue.DefaultValueToString(defaultValue)});");
            }
        }

        ssb.AppendLine($"TinyhandSerializer.DeserializeObject(ref reader, ref v2!, options);");
        ssb.AppendLine($"{ssb.FullObject} = v2!;");
    }

    /*internal void GenerateFormatter_ReconstructCore(ScopingStringBuilder ssb, GeneratorInformation info, string name)
    {
        if (this.MethodCondition_Reconstruct == MethodCondition.StaticMethod)
        {// Static method
            ssb.AppendLine($"{this.FullName}.Reconstruct{this.GenericsNumberString}(ref {name}, options);");
        }
        else if (this.MethodCondition_Reconstruct == MethodCondition.ExplicitlyDeclared)
        {// Explicitly declared (Interface.Method())
            ssb.AppendLine($"((ITinyhandReconstruct){name}).Reconstruct(options);");
        }
        else
        {// Member method
            ssb.AppendLine($"{name}.Reconstruct(options);");
        }
    }*/

    internal void GenerateFormatter_Reconstruct2(ScopingStringBuilder ssb, GeneratorInformation info, string originalName, object? defaultValue, bool reuseInstance)
    {// Called by GenerateDeserializeCore, GenerateDeserializeCore2
        if (!reuseInstance)
        {// New Instance
            ssb.AppendLine($"var v2 = {this.NewInstanceCode()};");
        }
        else
        {// Reuse Instance
            if (this.Kind.IsReferenceType())
            {// Reference type
                ssb.AppendLine($"var v2 = {originalName} ?? {this.NewInstanceCode()};");
            }
            else
            {// Value type
                ssb.AppendLine($"var v2 = {originalName};");
            }
        }

        ssb.AppendLine($"TinyhandSerializer.ReconstructObject(ref v2, options);");

        if (defaultValue != null)
        {
            if (this.MethodCondition_SetDefaultValue == MethodCondition.Declared)
            {
                ssb.AppendLine($"v2.{TinyhandBody.SetDefaultValueMethod}({VisceralDefaultValue.DefaultValueToString(defaultValue)});");
            }
            else if (this.MethodCondition_SetDefaultValue == MethodCondition.ExplicitlyDeclared)
            {
                ssb.AppendLine($"(({TinyhandBody.ITinyhandDefault})v2).{TinyhandBody.SetDefaultValueMethod}({VisceralDefaultValue.DefaultValueToString(defaultValue)});");
            }
        }

        ssb.AppendLine($"{ssb.FullObject} = v2!;");
    }

    /*internal void GenerateFormatter_Clone(ScopingStringBuilder ssb, GeneratorInformation info)
    {
        if (this.MethodCondition_Clone == MethodCondition.StaticMethod)
        {// Static method
            ssb.AppendLine($"return {this.FullName}.DeepClone(ref value, options);"); // {this.GenericsNumberString}
        }
        else if (this.MethodCondition_Clone == MethodCondition.ExplicitlyDeclared)
        {// Explicitly declared (Interface.Method())
            ssb.AppendLine($"return (value as ITinyhandClone<{this.FullName}>)?.DeepClone(options);");
        }
        else
        {// Member method
            ssb.AppendLine($"return value{this.QuestionMarkIfReferenceType}.DeepClone(options);");
        }
    }*/

    internal void GenerateDeserialize_Method(ScopingStringBuilder ssb, GeneratorInformation info)
    {
        string methodCode;
        string objectCode;

        if (this.MethodCondition_Deserialize == MethodCondition.MemberMethod)
        {
            info.GeneratingStaticMethod = false;
            methodCode = $"public {this.UnsafeDeserializeString}void Deserialize(ref TinyhandReader reader, TinyhandSerializerOptions options)";
            objectCode = "this";
        }
        else if (this.MethodCondition_Deserialize == MethodCondition.StaticMethod)
        {
            info.GeneratingStaticMethod = true;
            methodCode = $"public static {this.UnsafeDeserializeString}void Deserialize(scoped ref {this.RegionalName} v, ref TinyhandReader reader, TinyhandSerializerOptions options)"; // {this.GenericsNumberString}
            objectCode = "v";
        }
        else
        {
            return;
        }

        using (var m = ssb.ScopeBrace(methodCode))
        using (var v = ssb.ScopeObject(objectCode))
        {
            if (this.ObjectFlag.HasFlag(TinyhandObjectFlag.StringKeyObject))
            {// String Key
                this.GenerateDeserializerStringKey(ssb, info);
            }
            else
            {// Int Key
                this.GenerateDeserializerIntKey(ssb, info);
            }

            // ITinyhandSerializationCallback.OnAfterDeserialize
            this.Generate_OnAfterDeserialize(ssb, info);
        }
    }

    internal void GenerateDeserialize_Method2(ScopingStringBuilder ssb, GeneratorInformation info)
    {
        info.GeneratingStaticMethod = true;
        var methodCode = $"static {this.UnsafeDeserializeString}void ITinyhandSerialize<{this.RegionalName}>.Deserialize(ref TinyhandReader reader, scoped ref {this.RegionalName}{this.QuestionMarkIfReferenceType} v, TinyhandSerializerOptions options)";
        var objectCode = "v";

        using (var m = ssb.ScopeBrace(methodCode))
        using (var v = ssb.ScopeObject(objectCode))
        {
            if (this.Union != null)
            {
                this.Union.GenerateFormatter_Deserialize(ssb, info);
                return;
            }

            if (this.Kind.IsReferenceType())
            {
                using (var scopeNillCheck = ssb.ScopeBrace($"if (reader.TryReadNil())"))
                {
                    ssb.AppendLine("return;");
                }

                ssb.AppendLine();
            }

            // LockObject
            if (!string.IsNullOrEmpty(this.ObjectAttribute?.LockObject))
            {
                this.GenerateDeserialize_LockPrepare(ssb, info);
            }

            if (this.Kind.IsReferenceType())
            {
                ssb.AppendLine($"{ssb.FullObject} ??= {this.NewInstanceCode()};");
            }

            if (this.ObjectFlag.HasFlag(TinyhandObjectFlag.StringKeyObject))
            {// String Key
                this.GenerateDeserializerStringKey(ssb, info);
            }
            else
            {// Int Key
                this.GenerateDeserializerIntKey(ssb, info);
            }

            // ITinyhandSerializationCallback.OnAfterDeserialize
            this.Generate_OnAfterDeserialize(ssb, info);
        }
    }

    internal void GenerateDeserialize_LockPrepare(ScopingStringBuilder ssb, GeneratorInformation info)
    {
        var lockObject = this.ObjectAttribute?.LockObject;
        if (!string.IsNullOrEmpty(lockObject))
        {
            ssb.AppendLine($"var {TinyhandBody.LockObject} = {ssb.FullObject}{(this.Kind.IsReferenceType() ? "?" : string.Empty)}.{lockObject};");
            ssb.AppendLine($"var {TinyhandBody.LockTaken} = false;");
        }
    }

    internal void GenerateDeserialize_LockEnter(ScopingStringBuilder ssb, GeneratorInformation info)
    {
        if (!string.IsNullOrEmpty(this.ObjectAttribute?.LockObject))
        {
            if (this.ObjectAttribute!.LockObjectIsLockable)
            {
                ssb.AppendLine($"if ({TinyhandBody.LockObject} != null) {TinyhandBody.LockTaken} = {TinyhandBody.LockObject}.Enter();");
            }
            else
            {
                ssb.AppendLine($"if ({TinyhandBody.LockObject} != null) System.Threading.Monitor.Enter({TinyhandBody.LockObject}, ref {TinyhandBody.LockTaken});");
            }
        }
    }

    internal void GenerateDeserialize_LockExit(ScopingStringBuilder ssb, GeneratorInformation info)
    {
        if (!string.IsNullOrEmpty(this.ObjectAttribute?.LockObject))
        {
            if (this.ObjectAttribute!.LockObjectIsLockable)
            {
                ssb.AppendLine($"if ({TinyhandBody.LockTaken}) {TinyhandBody.LockObject}!.Exit();");
            }
            else
            {
                ssb.AppendLine($"if ({TinyhandBody.LockTaken}) System.Threading.Monitor.Exit({TinyhandBody.LockObject}!);");
            }
        }
    }

    internal void GenerateReconstructRemaining(ScopingStringBuilder ssb, GeneratorInformation info)
    {
        foreach (var x in this.Members.Where(x => x.ReconstructState == ReconstructState.Do && x.KeyAttribute == null))
        {
            this.GenerateReconstructCore(ssb, info, x);
        }
    }

    internal void GenerateReconstruct_Method(ScopingStringBuilder ssb, GeneratorInformation info)
    {
        string methodCode;
        string objectCode;

        if (this.MethodCondition_Reconstruct == MethodCondition.MemberMethod)
        {
            info.GeneratingStaticMethod = false;
            methodCode = $"public {this.UnsafeDeserializeString}void Reconstruct(TinyhandSerializerOptions options)";
            objectCode = "this";
        }
        else if (this.MethodCondition_Reconstruct == MethodCondition.StaticMethod)
        {
            info.GeneratingStaticMethod = true;
            methodCode = $"public static {this.UnsafeDeserializeString}void Reconstruct(ref {this.RegionalName} v, TinyhandSerializerOptions options)"; // {this.GenericsNumberString}
            objectCode = "v";
        }
        else
        {
            return;
        }

        using (var m = ssb.ScopeBrace(methodCode))
        using (var v = ssb.ScopeObject(objectCode))
        {
            foreach (var x in this.Members)
            {
                this.GenerateReconstructCore(ssb, info, x);
            }
        }
    }

    internal void GenerateReconstruct_Method2(ScopingStringBuilder ssb, GeneratorInformation info)
    {
        info.GeneratingStaticMethod = true;
        var methodCode = $"static {this.UnsafeDeserializeString}void ITinyhandReconstruct<{this.RegionalName}>.Reconstruct([NotNull] scoped ref {this.RegionalName}{this.QuestionMarkIfReferenceType} v, TinyhandSerializerOptions options)";
        var objectCode = "v";

        using (var m = ssb.ScopeBrace(methodCode))
        using (var v = ssb.ScopeObject(objectCode))
        {
            if (this.Union != null)
            {
                ssb.AppendLine("throw new TinyhandException(\"Reconstruct() is not supported in abstract class or interface.\");");
                return;
            }

            if (this.Kind.IsReferenceType())
            {
                ssb.AppendLine($"{ssb.FullObject} ??= {this.NewInstanceCode()};");
            }

            foreach (var x in this.Members)
            {
                this.GenerateReconstructCore(ssb, info, x);
            }
        }
    }

    /*internal void GenerateClone_Method(ScopingStringBuilder ssb, GeneratorInformation info)
    {
        string methodCode;
        string sourceObject;

        if (this.MethodCondition_Clone == MethodCondition.MemberMethod)
        {
            info.GeneratingStaticMethod = false;
            methodCode = $"public {this.UnsafeDeserializeString}{this.FullName} DeepClone(TinyhandSerializerOptions options)";
            sourceObject = "this";
        }
        else if (this.MethodCondition_Clone == MethodCondition.StaticMethod)
        {
            info.GeneratingStaticMethod = true;
            methodCode = $"public static {this.UnsafeDeserializeString}{this.FullName + this.QuestionMarkIfReferenceType} DeepClone(ref {this.RegionalName + this.QuestionMarkIfReferenceType} v, TinyhandSerializerOptions options)"; // {this.GenericsNumberString}
            sourceObject = "v";
        }
        else
        {
            return;
        }

        using (var m = ssb.ScopeBrace(methodCode))
        using (var v = ssb.ScopeObject("value"))
        {// this.x = value.x;
            if (this.MethodCondition_Clone == MethodCondition.StaticMethod && this.Kind.IsReferenceType())
            {
                ssb.AppendLine($"if (v == null) return null;");
            }

            ssb.AppendLine($"var value = {this.NewInstanceCode()};");
            foreach (var x in this.MembersWithFlag(TinyhandObjectFlag.CloneTarget))
            {
                string sourceName;
                if (x.RequiresGetter)
                {
                    var prefix = info.GeneratingStaticMethod ? (this.RegionalName + ".") : string.Empty;
                    sourceName = $"{prefix}{x.GetterDelegateIdentifier}!({sourceObject})";
                }
                else
                {
                    sourceName = this.GetSourceName(sourceObject, x);
                }

                this.GenerateCloneCore(ssb, info, x, sourceName);
            }

            ssb.AppendLine($"return value;");
        }
    }*/

    internal void GenerateClone_Method2(ScopingStringBuilder ssb, GeneratorInformation info)
    {
        info.GeneratingStaticMethod = true;
        var methodCode = $"static {this.UnsafeDeserializeString}{this.RegionalName}{this.QuestionMarkIfReferenceType} ITinyhandClone<{this.RegionalName}>.Clone(scoped ref {this.RegionalName}{this.QuestionMarkIfReferenceType} v, TinyhandSerializerOptions options)";
        var sourceObject = "v";

        using (var m = ssb.ScopeBrace(methodCode))
        using (var v = ssb.ScopeObject("value"))
        {// this.x = value.x;
            if (this.Union != null)
            {
                ssb.AppendLine("throw new TinyhandException(\"Clone() is not supported in abstract class or interface.\");");
                return;
            }

            if (this.Kind.IsReferenceType())
            {
                ssb.AppendLine($"if (v == null) return null;");
            }

            ssb.AppendLine($"var value = {this.NewInstanceCode()};");
            foreach (var x in this.MembersWithFlag(TinyhandObjectFlag.CloneTarget))
            {
                string sourceName;
                if (x.RequiresGetter)
                {
                    var prefix = info.GeneratingStaticMethod ? (this.RegionalName + ".") : string.Empty;
                    sourceName = $"{prefix}{x.GetterDelegateIdentifier}!({sourceObject})";
                }
                else
                {// Hidden members
                    sourceName = this.GetSourceName(sourceObject, x);
                }

                this.GenerateCloneCore(ssb, info, x, sourceName);
            }

            ssb.AppendLine($"return value;");
        }
    }

    internal void GenerateAddProperty(ScopingStringBuilder ssb, GeneratorInformation info)
    {
        foreach (var x in this.MembersWithFlag(TinyhandObjectFlag.SerializeTarget).Where(a => !string.IsNullOrEmpty(a.KeyAttribute?.PropertyName)))
        {
            if (x.TypeObjectWithNullable is not { } withNullable ||
                x.ContainingObject != this)
            {
                continue;
            }

            string setterAccessibility = string.Empty;
            if (x.KeyAttribute!.PropertyAccessibility == PropertyAccessibility.ProtectedSetter)
            {
                setterAccessibility = "protected ";
            }

            using (var m = ssb.ScopeBrace($"public {withNullable.FullNameWithNullable} {x.KeyAttribute!.PropertyName}"))
            using (var scopeObject = ssb.ScopeFullObject($"this.{x.SimpleName}"))
            {
                ssb.AppendLine($"get => {ssb.FullObject};");
                if (x.MaxLengthAttribute == null)
                {
                    ssb.AppendLine($"{setterAccessibility}set => {ssb.FullObject} = value;");
                }
                else
                {
                    using (var m2 = ssb.ScopeBrace($"{setterAccessibility}set"))
                    {
                        this.GenerateAddProperty_Setter(ssb, info, x, x.MaxLengthAttribute);
                    }
                }
            }
        }
    }

    internal void GenerateAddProperty_Setter(ScopingStringBuilder ssb, GeneratorInformation info, TinyhandObject x, MaxLengthAttributeMock attribute)
    {
        ssb.AppendLine($"{ssb.FullObject} = value;");
        if (x.TypeObject is not { } typeObject)
        {
            return;
        }

        if (typeObject.FullName == "string")
        {// string
            if (attribute.MaxLength >= 0)
            {
                using (var scopeIf = ssb.ScopeBrace($"if ({ssb.FullObject}.Length > {attribute.MaxLength})"))
                {// text = text.Substring(0, MaxLength);
                    ssb.AppendLine($"{ssb.FullObject} = {ssb.FullObject}.Substring(0, {attribute.MaxLength});");
                }
            }
        }
        else if (typeObject.Array_Rank == 1)
        {// T[]
            if (attribute.MaxLength >= 0)
            {
                using (var scopeIf = ssb.ScopeBrace($"if ({ssb.FullObject}.Length > {attribute.MaxLength})"))
                {// array = array[..MaxLength];
                    ssb.AppendLine($"{ssb.FullObject} = {ssb.FullObject}[..{attribute.MaxLength}];");
                }
            }

            if (attribute.MaxChildLength >= 0)
            {
                if (typeObject.Array_Element?.FullName == "string")
                {// string[]
                    using (var scopeFor = ssb.ScopeBrace($"for (var i = 0; i < {ssb.FullObject}.Length; i++)"))
                    {
                        using (var scopeIf = ssb.ScopeBrace($"if ({ssb.FullObject}[i].Length > {attribute.MaxChildLength})"))
                        {// text = text.Substring(0, MaxLength);
                            ssb.AppendLine($"{ssb.FullObject}[i] = {ssb.FullObject}[i].Substring(0, {attribute.MaxChildLength});");
                        }
                    }
                }
                else if (typeObject.Array_Element?.Array_Rank == 1)
                {// T[][]
                    using (var scopeFor = ssb.ScopeBrace($"for (var i = 0; i < {ssb.FullObject}.Length; i++)"))
                    {
                        using (var scopeIf = ssb.ScopeBrace($"if ({ssb.FullObject}[i].Length > {attribute.MaxChildLength})"))
                        {
                            ssb.AppendLine($"{ssb.FullObject}[i] = {ssb.FullObject}[i][..{attribute.MaxChildLength}];");
                        }
                    }
                }
            }
        }
        else if (typeObject.Generics_Kind == VisceralGenericsKind.ClosedGeneric &&
            typeObject.OriginalDefinition is { } baseObject &&
            baseObject.FullName == "System.Collections.Generic.List<T>" &&
            typeObject.Generics_Arguments.Length == 1)
        {// List<T>
            if (attribute.MaxLength >= 0)
            {
                using (var scopeIf = ssb.ScopeBrace($"if ({ssb.FullObject}.Count > {attribute.MaxLength})"))
                {// list = list.GetRange(0, MaxLength);
                    ssb.AppendLine($"{ssb.FullObject} = {ssb.FullObject}.GetRange(0, {attribute.MaxLength});");
                }
            }

            if (attribute.MaxChildLength >= 0)
            {
                if (typeObject.Generics_Arguments[0].FullName == "string")
                {// List<string>
                    using (var scopeFor = ssb.ScopeBrace($"for (var i = 0; i < {ssb.FullObject}.Count; i++)"))
                    {
                        using (var scopeIf = ssb.ScopeBrace($"if ({ssb.FullObject}[i].Length > {attribute.MaxChildLength})"))
                        {// text = text.Substring(0, MaxLength);
                            ssb.AppendLine($"{ssb.FullObject}[i] = {ssb.FullObject}[i].Substring(0, {attribute.MaxChildLength});");
                        }
                    }
                }
                else if (typeObject.Generics_Arguments[0].Array_Rank == 1)
                {// List<T[]>
                    using (var scopeFor = ssb.ScopeBrace($"for (var i = 0; i < {ssb.FullObject}.Count; i++)"))
                    {
                        using (var scopeIf = ssb.ScopeBrace($"if ({ssb.FullObject}[i].Length > {attribute.MaxChildLength})"))
                        {
                            ssb.AppendLine($"{ssb.FullObject}[i] = {ssb.FullObject}[i][..{attribute.MaxChildLength}];");
                        }
                    }
                }
            }
        }
    }

    internal void GenerateMemberNotNull_Attribute(ScopingStringBuilder ssb, GeneratorInformation info)
    {
        var firstFlag = true;
        foreach (var x in this.Members.Where(x => x.ReconstructState == ReconstructState.Do && x.ContainingObject == this))
        {// [MemberNotNull(nameof(A), nameof(B)]
            if (firstFlag)
            {
                ssb.Append("[MemberNotNull(nameof(");
                ssb.Append(x.SimpleName, false);
                firstFlag = false;
            }
            else
            {
                ssb.Append("), nameof(", false);
                ssb.Append(x.SimpleName, false);
            }
        }

        if (!firstFlag)
        {
            ssb.AppendLine("))]", false);
        }
    }

    internal void GenerateMemberNotNull_MemberMethod(ScopingStringBuilder ssb, GeneratorInformation info)
    {
        if (this.GetMembers(VisceralTarget.Method).Any(x => x.Method_Parameters.Length == 0 && x.SimpleName == "MemberNotNull"))
        {// MemberNotNull() already exists.
            return;
        }

        this.GenerateMemberNotNull_Attribute(ssb, info);
        using (var m = ssb.ScopeBrace($"public void MemberNotNull()"))
        {
        }
    }

    internal void GenerateMemberNotNull_StaticMethod(ScopingStringBuilder ssb, GeneratorInformation info)
    {
        this.GenerateMemberNotNull_Attribute(ssb, info);
        using (var m = ssb.ScopeBrace($"public static void MemberNotNull()"))
        {
        }
    }

    internal void GenerateMethod(ScopingStringBuilder ssb, GeneratorInformation info)
    {
        // Serialize/Deserialize/Reconstruct/Clone
        /*this.GenerateSerialize_Method(ssb, info);
        this.GenerateDeserialize_Method(ssb, info);
        this.GenerateReconstruct_Method(ssb, info);
        this.GenerateClone_Method(ssb, info);*/

        // Serialize/Deserialize/Reconstruct/Clone
        if (this.Generics_Kind != VisceralGenericsKind.ClosedGeneric)
        {
            if (this.MethodCondition_Serialize == MethodCondition.StaticMethod)
            {
                this.GenerateSerialize_Method2(ssb, info);
            }

            if (this.MethodCondition_Deserialize == MethodCondition.StaticMethod)
            {
                this.GenerateDeserialize_Method2(ssb, info);
            }

            if (this.MethodCondition_Reconstruct == MethodCondition.StaticMethod)
            {
                this.GenerateReconstruct_Method2(ssb, info);
            }

            if (this.MethodCondition_Clone == MethodCondition.StaticMethod)
            {
                this.GenerateClone_Method2(ssb, info);
            }

            // ITinyhandSerialize
            ssb.AppendLine("void ITinyhandSerialize.Deserialize(ref TinyhandReader reader, TinyhandSerializerOptions options)");
            ssb.AppendLine("    => TinyhandSerializer.DeserializeObject(ref reader, ref Unsafe.AsRef(this)!, options);");
            ssb.AppendLine("void ITinyhandSerialize.Serialize(ref TinyhandWriter writer, TinyhandSerializerOptions options)");
            ssb.AppendLine("    => TinyhandSerializer.SerializeObject(ref writer, this, options);");
        }

        this.GenerateAddProperty(ssb, info);

        return;
    }

    internal void PrepareAutomata()
    {
        if (this.Automata == null)
        {
            return;
        }

        var count = 0;
        foreach (var x in this.Automata.NodeList)
        {
            x.ReconstructIndex = -1;
            if (x.Member == null)
            {
                continue;
            }

            if (x.Member.NullableAnnotationIfReferenceType == Arc.Visceral.NullableAnnotation.NotAnnotated ||
                x.Member.Kind.IsValueType() ||
                x.Member.IsDefaultable ||
                x.Member.ReconstructState == ReconstructState.Do)
            {
                x.ReconstructIndex = count++;
            }
        }

        this.Automata.ReconstructCount = count;
    }

    internal void GenerateConstructor_Method(ScopingStringBuilder ssb, GeneratorInformation info)
    {
        // Array
        var array = this.MembersWithFlag(TinyhandObjectFlag.SerializeTarget).ToArray();
        if (array.Length == 0)
        {
            return;
        }

        // Check
        foreach (var x in this.GetMembers(VisceralTarget.Method).Where(x => x.Method_IsConstructor))
        {
            if (x.Method_Parameters.SequenceEqual(array.Select(y => y.TypeObject!.FullName)))
            {// Constructor with the same parameters found.
                return;
            }
        }

        if (this.Kind == VisceralObjectKind.Class)
        {
            if (!this.GetMembers(VisceralTarget.Method).Any(a => a.Method_IsConstructor && a.Method_Parameters.Length == 0 && a.symbol?.IsImplicitlyDeclared != true))
            {// No explicit default constructor
                using (var method = ssb.ScopeBrace($"public {this.SimpleName}()"))
                {
                }
            }
        }

        this.GenerateConstructorCore(ssb, info, true, array);
    }

    internal void GenerateConstructorCore(ScopingStringBuilder ssb, GeneratorInformation info, bool isConstructor, TinyhandObject[] array)
    {
        // Name
        var sb = new StringBuilder();
        if (isConstructor)
        {
            sb.Append($"public {this.SimpleName}");
        }
        else
        {
            sb.Append($"public void {TinyhandBody.SetMembersMethod}");
        }

        sb.Append("(");
        for (var n = 0; n < array.Length; n++)
        {
            var withNullable = array[n].TypeObjectWithNullable!;
            sb.Append(withNullable.Object.FullName);
            sb.Append(" v");
            sb.Append(n.ToString());
            if (n < array.Length - 1)
            {
                sb.Append(", ");
            }
        }

        sb.Append(")");

        if (isConstructor)
        {
            sb.Append(" : this()");
        }

        // Method
        using (var method = ssb.ScopeBrace(sb.ToString()))
        {
            for (var n = 0; n < array.Length; n++)
            {
                ssb.AppendLine($"this.{array[n].SimpleName} = v{n};");
            }
        }
    }

    internal void GenerateSetMembers_Method(ScopingStringBuilder ssb, GeneratorInformation info)
    {
        // Array
        var array = this.MembersWithFlag(TinyhandObjectFlag.SerializeTarget).Where(x => !x.IsInitOnly).ToArray();
        if (array.Length == 0)
        {
            return;
        }

        // Check
        foreach (var x in this.GetMembers(VisceralTarget.Method).Where(x => x.SimpleName == TinyhandBody.SetMembersMethod))
        {
            if (x.Method_Parameters.SequenceEqual(array.Select(y => y.FullName)))
            {// SetMembers with the same parameters found.
                return;
            }
        }

        this.GenerateConstructorCore(ssb, info, false, array);
    }

    internal IScope ScopeMember(ScopingStringBuilder ssb, TinyhandObject x)
    {// ssb.ScopeObject(x.SimpleNameOrAddedProperty) -> this.ScopeMember(ssb, x)
        if (x.ObjectFlag.HasFlag(TinyhandObjectFlag.HiddenMember) &&
            x.ContainingObject is not null)
        {// ((BaseClass)v).Member
            var name = $"(({x.ContainingObject.SimpleName}){ssb.FullObject}).{x.SimpleNameOrAddedProperty}";
            return ssb.ScopeFullObject(name);
        }
        else
        {// v.Member
            return ssb.ScopeObject(x.SimpleNameOrAddedProperty);
        }
    }

    internal string GetSourceName(string sourceObject, TinyhandObject x)
    {// ssb.ScopeObject(x.SimpleNameOrAddedProperty) -> this.ScopeMember(ssb, x)
        if (x.ObjectFlag.HasFlag(TinyhandObjectFlag.HiddenMember) &&
            x.ContainingObject is not null)
        {// ((BaseClass)v).Member
            return $"(({x.ContainingObject.SimpleName}){sourceObject}).{x.SimpleNameOrAddedProperty}";
        }
        else
        {// v.Member
            return sourceObject + "." + x.SimpleNameOrAddedProperty;
        }
    }

    internal void GenerateDeserializeCore(ScopingStringBuilder ssb, GeneratorInformation info, TinyhandObject? x)
    {// Integer key
        var withNullable = x?.TypeObjectWithNullable;
        if (x == null || withNullable == null)
        {// no object
            ssb.AppendLine("if (numberOfData-- > 0) reader.Skip();");
            return;
        }

        ScopingStringBuilder.IScope? initSetter = null;
        var destObject = ssb.FullObject; // Hidden members
        using (var m = this.ScopeMember(ssb, x))
        {
            var originalName = ssb.FullObject;
            var coder = CoderResolver.Instance.TryGetCoder(withNullable);
            using (var valid = ssb.ScopeBrace($"if (numberOfData-- > 0 && !reader.TryReadNil())"))
            {
                InitSetter_Start();

                if (withNullable.Object.ObjectAttribute?.UseResolver == false &&
                    (withNullable.Object.ObjectAttribute != null || withNullable.Object.HasITinyhandSerializeConstraint()))
                {// TinyhandObject. For the purpose of default value and instance reuse.
                    withNullable.Object.GenerateFormatter_Deserialize2(ssb, info, originalName, x.DefaultValue, x.ObjectFlag.HasFlag(TinyhandObjectFlag.ReuseInstanceTarget));
                }
                else if (coder != null)
                {
                    coder.CodeDeserializer(ssb, info, true);
                }
                else
                {
                    if (x.TypeObject != null && (x.TypeObject.Kind != VisceralObjectKind.Error && x.TypeObject.Kind != VisceralObjectKind.TypeParameter))
                    {
                        this.Body.ReportDiagnostic(TinyhandBody.Warning_NoCoder, x.Location, withNullable.FullName);
                    }

                    if (x.HasNullableAnnotation || withNullable.Object.Kind.IsValueType() || x.TypeObject?.IsTypeParameterWithValueTypeConstraint() == true)
                    {// T?
                        ssb.AppendLine($"{ssb.FullObject} = options.Resolver.GetFormatter<{withNullable.Object.FullName}>().Deserialize(ref reader, options);");
                    }
                    else
                    {// T
                        ssb.AppendLine($"var f = options.Resolver.GetFormatter<{withNullable.Object.FullName}>();");
                        ssb.AppendLine($"{ssb.FullObject} = f.Deserialize(ref reader, options) ?? f.Reconstruct(options);");
                    }
                }

                InitSetter_End();
            }

            if (x!.IsDefaultable)
            {// Default
                using (var invalid = ssb.ScopeBrace("else"))
                {
                    InitSetter_Start();
                    ssb.AppendLine($"{ssb.FullObject} = {VisceralDefaultValue.DefaultValueToString(x.DefaultValue)};");
                    InitSetter_End();
                }
            }
            else if (x.ReconstructState == ReconstructState.Do)
            {
                using (var invalid = ssb.ScopeBrace("else"))
                {
                    InitSetter_Start();
                    if (withNullable.Object.ObjectAttribute != null)
                    {// TinyhandObject. For the purpose of default value and instance reuse.
                        withNullable.Object.GenerateFormatter_Reconstruct2(ssb, info, originalName, x.DefaultValue, x.ObjectFlag.HasFlag(TinyhandObjectFlag.ReuseInstanceTarget));
                    }
                    else if (coder != null)
                    {
                        coder.CodeReconstruct(ssb, info);
                    }
                    else
                    {
                        ssb.AppendLine($"{ssb.FullObject} = options.Resolver.GetFormatter<{withNullable.Object.FullName}>().Reconstruct(options);");
                    }

                    InitSetter_End();
                }
            }
        }

        void InitSetter_Start()
        {
            if (x.SetterDelegateIdentifier != null || x.IsReadOnly)
            {// TypeName vd;
                initSetter = ssb.ScopeFullObject("vd");
                ssb.AppendLine(withNullable.FullNameWithNullable + " vd;");
            }
        }

        void InitSetter_End()
        {
            if (initSetter != null)
            {
                initSetter.Dispose();
                initSetter = null;

                if (x.SetterDelegateIdentifier != null)
                {// SetterDelegate!(obj, vd);
                    var prefix = info.GeneratingStaticMethod ? (this.RegionalName + ".") : string.Empty;
                    ssb.AppendLine($"{prefix}{x.SetterDelegateIdentifier}!({this.InIfStruct}{destObject}, vd);");
                }
                else if (x.IsReadOnly)
                {
                    if (withNullable.Object.IsUnmanagedType)
                    {// fixed (ulong* ptr = &this.Id0) *ptr = 11;
                        ssb.AppendLine($"fixed ({withNullable.FullNameWithNullable}* ptr = &{this.GetSourceName(destObject, x)}) *ptr = vd;"); // {destObject}.{x.SimpleName}
                    }
                    else
                    {// Unsafe.AsRef({this.array) = vd;
                        ssb.AppendLine($"Unsafe.AsRef({this.GetSourceName(destObject, x)}) = vd;"); // {destObject}.{x.SimpleName}
                    }
                }
            }
        }
    }

    internal void GenerateDeserializeCore2(ScopingStringBuilder ssb, GeneratorInformation info, TinyhandObject? x)
    {// String key
        var withNullable = x?.TypeObjectWithNullable;
        if (x == null || withNullable == null)
        {// no object
            ssb.GotoSkipLabel();
            return;
        }

        ScopingStringBuilder.IScope? initSetter = null;
        var destObject = ssb.FullObject; // Hidden members
        using (var m = this.ScopeMember(ssb, x))
        {
            var originalName = ssb.FullObject;
            var coder = CoderResolver.Instance.TryGetCoder(withNullable);
            using (var valid = ssb.ScopeBrace($"if (!reader.TryReadNil())"))
            {
                InitSetter_Start();

                if (withNullable.Object.ObjectAttribute?.UseResolver == false &&
                    (withNullable.Object.ObjectAttribute != null || withNullable.Object.HasITinyhandSerializeConstraint()))
                {
                    withNullable.Object.GenerateFormatter_Deserialize2(ssb, info, originalName, x.DefaultValue, x.ObjectFlag.HasFlag(TinyhandObjectFlag.ReuseInstanceTarget));
                }
                else if (coder != null)
                {
                    coder.CodeDeserializer(ssb, info, true);
                }
                else
                {
                    if (x.TypeObject != null && (x.TypeObject.Kind != VisceralObjectKind.Error && x.TypeObject.Kind != VisceralObjectKind.TypeParameter))
                    {
                        this.Body.ReportDiagnostic(TinyhandBody.Warning_NoCoder, x.Location, withNullable.FullName);
                    }

                    if (x.HasNullableAnnotation || withNullable.Object.Kind.IsValueType() || x.TypeObject?.IsTypeParameterWithValueTypeConstraint() == true)
                    {// T?
                        ssb.AppendLine($"{ssb.FullObject} = options.Resolver.GetFormatter<{withNullable.Object.FullName}>().Deserialize(ref reader, options);");
                    }
                    else
                    {// T
                        ssb.AppendLine($"var f = options.Resolver.GetFormatter<{withNullable.Object.FullName}>();");
                        ssb.AppendLine($"{ssb.FullObject} = f.Deserialize(ref reader, options) ?? f.Reconstruct(options);");
                    }
                }

                InitSetter_End();
            }

            if (x!.IsDefaultable)
            {// Default
                using (var invalid = ssb.ScopeBrace("else"))
                {
                    InitSetter_Start();
                    ssb.AppendLine($"{ssb.FullObject} = {VisceralDefaultValue.DefaultValueToString(x.DefaultValue)};");

                    InitSetter_End();
                }
            }
            else if (x.ReconstructState == ReconstructState.Do)
            {
                using (var invalid = ssb.ScopeBrace("else"))
                {
                    InitSetter_Start();
                    if (withNullable.Object.ObjectAttribute != null)
                    {// TinyhandObject. For the purpose of default value and instance reuse.
                        withNullable.Object.GenerateFormatter_Reconstruct2(ssb, info, originalName, x.DefaultValue, x.ObjectFlag.HasFlag(TinyhandObjectFlag.ReuseInstanceTarget));
                    }
                    else if (coder != null)
                    {
                        coder.CodeReconstruct(ssb, info);
                    }
                    else
                    {
                        ssb.AppendLine($"{ssb.FullObject} = options.Resolver.GetFormatter<{withNullable.Object.FullName}>().Reconstruct(options);");
                    }

                    InitSetter_End();
                }
            }
        }

        void InitSetter_Start()
        {
            if (x.SetterDelegateIdentifier != null || x.IsReadOnly)
            {// TypeName vd;
                initSetter = ssb.ScopeFullObject("vd");
                ssb.AppendLine(withNullable.FullNameWithNullable + " vd;");
            }
        }

        void InitSetter_End()
        {
            if (initSetter != null)
            {
                initSetter.Dispose();
                initSetter = null;

                if (x.SetterDelegateIdentifier != null)
                {// SetterDelegate!(obj, vd);
                    var prefix = info.GeneratingStaticMethod ? (this.RegionalName + ".") : string.Empty;
                    ssb.AppendLine($"{prefix}{x.SetterDelegateIdentifier}!({this.InIfStruct}{destObject}, vd);");
                }
                else if (x.IsReadOnly)
                {
                    if (withNullable.Object.IsUnmanagedType)
                    {// fixed (ulong* ptr = &this.Id0) *ptr = 11;
                        ssb.AppendLine($"fixed ({withNullable.FullNameWithNullable}* ptr = &{this.GetSourceName(destObject, x)}) *ptr = vd;"); // {destObject}.{x.SimpleName}
                    }
                    else
                    {// Unsafe.AsRef({this.array) = vd;
                        ssb.AppendLine($"Unsafe.AsRef({this.GetSourceName(destObject, x)}) = vd;"); // {destObject}.{x.SimpleName}
                    }
                }
            }
        }
    }

    internal void GenerateReconstructCore(ScopingStringBuilder ssb, GeneratorInformation info, TinyhandObject x)
    {// Called by GenerateReconstruct()
        var withNullable = x?.TypeObjectWithNullable;
        if (x == null || withNullable == null)
        {// no object
            return;
        }

        ScopingStringBuilder.IScope? initSetter = null;
        ScopingStringBuilder.IScope? emptyBrace = null;
        var destObject = ssb.FullObject; // Hidden members

        using (var c2 = this.ScopeMember(ssb, x))
        {
            var originalName = ssb.FullObject;
            if (x.IsDefaultable)
            {// Default
                InitSetter_Start(true);
                ssb.AppendLine($"{ssb.FullObject} = {VisceralDefaultValue.DefaultValueToString(x.DefaultValue)};");
                InitSetter_End();
                return;
            }

            if (x.ReconstructState != ReconstructState.Do)
            {
                return;
            }

            // var nullCheckCode = withNullable.Object.Kind.IsReferenceType() && !x.ObjectFlag.HasFlag(TinyhandObjectFlag.ReuseInstanceTarget) ? $"if ({ssb.FullObject} == null)" : string.Empty;

            if (withNullable.Object.ObjectAttribute != null &&
                withNullable.Object.ObjectAttribute.UseResolver == false)
            {// TinyhandObject. For the purpose of default value and instance reuse.
                using (var c = ssb.ScopeBrace(string.Empty))
                {
                    InitSetter_Start();
                    withNullable.Object.GenerateFormatter_Reconstruct2(ssb, info, originalName, x.DefaultValue, x.ObjectFlag.HasFlag(TinyhandObjectFlag.ReuseInstanceTarget));
                    InitSetter_End();
                }
            }
            else if (CoderResolver.Instance.TryGetCoder(withNullable) is { } coder)
            {// Coder
                using (var c = ssb.ScopeBrace(string.Empty))
                {
                    InitSetter_Start();
                    coder.CodeReconstruct(ssb, info);
                    InitSetter_End();
                }
            }
            else
            {// Default constructor
                InitSetter_Start(true);
                ssb.AppendLine($"{ssb.FullObject} = {withNullable.Object.NewInstanceCode()};");
                InitSetter_End();
            }
        }

        void InitSetter_Start(bool brace = false)
        {
            if (x.SetterDelegateIdentifier != null || x.IsReadOnly)
            {// TypeName vd;
                if (brace)
                {
                    emptyBrace = ssb.ScopeBrace(string.Empty);
                }

                initSetter = ssb.ScopeFullObject("vd");
                ssb.AppendLine(withNullable.FullNameWithNullable + " vd;");
            }
        }

        void InitSetter_End()
        {
            if (initSetter != null)
            {
                initSetter.Dispose();
                initSetter = null;

                if (x.SetterDelegateIdentifier != null)
                {// SetterDelegate!(obj, vd);
                    var prefix = info.GeneratingStaticMethod ? (this.RegionalName + ".") : string.Empty;
                    ssb.AppendLine($"{prefix}{x.SetterDelegateIdentifier}!({this.InIfStruct}{destObject}, vd);");
                }
                else if (x.IsReadOnly)
                {
                    if (withNullable.Object.IsUnmanagedType)
                    {// fixed (ulong* ptr = &this.Id0) *ptr = 11;
                        ssb.AppendLine($"fixed ({withNullable.FullNameWithNullable}* ptr = &{destObject}.{x.SimpleName}) *ptr = vd;");
                    }
                    else
                    {// Unsafe.AsRef({this.array) = vd;
                        ssb.AppendLine($"Unsafe.AsRef({destObject}.{x.SimpleName}) = vd;");
                    }
                }

                if (emptyBrace != null)
                {
                    emptyBrace.Dispose();
                    emptyBrace = null;
                }
            }
        }
    }

    internal void GenerateReconstructCore2(ScopingStringBuilder ssb, GeneratorInformation info, TinyhandObject x, int reconstructIndex)
    {// Called by Automata
        var withNullable = x?.TypeObjectWithNullable;
        if (x == null || withNullable == null)
        {// no object
            return;
        }

        ScopingStringBuilder.IScope? initSetter = null;
        var destObject = ssb.FullObject;

        using (var c = ssb.ScopeObject(x.SimpleNameOrAddedProperty))
        {
            var originalName = ssb.FullObject;
            if (x.IsDefaultable)
            {// Default
                using (var conditionDeserialized = ssb.ScopeBrace($"if (!deserializedFlag[{reconstructIndex}])"))
                {
                    InitSetter_Start();
                    ssb.AppendLine($"{ssb.FullObject} = {VisceralDefaultValue.DefaultValueToString(x.DefaultValue)};");
                    InitSetter_End();
                }

                return;
            }

            // var nullCheckCode = withNullable.Object.Kind.IsReferenceType() && !x.ObjectFlag.HasFlag(TinyhandObjectFlag.ReuseInstanceTarget) ? $"if (!deserializedFlag[{reconstructIndex}] && {ssb.FullObject} == null)" : $"if (!deserializedFlag[{reconstructIndex}])";
            var nullCheckCode = $"if (!deserializedFlag[{reconstructIndex}])";

            using (var conditionDeserialized = ssb.ScopeBrace(nullCheckCode))
            {
                InitSetter_Start();

                if (x.NullableAnnotationIfReferenceType == Arc.Visceral.NullableAnnotation.NotAnnotated || x.ReconstructState == ReconstructState.Do)
                {// T
                    if (withNullable.Object.ObjectAttribute != null &&
                        withNullable.Object.ObjectAttribute.UseResolver == false)
                    {// TinyhandObject. For the purpose of default value and instance reuse.
                        withNullable.Object.GenerateFormatter_Reconstruct2(ssb, info, originalName, x.DefaultValue, x.ObjectFlag.HasFlag(TinyhandObjectFlag.ReuseInstanceTarget));
                    }
                    else if (CoderResolver.Instance.TryGetCoder(withNullable) is { } coder)
                    {
                        coder.CodeReconstruct(ssb, info);
                    }
                    else
                    {// Default constructor
                        ssb.AppendLine($"{ssb.FullObject} = {withNullable.Object.NewInstanceCode()};");
                    }
                }

                InitSetter_End();
            }
        }

        void InitSetter_Start()
        {
            if (x.SetterDelegateIdentifier != null || x.IsReadOnly)
            {// TypeName vd;
                initSetter = ssb.ScopeFullObject("vd");
                ssb.AppendLine(withNullable.FullNameWithNullable + " vd;");
            }
        }

        void InitSetter_End()
        {
            if (initSetter != null)
            {
                initSetter.Dispose();
                initSetter = null;

                if (x.SetterDelegateIdentifier != null)
                {// SetterDelegate!(obj, vd);
                    var prefix = info.GeneratingStaticMethod ? (this.RegionalName + ".") : string.Empty;
                    ssb.AppendLine($"{prefix}{x.SetterDelegateIdentifier}!({this.InIfStruct}{destObject}, vd);");
                }
                else if (x.IsReadOnly)
                {
                    if (withNullable.Object.IsUnmanagedType)
                    {// fixed (ulong* ptr = &this.Id0) *ptr = 11;
                        ssb.AppendLine($"fixed ({withNullable.FullNameWithNullable}* ptr = &{destObject}.{x.SimpleName}) *ptr = vd;");
                    }
                    else
                    {// Unsafe.AsRef({this.array) = vd;
                        ssb.AppendLine($"Unsafe.AsRef({destObject}.{x.SimpleName}) = vd;");
                    }
                }
            }
        }
    }

    internal void GenerateCloneCore(ScopingStringBuilder ssb, GeneratorInformation info, TinyhandObject x, string sourceObject)
    {// Called by GenerateClone()
        var withNullable = x?.TypeObjectWithNullable;
        if (x == null || withNullable == null)
        {// no object
            return;
        }

        ScopingStringBuilder.IScope? initSetter = null;
        ScopingStringBuilder.IScope? emptyBrace = null;
        var destObject = ssb.FullObject; // Hidden members
        using (var d = this.ScopeMember(ssb, x))
        {
            if (withNullable.Object.ObjectAttribute != null &&
                withNullable.Object.ObjectAttribute.UseResolver == false)
            {// TinyhandObject.
                InitSetter_Start(true);
                ssb.AppendLine($"{ssb.FullObject} = TinyhandSerializer.CloneObject({sourceObject}, options)!;");
                InitSetter_End();
            }
            else if (CoderResolver.Instance.TryGetCoder(withNullable) is { } coder)
            {// Coder
                using (var c = ssb.ScopeBrace(string.Empty))
                {
                    InitSetter_Start();
                    coder.CodeClone(ssb, info, sourceObject);
                    InitSetter_End();
                }
            }
            else
            {// Other
                if (x.TypeObject != null && (x.TypeObject.Kind != VisceralObjectKind.Error && x.TypeObject.Kind != VisceralObjectKind.TypeParameter))
                {
                    this.Body.ReportDiagnostic(TinyhandBody.Warning_NoCoder, x.Location, withNullable.FullName);
                }

                InitSetter_Start(true);
                ssb.AppendLine($"{ssb.FullObject} = options.Resolver.GetFormatter<{withNullable.FullNameWithNullable}>().Clone({sourceObject}, options)!;");
                InitSetter_End();
            }

            void InitSetter_Start(bool brace = false)
            {
                if (x.SetterDelegateIdentifier != null || x.IsReadOnly)
                {// TypeName vd;
                    if (brace)
                    {
                        emptyBrace = ssb.ScopeBrace(string.Empty);
                    }

                    initSetter = ssb.ScopeFullObject("vd");
                    ssb.AppendLine(withNullable.FullNameWithNullable + " vd;");
                }
            }

            void InitSetter_End()
            {
                if (initSetter != null)
                {
                    initSetter.Dispose();
                    initSetter = null;

                    if (x.SetterDelegateIdentifier != null)
                    {// SetterDelegate!(obj, vd);
                        var prefix = info.GeneratingStaticMethod ? (this.RegionalName + ".") : string.Empty;
                        ssb.AppendLine($"{prefix}{x.SetterDelegateIdentifier}!({this.InIfStruct}{destObject}, vd);");
                    }
                    else if (x.IsReadOnly)
                    {
                        /*if (this.Kind == VisceralObjectKind.Struct)
                        {// *(ulong*)&value.Id0 = vd;
                            ssb.AppendLine($"*({withNullable.FullNameWithNullable}*)&{destObject}.{x.SimpleName} = vd;");
                        }
                        else
                        {// fixed (ulong* ptr = &this.Id0) *ptr = 11;
                            ssb.AppendLine($"fixed ({withNullable.FullNameWithNullable}* ptr = &{destObject}.{x.SimpleName}) *ptr = vd;");
                        }*/

                        if (!withNullable.Object.IsUnmanagedType)
                        {// Unsafe.AsRef({this.array) = vd;
                            ssb.AppendLine($"Unsafe.AsRef({this.GetSourceName(destObject, x)}) = vd;"); // {destObject}.{x.SimpleName}
                        }
                        else
                        {
                            if (this.Kind == VisceralObjectKind.Struct)
                            {// *(ulong*)&value.Id0 = vd;
                                ssb.AppendLine($"*({withNullable.FullNameWithNullable}*)&{this.GetSourceName(destObject, x)} = vd;"); // {destObject}.{x.SimpleName}
                            }
                            else
                            {// fixed (ulong* ptr = &this.Id0) *ptr = 11;
                                ssb.AppendLine($"fixed ({withNullable.FullNameWithNullable}* ptr = &{this.GetSourceName(destObject, x)}) *ptr = vd;"); // {destObject}.{x.SimpleName}
                            }
                        }
                    }

                    if (emptyBrace != null)
                    {
                        emptyBrace.Dispose();
                        emptyBrace = null;
                    }
                }
            }
        }
    }

    internal void GenerateDeserializerIntKey(ScopingStringBuilder ssb, GeneratorInformation info)
    {
        if (this.IntKey_Array == null)
        {
            return;
        }

        if (this.Kind.IsValueType())
        {// Value type
            ssb.AppendLine("if (reader.TryReadNil()) throw new TinyhandException(\"Data is Nil, struct can not be null.\");");
        }

        ssb.AppendLine("var numberOfData = reader.ReadArrayHeader();");

        using (var security = ssb.ScopeSecurityDepth())
        {
            if (!string.IsNullOrEmpty(this.ObjectAttribute?.LockObject))
            {// LockObject
                this.GenerateDeserialize_LockEnter(ssb, info);
            }

            foreach (var x in this.IntKey_Array)
            {
                this.GenerateDeserializeCore(ssb, info, x);
            }

            ssb.AppendLine("while (numberOfData-- > 0) reader.Skip();");

            this.GenerateReconstructRemaining(ssb, info);
        }

        if (!string.IsNullOrEmpty(this.ObjectAttribute?.LockObject))
        {// LockObject
            using (var finallyScope = ssb.ScopeBrace("finally"))
            {
                this.GenerateDeserialize_LockExit(ssb, info);
                ssb.AppendLine("reader.Depth--;");
            }
        }
        else
        {
            ssb.AppendLine("finally { reader.Depth--; }");
        }
    }

    internal void GenerateDeserializerStringKey(ScopingStringBuilder ssb, GeneratorInformation info)
    {
        if (this.Automata == null)
        {
            return;
        }

        if (this.Kind.IsValueType())
        {// Value type
            ssb.AppendLine("if (reader.TryReadNil()) throw new TinyhandException(\"Data is Nil, struct can not be null.\");");
        }

        ssb.AppendLine("ulong key;");
        if (this.Automata.ReconstructCount > 0)
        {
            ssb.AppendLine($"var deserializedFlag = new bool[{this.Automata.ReconstructCount}];");
        }

        ssb.AppendLine("var numberOfData = reader.ReadMapHeader2();");

        using (var security = ssb.ScopeSecurityDepth())
        {
            if (!string.IsNullOrEmpty(this.ObjectAttribute?.LockObject))
            {// LockObject
                this.GenerateDeserialize_LockEnter(ssb, info);
            }

            using (var loop = ssb.ScopeBrace("while (numberOfData-- > 0)"))
            {
                ssb.AppendLine("var utf8 = reader.ReadStringSpan();");
                using (var c = ssb.ScopeBrace("if (utf8.Length == 0)"))
                {
                    ssb.GotoSkipLabel();
                }

                ssb.AppendLine("key = global::Arc.Visceral.VisceralTrieHelper.ReadKey(ref utf8);");

                this.Automata.GenerateDeserialize(ssb, info);

                ssb.AppendLine("continue;");
                ssb.AppendLine("SkipLabel:", false);
                ssb.ReaderSkip();
            }

            // Reconstruct
            this.Automata.GenerateReconstruct(ssb, info);
            this.GenerateReconstructRemaining(ssb, info);
        }

        if (!string.IsNullOrEmpty(this.ObjectAttribute?.LockObject))
        {// LockObject
            using (var finallyScope = ssb.ScopeBrace("finally"))
            {
                this.GenerateDeserialize_LockExit(ssb, info);
                ssb.AppendLine("reader.Depth--;");
            }
        }
        else
        {
            ssb.AppendLine("finally { reader.Depth--; }");
        }
    }

    internal void GenerateSerializeCore(ScopingStringBuilder ssb, GeneratorInformation info, TinyhandObject? x, bool skipDefaultValue)
    {
        var withNullable = x?.TypeObjectWithNullable;
        if (x == null || withNullable == null)
        {// no object
            ssb.AppendLine("writer.WriteNil();");
            return;
        }

        ScopingStringBuilder.IScope? v1 = null;
        ScopingStringBuilder.IScope v2;
        if (x.RequiresGetter)
        {
            v1 = ssb.ScopeBrace(string.Empty);
            var prefix = info.GeneratingStaticMethod ? (this.RegionalName + ".") : string.Empty;
            ssb.AppendLine($"var vd = {prefix}{x.GetterDelegateIdentifier}!({ssb.FullObject});");
            v2 = ssb.ScopeFullObject("vd");
        }
        else
        {
            v2 = this.ScopeMember(ssb, x); // Hidden members
        }

        ScopingStringBuilder.IScope? skipDefaultValueScope = null;
        if (skipDefaultValue)
        {
            if (x.IsDefaultable)
            {
                using (var scopeDefault = ssb.ScopeBrace($"if ({ssb.FullObject} == {VisceralDefaultValue.DefaultValueToString(x.DefaultValue)})"))
                {
                    ssb.AppendLine($"if (!options.IsSignatureMode) writer.WriteNil();");
                }

                skipDefaultValueScope = ssb.ScopeBrace("else");
            }
            else if (withNullable.Object.DefaultInterface is { } defaultInterface)
            {
                if (withNullable.Object.MethodCondition_CanSkipSerialization == MethodCondition.Declared)
                {
                    using (var scopeDefault = ssb.ScopeBrace($"if ({ssb.FullObject}{withNullable.Object.QuestionMarkIfReferenceType}.{TinyhandBody.CanSkipSerializationMethod}() == true)"))
                    {
                        ssb.AppendLine($"if (!options.IsSignatureMode) writer.WriteNil();");
                    }

                    skipDefaultValueScope = ssb.ScopeBrace("else");
                }
                else if (withNullable.Object.MethodCondition_CanSkipSerialization == MethodCondition.ExplicitlyDeclared)
                {
                    using (var scopeDefault = ssb.ScopeBrace($"if ((({defaultInterface.FullName}){ssb.FullObject}{withNullable.Object.QuestionMarkIfReferenceType}).{TinyhandBody.CanSkipSerializationMethod}() == true)"))
                    {
                        ssb.AppendLine($"if (!options.IsSignatureMode) writer.WriteNil();");
                    }

                    skipDefaultValueScope = ssb.ScopeBrace("else");
                }
            }
        }

        var coder = CoderResolver.Instance.TryGetCoder(withNullable);
        if (coder != null)
        {// Coder
            coder.CodeSerializer(ssb, info);
        }
        else if (withNullable.Object.ObjectAttribute?.UseResolver == false &&
            (withNullable.Object.ObjectAttribute != null ||
            withNullable.Object.HasITinyhandSerializeConstraint()))
        {// TinyhandObject or Type parameter with ITinyhandSerialize constraint.
            using (ssb.ScopeBrace(string.Empty))
            {
                ssb.AppendLine($"TinyhandSerializer.SerializeObject(ref writer, {ssb.FullObject}, options);");
            }
        }
        else
        {// Formatter
            if (x.HasNullableAnnotation)
            {
                ssb.AppendLine($"if ({ssb.FullObject} == null) writer.WriteNil();");
                ssb.AppendLine($"else options.Resolver.GetFormatter<{withNullable.Object.FullName}>().Serialize(ref writer, {ssb.FullObject}, options);");
            }
            else
            {
                ssb.AppendLine($"options.Resolver.GetFormatter<{withNullable.Object.FullName}>().Serialize(ref writer, {ssb.FullObject}, options);");
            }
        }

        skipDefaultValueScope?.Dispose();

        v2.Dispose();
        v1?.Dispose();
    }

    internal void GenerateMaxLength(ScopingStringBuilder ssb, GeneratorInformation info, TinyhandObject typeObject, MaxLengthAttributeMock attribute)
    {
        if (typeObject.FullName == "string")
        {// string
            if (attribute.MaxLength >= 0)
            {
                using (var scopeIf = ssb.ScopeBrace($"if ({ssb.FullObject}.Length > {attribute.MaxLength})"))
                {// text = text.Substring(0, MaxLength);
                    ssb.AppendLine($"{ssb.FullObject} = {ssb.FullObject}.Substring(0, {attribute.MaxLength});");
                }
            }
        }
        else if (typeObject.Array_Rank == 1)
        {// T[]
            if (attribute.MaxLength >= 0)
            {
                using (var scopeIf = ssb.ScopeBrace($"if ({ssb.FullObject}.Length > {attribute.MaxLength})"))
                {// array = array[..MaxLength];
                    ssb.AppendLine($"{ssb.FullObject} = {ssb.FullObject}[..{attribute.MaxLength}];");
                }
            }

            if (typeObject.Array_Element?.FullName == "string" &&
            attribute.MaxChildLength >= 0)
            {// string[]
                using (var scopeFor = ssb.ScopeBrace($"for (var mi = 0; mi < {ssb.FullObject}.Length; mi++)"))
                {
                    using (var scopeIf = ssb.ScopeBrace($"if ({ssb.FullObject}[mi].Length > {attribute.MaxChildLength})"))
                    {// text = text.Substring(0, MaxLength);
                        ssb.AppendLine($"{ssb.FullObject}[mi] = {ssb.FullObject}[mi].Substring(0, {attribute.MaxChildLength});");
                    }
                }
            }
        }
        else if (typeObject.Generics_Kind == VisceralGenericsKind.ClosedGeneric &&
            typeObject.OriginalDefinition is { } baseObject &&
            baseObject.FullName == "System.Collections.Generic.List<T>" &&
            typeObject.Generics_Arguments.Length == 1)
        {// List<T>
            if (attribute.MaxLength >= 0)
            {
                using (var scopeIf = ssb.ScopeBrace($"if ({ssb.FullObject}.Count > {attribute.MaxLength})"))
                {// list = list.GetRange(0, MaxLength);
                    ssb.AppendLine($"{ssb.FullObject} = {ssb.FullObject}.GetRange(0, {attribute.MaxLength});");
                }
            }

            if (typeObject.Generics_Arguments[0].FullName == "string" &&
                attribute.MaxChildLength >= 0)
            {// List<string>
                using (var scopeFor = ssb.ScopeBrace($"for (var mi = 0; mi < {ssb.FullObject}.Count; mi++)"))
                {
                    using (var scopeIf = ssb.ScopeBrace($"if ({ssb.FullObject}[mi].Length > {attribute.MaxChildLength})"))
                    {// text = text.Substring(0, MaxLength);
                        ssb.AppendLine($"{ssb.FullObject}[mi] = {ssb.FullObject}[mi].Substring(0, {attribute.MaxChildLength});");
                    }
                }
            }
        }
    }

    internal void GenerateSerializerIntKey(ScopingStringBuilder ssb, GeneratorInformation info)
    {
        if (this.IntKey_Array == null)
        {
            return;
        }

        ssb.AppendLine($"if (!options.IsSignatureMode) writer.WriteArrayHeader({this.IntKey_Array.Length});");
        var skipDefaultValue = this.ObjectAttribute?.SkipSerializingDefaultValue == true;
        foreach (var x in this.IntKey_Array)
        {
            if (x?.KeyAttribute?.Condition == false)
            {// Conditional
                using (var scopeIf = ssb.ScopeBrace("if (options.IsConditionalMode)"))
                {
                    ssb.AppendLine("writer.WriteNil();");
                }

                using (var scopeIf = ssb.ScopeBrace("else if (options.IsSignatureMode)"))
                {
                }

                using (var scopeElse = ssb.ScopeBrace("else"))
                {
                    this.GenerateSerializeCore(ssb, info, x, skipDefaultValue);
                }
            }
            else
            {
                this.GenerateSerializeCore(ssb, info, x, skipDefaultValue);
            }
        }
    }

    internal void GenerateSerializerStringKey(ScopingStringBuilder ssb, GeneratorInformation info)
    {
        if (this.Automata == null)
        {
            return;
        }

        var cf = this.OriginalDefinition; // For generics. get Class<T>
        if (cf == null)
        {
            cf = this;
        }

        ssb.AppendLine($"writer.WriteMapHeader({this.Automata.NodeList.Count});");
        var skipDefaultValue = this.ObjectAttribute?.SkipSerializingDefaultValue == true;
        foreach (var x in this.Automata.NodeList)
        {
            ssb.AppendLine($"writer.WriteString({cf.LocalName}.{x.Identifier});");

            if (x.Member?.KeyAttribute?.Condition == false)
            {// Conditional
                using (var scopeIf = ssb.ScopeBrace("if (options.IsConditionalMode)"))
                {
                    ssb.AppendLine("writer.WriteNil();");
                }

                using (var scopeIf = ssb.ScopeBrace("else if (options.IsSignatureMode)"))
                {
                }

                using (var scopeElse = ssb.ScopeBrace("else"))
                {
                    this.GenerateSerializeCore(ssb, info, x.Member, skipDefaultValue);
                }
            }
            else
            {
                this.GenerateSerializeCore(ssb, info, x.Member, skipDefaultValue);
            }
        }
    }

    internal void GenerateStringKeyFields(ScopingStringBuilder ssb, GeneratorInformation info)
    {
        if (this.Automata == null || this.Automata.NodeList.Count == 0)
        {
            return;
        }

        ssb.AppendLine();
        foreach (var x in this.Automata.NodeList)
        {
            if (x.Utf8Name == null)
            {
                continue;
            }

            ssb.Append($"private static ReadOnlySpan<byte> {x.Identifier} => new byte[] {{ ");
            foreach (var y in x.Utf8Name)
            {
                ssb.Append($"{y}, ", false);
            }

            ssb.Append("};\r\n", false);
        }
    }

    internal bool HasITinyhandSerializeConstraint()
    {
        if (this.symbol is ITypeParameterSymbol tps)
        {
            return tps.ConstraintTypes.Any(x => x.Name == "ITinyhandSerialize");
        }

        return false;
    }

    internal bool IsReadableFrom(TinyhandObject obj)
    {
        if (this.ContainingObject != obj)
        {
            if (this.Kind == VisceralObjectKind.Field)
            {
                if (this.Field_IsPrivate)
                {
                    return false;
                }
            }
            else if (this.Kind == VisceralObjectKind.Property)
            {
                if (this.Property_IsPrivateGetter)
                {
                    return false;
                }
            }
        }

        return true;
    }
}
