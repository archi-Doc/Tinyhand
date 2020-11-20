// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

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

    public enum FormatterCondition
    {
        MemberMethod, // member method
        StaticMethod, // static method
        Declared, // declared
    }

    [Flags]
    public enum TinyhandObjectFlag
    {
        Configured = 1 << 0,
        RelationConfigured = 1 << 1,
        Checked = 1 << 2,

        StringKeyObject = 1 << 3,
        IntKeyConflicted = 1 << 4,
    }

    public class TinyhandObject : VisceralObjectBase<TinyhandObject>
    {
        public TinyhandObject()
        {
        }

        public new TinyhandBody Body => (TinyhandBody)((VisceralObjectBase<TinyhandObject>)this).Body;

        public TinyhandObjectFlag ObjectFlag { get; private set; }

        public TinyhandObjectAttributeFake? ObjectAttribute { get; private set; }

        public KeyAttributeFake? KeyAttribute { get; private set; }

        public VisceralAttribute? KeyVisceralAttribute { get; private set; }

        public IgnoreMemberAttributeFake? IgnoreMemberAttribute { get; private set; }

        public ReconstructAttributeFake? ReconstructAttribute { get; private set; }

        public ReconstructState ReconstructState { get; private set; }

        public List<TinyhandObject> Members { get; private set; } = new();

        // public bool DefaultValueFlag { get; private set; } // True if a default value is set.

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

        public FormatterCondition FormatterCondition_Serialize { get; private set; }

        public FormatterCondition FormatterCondition_Reconstruct { get; private set; }

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

        public bool HasTinyhandSerializationCallback { get; private set; }

        public bool HasExplicitOnBeforeSerialize { get; private set; } // Has Interface.Method() {} instead of Method() {}

        public bool HasExplicitOnAfterDeserialize { get; private set; }

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
            if (this.AllAttributes.FirstOrDefault(x => x.FullName == TinyhandObjectAttributeFake.FullName) is { } objectAttribute)
            {
                try
                {
                    this.ObjectAttribute = TinyhandObjectAttributeFake.FromArray(objectAttribute.ConstructorArguments, objectAttribute.NamedArguments);
                }
                catch (InvalidCastException)
                {
                    this.Body.ReportDiagnostic(TinyhandBody.Error_AttributePropertyError, objectAttribute.Location);
                }
            }

            // KeyAttribute
            if (this.AllAttributes.FirstOrDefault(x => x.FullName == KeyAttributeFake.FullName) is { } keyAttribute)
            {
                this.KeyVisceralAttribute = keyAttribute;
                try
                {
                    this.KeyAttribute = KeyAttributeFake.FromArray(keyAttribute.ConstructorArguments, keyAttribute.NamedArguments);
                }
                catch (ArgumentNullException)
                {
                    this.Body.ReportDiagnostic(TinyhandBody.Error_KeyAttributeError, keyAttribute.Location);
                }
            }

            // IgnoreMemberAttribute
            if (this.AllAttributes.FirstOrDefault(x => x.FullName == IgnoreMemberAttributeFake.FullName) is { } ignoreMemberAttribute)
            {
                try
                {
                    this.IgnoreMemberAttribute = IgnoreMemberAttributeFake.FromArray(ignoreMemberAttribute.ConstructorArguments, ignoreMemberAttribute.NamedArguments);
                }
                catch (InvalidCastException)
                {
                    this.Body.ReportDiagnostic(TinyhandBody.Error_AttributePropertyError, ignoreMemberAttribute.Location);
                }
            }

            // ReconstructAttribute
            if (this.AllAttributes.FirstOrDefault(x => x.FullName == ReconstructAttributeFake.FullName) is { } reconstructAttribute)
            {
                try
                {
                    this.ReconstructAttribute = ReconstructAttributeFake.FromArray(reconstructAttribute.ConstructorArguments, reconstructAttribute.NamedArguments);
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

            // DefaultValueAttribute
            if (this.AllAttributes.FirstOrDefault(x => x.FullName == typeof(DefaultValueAttribute).FullName) is { } defaultValueAttribute)
            {
                this.DefaultValueLocation = defaultValueAttribute.Location;
                if (defaultValueAttribute.ConstructorArguments?.Length > 0)
                {
                    this.DefaultValue = defaultValueAttribute.ConstructorArguments[0];
                }
            }

            // Formatter condition (Serialize/Deserialize)
            this.FormatterCondition_Serialize = FormatterCondition.MemberMethod;
            if (this.AllInterfaces.Any(x => x == "Tinyhand.ITinyhandSerialize"))
            {
                this.FormatterCondition_Serialize = FormatterCondition.Declared;
            }
            else if (this.Generics_IsGeneric)
            {
                this.FormatterCondition_Serialize = FormatterCondition.StaticMethod;
            }

            // Formatter condition(Reconstruct)
            this.FormatterCondition_Reconstruct = FormatterCondition.MemberMethod;
            if (this.AllInterfaces.Any(x => x == "Tinyhand.ITinyhandReconstruct"))
            {
                this.FormatterCondition_Reconstruct = FormatterCondition.Declared;
            }
            else if (this.Generics_IsGeneric)
            {
                this.FormatterCondition_Reconstruct = FormatterCondition.StaticMethod;
            }

            // ITinyhandSerializationCallback
            if (this.AllInterfaces.Any(x => x == "Tinyhand.ITinyhandSerializationCallback"))
            {
                this.HasTinyhandSerializationCallback = true;
                this.HasExplicitOnBeforeSerialize = this.GetMembers(VisceralTarget.Method).Any(x => x.SimpleName == "Tinyhand.ITinyhandSerializationCallback.OnBeforeSerialize");
                this.HasExplicitOnAfterDeserialize = this.GetMembers(VisceralTarget.Method).Any(x => x.SimpleName == "Tinyhand.ITinyhandSerializationCallback.OnAfterDeserialize");
            }

            // Members: Property
            foreach (var x in this.AllMembers.Where(x => x.Kind == VisceralObjectKind.Property))
            {
                if (x.TypeObject != null && !x.IsStatic && x.Kind.IsValue())
                {
                    x.Configure();
                    this.Members.Add(x);
                }
            }

            // Members: Field
            foreach (var x in this.AllMembers.Where(x => x.Kind == VisceralObjectKind.Field))
            {
                if (x.TypeObject != null && !x.IsStatic && x.Kind.IsValue())
                {
                    x.Configure();
                    this.Members.Add(x);
                }
            }
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

            var cf = this.ConstructedFrom;
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

            // KeyAsPropertyName
            if (this.ObjectAttribute!.KeyAsPropertyName == true)
            {
                foreach (var x in this.GetMembers(VisceralTarget.FieldProperty))
                {
                    if (x.KeyAttribute == null)
                    {
                        if (!x.IsSerializable || x.IsReadOnly)
                        {// Not serializable
                            continue;
                        }
                        else if (!x.IsPublic && this.ObjectAttribute?.IncludePrivateMembers != true)
                        {// protected or private, if IncludePrivateMembers is false.
                            continue;
                        }
                        else if (x.IgnoreMemberAttribute != null)
                        {// KeyAttribute and IgnoreMemberAttribute are exclusive.
                            continue;
                        }

                        x.KeyAttribute = new KeyAttributeFake(x.SimpleName);
                    }
                    else if (x.KeyAttribute.IntKey != null)
                    {
                    }
                }
            }

            // String key
            if (this.Members.Any(x => x.KeyAttribute?.StringKey != null))
            {
                this.ObjectFlag |= TinyhandObjectFlag.StringKeyObject;

                this.CheckObject_StringKey();
            }
            else
            {// Int key
                this.CheckObject_IntKey();
            }

            // Check members.
            foreach (var x in this.Members)
            {
                x.CheckMember(this);
            }
        }

        private void CheckObject_StringKey()
        {
            this.Automata = new Automata(this);

            foreach (var x in this.Members)
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
        }

        private void CheckObject_IntKey()
        {
            // Integer key
            foreach (var x in this.Members)
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

            foreach (var x in this.Members)
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

            foreach (var x in this.Members.Where(x => x.ObjectFlag.HasFlag(TinyhandObjectFlag.IntKeyConflicted)))
            {
                this.Body.ReportDiagnostic(TinyhandBody.Error_IntKeyConflicted, x.KeyVisceralAttribute?.Location);
            }

            if (this.IntKey_Max >= 10 && this.IntKey_Max > (this.IntKey_Number * 2))
            {// Too many unused key.
                this.Body.ReportDiagnostic(TinyhandBody.Warning_IntKeyUnused, this.Location);
            }

            return;
        }

        /*
        /// <summary>
        /// Check circular dependency.
        /// </summary>
        /// <param name="parent">The object to check.</param>
        /// <returns>Returns true if circular dependency detected.</returns>
        public bool CheckCircular(TinyhandObject parent)
        {
            var ret = false;
            var circularCheck = new Stack<TinyhandObject>();
            circularCheck.Push(parent);

            CheckCircularCore(this);

            return ret;

            void CheckCircularCore(TinyhandObject obj)
            {
                var typeObject = obj.TypeObject;
                if (typeObject == null)
                {
                    return;
                }

                circularCheck.Push(typeObject);

                foreach (var x in typeObject.Members)
                {
                    if (x.TypeObject == null)
                    {
                        continue;
                    }

                    if (circularCheck.Contains(x.TypeObject))
                    {// Circular dependency
                        ret = true;
                        // diagnosticList.Add(Diagnostic.Create(TinyhandBody.Warning_Circular, x.Location, x.TypeObject?.FullName));
                    }
                    else
                    {
                        CheckCircularCore(x);
                    }
                }

                circularCheck.Pop();
            }
        }*/

        public void CheckMember(TinyhandObject parent)
        {
            // Avoid this.TypeObject!
            if (this.TypeObject == null)
            {
                return;
            }

            if (this.KeyAttribute != null)
            {// Has KeyAttribute
                if (!this.IsSerializable || this.IsReadOnly)
                {// Not serializable
                    this.Body.ReportDiagnostic(TinyhandBody.Error_NotSerializableMember, this.Location, this.SimpleName);
                }

                /* else if (!this.IsPublic && parent.ObjectAttribute?.IncludePrivateMembers != true)
                {// protected or private, if IncludePrivateMembers is false.
                    this.Body.ReportDiagnostic(TinyhandBody.Error_NotPublicMember, this.Location, this.SimpleName);
                }*/

                if (this.IgnoreMemberAttribute != null)
                {// KeyAttribute and IgnoreMemberAttribute are exclusive.
                    this.Body.ReportDiagnostic(TinyhandBody.Warning_KeyAndIgnoreAttribute, this.Location);
                }

                if (this.TypeObjectWithNullable != null && this.TypeObjectWithNullable.Object.ObjectAttribute == null && CoderResolver.Instance.IsCoderOrFormatterAvailable(this.TypeObjectWithNullable) == false)
                {// No Coder or Formatter
                    this.Body.ReportDiagnostic(TinyhandBody.Error_ObjectAttributeRequired, this.Location);
                }
            }
            else
            {// No KeyAttribute
                if (!this.IsPublic && parent.ObjectAttribute?.IncludePrivateMembers != true)
                {// protected or private AND IncludePrivateMembers is false.
                }
                else if (this.IgnoreMemberAttribute != null)
                {// Has IgnoreMemberAttribute
                }
                else
                {
                    this.Body.ReportDiagnostic(TinyhandBody.Error_KeyAttributeRequired, this.Location);
                }
            }

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

                // Avoid no KeyAttribute member
                if (this.KeyAttribute == null)
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
                            this.Body.ReportDiagnostic(TinyhandBody.Warning_Circular, this.Location, this.TypeObject.FullName);
                        }
                        else if (condition == ReconstructCondition.NoDefaultConstructor)
                        {
                            this.Body.ReportDiagnostic(TinyhandBody.Warning_NoDefaultConstructor, this.Location, this.TypeObject.FullName);
                        }
                        else if (condition == ReconstructCondition.NotReferenceType)
                        {
                            this.Body.ReportDiagnostic(TinyhandBody.Warning_NotReferenceType, this.Location, this.TypeObject.FullName);
                        }
                    }
                }
            }

            if (this.DefaultValue != null)
            {
                this.DefaultValueTypeName = VisceralHelper.Primitives_ShortenName(this.DefaultValue.GetType().FullName);
                if (VisceralDefaultValue.IsDefaultableType(this.TypeObject.SimpleName))
                {// Memeber is defaultable
                    this.IsDefaultable = true;
                    if (this.TypeObject.SimpleName != this.DefaultValueTypeName)
                    {// Type does not match.
                        this.DefaultValue = null;
                        this.Body.ReportDiagnostic(TinyhandBody.Warning_DefaultValueType, this.DefaultValueLocation ?? this.Location);
                    }
                }
                else if (this.TypeObject.Kind == VisceralObjectKind.Enum)
                {// Enum
                    if (this.DefaultValueTypeName != null && VisceralDefaultValue.IsEnumUnderlyingType(this.DefaultValueTypeName))
                    {
                        /* var idx = (int)this.DefaultValue;
                        if (idx >= 0 && idx < this.TypeObject.AllMembers.Length)
                        {
                            this.DefaultValue = new EnumString(this.TypeObject.AllMembers[idx].FullName);
                        }*/
                        if (this.TypeObject.Enum_GetEnumObjectFromObject(this.DefaultValue) is { } enumObject)
                        {
                            this.DefaultValue = new EnumString(enumObject.FullName);
                            this.IsDefaultable = true;
                        }
                        else if (this.DefaultValueTypeName != this.TypeObject.Enum_UnderlyingTypeObject?.FullName)
                        {
                            this.Body.ReportDiagnostic(TinyhandBody.Warning_DefaultValueType, this.DefaultValueLocation ?? this.Location);
                        }
                    }
                    else
                    {// Type does not match.
                        this.DefaultValue = null;
                        this.Body.ReportDiagnostic(TinyhandBody.Warning_DefaultValueType, this.DefaultValueLocation ?? this.Location);
                    }
                }
                else
                {// Other (Constructor is required)
                    this.IsDefaultable = false;
                    if (!this.TypeObject.AllMembers.Any(x => x.Method_IsConstructor && x.Method_Parameters.Length == 1 && x.Method_Parameters[0] == this.DefaultValueTypeName))
                    {// Type-mathed constructor is required.
                        this.DefaultValue = null;
                        this.Body.ReportDiagnostic(TinyhandBody.Warning_DefaultValueConstructor, this.DefaultValueLocation ?? this.Location);
                    }
                }
            }
        }

        public void Check()
        {
            if (this.ObjectFlag.HasFlag(TinyhandObjectFlag.Checked))
            {
                return;
            }

            this.ObjectFlag |= TinyhandObjectFlag.Checked;

            Debug.Assert(this.ObjectAttribute != null);
            this.CheckObject();
        }

        public static void GenerateLoader(ScopingStringBuilder ssb, GeneratorInformation info, List<TinyhandObject> list, bool appendNamespace)
        {
            var classFormat = "__gen__tf__{0:D4}";
            var list2 = list.SelectMany(x => x.ConstructedObjects).Where(x => x.ObjectAttribute != null);

            ssb.AppendLine("[ModuleInitializer]");
            using (var m = ssb.ScopeBrace("internal static void __gen__load()"))
            {
                foreach (var x in list2)
                {
                    var name = string.Format(classFormat, x.FormatterNumber);
                    var typeName = appendNamespace ? x.FullName : x.LocalName;
                    ssb.AppendLine($"GeneratedResolver.Instance.SetFormatter<{typeName}>(new {name}());");
                }
            }

            ssb.AppendLine();

            foreach (var x in list2)
            {
                var name = string.Format(classFormat, x.FormatterNumber);
                var typeName = appendNamespace ? x.FullName : x.LocalName;
                using (var cls = ssb.ScopeBrace($"class {name}: ITinyhandFormatter<{typeName}>"))
                {
                    if (x.FormatterCondition_Serialize == FormatterCondition.StaticMethod)
                    {// Static method
                        ssb.AppendLine($"public void Serialize(ref TinyhandWriter w, {typeName + x.QuestionMarkIfReferenceType} v, TinyhandSerializerOptions o) => {typeName}.Serialize(ref w, v, o);");
                        ssb.AppendLine($"public {typeName + x.QuestionMarkIfReferenceType} Deserialize(ref TinyhandReader r, TinyhandSerializerOptions o) => {typeName}.Deserialize{x.GenericsNumberString}(ref r, o);");
                    }
                    else
                    {// Member method
                        using (var s = ssb.ScopeBrace($"public void Serialize(ref TinyhandWriter w, {typeName + x.QuestionMarkIfReferenceType} v, TinyhandSerializerOptions o)"))
                        {
                            if (x.Kind.IsReferenceType())
                            {// Reference type
                                ssb.AppendLine("if (v == null) w.WriteNil();");
                                ssb.AppendLine("else v.Serialize(ref w, o);");
                            }
                            else
                            {// Value type
                                ssb.AppendLine("v.Serialize(ref w, o);");
                            }
                        }

                        using (var d = ssb.ScopeBrace($"public {typeName + x.QuestionMarkIfReferenceType} Deserialize(ref TinyhandReader r, TinyhandSerializerOptions o)"))
                        {
                            if (x.Kind.IsReferenceType())
                            {// Reference type
                                ssb.AppendLine("if (r.TryReadNil()) return default;");
                            }

                            ssb.AppendLine($"var v = new {typeName}();");
                            ssb.AppendLine("v.Deserialize(ref r, o);");
                            ssb.AppendLine("return v;");
                        }
                    }

                    if (x.FormatterCondition_Reconstruct == FormatterCondition.StaticMethod)
                    {// Static method
                        ssb.AppendLine($"public {typeName} Reconstruct(TinyhandSerializerOptions o) => {typeName}.Reconstruct{x.GenericsNumberString}(o);");
                    }
                    else
                    {// Member method
                        using (var r = ssb.ScopeBrace($"public {typeName} Reconstruct(TinyhandSerializerOptions o)"))
                        {
                            ssb.AppendLine($"var v = new {typeName}();");
                            ssb.AppendLine("v.Reconstruct(o);");
                            ssb.AppendLine("return v;");
                        }
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
                if (this.FormatterCondition_Serialize == FormatterCondition.MemberMethod)
                {
                    interfaceString = " : ITinyhandSerialize";
                }

                if (this.FormatterCondition_Reconstruct == FormatterCondition.MemberMethod)
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
                    GenerateLoader(ssb, info, this.Children, false);
                }
            }
        }

        internal void Generate_OnBeforeSerialize(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            if (this.HasTinyhandSerializationCallback)
            {
                if (this.HasExplicitOnBeforeSerialize)
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
            if (this.HasTinyhandSerializationCallback)
            {
                if (this.HasExplicitOnAfterDeserialize)
                {
                    ssb.AppendLine($"((Tinyhand.ITinyhandSerializationCallback){ssb.FullObject}).OnAfterDeserialize();");
                }
                else
                {
                    ssb.AppendLine($"((Tinyhand.ITinyhandSerializationCallback){ssb.FullObject}).OnAfterDeserialize();");

                    ssb.AppendLine($"{ssb.FullObject}.OnAfterDeserialize();");
                }
            }
        }

        internal void GenerateSerialize_MemberMethod(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            using (var m = ssb.ScopeBrace($"public void Serialize(ref TinyhandWriter writer, TinyhandSerializerOptions options)"))
            using (var v = ssb.ScopeObject("this"))
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

        internal void GenerateSerialize_StaticMethod(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            using (var m = ssb.ScopeBrace($"public static void Serialize(ref TinyhandWriter writer, {this.LocalName + this.QuestionMarkIfReferenceType} value, TinyhandSerializerOptions options)"))
            using (var v = ssb.ScopeObject("value"))
            {
                if (this.Kind.IsReferenceType())
                {
                    using (var e = ssb.ScopeBrace($"if ({v.FullObject} == null)"))
                    {
                        ssb.AppendLine("writer.WriteNil();");
                        ssb.AppendLine("return;");
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
            }
        }

        internal void GenerateDeserialize_MemberMethod(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            using (var m = ssb.ScopeBrace($"public void Deserialize(ref TinyhandReader reader, TinyhandSerializerOptions options)"))
            using (var v = ssb.ScopeObject("this"))
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

        internal void GenerateDeserialize_StaticMethod(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            using (var m = ssb.ScopeBrace($"public static {this.LocalName + this.QuestionMarkIfReferenceType} Deserialize{this.GenericsNumberString}(ref TinyhandReader reader, TinyhandSerializerOptions options)"))
            {
                ssb.AppendLine("if (reader.TryReadNil()) return default;"); // Nil checked
                ssb.AppendLine($"var v = new {this.FullName}();");

                using (var v = ssb.ScopeObject("v"))
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

                    ssb.AppendLine($"return {v.FullObject};");
                }
            }
        }

        internal void GenerateReconstructCore(ScopingStringBuilder ssb, GeneratorInformation info, TinyhandObject? x)
        {
            var withNullable = x?.TypeObjectWithNullable;
            if (x == null || withNullable == null)
            {// no object
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

        internal void GenerateReconstruct_MemberMethod(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            using (var m = ssb.ScopeBrace($"public void Reconstruct(TinyhandSerializerOptions options)"))
            using (var v = ssb.ScopeObject("this"))
            {
                foreach (var x in this.Members.Where(x => x.ReconstructState == ReconstructState.Do))
                {
                    using (var c = ssb.ScopeObject(x.SimpleName))
                    {
                        this.GenerateReconstructCore(ssb, info, x);
                    }
                }
            }
        }

        internal void GenerateReconstruct_StaticMethod(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            using (var m = ssb.ScopeBrace($"public static {this.LocalName} Reconstruct{this.GenericsNumberString}(TinyhandSerializerOptions options)"))
            using (var v = ssb.ScopeObject("v"))
            {
                ssb.AppendLine($"var {v.FullObject} = new {this.FullName}();");

                foreach (var x in this.Members.Where(x => x.ReconstructState == ReconstructState.Do))
                {
                    using (var c = ssb.ScopeObject(x.SimpleName))
                    {
                        this.GenerateReconstructCore(ssb, info, x);
                    }
                }

                ssb.AppendLine($"return {v.FullObject};");
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

            // Serialize/Deserialize
            if (this.FormatterCondition_Serialize == FormatterCondition.MemberMethod)
            {
                this.GenerateSerialize_MemberMethod(ssb, info);
                this.GenerateDeserialize_MemberMethod(ssb, info);
            }
            else if (this.FormatterCondition_Serialize == FormatterCondition.StaticMethod)
            {
                this.GenerateSerialize_StaticMethod(ssb, info);
                this.GenerateDeserialize_StaticMethod(ssb, info);
            }

            // Reconstruct
            if (this.FormatterCondition_Reconstruct == FormatterCondition.MemberMethod)
            {
                this.GenerateReconstruct_MemberMethod(ssb, info);
            }
            else if (this.FormatterCondition_Serialize == FormatterCondition.StaticMethod)
            {
                this.GenerateReconstruct_StaticMethod(ssb, info);
            }

            // MemberNotNull
            if (this.FormatterCondition_Reconstruct == FormatterCondition.MemberMethod)
            {
                this.GenerateMemberNotNull_MemberMethod(ssb, info);
            }
            else if (this.FormatterCondition_Serialize == FormatterCondition.StaticMethod)
            {
                this.GenerateMemberNotNull_StaticMethod(ssb, info);
            }

            return;
        }

        internal void GenerateDeserializeCore(ScopingStringBuilder ssb, GeneratorInformation info, TinyhandObject? x)
        {
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
        {
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
        {
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

            ssb.AppendLine($"writer.WriteMapHeader({this.Automata.NodeList.Count});");
            var skipDefaultValue = this.ObjectAttribute?.SkipSerializingDefaultValue == true;
            foreach (var x in this.Automata.NodeList)
            {
                ssb.AppendLine($"writer.WriteString({this.SimpleName}.{string.Format(TinyhandBody.StringKeyFieldFormat, x.Index)});");
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
