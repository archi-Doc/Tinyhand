﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Arc.Visceral;
using Microsoft.CodeAnalysis;
using Tinyhand.Coders;

#pragma warning disable SA1202 // Elements should be ordered by access
#pragma warning disable SA1204 // Static elements should appear before instance elements
#pragma warning disable SA1310 // Field names should not contain underscore
#pragma warning disable SA1401 // Fields should be private
#pragma warning disable SA1405 // Debug.Assert should provide message text
#pragma warning disable SA1602 // Enumeration items should be documented

namespace Tinyhand.Generator
{
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
        OverwriteTarget = 1 << 8,

        HasITinyhandSerializationCallback = 1 << 9, // Has ITinyhandSerializationCallback interface
        HasExplicitOnBeforeSerialize = 1 << 10, // ITinyhandSerializationCallback.OnBeforeSerialize()
        HasExplicitOnAfterDeserialize = 1 << 11, // ITinyhandSerializationCallback.OnAfterDeserialize()
        HasITinyhandSerialize = 1 << 12, // Has ITinyhandSerialize interface
        HasITinyhandReconstruct = 1 << 13, // Has ITinyhandReconstruct interface
    }

    public class TinyhandObject : VisceralObjectBase<TinyhandObject>
    {
        public TinyhandObject()
        {
        }

        public new TinyhandBody Body => (TinyhandBody)((VisceralObjectBase<TinyhandObject>)this).Body;

        public TinyhandObjectFlag ObjectFlag { get; private set; }

        public TinyhandObjectAttributeMock? ObjectAttribute { get; private set; }

        public KeyAttributeMock? KeyAttribute { get; private set; }

        public VisceralAttribute? KeyVisceralAttribute { get; private set; }

        public IgnoreMemberAttributeMock? IgnoreMemberAttribute { get; private set; }

        public ReconstructAttributeMock? ReconstructAttribute { get; private set; }

        public ReconstructState ReconstructState { get; private set; }

        public OverwriteAttributeMock? OverwriteAttribute { get; private set; }

        public TinyhandObject[] Members { get; private set; } = Array.Empty<TinyhandObject>(); // Members have valid TypeObject && not static && property or field

        public IEnumerable<TinyhandObject> MembersWithFlag(TinyhandObjectFlag flag) => this.Members.Where(x => x.ObjectFlag.HasFlag(flag));

        public object? DefaultValue { get; private set; }

        public string? DefaultValueTypeName { get; private set; }

        public Location? DefaultValueLocation { get; private set; }

        public bool IsDefaultable { get; private set; }

        public List<TinyhandObject>? Children { get; private set; } // The opposite of ContainingObject

        public List<TinyhandObject>? ConstructedObjects { get; private set; } // The opposite of ConstructedFrom

        public int GenericsNumber { get; private set; }

        public string? GenericsNumberString { get; private set; }

        public int FormatterNumber { get; private set; }

        internal Automata? Automata { get; private set; }

        public TinyhandObject[]? IntKey_Array;

        public int IntKey_Min { get; private set; } = -1;

        public int IntKey_Max { get; private set; } = -1;

        public int IntKey_Number { get; private set; } = 0;

        public MethodCondition MethodCondition_Serialize { get; private set; }

        public MethodCondition MethodCondition_Deserialize { get; private set; }

        public MethodCondition MethodCondition_Reconstruct { get; private set; }

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
                if (this.TypeObject?.Kind.IsReferenceType() == true)
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

        public void Configure()
        {
            if (this.ObjectFlag.HasFlag(TinyhandObjectFlag.Configured))
            {
                return;
            }

            this.ObjectFlag |= TinyhandObjectFlag.Configured;

            // Open generic type is not supported.
            var genericsType = this.Generics_Kind;
            if (genericsType == VisceralGenericsKind.OpenGeneric)
            {
                return;
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

            // KeyAttribute
            if (this.AllAttributes.FirstOrDefault(x => x.FullName == KeyAttributeMock.FullName) is { } keyAttribute)
            {
                this.KeyVisceralAttribute = keyAttribute;
                try
                {
                    this.KeyAttribute = KeyAttributeMock.FromArray(keyAttribute.ConstructorArguments, keyAttribute.NamedArguments);
                }
                catch (ArgumentNullException)
                {
                    this.Body.ReportDiagnostic(TinyhandBody.Error_KeyAttributeError, keyAttribute.Location);
                }
            }

            // IgnoreMemberAttribute
            if (this.AllAttributes.FirstOrDefault(x => x.FullName == IgnoreMemberAttributeMock.FullName) is { } ignoreMemberAttribute)
            {
                try
                {
                    this.IgnoreMemberAttribute = IgnoreMemberAttributeMock.FromArray(ignoreMemberAttribute.ConstructorArguments, ignoreMemberAttribute.NamedArguments);
                }
                catch (InvalidCastException)
                {
                    this.Body.ReportDiagnostic(TinyhandBody.Error_AttributePropertyError, ignoreMemberAttribute.Location);
                }
            }

            // ReconstructAttribute
            if (this.AllAttributes.FirstOrDefault(x => x.FullName == ReconstructAttributeMock.FullName) is { } reconstructAttribute)
            {
                try
                {
                    this.ReconstructAttribute = ReconstructAttributeMock.FromArray(reconstructAttribute.ConstructorArguments, reconstructAttribute.NamedArguments);
                }
                catch (InvalidCastException)
                {
                    this.Body.ReportDiagnostic(TinyhandBody.Error_AttributePropertyError, reconstructAttribute.Location);
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

            // OverwriteAttribute
            if (this.AllAttributes.FirstOrDefault(x => x.FullName == OverwriteAttributeMock.FullName) is { } overwriteAttribute)
            {
                try
                {
                    this.OverwriteAttribute = OverwriteAttributeMock.FromArray(overwriteAttribute.ConstructorArguments, overwriteAttribute.NamedArguments);
                }
                catch (InvalidCastException)
                {
                    this.Body.ReportDiagnostic(TinyhandBody.Error_AttributePropertyError, overwriteAttribute.Location);
                }
            }

            // DefaultValueAttribute
            if (this.AllAttributes.FirstOrDefault(x => x.FullName == typeof(DefaultValueAttribute).FullName) is { } defaultValueAttribute)
            {
                this.DefaultValueLocation = defaultValueAttribute.Location;
                if (defaultValueAttribute.ConstructorArguments?.Length > 0)
                {
                    this.DefaultValue = defaultValueAttribute.ConstructorArguments[0];
                }
            }

            if (this.ObjectAttribute != null)
            {// TinyhandObject
                this.ConfigureObject();
            }
        }

        public void ConfigureObject()
        {
            // Method condition (Serialize/Deserialize)
            this.MethodCondition_Serialize = MethodCondition.MemberMethod;
            this.MethodCondition_Deserialize = MethodCondition.MemberMethod;
            if (this.AllInterfaces.Any(x => x == "Tinyhand.ITinyhandSerialize"))
            {// ITinyhandSerialize implemented
                this.ObjectFlag |= TinyhandObjectFlag.HasITinyhandSerialize;

                if (this.GetMembers(VisceralTarget.Method).Any(x => x.SimpleName == "Tinyhand.ITinyhandSerialize.Serialize"))
                {
                    this.MethodCondition_Serialize = MethodCondition.ExplicitlyDeclared;
                }
                else
                {
                    this.MethodCondition_Serialize = MethodCondition.Declared;
                }

                if (this.GetMembers(VisceralTarget.Method).Any(x => x.SimpleName == "Tinyhand.ITinyhandSerialize.Deserialize"))
                {
                    this.MethodCondition_Deserialize = MethodCondition.ExplicitlyDeclared;
                }
                else
                {
                    this.MethodCondition_Deserialize = MethodCondition.Declared;
                }
            }
            else if (this.Generics_IsGeneric)
            {
                this.MethodCondition_Serialize = MethodCondition.StaticMethod;
                this.MethodCondition_Deserialize = MethodCondition.StaticMethod;
            }

            // Method condition (Reconstruct)
            this.MethodCondition_Reconstruct = MethodCondition.MemberMethod;
            if (this.AllInterfaces.Any(x => x == "Tinyhand.ITinyhandReconstruct"))
            {// ITinyhandReconstruct implemented
                this.ObjectFlag |= TinyhandObjectFlag.HasITinyhandReconstruct;

                if (this.GetMembers(VisceralTarget.Method).Any(x => x.SimpleName == "Tinyhand.ITinyhandReconstruct.Reconstruct"))
                {
                    this.MethodCondition_Reconstruct = MethodCondition.ExplicitlyDeclared;
                }
                else
                {
                    this.MethodCondition_Reconstruct = MethodCondition.Declared;
                }
            }
            else if (this.Generics_IsGeneric)
            {
                this.MethodCondition_Reconstruct = MethodCondition.StaticMethod;
            }

            // ITinyhandSerializationCallback
            if (this.AllInterfaces.Any(x => x == "Tinyhand.ITinyhandSerializationCallback"))
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
                if (x.TypeObject != null && !x.IsStatic)
                { // Valid TypeObject && not static
                    x.Configure();
                    list.Add(x);
                }
            }

            // Members: Field
            foreach (var x in this.AllMembers.Where(x => x.Kind == VisceralObjectKind.Field))
            {
                if (x.TypeObject != null && !x.IsStatic)
                { // Valid TypeObject && not static
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

            if (this.Generics_Kind != VisceralGenericsKind.OpenGeneric)
            {
                // Add default coder (options.Resolver.GetFormatter<T>()...)
                if (this.TypeObjectWithNullable != null)
                {
                    FormatterResolver.Instance.AddFormatter(this.TypeObjectWithNullable);
                }

                if (cf.ConstructedObjects == null)
                {
                    cf.ConstructedObjects = new();
                }

                if (!cf.ConstructedObjects.Contains(this))
                {
                    cf.ConstructedObjects.Add(this);
                }
            }
        }

        public void CheckObject()
        {
            // partial class required.
            if (!this.IsPartial)
            {
                this.Body.ReportDiagnostic(TinyhandBody.Error_NotPartial, this.Location, this.FullName);
            }

            // default constructor required.
            if (this.Kind.IsReferenceType())
            {
                if (this.GetMembers(VisceralTarget.Method).Any(a => a.Method_IsConstructor && a.Method_Parameters.Length == 0) != true)
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

            // Target
            foreach (var x in this.Members)
            {
                if (!x.IsSerializable || x.IsReadOnly)
                {// Not serializable
                    continue;
                }
                else if (!x.IsPublic && this.ObjectAttribute?.IncludePrivateMembers != true)
                {// Skip protected or private members if IncludePrivateMembers is false.
                    continue;
                }

                x.ObjectFlag |= TinyhandObjectFlag.Target;
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

            // Check members.
            foreach (var x in this.Members)
            {
                x.CheckMember(this);
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
                            x.ObjectFlag |= TinyhandObjectFlag.SerializeTarget;
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
                        if (this.ObjectAttribute!.KeyAsPropertyName)
                        {// KeyAsPropertyName
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

                if (stringKeyExists || (!intKeyExists && this.ObjectAttribute!.KeyAsPropertyName == true))
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
                    this.Automata.AddNode(s, x);
                }
            }

            if (this.Generics_Kind == VisceralGenericsKind.CloseGeneric)
            {
                if (this.OriginalDefinition is { } cf && cf.Automata == null)
                {
                    cf.Automata = this.Automata; // Open generic class<T> requires string key information.
                }
            }
        }

        private void CheckObject_IntKey()
        {
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
                    if (this.IntKey_Array[i] != null)
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

            if (this.IntKey_Max >= 10 && this.IntKey_Max > (this.IntKey_Number * 2))
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
            {// Not serializable
                if (this.KeyAttribute != null || this.ReconstructAttribute != null)
                {
                    this.Body.ReportDiagnostic(TinyhandBody.Error_NotSerializableMember, this.Location, this.SimpleName);
                }
            }

            if (this.KeyAttribute != null)
            {// Has KeyAttribute
                this.Body.DebugAssert(this.ObjectFlag.HasFlag(TinyhandObjectFlag.SerializeTarget), $"{this.FullName}: KeyAttribute and SerializeTarget are inconsistent.");

                if (this.TypeObjectWithNullable != null && this.TypeObjectWithNullable.Object.ObjectAttribute == null && CoderResolver.Instance.IsCoderOrFormatterAvailable(this.TypeObjectWithNullable) == false)
                {// No Coder or Formatter
                    this.Body.ReportDiagnostic(TinyhandBody.Error_ObjectAttributeRequired, this.Location);
                }
            }
            else
            {// No KeyAttribute
                if (this.ObjectFlag.HasFlag(TinyhandObjectFlag.SerializeTarget))
                {
                    this.Body.ReportDiagnostic(TinyhandBody.Error_KeyAttributeRequired, this.Location);
                }
            }

            // ReconstructTarget
            if (parent.ObjectFlag.HasFlag(TinyhandObjectFlag.HasITinyhandSerialize))
            {// ITinyhandSerialize is implemented.
                if (this.ObjectFlag.HasFlag(TinyhandObjectFlag.Target) && this.TypeObject.Kind.IsReferenceType() == true)
                { // Target && Reference type
                    this.ObjectFlag |= TinyhandObjectFlag.ReconstructTarget;
                }
            }
            else
            {
                if (this.ObjectFlag.HasFlag(TinyhandObjectFlag.SerializeTarget) && this.TypeObject.Kind.IsReferenceType() == true)
                { // SerializeTarget && Reference type
                    this.ObjectFlag |= TinyhandObjectFlag.ReconstructTarget;
                }
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

            if (this.DefaultValue != null)
            {
                if (VisceralDefaultValue.IsDefaultableType(this.TypeObject.SimpleName))
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
                {// Other (Constructor is required)
                    this.IsDefaultable = false;
                    this.DefaultValueTypeName = VisceralHelper.Primitives_ShortenName(this.DefaultValue.GetType().FullName);
                    if (!this.TypeObject.AllMembers.Any(x => x.Kind == VisceralObjectKind.Method
                    && x.IsPublic
                    && x.SimpleName == TinyhandBody.SetDefaultMethod
                    && x.Method_Parameters.Length == 1
                    && x.Method_Parameters[0] == this.DefaultValueTypeName))
                    {// SetDefault(type value) is required.
                        this.DefaultValue = null;
                        this.Body.ReportDiagnostic(TinyhandBody.Warning_SetDefaultMethod, this.DefaultValueLocation ?? this.Location, this.DefaultValueTypeName);
                    }

                    /*if (!this.TypeObject.AllMembers.Any(x => x.Method_IsConstructor && x.Method_Parameters.Length == 1 && x.Method_Parameters[0] == this.DefaultValueTypeName))
                    {// Type-mathed constructor is required.
                        this.DefaultValue = null;
                        this.Body.ReportDiagnostic(TinyhandBody.Warning_DefaultValueConstructor, this.DefaultValueLocation ?? this.Location);
                    }*/
                }
            }

            // OverwriteTarget
            var overwriteFlag = this.TypeObject!.ObjectAttribute?.Overwrite == true;
            if (this.OverwriteAttribute?.Overwrite == true)
            {
                overwriteFlag = true;
            }
            else if (this.OverwriteAttribute?.Overwrite == false)
            {
                overwriteFlag = false;
            }

            if (overwriteFlag)
            {
                this.ObjectFlag |= TinyhandObjectFlag.OverwriteTarget;
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

                // Avoid reconstruct T?
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
        {
            var classFormat = "__gen__tf__{0:D4}";
            var list2 = list.SelectMany(x => x.ConstructedObjects).Where(x => x.ObjectAttribute != null);

            if (list.Count > 0 && list[0].ContainingObject is { } containingObject)
            {
                // info.ModuleInitializerClass.Add(containingObject.FullName);
                var constructedList = containingObject.ConstructedObjects;
                if (constructedList != null && constructedList.Count > 0)
                {
                    info.ModuleInitializerClass.Add(constructedList[0].FullName);
                }
            }

            using (var m = ssb.ScopeBrace("internal static void __gen__load()"))
            {
                foreach (var x in list2)
                {
                    var name = string.Format(classFormat, x.FormatterNumber);
                    ssb.AppendLine($"GeneratedResolver.Instance.SetFormatter<{x.FullName}>(new {name}());");
                }
            }

            ssb.AppendLine();

            foreach (var x in list2)
            {
                var name = string.Format(classFormat, x.FormatterNumber);
                using (var cls = ssb.ScopeBrace($"class {name}: ITinyhandFormatter<{x.FullName}>"))
                {
                    // Serialize
                    using (var s = ssb.ScopeBrace($"public void Serialize(ref TinyhandWriter writer, {x.FullName + x.QuestionMarkIfReferenceType} v, TinyhandSerializerOptions options)"))
                    using (var value = ssb.ScopeObject("v"))
                    {
                        x.GenerateFormatter_Serialize(ssb, info);
                    }

                    // Deserialize
                    using (var d = ssb.ScopeBrace($"public {x.FullName + x.QuestionMarkIfReferenceType} Deserialize(ref TinyhandReader reader, TinyhandSerializerOptions options)"))
                    {
                        x.GenerateFormatter_Deserialize(ssb, info);
                    }

                    // Reconstruct
                    using (var r = ssb.ScopeBrace($"public {x.FullName} Reconstruct(TinyhandSerializerOptions options)"))
                    {
                        x.GenerateFormatter_Reconstruct(ssb, info);
                    }
                }
            }
        }

        internal void Generate(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            if (this.ConstructedObjects == null)
            {
                return;
            }

            var interfaceString = string.Empty;
            if (this.ObjectAttribute != null)
            {
                if (this.MethodCondition_Serialize == MethodCondition.MemberMethod)
                {
                    interfaceString = " : ITinyhandSerialize";
                }

                if (this.MethodCondition_Reconstruct == MethodCondition.MemberMethod)
                {
                    if (interfaceString == string.Empty)
                    {
                        interfaceString = " : ITinyhandReconstruct";
                    }
                    else
                    {
                        interfaceString += ", ITinyhandReconstruct";
                    }
                }
            }

            using (var cls = ssb.ScopeBrace($"{this.AccessibilityName} partial {this.KindName} {this.LocalName}{interfaceString}"))
            {
                var genericsNumber = 0;
                foreach (var x in this.ConstructedObjects)
                {
                    if (x.ObjectAttribute == null)
                    {
                        continue;
                    }

                    if (genericsNumber++ > 0)
                    {
                        ssb.AppendLine();
                    }

                    x.GenericsNumber = genericsNumber;
                    x.Generate2(ssb, info);
                }

                if (this.ObjectAttribute != null && info.UseMemberNotNull)
                {// MemberNotNull
                    if (this.MethodCondition_Reconstruct == MethodCondition.MemberMethod)
                    {
                        this.GenerateMemberNotNull_MemberMethod(ssb, info);
                    }
                    else if (this.MethodCondition_Serialize == MethodCondition.StaticMethod)
                    {
                        this.GenerateMemberNotNull_StaticMethod(ssb, info);
                    }
                }

                // StringKey fields
                this.GenerateStringKeyFields(ssb, info);

                if (this.Children?.Count > 0)
                {// Generate children and loader.
                    ssb.AppendLine();
                    foreach (var x in this.Children)
                    {
                        x.Generate(ssb, info);
                    }

                    ssb.AppendLine();
                    GenerateLoader(ssb, info, this.Children);
                }
            }
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
                methodCode = "public void Serialize(ref TinyhandWriter writer, TinyhandSerializerOptions options)";
                objectCode = "this";
            }
            else if (this.MethodCondition_Serialize == MethodCondition.StaticMethod)
            {
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

        internal void GenerateFormatter_Serialize(ScopingStringBuilder ssb, GeneratorInformation info)
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
        }

        internal void GenerateFormatter_Deserialize(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            if (this.Kind.IsReferenceType())
            {// Reference type
                ssb.AppendLine("if (reader.TryReadNil()) return default;");
            }

            ssb.AppendLine($"var v = new {this.FullName}();");

            if (this.MethodCondition_Deserialize == MethodCondition.StaticMethod)
            {// Static method
                ssb.AppendLine($"{this.FullName}.Deserialize{this.GenericsNumberString}(ref v, ref reader, options);");
            }
            else if (this.MethodCondition_Deserialize == MethodCondition.ExplicitlyDeclared)
            {// Explicitly declared (Interface.Method())
                ssb.AppendLine("((ITinyhandSerialize)v).Deserialize(ref reader, options);");
            }
            else
            {// Member method
                ssb.AppendLine("v.Deserialize(ref reader, options);");
            }

            ssb.AppendLine("return v;");
        }

        internal void GenerateFormatter_Reconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"var v = new {this.FullName}();");

            if (this.MethodCondition_Reconstruct == MethodCondition.StaticMethod)
            {// Static method
                ssb.AppendLine($"{this.FullName}.Reconstruct{this.GenericsNumberString}(ref v, options);");
            }
            else if (this.MethodCondition_Reconstruct == MethodCondition.ExplicitlyDeclared)
            {// Explicitly declared (Interface.Method())
                ssb.AppendLine("((ITinyhandReconstruct)v).Reconstruct(options);");
            }
            else
            {// Member method
                ssb.AppendLine("v.Reconstruct(options);");
            }

            ssb.AppendLine("return v;");
        }

        internal void GenerateDeserialize_Method(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            string methodCode;
            string objectCode;

            if (this.MethodCondition_Deserialize == MethodCondition.MemberMethod)
            {
                methodCode = "public void Deserialize(ref TinyhandReader reader, TinyhandSerializerOptions options)";
                objectCode = "this";
            }
            else if (this.MethodCondition_Deserialize == MethodCondition.StaticMethod)
            {
                methodCode = $"public static void Deserialize{this.GenericsNumberString}(ref {this.RegionalName} v, ref TinyhandReader reader, TinyhandSerializerOptions options)";
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

        internal void GenerateReconstructCore(ScopingStringBuilder ssb, GeneratorInformation info, TinyhandObject? x)
        {// Called by GenerateReconstruct()
            var withNullable = x?.TypeObjectWithNullable;
            if (x == null || withNullable == null)
            {// no object
                return;
            }

            if (x.IsDefaultable)
            {// Default
                ssb.AppendLine($"{ssb.FullObject} = {VisceralDefaultValue.DefaultValueToString(x.DefaultValue)};");
                return;
            }

            if (x.ReconstructState != ReconstructState.Do)
            {
                return;
            }

            if (!withNullable.Object.Kind.IsReferenceType())
            {// Not a reference type
                return;
            }

            var coder = CoderResolver.Instance.TryGetCoder(withNullable);
            if (coder != null)
            {// Coder
                using (var c = ssb.ScopeBrace($"if ({ssb.FullObject} == null) "))
                {
                    coder.CodeReconstruct(ssb, info);
                }
            }
            else if (withNullable.Object.ObjectAttribute != null)
            {// TinyhandObject
                ssb.AppendLine($"if ({ssb.FullObject} == null) {ssb.FullObject} = options.Resolver.GetFormatter<{withNullable.Object.FullName}>().Reconstruct(options);");
            }
            else
            {// Default constructor
                ssb.AppendLine($"if ({ssb.FullObject} == null) {ssb.FullObject} = new {withNullable.Object.FullName}();");
            }
        }

        internal void GenerateReconstructRemaining(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            foreach (var x in this.Members.Where(x => x.ReconstructState == ReconstructState.Do && x.KeyAttribute == null))
            {
                using (var c = ssb.ScopeObject(x.SimpleName))
                {
                    this.GenerateReconstructCore(ssb, info, x);
                }
            }
        }

        internal void GenerateReconstruct_Method(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            string methodCode;
            string objectCode;

            if (this.MethodCondition_Reconstruct == MethodCondition.MemberMethod)
            {
                methodCode = "public void Reconstruct(TinyhandSerializerOptions options)";
                objectCode = "this";
            }
            else if (this.MethodCondition_Reconstruct == MethodCondition.StaticMethod)
            {
                methodCode = $"public static void Reconstruct{this.GenericsNumberString}(ref {this.RegionalName} v, TinyhandSerializerOptions options)";
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
                    using (var c = ssb.ScopeObject(x.SimpleName))
                    {
                        this.GenerateReconstructCore(ssb, info, x);
                    }
                }
            }
        }

        internal void GenerateMemberNotNull_Attribute(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            var firstFlag = true;
            foreach (var x in this.Members.Where(x => x.ReconstructState == ReconstructState.Do))
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

        internal void Generate2(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            this.FormatterNumber = info.FormatterCount++;
            this.GenericsNumberString = this.GenericsNumber > 1 ? this.GenericsNumber.ToString() : string.Empty;

            // Serialize/Deserialize/Reconstruct
            this.GenerateSerialize_Method(ssb, info);
            this.GenerateDeserialize_Method(ssb, info);
            this.GenerateReconstruct_Method(ssb, info);

            return;
        }

        internal void GenerateDeserializeCore(ScopingStringBuilder ssb, GeneratorInformation info, TinyhandObject? x)
        {// Integer key
            var withNullable = x?.TypeObjectWithNullable;
            if (x == null || withNullable == null)
            {// no object
                ssb.AppendLine("if (numberOfData-- > 0) reader.Skip();");
                return;
            }

            using (var m = ssb.ScopeObject(x.SimpleName))
            {
                var coder = CoderResolver.Instance.TryGetCoder(withNullable);
                using (var valid = ssb.ScopeBrace($"if (numberOfData-- > 0 && !reader.TryReadNil())"))
                {
                    if (coder != null)
                    {
                        coder.CodeDeserializer(ssb, info, true);
                    }
                    else
                    {
                        this.Body.ReportDiagnostic(TinyhandBody.Warning_NoCoder, x.Location, withNullable.FullName);
                        if (x.HasNullableAnnotation || withNullable.Object.Kind.IsValueType())
                        {// T?
                            ssb.AppendLine($"{m.FullObject} = options.Resolver.GetFormatter<{withNullable.Object.FullName}>().Deserialize(ref reader, options);");
                        }
                        else
                        {// T
                            ssb.AppendLine($"var f = options.Resolver.GetFormatter<{withNullable.Object.FullName}>();");
                            ssb.AppendLine($"{m.FullObject} = f.Deserialize(ref reader, options) ?? f.Reconstruct(options);");
                        }
                    }
                }

                if (x!.IsDefaultable)
                {// Default
                    using (var invalid = ssb.ScopeBrace("else"))
                    {
                        ssb.AppendLine($"{m.FullObject} = {VisceralDefaultValue.DefaultValueToString(x.DefaultValue)};");
                    }
                }
                else if (x.ReconstructState == ReconstructState.Do)
                {
                    using (var invalid = ssb.ScopeBrace("else"))
                    {
                        if (coder != null)
                        {
                            coder.CodeReconstruct(ssb, info);
                        }
                        else
                        {
                            ssb.AppendLine($"{m.FullObject} = options.Resolver.GetFormatter<{withNullable.Object.FullName}>().Reconstruct(options);");
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

            using (var m = ssb.ScopeObject(x.SimpleName))
            {
                var coder = CoderResolver.Instance.TryGetCoder(withNullable);
                using (var valid = ssb.ScopeBrace($"if (!reader.TryReadNil())"))
                {
                    if (coder != null)
                    {
                        coder.CodeDeserializer(ssb, info, true);
                    }
                    else
                    {
                        this.Body.ReportDiagnostic(TinyhandBody.Warning_NoCoder, x.Location, withNullable.FullName);
                        if (x.HasNullableAnnotation || withNullable.Object.Kind.IsValueType())
                        {// T?
                            ssb.AppendLine($"{m.FullObject} = options.Resolver.GetFormatter<{withNullable.Object.FullName}>().Deserialize(ref reader, options);");
                        }
                        else
                        {// T
                            ssb.AppendLine($"var f = options.Resolver.GetFormatter<{withNullable.Object.FullName}>();");
                            ssb.AppendLine($"{m.FullObject} = f.Deserialize(ref reader, options) ?? f.Reconstruct(options);");
                        }
                    }
                }

                if (x!.IsDefaultable)
                {// Default
                    using (var invalid = ssb.ScopeBrace("else"))
                    {
                        ssb.AppendLine($"{m.FullObject} = {VisceralDefaultValue.DefaultValueToString(x.DefaultValue)};");
                    }
                }
                else if (x.ReconstructState == ReconstructState.Do)
                {
                    using (var invalid = ssb.ScopeBrace("else"))
                    {
                        if (coder != null)
                        {
                            coder.CodeReconstruct(ssb, info);
                        }
                        else
                        {
                            ssb.AppendLine($"{m.FullObject} = options.Resolver.GetFormatter<{withNullable.Object.FullName}>().Reconstruct(options);");
                        }
                    }
                }
            }
        }

        internal void GenerateReconstructCore2(ScopingStringBuilder ssb, GeneratorInformation info, TinyhandObject? x, int reconstructIndex)
        {// Called by Automata
            var withNullable = x?.TypeObjectWithNullable;
            if (x == null || withNullable == null)
            {// no object
                return;
            }

            if (x.IsDefaultable)
            {// Default
                using (var m = ssb.ScopeObject(x.SimpleName))
                using (var conditionDeserialized = ssb.ScopeBrace($"if (!deserializedFlag[{reconstructIndex}])"))
                {
                    ssb.AppendLine($"{ssb.FullObject} = {VisceralDefaultValue.DefaultValueToString(x.DefaultValue)};");
                }

                return;
            }

            if (!withNullable.Object.Kind.IsReferenceType())
            {// Not a reference type
                return;
            }

            using (var m = ssb.ScopeObject(x.SimpleName))
            using (var conditionDeserialized = ssb.ScopeBrace($"if (!deserializedFlag[{reconstructIndex}] && {ssb.FullObject} == null)"))
            {
                if (x.NullableAnnotationIfReferenceType == Arc.Visceral.NullableAnnotation.NotAnnotated || x.ReconstructState == ReconstructState.Do)
                {// T
                    var coder = CoderResolver.Instance.TryGetCoder(withNullable);
                    if (coder != null)
                    {
                        coder.CodeReconstruct(ssb, info);
                    }
                    else if (withNullable.Object.ObjectAttribute != null)
                    {// TinyhandObject
                        ssb.AppendLine($"{ssb.FullObject} = options.Resolver.GetFormatter<{withNullable.Object.FullName}>().Reconstruct(options);");
                    }
                    else
                    {// Default constructor
                        ssb.AppendLine($"{ssb.FullObject} = new {withNullable.Object.FullName}();");
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
                foreach (var x in this.IntKey_Array)
                {
                    this.GenerateDeserializeCore(ssb, info, x);
                }

                ssb.AppendLine("while (numberOfData-- > 0) reader.Skip();");

                this.GenerateReconstructRemaining(ssb, info);
            }

            ssb.RestoreSecurityDepth();
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

            this.Automata.PrepareReconstruct();

            ssb.AppendLine("ulong key;");
            if (this.Automata.ReconstructCount > 0)
            {
                ssb.AppendLine($"var deserializedFlag = new bool[{this.Automata.ReconstructCount}];");
            }

            ssb.AppendLine("var numberOfData = reader.ReadMapHeader();");

            using (var security = ssb.ScopeSecurityDepth())
            {
                using (var loop = ssb.ScopeBrace("while (numberOfData-- > 0)"))
                {
                    ssb.AppendLine("var utf8 = reader.ReadStringSpan();");
                    using (var c = ssb.ScopeBrace("if (utf8.Length == 0)"))
                    {
                        ssb.GotoSkipLabel();
                    }

                    ssb.AppendLine("key = global::Tinyhand.Generator.AutomataKey.GetKey(ref utf8);");

                    this.Automata.GenerateDeserialize(ssb, info);

                    ssb.AppendLine("continue;");
                    ssb.AppendLine("SkipLabel:", false);
                    ssb.ReaderSkip();
                }

                // Reconstruct
                this.Automata.GenerateReconstruct(ssb, info);
                this.GenerateReconstructRemaining(ssb, info);
            }

            ssb.RestoreSecurityDepth();
        }

        internal void GenerateSerializeCore(ScopingStringBuilder ssb, GeneratorInformation info, TinyhandObject? x, bool skipDefaultValue)
        {
            var withNullable = x?.TypeObjectWithNullable;
            if (x == null || withNullable == null)
            {// no object
                ssb.AppendLine("writer.WriteNil();");
                return;
            }

            using (var v2 = ssb.ScopeObject(x.SimpleName))
            {
                ScopingStringBuilder.IScope? skipDefaultValueScope = null;
                if (skipDefaultValue)
                {
                    if (x.IsDefaultable)
                    {
                        ssb.AppendLine($"if ({v2.FullObject} == {VisceralDefaultValue.DefaultValueToString(x.DefaultValue)}) {{ writer.WriteNil(); }}");
                        skipDefaultValueScope = ssb.ScopeBrace("else");
                    }
                    else if (withNullable.Object.AllMembers.Any(a => a.Kind == VisceralObjectKind.Method &&
                    a.SimpleName == "CompareDefaultValue" && a.Method_Parameters.Length == 1 && a.Method_Parameters[0] == x.DefaultValueTypeName) == true)
                    {
                        ssb.AppendLine($"if ({v2.FullObject}.CompareDefaultValue({VisceralDefaultValue.DefaultValueToString(x.DefaultValue)})) {{ writer.WriteNil(); }}");
                        skipDefaultValueScope = ssb.ScopeBrace("else");
                    }
                }

                var coder = CoderResolver.Instance.TryGetCoder(withNullable);
                if (coder != null)
                {// Coder
                    coder.CodeSerializer(ssb, info);
                }
                else
                {// Formatter
                    if (x.HasNullableAnnotation)
                    {
                        ssb.AppendLine($"if ({v2.FullObject} == null) writer.WriteNil();");
                        ssb.AppendLine($"else options.Resolver.GetFormatter<{withNullable.Object.FullName}>().Serialize(ref writer, {v2.FullObject},options);");
                    }
                    else
                    {
                        ssb.AppendLine($"options.Resolver.GetFormatter<{withNullable.Object.FullName}>().Serialize(ref writer, {v2.FullObject},options);");
                    }
                }

                skipDefaultValueScope?.Dispose();
            }
        }

        internal void GenerateSerializerIntKey(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            if (this.IntKey_Array == null)
            {
                return;
            }

            ssb.AppendLine($"writer.WriteArrayHeader({this.IntKey_Array.Length});");
            var skipDefaultValue = this.ObjectAttribute?.SkipSerializingDefaultValue == true;
            foreach (var x in this.IntKey_Array)
            {
                this.GenerateSerializeCore(ssb, info, x, skipDefaultValue);
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
                ssb.AppendLine($"writer.WriteString({cf.LocalName}.{string.Format(TinyhandBody.StringKeyFieldFormat, x.Index)});");
                this.GenerateSerializeCore(ssb, info, x.Member, skipDefaultValue);
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

                ssb.Append($"private static ReadOnlySpan<byte> {string.Format(TinyhandBody.StringKeyFieldFormat, x.Index)} => new byte[] {{ ");
                foreach (var y in x.Utf8Name)
                {
                    ssb.Append($"{y}, ", false);
                }

                ssb.Append("};\r\n", false);
            }
        }
    }
}
