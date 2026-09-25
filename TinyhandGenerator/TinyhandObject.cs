// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Arc.Visceral;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Tinyhand.Coders;
using Tinyhand.Generator.Internal;

#pragma warning disable SA1202 // Elements should be ordered by access
#pragma warning disable SA1204 // Static elements should appear before instance elements
#pragma warning disable SA1310 // Field names should not contain underscore
#pragma warning disable SA1401 // Fields should be private
#pragma warning disable SA1602 // Enumeration items should be documented

namespace Tinyhand.Generator;

public enum ConvertToStringMode
{
    NotSpecified,
    ConvertToString,
    NoConvertToString,
}

public enum ReconstructMode
{
    IfPreferable, // Reconstruct a member unless no default constructor is available or members depends on each other (cricular dependency).
    Always, // An exception is thrown if a member doesn't have a default counsturcotr or members depends on each other.
    Never, // Don't reconstruct.
}

public enum MethodImplementationKind
{
    MemberMethod, // member method (generated)
    StaticMethod, // static method (generated, for generic class)
    Declared, // declared (user-declared)
    ExplicitlyDeclared, // declared (explicit interface)
}

[Flags]
public enum TinyhandObjectFlags
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
    AddPropertyTarget = 1 << 11,
    UnsafeConstructor = 1 << 12,
    MinimumConstructorPrepared = 1 << 13,

    DerivedFromStoragePoint = 1 << 20, // Derived from StoragePoint (thread-safe)
    HasIStringConvertible = 1 << 21, // Has IStringConvertible interface
    HasITinyhandSerializable = 1 << 22, // Has ITinyhandSerializable interface
    CanCreateInstance = 1 << 23, // Can create an instance
    InterfaceImplemented = 1 << 24, // ITinyhandSerializable, ITinyhandReconstructable, ITinyhandCloneable
    IStructuralObjectImplemented = 1 << 25, // IStructuralObject interface is implemented
    HasITinyhandCustomJournal = 1 << 26, // Has ITinyhandCustomJournal interface
    HasValueLinkObject = 1 << 27, // Has ValueLinkgObject attribute
    IsRepeatableRead = 1 << 28, // IsolationLevel.RepeatableRead
    HasIIntegralityObject = 1 << 29, // Has IIntegralityObject interface
    ExternalObject = 1 << 30, // Has external attribute
    RequiresUnsafeDeserialize = 1 << 31, // Requires unsafe deserialize
}

public class TinyhandObject : VisceralObjectBase<TinyhandObject>
{
    public TinyhandObject()
    {
    }

    public new TinyhandBody Body => (TinyhandBody)((VisceralObjectBase<TinyhandObject>)this).Body;

    public TinyhandObjectFlags ObjectFlags { get; private set; }

    public TinyhandObjectAttributeData? ObjectAttribute { get; private set; }

    public TinyhandUnion? Union { get; set; }

    public KeyAttributeData? KeyAttribute { get; private set; }

    public VisceralAttribute? KeyVisceralAttribute { get; private set; }

    public IgnoreMemberAttributeData? IgnoreMemberAttribute { get; private set; }

    public ReconstructAttributeData? ReconstructAttribute { get; private set; }

    public ReconstructMode ReconstructMode { get; private set; }

    public ReuseAttributeData? ReuseAttribute { get; private set; }

    public MaxLengthAttributeData? MaxLengthAttribute { get; private set; }

    public TinyhandObject? PublicMinimumConstructor { get; private set; }

    public TinyhandObject? MinimumConstructor { get; private set; }

    public TinyhandObject? PrimaryConstructor { get; private set; }

    public TinyhandObject[] Members { get; private set; } = Array.Empty<TinyhandObject>(); // Members is not static && property or field

    public IEnumerable<TinyhandObject> GetMembersWithFlag(TinyhandObjectFlags flag) => this.Members.Where(x => x.ObjectFlags.HasFlag(flag));

    public List<TinyhandCallbackMethod>? CallbackMethods { get; private set; }

    public bool IsDefaultable => this.DefaultValue is not null &&
        (this.TypeObject?.Kind == VisceralObjectKind.Struct || this.TypeObject?.FullName == "string");

    public string? DefaultValue { get; private set; }

    // public string? DefaultValueTypeName { get; private set; }

    // public Location? DefaultValueLocation { get; private set; }

    public TinyhandObject? DefaultInterface { get; private set; }

    public bool SupportsStructuralObject => this.ObjectAttribute?.Structural == true || this.ObjectFlags.HasFlag(TinyhandObjectFlags.IStructuralObjectImplemented);

    public bool IsAbstractOrInterface => this.Kind == VisceralObjectKind.Interface || (this.symbol is INamedTypeSymbol nts && nts.IsAbstract);

    public List<TinyhandObject>? Children { get; private set; } // The opposite of ContainingObject

    public List<TinyhandObject>? ConstructedObjects { get; private set; } // The opposite of ConstructedFrom

    public VisceralIdentifier Identifier { get; private set; } = VisceralIdentifier.Default;

    public TinyhandObject? ClosedGenericHint { get; private set; }

    public string UnsafeModifier => this.ObjectFlags.HasFlag(TinyhandObjectFlags.RequiresUnsafeDeserialize) ? "unsafe " : string.Empty;

    public string InModifierIfStruct => this.Kind == VisceralObjectKind.Struct ? "in " : string.Empty;

    public TinyhandObject?[]? IntKey_Array;

    public int IntKey_IncludedCount;

    /// <summary>
    /// Gets a value indicating whether members equal to their default values are written as nil.<br/>
    /// A struct is deserialized from default(T) rather than from its constructor, so its initializers could not restore a skipped value.
    /// </summary>
    public bool SkipDefaultValues => this.ObjectAttribute?.SkipDefaultValues == true && !this.Kind.IsValueType();

    internal VisceralTrieString<TinyhandObject>? StringTrie;

    internal int StringTrieReconstructNumber = 0;

    public int IntKey_Min { get; private set; } = -1;

    public int IntKey_Max { get; private set; } = -1;

    public int IntKey_Count { get; private set; } = 0;

    public bool HasAlternateKey => this.ObjectAttribute?.AddAlternateKey == true;

    public MethodImplementationKind MethodCondition_Serialize { get; private set; }

    public MethodImplementationKind MethodCondition_Deserialize { get; private set; }

    public MethodImplementationKind MethodCondition_GetTypeIdentifier { get; private set; } // GetTypeIdentifierCode

    public MethodImplementationKind MethodCondition_Reconstruct { get; private set; }

    public MethodImplementationKind MethodCondition_CanSkipSerialization { get; private set; }

    // public MethodImplementationKind MethodCondition_SetDefaultValue { get; private set; }

    public MethodImplementationKind MethodCondition_Clone { get; private set; }

    public MethodImplementationKind MethodCondition_WriteCustomLocator { get; private set; }

    public MethodImplementationKind MethodCondition_ReadCustomRecord { get; private set; }

    public bool RequiresGetAccessor { get; private set; }

    public bool RequiresSetAccessor { get; private set; }

    public string? RefFieldDelegate { get; private set; } // ref RefFieldDelegate(class), ref RefFieldDelegate(in struct)

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
            if (!string.IsNullOrEmpty(this.KeyAttribute?.PropertyName) &&
                this.KeyAttribute!.PropertyAccessibility != PropertyAccessibility.GetterOnly &&
                !this.ObjectFlags.HasFlag(TinyhandObjectFlags.IsRepeatableRead))
            {
                return this.KeyAttribute!.PropertyName;
            }
            else
            {
                return this.SimpleName;
            }
        }
    }

    public void ConfigureIfAttributed()
    {// Configure the type (assuming an external assembly).
        if (this.ObjectFlags.HasFlag(TinyhandObjectFlags.Configured))
        {
            return;
        }

        if (this.IsSystem)
        {
            this.ObjectFlags |= TinyhandObjectFlags.Configured;
            return;
        }

        foreach (var x in this.AllAttributes)
        {
            if (x.FullName == TinyhandObjectAttributeData.FullName)
            {
                this.Configure();
                break;
            }
            else if (x.FullName == TinyhandUnionAttributeData.FullName)
            {
                this.Configure();
                break;
            }
        }

        this.ObjectFlags |= TinyhandObjectFlags.Configured;
    }

    public void Configure()
    {
        if (this.ObjectFlags.HasFlag(TinyhandObjectFlags.Configured))
        {
            return;
        }

        this.ObjectFlags |= TinyhandObjectFlags.Configured;

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
        if (this.AllAttributes.FirstOrDefault(x => x.FullName == TinyhandObjectAttributeData.FullName) is { } objectAttribute)
        {
            try
            {
                this.ObjectAttribute = TinyhandObjectAttributeData.FromArray(objectAttribute.ConstructorArguments, objectAttribute.NamedArguments);
            }
            catch (InvalidCastException)
            {
                this.Body.ReportDiagnostic(TinyhandBody.Error_AttributePropertyTypeMismatch, objectAttribute.Location);
            }
        }

        // UnionAttribute
        this.Union = TinyhandUnion.CreateFromObject(this);
        if (this.Union != null && this.ObjectAttribute == null)
        {// Add ObjectAttribute
            this.ObjectAttribute = new TinyhandObjectAttributeData();
        }

        // UnionToAttribute
        /*if (this.Generics_Kind != VisceralGenericsKind.ClosedGeneric)
        {// Avoid duplication
            foreach (var x in this.AllAttributes.Where(a => a.FullName == TinyhandUnionToAttributeData.FullName))
            {
                TinyhandUnionToAttributeData unionTo;
                try
                {
                    unionTo = TinyhandUnionToAttributeData.FromArray(x.ConstructorArguments, x.NamedArguments, x.Location);
                    if (unionTo.BaseType == null)
                    {
                        continue;
                    }
                }
                catch (InvalidCastException)
                {
                    this.Body.ReportDiagnostic(TinyhandBody.Error_AttributePropertyTypeMismatch, x.Location);
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
            if (x.FullName == KeyAttributeData.FullName)
            {// KeyAttribute
                this.KeyVisceralAttribute = x;
                try
                {
                    this.KeyAttribute = KeyAttributeData.FromArray(x.ConstructorArguments, x.NamedArguments);
                }
                catch (ArgumentNullException)
                {
                    this.Body.ReportDiagnostic(TinyhandBody.Error_InvalidKeyAttribute, x.Location);
                }
            }
            else if (x.FullName == MemberNameAsKeyAttributeData.FullName)
            {// KeyAsNameAttribute
                if (this.KeyAttribute != null)
                {// KeyAttribute and KeyAsNameAttribute are exclusive.
                    this.Body.ReportDiagnostic(TinyhandBody.Warning_KeyAsNameExclusive, x.Location);
                }
                else
                {// KeyAsNameAttribute to KeyAttribute.
                    this.KeyVisceralAttribute = x;
                    this.KeyAttribute = new KeyAttributeData(this.SimpleName);
                    /*try
                    {
                        var v = VisceralHelper.GetValue(-1, nameof(KeyAttributeData.ConvertToString), x.ConstructorArguments, x.NamedArguments);
                        if (v != null)
                        {
                            this.KeyAttribute.ConvertToString = (bool)v;
                        }
                    }
                    catch
                    {
                    }*/
                }
            }
            else if (x.FullName == IgnoreMemberAttributeData.FullName)
            {// IgnoreMemberAttribute
                try
                {
                    this.IgnoreMemberAttribute = IgnoreMemberAttributeData.FromArray(x.ConstructorArguments, x.NamedArguments);
                }
                catch (InvalidCastException)
                {
                    this.Body.ReportDiagnostic(TinyhandBody.Error_AttributePropertyTypeMismatch, x.Location);
                }
            }
            else if (x.FullName == ReconstructAttributeData.FullName)
            {// ReconstructAttribute
                try
                {
                    this.ReconstructAttribute = ReconstructAttributeData.FromArray(x.ConstructorArguments, x.NamedArguments);
                }
                catch (InvalidCastException)
                {
                    this.Body.ReportDiagnostic(TinyhandBody.Error_AttributePropertyTypeMismatch, x.Location);
                }
            }
            else if (x.FullName == ReuseAttributeData.FullName)
            {// ReuseAttribute
                try
                {
                    this.ReuseAttribute = ReuseAttributeData.FromArray(x.ConstructorArguments, x.NamedArguments);
                }
                catch (InvalidCastException)
                {
                    this.Body.ReportDiagnostic(TinyhandBody.Error_AttributePropertyTypeMismatch, x.Location);
                }
            }
            else if (x.FullName == MaxLengthAttributeData.FullName)
            {// MaxLengthAttribute
                try
                {
                    this.MaxLengthAttribute = MaxLengthAttributeData.FromArray(x.ConstructorArguments, x.NamedArguments);
                }
                catch (InvalidCastException)
                {
                    this.Body.ReportDiagnostic(TinyhandBody.Error_AttributePropertyTypeMismatch, x.Location);
                }
            }
            else if (x.FullName == ValueLinkObjectAttributeData.FullName)
            {
                try
                {
                    var valueLinkAttribute = ValueLinkObjectAttributeData.FromArray(x.ConstructorArguments, x.NamedArguments);

                    this.ObjectFlags |= TinyhandObjectFlags.HasValueLinkObject;
                    if (valueLinkAttribute.Isolation == IsolationLevel.RepeatableRead)
                    {
                        this.ObjectFlags |= TinyhandObjectFlags.IsRepeatableRead;
                    }

                    if (valueLinkAttribute.Integrality)
                    {
                        this.ObjectFlags |= TinyhandObjectFlags.HasIIntegralityObject;
                    }
                }
                catch (InvalidCastException)
                {
                    this.Body.ReportDiagnostic(TinyhandBody.Error_AttributePropertyTypeMismatch, x.Location);
                }
            }
        }

        if (this.ReconstructAttribute != null)
        {
            if (this.ReconstructAttribute.Reconstruct == true)
            {
                this.ReconstructMode = ReconstructMode.Always;
            }
            else
            {
                this.ReconstructMode = ReconstructMode.Never;
            }
        }

        if (this.ObjectAttribute != null)
        {// TinyhandObject
            this.ConfigureObject();
        }
    }

    private string? GetDefaultValue(TinyhandObject typeObject, ISymbol? symbol)
    {
        if (symbol is null)
        {
            return default;
        }

        foreach (var x in symbol.DeclaringSyntaxReferences)
        {
            var (equalsSyntax, syntaxTree) = x.GetSyntax() switch
            {
                PropertyDeclarationSyntax property => (property.Initializer, property.SyntaxTree),
                VariableDeclaratorSyntax variable => (variable.Initializer, variable.SyntaxTree),
                _ => default,
            };

            if (equalsSyntax is null)
            {
                continue;
            }

            var rawValue = equalsSyntax.Value.ToString();

            if (typeObject.Kind == VisceralObjectKind.Enum)
            {// Enum: the initializer text may depend on the using directives of its source file, so the constant is cast instead.
                var enumConstant = this.Body.Compilation.GetSemanticModel(syntaxTree).GetConstantValue(equalsSyntax.Value);
                return enumConstant.Value is IFormattable enumValue ? $"(({typeObject.FullName})({enumValue.ToString(null, CultureInfo.InvariantCulture)}))" : default;
            }
            else if (!typeObject.IsPrimitive &&
                typeObject.Kind == VisceralObjectKind.Struct)
            {// Struct is not supported.
                return default;
            }

            if (rawValue == "null" ||
                rawValue == "null!" ||
                rawValue == "default" ||
                rawValue == "default!")
            {// Primary constants
                if (typeObject.Kind == VisceralObjectKind.Class)
                {
                    return rawValue;
                }
                else
                {
                    return default;
                }

                /*if (typeObject.ObjectAttribute is not null)
                {
                    return rawValue;
                }
                else
                {
                    return default;
                }*/
            }
            else if (rawValue == "[]")
            {
                if (typeObject.Array_Rank > 0)
                {
                    return rawValue;
                }
                else
                {
                    return default;
                }
            }
            else if (rawValue == "true" ||
                rawValue == "false" ||
                rawValue == "string.Empty" ||
                rawValue == "\"\"" ||
                rawValue == "new()" ||
                rawValue.StartsWith("new "))
            {// Not default value
                return default;
            }

            /*if (symbol is IPropertySymbol ps && ps.Type.TypeKind == TypeKind.Enum)
            {
                return rawValue;
            }
            else if (symbol is IFieldSymbol fs && fs.Type.TypeKind == TypeKind.Enum)
            {
                return rawValue;
            }*/

            // var cv = equalsSyntax.Value.ToString();
            var model = this.Body.Compilation.GetSemanticModel(syntaxTree);
            var constantValue = model.GetConstantValue(equalsSyntax.Value);
            if (constantValue.Value is not { } valueObject)
            {
                return default;
            }

            /*if (valueObject is float f)
            {
                return FloatToDefaultString(f);
            }
            else if (valueObject is double d)
            {
                return DoubleToDefaultString(d);
            }
            else
            {
                return valueObject.ToString();
            }*/

            // The literal is C# code, so it must not depend on the current culture (e.g. U+2212 as the negative sign).
            return valueObject switch
            {
                bool => default, // Same as the true and false literals above.
                char c => Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(c, true),
                string s => Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(s, true),
                uint u => u.ToString(CultureInfo.InvariantCulture) + "u",
                long l => l.ToString(CultureInfo.InvariantCulture) + "L",
                ulong ul => ul.ToString(CultureInfo.InvariantCulture) + "ul",
                float f => FloatToDefaultString(f),
                double d => DoubleToDefaultString(d),
                decimal m => m.ToString(CultureInfo.InvariantCulture) + "m",
                IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture), // sbyte, byte, short, ushort, int
                _ => default,
            };

            static string FloatToDefaultString(float f)
            {
                if (float.IsNaN(f))
                {
                    return "float.NaN";
                }
                else if (float.IsPositiveInfinity(f))
                {
                    return "float.PositiveInfinity";
                }
                else if (float.IsNegativeInfinity(f))
                {
                    return "float.NegativeInfinity";
                }
                else
                {
                    return f.ToString("R", CultureInfo.InvariantCulture) + "f";
                }
            }

            static string DoubleToDefaultString(double d)
            {
                if (double.IsNaN(d))
                {
                    return "double.NaN";
                }
                else if (double.IsPositiveInfinity(d))
                {
                    return "double.PositiveInfinity";
                }
                else if (double.IsNegativeInfinity(d))
                {
                    return "double.NegativeInfinity";
                }
                else
                {
                    return d.ToString("R", CultureInfo.InvariantCulture) + "d";
                }
            }
        }

        return default;
    }

    private bool MethodCompare_Serialize(IMethodSymbol ms)
        => ms.IsStatic && ms.ReturnsVoid && ms.Parameters.Length == 3 &&
        ms.Parameters[0].RefKind == RefKind.Ref && ms.Parameters[0].Type.Name == "TinyhandWriter" &&
        ms.Parameters[1].RefKind == RefKind.Ref && ms.Parameters[1].Type.Name == this.SimpleName &&
        ms.Parameters[2].RefKind == RefKind.None && ms.Parameters[2].Type.Name == "TinyhandSerializerOptions";

    private bool MethodCompare_Deserialize(IMethodSymbol ms)
        => ms.IsStatic && ms.ReturnsVoid && ms.Parameters.Length == 3 &&
        ms.Parameters[0].RefKind == RefKind.Ref && ms.Parameters[0].Type.Name == "TinyhandReader" &&
        ms.Parameters[1].RefKind == RefKind.Ref && ms.Parameters[1].Type.Name == this.SimpleName &&
        ms.Parameters[2].RefKind == RefKind.None && ms.Parameters[2].Type.Name == "TinyhandSerializerOptions";

    private bool MethodCompare_GetTypeIdentifier(IMethodSymbol ms)
        => ms.IsStatic && ms.ReturnType.Name == "UInt64" && ms.Parameters.Length == 0;

    private bool MethodCompare_Reconstruct(IMethodSymbol ms)
        => ms.IsStatic && ms.ReturnsVoid && ms.Parameters.Length == 2 &&
        ms.Parameters[0].RefKind == RefKind.Ref && ms.Parameters[0].Type.Name == this.SimpleName &&
        ms.Parameters[1].RefKind == RefKind.None && ms.Parameters[1].Type.Name == "TinyhandSerializerOptions";

    private bool MethodCompare_Clone(IMethodSymbol ms)
        => ms.IsStatic && ms.ReturnType.Name == this.SimpleName && ms.Parameters.Length == 2 &&
        ms.Parameters[0].RefKind == RefKind.Ref && ms.Parameters[0].Type.Name == this.SimpleName &&
        ms.Parameters[1].RefKind == RefKind.None && ms.Parameters[1].Type.Name == "TinyhandSerializerOptions";

    private void ConfigureObject()
    {
        if (this.ObjectAttribute?.External == true)
        {
            this.ObjectFlags |= TinyhandObjectFlags.ExternalObject;
            return;
        }

        // Method condition (Serialize/Deserialize)
        this.MethodCondition_Serialize = MethodImplementationKind.StaticMethod;
        this.MethodCondition_Deserialize = MethodImplementationKind.StaticMethod;
        this.MethodCondition_GetTypeIdentifier = MethodImplementationKind.StaticMethod; // GetTypeIdentifierCode
        this.MethodCondition_Reconstruct = MethodImplementationKind.StaticMethod;
        this.MethodCondition_Clone = MethodImplementationKind.StaticMethod;

        var className = this.FullName.RemoveWhitespace(); // Class<T1, T2> -> Class<T1,T2>
        var serializeInterface = $"Tinyhand.ITinyhandSerializable<{className}>";
        var serializeName = $"{serializeInterface}.Serialize";
        var deserializeName = $"{serializeInterface}.Deserialize";
        var getTypeIdentifierName = $"{serializeInterface}.GetTypeIdentifier"; // GetTypeIdentifierCode
        var reconstructName = $"Tinyhand.ITinyhandReconstructable<{className}>.Reconstruct";
        var cloneName = $"Tinyhand.ITinyhandCloneable<{className}>.Clone";

        foreach (var x in this.GetMembers(VisceralTarget.Method))
        {
            if (x.symbol is not IMethodSymbol ms)
            {
                continue;
            }

            /*if (this.MethodCompare_Serialize(ms))
            {// For debugging
                this.Body.ReportDiagnostic(TinyhandBody.Warning_Information, ms.Locations.FirstOrDefault(), $"Serialize {serializeName} - {ms.Name} - {x.LocalName}");
            }*/

            if (ms.Name == "Serialize" && this.MethodCompare_Serialize(ms))
            {
                this.MethodCondition_Serialize = MethodImplementationKind.Declared;
                this.ObjectFlags |= TinyhandObjectFlags.HasITinyhandSerializable;
            }
            else if (ms.Name == serializeName && this.MethodCompare_Serialize(ms))
            {
                this.MethodCondition_Serialize = MethodImplementationKind.ExplicitlyDeclared;
                this.ObjectFlags |= TinyhandObjectFlags.HasITinyhandSerializable;
            }
            else if (ms.Name == "Deserialize" && this.MethodCompare_Deserialize(ms))
            {
                this.MethodCondition_Deserialize = MethodImplementationKind.Declared;
                this.ObjectFlags |= TinyhandObjectFlags.HasITinyhandSerializable;
            }
            else if (ms.Name == deserializeName && this.MethodCompare_Deserialize(ms))
            {
                this.MethodCondition_Deserialize = MethodImplementationKind.ExplicitlyDeclared;
                this.ObjectFlags |= TinyhandObjectFlags.HasITinyhandSerializable;
            }
            else if (ms.Name == "GetTypeIdentifier" && this.MethodCompare_GetTypeIdentifier(ms))
            {// GetTypeIdentifierCode
                this.MethodCondition_GetTypeIdentifier = MethodImplementationKind.Declared;
            }
            else if (ms.Name == getTypeIdentifierName && this.MethodCompare_GetTypeIdentifier(ms))
            {
                this.MethodCondition_GetTypeIdentifier = MethodImplementationKind.ExplicitlyDeclared;
            }
            else if (ms.Name == "Reconstruct" && this.MethodCompare_Reconstruct(ms))
            {
                this.MethodCondition_Reconstruct = MethodImplementationKind.Declared;
            }
            else if (ms.Name == reconstructName && this.MethodCompare_Reconstruct(ms))
            {
                this.MethodCondition_Reconstruct = MethodImplementationKind.ExplicitlyDeclared;
            }
            else if (ms.Name == "Clone" && this.MethodCompare_Clone(ms))
            {
                this.MethodCondition_Clone = MethodImplementationKind.Declared;
            }
            else if (ms.Name == cloneName && this.MethodCompare_Clone(ms))
            {
                this.MethodCondition_Clone = MethodImplementationKind.ExplicitlyDeclared;
            }
        }

        // Method condition (Default)
        var defaultInterface = this.Interfaces.FirstOrDefault(x => x.FullName.StartsWith($"{TinyhandBody.Namespace}.{TinyhandBody.ITinyhandDefaultName}") &&
        x.SimpleName == TinyhandBody.ITinyhandDefaultName);
        if (defaultInterface != null)
        {// ITinyhandDefault implemented
            this.DefaultInterface = defaultInterface;

            if (this.GetMembers(VisceralTarget.Method).Any(x => x.SimpleName == $"{this.DefaultInterface.FullName}.{TinyhandBody.CanSkipSerializationMethod}"))
            {
                this.MethodCondition_CanSkipSerialization = MethodImplementationKind.ExplicitlyDeclared;
            }
            else
            {
                this.MethodCondition_CanSkipSerialization = MethodImplementationKind.Declared;
            }

            /*if (this.GetMembers(VisceralTarget.Method).Any(x => x.SimpleName == $"{this.DefaultInterface.FullName}.{TinyhandBody.SetDefaultValueMethod}"))
            {
                this.MethodCondition_SetDefaultValue = MethodImplementationKind.ExplicitlyDeclared;
            }
            else
            {
                this.MethodCondition_SetDefaultValue = MethodImplementationKind.Declared;
            }*/
        }

        // Method condition (IStructuralObject)
        var structuralInterface = $"{TinyhandBody.Namespace}.{TinyhandBody.IStructuralObjectName}";
        if (this.Interfaces.Any(x => x.FullName == structuralInterface))
        {// IStructuralObject implemented
            this.ObjectFlags |= TinyhandObjectFlags.IStructuralObjectImplemented;
        }

        structuralInterface = $"{TinyhandBody.Namespace}.{TinyhandBody.ITinyhandCustomJournalName}";
        this.MethodCondition_WriteCustomLocator = MethodImplementationKind.MemberMethod;
        this.MethodCondition_ReadCustomRecord = MethodImplementationKind.MemberMethod;
        if (this.Interfaces.Any(x => x.FullName == structuralInterface))
        {// ITinyhandCustomJournal implemented
            this.ObjectFlags |= TinyhandObjectFlags.HasITinyhandCustomJournal;

            var methodName = structuralInterface + ".WriteCustomLocator";
            if (this.GetMembers(VisceralTarget.Method).Any(x => x.SimpleName == methodName))
            {
                this.MethodCondition_WriteCustomLocator = MethodImplementationKind.ExplicitlyDeclared;
            }
            else
            {
                this.MethodCondition_WriteCustomLocator = MethodImplementationKind.Declared;
            }

            methodName = structuralInterface + ".ReadCustomRecord";
            if (this.GetMembers(VisceralTarget.Method).Any(x => x.SimpleName == methodName))
            {
                this.MethodCondition_ReadCustomRecord = MethodImplementationKind.ExplicitlyDeclared;
            }
            else
            {
                this.MethodCondition_ReadCustomRecord = MethodImplementationKind.Declared;
            }
        }

        if (this.Interfaces.Any(x => x.FullName.StartsWith(TinyhandBody.IStringConvertiblePrefix)))
        {// IStringConvertible implemented
            this.ObjectFlags |= TinyhandObjectFlags.HasIStringConvertible;
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

        // Callback methods
        foreach (var method in this.GetMembers(VisceralTarget.Method))
        {
            if (method.ContainingObject != this)
            {
                continue;
            }

            if (TinyhandCallbackMethod.TryCreate(method) is { } callbackMethod)
            {
                this.CallbackMethods ??= new();
                this.CallbackMethods.Add(callbackMethod);
            }
        }
    }

    public void ConfigureRelation()
    {// Create an object tree.
        if (this.ObjectFlags.HasFlag(TinyhandObjectFlags.RelationConfigured))
        {
            return;
        }

        this.ObjectFlags |= TinyhandObjectFlags.RelationConfigured;

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
                this.Body.CoderResolver.ObjectResolver.AddCoder(this.TypeObjectWithNullable);
            }
            else
            {
                this.Body.CoderResolver.FormatterResolver.AddFormatter(this.TypeObjectWithNullable);
            }

            /*if (this.Generics_Kind == VisceralGenericsKind.ClosedGeneric &&
                this.ContainingObject != null &&
                this.ContainingObject.OriginalDefinition is { } od)
            {// Requires Class<T>.NestedClass<int> formatter.
                var typeName = od.FullName + "." + this.LocalName;
                this.Body.CoderResolver.FormatterResolver.AddFormatter(this.Kind, typeName);
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
            this.ObjectFlags |= TinyhandObjectFlags.CanCreateInstance;
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

        if (this.ObjectFlags.HasFlag(TinyhandObjectFlags.CanCreateInstance))
        {// Type which can create an instance
            // default constructor required.
            if (this.Kind.IsReferenceType())
            {
                this.PreparePrimaryConstructor();
                this.PrepareMinimumConstructor();
                if (this.ObjectAttribute?.UseServiceProvider == true)
                {// Use ServiceProvider
                }
                else if (this.PrimaryConstructor is not null)
                {// PrimaryConstructor
                }
                else if (this.PublicMinimumConstructor is null ||
                    this.PublicMinimumConstructor.Method_Parameters.Length > 0)
                {
                    this.ObjectFlags |= TinyhandObjectFlags.UnsafeConstructor;
                    // this.Body.ReportDiagnostic(TinyhandBody.Error_NoDefaultConstructor, this.Location, this.FullName);
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

        if (this.ObjectAttribute?.ImplicitMemberNameAsKey == true && this.ObjectAttribute?.ExplicitKeysOnly == true)
        {
            this.Body.ReportDiagnostic(TinyhandBody.Error_ImplicitExplicitKey, this.Location, this.FullName);
        }

        if (this.ObjectAttribute?.AddImmutable == true &&
            this.Kind != VisceralObjectKind.Class)
        {
            this.Body.ReportDiagnostic(TinyhandBody.Error_AddImmutableNotClass, this.Location);
        }

        // Union
        this.Union?.CheckAndPrepare();

        // Target
        var structuralRequired = false;
        foreach (var x in this.Members)
        {
            if (!x.IsSerializable || x.IsReadOnly)
            {// Not serializable
                continue;
            }

            if (x.ContainingObject is { } containingObject)
            {
                containingObject.ConfigureIfAttributed();
                if (containingObject.ObjectAttribute?.ExplicitKeysOnly == true)
                {// Explicit key only
                    if (x.KeyAttribute == null)
                    {
                        continue;
                    }
                }
            }

            if (!x.IsPublic && this.ObjectAttribute?.IncludePrivateMembers != true)
            {// Skip protected or private members if IncludePrivateMembers is false.
                continue;
            }

            if (x.TypeObject?.OriginalDefinition is { } typeObject)
            {
                typeObject.ConfigureIfAttributed();
                if (typeObject.SupportsStructuralObject)
                {
                    structuralRequired = true;
                }
            }

            x.ObjectFlags |= TinyhandObjectFlags.Target | TinyhandObjectFlags.CloneTarget;
        }

        if (structuralRequired && !this.SupportsStructuralObject)
        {
            this.Body.ReportDiagnostic(TinyhandBody.Warning_StructuralRequired, this.Location, this.FullName);
        }

        // Key, SerializeTarget
        this.CheckObject_Key();

        // ReconstructTarget
        if (this.ObjectFlags.HasFlag(TinyhandObjectFlags.HasITinyhandSerializable))
        {// ITinyhandSerializable is implemented.
            foreach (var x in this.GetMembersWithFlag(TinyhandObjectFlags.Target))
            {
                if (x.TypeObject?.Kind.IsReferenceType() == true)
                {
                    x.ObjectFlags |= TinyhandObjectFlags.ReconstructTarget;
                }
            }
        }
        else
        {
            foreach (var x in this.GetMembersWithFlag(TinyhandObjectFlags.SerializeTarget))
            {
                if (x.TypeObject?.Kind.IsReferenceType() == true)
                {
                    x.ObjectFlags |= TinyhandObjectFlags.ReconstructTarget;
                }
            }
        }

        // LockMemberName
        var lockObjectName = this.ObjectAttribute?.LockMemberName;
        if (string.IsNullOrEmpty(lockObjectName) && this.ObjectAttribute is not null)
        {// Try to get the lock object of base objects.
            var baseObject = this.BaseObject?.OriginalDefinition;
            while (baseObject != null)
            {
                baseObject.ConfigureIfAttributed();
                if (!string.IsNullOrEmpty(baseObject.ObjectAttribute?.LockMemberName))
                {
                    lockObjectName = baseObject.ObjectAttribute!.LockMemberName!;
                    this.ObjectAttribute.LockMemberName = baseObject.ObjectAttribute!.LockMemberName!;
                    break;
                }

                baseObject = baseObject.BaseObject?.OriginalDefinition;
            }
        }

        // Derived from StoragePoint<TData>
        if (this.IsDerivedFrom(TinyhandBody.StoragePointName))
        {
            this.ObjectFlags |= TinyhandObjectFlags.DerivedFromStoragePoint;
            this.ObjectAttribute?.Structural = true; // Enable Structual to ensure StoragePoint works correctly.
        }

        if (!string.IsNullOrEmpty(lockObjectName))
        {
            var lockObject = this.AllMembers.FirstOrDefault(x => x.SimpleName == lockObjectName);
            if (lockObject == null)
            {// Not found
                this.Body.ReportDiagnostic(TinyhandBody.Error_LockObjectNotFound, this.Location);
            }
            else if (lockObject.TypeObject is { } typeObject)
            {
                if (!lockObject.IsReadableFrom(this))
                {// Not accessible
                    this.Body.ReportDiagnostic(TinyhandBody.Error_LockObjectNotAccessible, this.Location);
                }
                else if (!typeObject.Kind.IsReferenceType())
                {// Not reference type
                    this.Body.ReportDiagnostic(TinyhandBody.Error_LockObjectNotReferenceType, this.Location);
                }

                /*if (typeObject.FullName == TinyhandBody.ILockable ||
                    typeObject.AllInterfaces.Any(x => x == TinyhandBody.ILockable))
                {// ILockable
                    this.ObjectAttribute!.LockObjectType = LockObjectType.Lockable;
                }*/
                if (typeObject.FullName == TinyhandBody.SemaphoreLockFullName)
                {// Arc.Threading.SemaphoreLock
                    this.ObjectAttribute!.LockObjectType = LockObjectType.SemaphoreLock;
                }
                else if (typeObject.FullName == TinyhandBody.LockFullName)
                {// System.Threading.Lock
                    this.ObjectAttribute!.LockObjectType = LockObjectType.Lock;
                }
                else
                {
                    this.ObjectAttribute!.LockObjectType = LockObjectType.Object;
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

    private void PrepareMinimumConstructor()
    {
        if (this.ObjectFlags.HasFlag(TinyhandObjectFlags.MinimumConstructorPrepared))
        {
            return;
        }
        else
        {
            this.ObjectFlags |= TinyhandObjectFlags.MinimumConstructorPrepared;
        }

        TinyhandObject? publicConstructor = default;
        TinyhandObject? constructor = default;

        foreach (var x in this.GetMembers(VisceralTarget.Method).Where(a => a.Method_IsConstructor && a.ContainingObject == this))
        {
            if (x.MethodIncludesParameterWithRef())
            {
                continue;
            }

            if (x.IsPublic)
            {
                if (publicConstructor is null ||
                    publicConstructor.Method_Parameters.Length > x.Method_Parameters.Length)
                {
                    publicConstructor = x;
                }
            }
            else if (x.symbol is { } symbol &&
                symbol.DeclaredAccessibility != Accessibility.Private)
            {
                if (constructor is null ||
                    constructor.Method_Parameters.Length > x.Method_Parameters.Length)
                {
                    constructor = x;
                }
            }
        }

        this.PublicMinimumConstructor = publicConstructor;
        this.MinimumConstructor = constructor;
    }

    private void PreparePrimaryConstructor()
    {
        IMethodSymbol? constructor = default;
        var type = (INamedTypeSymbol)this.symbol!;

        foreach (var ctor in type.InstanceConstructors)
        {
            foreach (var syntaxRef in ctor.DeclaringSyntaxReferences)
            {
                if (syntaxRef.GetSyntax() is TypeDeclarationSyntax tds
                    && tds.ParameterList is not null)
                {
                    constructor = ctor;
                    goto Exit;
                }
            }

            // if (ctor.IsImplicitlyDeclared &&
            //    ctor.Parameters.Length > 0 &&
            //    ctor.DeclaringSyntaxReferences.Length == 0)
            // {
            //    constructor = ctor;
            //    goto Exit;
            // }
        }

Exit:
        if (constructor == null)
        {
            return;
        }

        this.PrimaryConstructor = this.Body.Add(constructor);
    }

    private void CheckObject_Key()
    {
        if (this.ObjectFlags.HasFlag(TinyhandObjectFlags.HasITinyhandSerializable) &&
            (this.MethodCondition_Serialize == MethodImplementationKind.Declared || this.MethodCondition_Serialize == MethodImplementationKind.ExplicitlyDeclared) &&
            (this.MethodCondition_Deserialize == MethodImplementationKind.Declared || this.MethodCondition_Deserialize == MethodImplementationKind.ExplicitlyDeclared))
        {// ITinyhandSerializable is implemented. KeyAttribute is ignored.
            foreach (var x in this.GetMembersWithFlag(TinyhandObjectFlags.Target))
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
                    if (x.ObjectFlags.HasFlag(TinyhandObjectFlags.Target) || x.KeyAttribute != null)
                    {// Target or has [Key]
                        x.ObjectFlags |= TinyhandObjectFlags.SerializeTarget | TinyhandObjectFlags.CloneTarget;
                    }
                }
            }

            // Search keys
            var intKeyExists = false;
            var stringKeyExists = false;
            foreach (var x in this.GetMembersWithFlag(TinyhandObjectFlags.SerializeTarget))
            {
                if (x.KeyAttribute == null)
                {// No KeyAttribute
                    if (this.ObjectAttribute!.ImplicitMemberNameAsKey)
                    {// ImplicitMemberNameAsKey
                        x.KeyAttribute = new KeyAttributeData(x.SimpleName);
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

                if (x.KeyAttribute is not null &&
                    x.TypeObject is not null)
                {// Get default value
                    if (this.GetDefaultValue(x.TypeObject, x.symbol) is { } defaultValue)
                    {
                        x.DefaultValue = defaultValue;
                    }
                }
            }

            if (stringKeyExists || (!intKeyExists && this.ObjectAttribute!.ImplicitMemberNameAsKey == true))
            {// String key
                if (this.ObjectAttribute?.AddAlternateKey == true)
                {
                    this.ObjectAttribute.AddAlternateKey = false;
                    this.Body.ReportDiagnostic(TinyhandBody.Warning_InvalidAlternateKey, this.Location);
                }

                this.ObjectFlags |= TinyhandObjectFlags.StringKeyObject;
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
        this.StringTrie ??= new(this);

        foreach (var x in this.GetMembersWithFlag(TinyhandObjectFlags.SerializeTarget))
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

                var r = this.StringTrie.AddNode(s, x);
                if (r.Result == VisceralTrieAddNodeResult.KeyCollision)
                {
                    this.Body.ReportDiagnostic(TinyhandBody.Error_StringKeyConflict, x.KeyVisceralAttribute?.Location);
                }
            }
        }
    }

    private void CheckObject_IntKey()
    {
        // Reserved keys
        var reservedMax = 0;
        var baseObject = this.BaseObject;
        while (baseObject != null)
        {
            baseObject.ConfigureIfAttributed();
            if (baseObject.ObjectAttribute?.ReservedKeyCount is int reservedKeyCount)
            {
                reservedMax = Math.Max(reservedMax, reservedKeyCount);
            }

            baseObject = baseObject.BaseObject;
        }

        // Integer key
        foreach (var x in this.GetMembersWithFlag(TinyhandObjectFlags.SerializeTarget))
        {
            if (x.KeyAttribute?.IntKey is int i)
            {
                if (i < 0 || i > TinyhandBody.MaxIntegerKey)
                {
                    this.Body.ReportDiagnostic(TinyhandBody.Error_IntKeyOutOfRange, x.KeyVisceralAttribute?.Location);
                }
                else
                {
                    this.IntKey_Count++;
                    this.IntKey_Max = Math.Max(this.IntKey_Max, i);
                    this.IntKey_Min = Math.Min(this.IntKey_Min, i);
                }
            }
        }

        this.IntKey_Array = new TinyhandObject[this.IntKey_Max + 1];
        this.IntKey_IncludedCount = this.IntKey_Max + 1;

        foreach (var x in this.GetMembersWithFlag(TinyhandObjectFlags.SerializeTarget))
        {
            if (x.KeyAttribute?.IntKey is int i && i >= 0 && i <= TinyhandBody.MaxIntegerKey)
            {
                if (i < reservedMax && x.ContainingObject == this && !x.KeyAttribute.IgnoreKeyReservation)
                {// Reserved
                    this.Body.ReportDiagnostic(TinyhandBody.Error_IntKeyReserved, x.KeyVisceralAttribute?.Location, reservedMax - 1);
                }
                else if (this.IntKey_Array[i] is not null)
                {// Conflict
                    this.IntKey_Array[i]!.ObjectFlags |= TinyhandObjectFlags.IntKeyConflicted;
                    x.ObjectFlags |= TinyhandObjectFlags.IntKeyConflicted;
                }
                else
                {
                    this.IntKey_Array[i] = x;
                }

                if (x.KeyAttribute.Exclude)
                {
                    this.IntKey_IncludedCount--;
                }
            }
        }

        foreach (var x in this.GetMembersWithFlag(TinyhandObjectFlags.IntKeyConflicted))
        {
            this.Body.ReportDiagnostic(TinyhandBody.Error_IntKeyConflict, x.KeyVisceralAttribute?.Location);
        }

        var unusedKeys = this.IntKey_Max - (reservedMax + 1);
        if (unusedKeys >= 10 && unusedKeys > (this.IntKey_Count * 2))
        {// Too many unused key.
            this.Body.ReportDiagnostic(TinyhandBody.Warning_IntKeyUnused, this.Location);
        }

        // Dual key
        if (this.HasAlternateKey)
        {
            this.StringTrie ??= new(this);
            foreach (var x in this.IntKey_Array)
            {
                if (x?.KeyAttribute is { } keyAttribute)
                {
                    if (string.IsNullOrEmpty(keyAttribute.AlternateKey))
                    {
                        keyAttribute.AlternateKey = x.SimpleName;
                    }

                    var r = this.StringTrie.AddNode(keyAttribute.AlternateKey, x);
                    if (r.Result == VisceralTrieAddNodeResult.KeyCollision)
                    {
                        this.Body.ReportDiagnostic(TinyhandBody.Error_StringKeyConflict, x.KeyVisceralAttribute?.Location);
                    }
                }
            }
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

        if (this.IsRequired)
        {
            parent.ObjectFlags |= TinyhandObjectFlags.UnsafeConstructor;
        }

        if (this.TypeObject.IsDerivedFrom(TinyhandBody.StoragePointName))
        {
            this.ObjectFlags |= TinyhandObjectFlags.DerivedFromStoragePoint;
            this.ObjectAttribute?.Structural = true; // Enable Structual to ensure StoragePoint works correctly.
        }

        if (!this.IsSerializable || this.IsReadOnly)
        {// Not serializable (before)
            if (this.KeyAttribute != null || this.ReconstructAttribute != null)
            {
                if (this.Kind == VisceralObjectKind.Field)
                {// Requires unsafe deserialize method
                    parent.ObjectFlags |= TinyhandObjectFlags.RequiresUnsafeDeserialize;
                    this.Body.RequiresUnsafeBlocks = true;
                }
                else if (this.Kind == VisceralObjectKind.Property)
                {// Getter-only property is not supported.
                    if (!this.IsSameAssembly(parent))
                    {// Skip the check process because the IsReadOnly property in the external assembly's private getter is set to true.
                    }
                    else
                    {
                        this.Body.ReportDiagnostic(TinyhandBody.Error_NotSerializableMember, this.Location, this.SimpleName);
                    }
                }
            }
        }

        if (this.KeyAttribute != null)
        {// Has KeyAttribute
            if ((parent.MethodCondition_Serialize == MethodImplementationKind.Declared || parent.MethodCondition_Serialize == MethodImplementationKind.ExplicitlyDeclared) &&
            (parent.MethodCondition_Deserialize == MethodImplementationKind.Declared || parent.MethodCondition_Deserialize == MethodImplementationKind.ExplicitlyDeclared))
            {// Key validation is skipped because a customized function is implemented.
            }
            else
            {
                this.Body.DebugAssert(this.ObjectFlags.HasFlag(TinyhandObjectFlags.SerializeTarget), $"{this.FullName}: KeyAttribute and SerializeTarget are inconsistent.");

                if (this.TypeObject.Kind == VisceralObjectKind.Error)
                {// Error object is treated as an external object that implements ITinyhandSerialize outside the control of the generator.
                    this.TypeObject.ObjectAttribute ??= TinyhandObjectAttributeData.ExternalObject;
                }

                if (// parent.Generics_Kind != VisceralGenericsKind.OpenGeneric &&
                    this.TypeObjectWithNullable != null &&
                    !this.TypeObject.ContainsTypeParameter &&
                    this.TypeObjectWithNullable.Object.ObjectAttribute == null &&
                    this.TypeObject.Kind != VisceralObjectKind.Error &&
                    this.Body.CoderResolver.IsCoderOrFormatterAvailable(this.TypeObjectWithNullable) == false)
                {// No Coder or Formatter
                    /*var obj = this.TypeObjectWithNullable.Object;
                    obj.Configure();
                    if (obj.ObjectAttribute == null)*/
                    {
                        this.Body.CoderResolver.IsCoderOrFormatterAvailable(this.TypeObjectWithNullable);
                        this.Body.ReportDiagnostic(TinyhandBody.Error_ObjectAttributeRequired, this.Location, this.TypeObject.FullName);
                    }
                }

                /*if (this.KeyAttribute.ConvertToString)
            {
                if (this.TypeObject is { } typeObject)
                {
                    if (!typeObject.Interfaces.Any(x => x.FullName.StartsWith(TinyhandBody.IStringConvertiblePrefix)))
                    {
                        this.Body.ReportDiagnostic(TinyhandBody.Warning_ConvertToString, this.Location);

                        this.KeyAttribute.ConvertToString = false;
                    }
                }
            }*/
            }
        }
        else
        {// No KeyAttribute
            if (this.ObjectFlags.HasFlag(TinyhandObjectFlags.SerializeTarget))
            {
                this.Body.ReportDiagnostic(TinyhandBody.Error_KeyAttributeRequired, this.Location);
            }

            if (this.ObjectFlags.HasFlag(TinyhandObjectFlags.CloneTarget))
            {// Exclude clone target
                if (/*parent.Generics_Kind != VisceralGenericsKind.OpenGeneric &&*/
                this.TypeObjectWithNullable != null &&
                this.TypeObjectWithNullable.Object.ObjectAttribute == null &&
this.Body.CoderResolver.IsCoderOrFormatterAvailable(this.TypeObjectWithNullable) == false)
                {// No Coder or Formatter
                    this.ObjectFlags &= ~TinyhandObjectFlags.CloneTarget;
                }
                else if (this.IgnoreMemberAttribute != null)
                {// [IgnoreMember]
                    this.ObjectFlags &= ~TinyhandObjectFlags.CloneTarget;
                }
            }
        }

        /*if (this.DefaultValue != null)
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
        }*/

        // ReconstructTarget
        if (parent.ObjectFlags.HasFlag(TinyhandObjectFlags.HasITinyhandSerializable))
        {// ITinyhandSerializable is implemented.
            if (this.ObjectFlags.HasFlag(TinyhandObjectFlags.SerializeTarget) && this.TypeObject.Kind.IsReferenceType())
            { // SerializeTarget && Reference type
                this.ObjectFlags |= TinyhandObjectFlags.ReconstructTarget;
            }
        }
        else
        {
            if (this.ObjectFlags.HasFlag(TinyhandObjectFlags.SerializeTarget) &&
                (this.TypeObject.Kind.IsReferenceType() ||
                this.TypeObject.ObjectAttribute != null))
            { // SerializeTarget && (Reference type || TinyhandObject)
                this.ObjectFlags |= TinyhandObjectFlags.ReconstructTarget;
            }
        }

        if (this.TypeObject.Kind == VisceralObjectKind.Error && this.ObjectFlags.HasFlag(TinyhandObjectFlags.SerializeTarget))
        {// Error type
            this.ObjectFlags |= TinyhandObjectFlags.ReconstructTarget;
        }

        if (this.ReconstructAttribute?.Reconstruct == true)
        {// Reconstruct(true)
            this.ObjectFlags |= TinyhandObjectFlags.ReconstructTarget;
            this.ReconstructMode = ReconstructMode.Always;
        }
        else if (this.ReconstructAttribute?.Reconstruct == false)
        {// Reconstruct(false)
            this.ObjectFlags &= ~TinyhandObjectFlags.ReconstructTarget;
            this.ReconstructMode = ReconstructMode.Never;
        }

        if (!this.ObjectFlags.HasFlag(TinyhandObjectFlags.ReconstructTarget))
        {// Not ReconstructTarget
            this.ReconstructMode = ReconstructMode.Never;
        }
        else
        {// ReconstructTarget
            this.CheckMember_Reconstruct(parent);
        }

        // Check ReconstructTarget
        if (!this.ObjectFlags.HasFlag(TinyhandObjectFlags.ReconstructTarget))
        {// Not ReconstructTarget
            this.Body.DebugAssert(this.ReconstructMode == ReconstructMode.Never, "this.ReconstructMode == ReconstructMode.Never");
        }
        else
        {// ReconstructTarget
            this.Body.DebugAssert(this.ReconstructMode == ReconstructMode.Always, "this.ReconstructMode == ReconstructMode.Always");
        }

        // ReuseInstanceTarget
        var reuseInstanceFlag = parent.ObjectAttribute?.ReuseMembers == true;
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
            this.ObjectFlags |= TinyhandObjectFlags.ReuseInstanceTarget;
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
                this.ObjectFlags |= TinyhandObjectFlags.HiddenMember;
                break;
            }

            parentObject = parentObject.BaseObject;
        }

        // Requires getter/setter
        if (this.IsInitOnly)
        {
            this.RequiresSetAccessor = true;
        }
        else if (this.Kind == VisceralObjectKind.Property)
        {
            if (this.IsReadOnly)
            {
                this.RequiresSetAccessor = true;
            }
        }

        if (this.ContainingObject != parent)
        {
            if (this.ObjectFlags.HasFlag(TinyhandObjectFlags.HiddenMember))
            {// Hidden members
                if (this.Kind == VisceralObjectKind.Field)
                {
                    if (!this.Field_IsPublic)
                    {
                        this.RequiresGetAccessor = true;
                        this.RequiresSetAccessor = true;
                    }
                }
                else if (this.Kind == VisceralObjectKind.Property)
                {
                    if (!this.Property_IsPublicGetter)
                    {
                        this.RequiresGetAccessor = true;
                    }

                    if (!this.Property_IsPublicSetter)
                    {
                        this.RequiresSetAccessor = true;
                    }
                }
            }
            else
            {// Other
                if (this.Kind == VisceralObjectKind.Field)
                {
                    if (this.Field_IsPrivate)
                    {
                        this.RequiresGetAccessor = true;
                        this.RequiresSetAccessor = true;
                    }
                }
                else if (this.Kind == VisceralObjectKind.Property)
                {
                    if (this.Property_IsPrivateGetter)
                    {
                        this.RequiresGetAccessor = true;
                    }

                    if (this.Property_IsPrivateSetter)
                    {
                        this.RequiresSetAccessor = true;
                    }
                }
            }
        }

        // Add property
        if (this.KeyAttribute != null &&
            !string.IsNullOrEmpty(this.KeyAttribute.PropertyName) &&
            this.ContainingObject == parent)
        {
            if (this.Kind != VisceralObjectKind.Field)
            {
                this.Body.ReportDiagnostic(TinyhandBody.Error_PropertyNameRequiresField, this.KeyVisceralAttribute?.Location);
            }

            if (!parent.Identifier.Add(this.KeyAttribute.PropertyName))
            {
                this.Body.ReportDiagnostic(TinyhandBody.Error_DuplicateKeyword, this.KeyVisceralAttribute?.Location, parent.SimpleName, this.KeyAttribute.PropertyName);
            }

            this.ObjectFlags |= TinyhandObjectFlags.AddPropertyTarget;
            this.RequiresGetAccessor = false;
            if (parent.ObjectFlags.HasFlag(TinyhandObjectFlags.IsRepeatableRead))
            {// Repeatable read
                this.RequiresSetAccessor = false; // Main
                // this.ObjectFlags |= TinyhandObjectFlags.IsRepeatableRead; // Alternative
            }
            else
            {// Other
                this.RequiresSetAccessor = false;
            }
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
                this.Body.ReportDiagnostic(TinyhandBody.Warning_MaxLengthUnsupportedType, this.Location);
            }

            if (!this.IsPartialProperty &&
                string.IsNullOrEmpty(this.KeyAttribute?.PropertyName) &&
                !parent.ObjectFlags.HasFlag(TinyhandObjectFlags.IsRepeatableRead))
            {// No add property and not repeatable read.
                this.Body.ReportDiagnostic(TinyhandBody.Warning_MaxLengthRequiresPropertyName, this.Location);
            }
        }

        if (this.ObjectFlags.HasFlag(TinyhandObjectFlags.SerializeTarget) &&
            this.IsPartialProperty &&
            this.ContainingObject == parent)
        {
            this.ObjectFlags |= TinyhandObjectFlags.AddPropertyTarget;
        }
    }

    private void CheckMember_Reconstruct(TinyhandObject parent)
    {
        if (this.ReconstructMode == ReconstructMode.IfPreferable)
        {
            // Parent's ReconstructMembers is false
            if (parent.ObjectAttribute?.ReconstructMembers == false)
            {
                this.ReconstructMode = ReconstructMode.Never;
            }

            // Avoid reconstruct "T?"
            if (this.NullableAnnotationIfReferenceType == Arc.Visceral.NullableAnnotation.Annotated)
            {
                this.ReconstructMode = ReconstructMode.Never;
            }
        }
        else if (this.ReconstructMode == ReconstructMode.Always)
        {
            if (this.IsReadOnly)
            {
                this.Body.ReportDiagnostic(TinyhandBody.Error_ReadonlyMember, this.Location, this.SimpleName);
                this.ReconstructMode = ReconstructMode.Never;
            }
        }

        if (this.ReconstructMode != ReconstructMode.Never)
        {// ReconstructMode.IfPreferable or ReconstructMode.Always
            var condition = this.ReconstructCondition;

            if (condition == ReconstructCondition.Reconstructable)
            {// Can reconstruct.
                this.ReconstructMode = ReconstructMode.Always;
            }
            else
            {// Cannot reconstruct.
                if (this.ReconstructMode == ReconstructMode.IfPreferable)
                {
                    this.ReconstructMode = ReconstructMode.Never;
                }
                else
                {// Warning
                    this.ReconstructMode = ReconstructMode.Never;
                    if (condition == ReconstructCondition.CircularDependency)
                    {
                        this.Body.ReportDiagnostic(TinyhandBody.Warning_CircularDependency, this.Location, this.TypeObject!.FullName);
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

        if (this.ReconstructMode != ReconstructMode.Always)
        {
            this.ObjectFlags &= ~TinyhandObjectFlags.ReconstructTarget;
        }
    }

    public void Check()
    {
        if (this.ObjectFlags.HasFlag(TinyhandObjectFlags.Checked))
        {
            return;
        }

        this.ObjectFlags |= TinyhandObjectFlags.Checked;

        this.Body.DebugAssert(this.ObjectAttribute != null, "this.ObjectAttribute != null");
        this.CheckObject();
    }

    internal void Generate(ScopingStringBuilder ssb, GenerationContext info)
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
                }
            }

            return;
        }

        if (this.ObjectFlags.HasFlag(TinyhandObjectFlags.ExternalObject))
        {
            return;
        }

        this.ObjectFlags |= TinyhandObjectFlags.InterfaceImplemented;
        var interfaceString = string.Empty;
        if (this.ObjectAttribute != null)
        {
            interfaceString = $" : ITinyhandSerializable<{this.RegionalName}>, ITinyhandReconstructable<{this.RegionalName}>, ITinyhandCloneable<{this.RegionalName}>";

            if (this.ObjectAttribute.Structural && !this.ObjectFlags.HasFlag(TinyhandObjectFlags.IStructuralObjectImplemented))
            {
                interfaceString += $", {TinyhandBody.IStructuralObjectName}";
            }

            interfaceString += $", ITinyhandSerializable";
            if (this.MethodCondition_Serialize == MethodImplementationKind.MemberMethod ||
                this.MethodCondition_Serialize == MethodImplementationKind.StaticMethod)
            {
                interfaceString += $", ITinyhandSingleLayoutSerializable";
            }
        }

        // Prepare generator information
        info.EnumAsString = this.ObjectAttribute?.EnumAsString == true;

        using (var cls = ssb.ScopeBrace($"{this.AccessibilityName} partial {this.KindName} {this.LocalName}{interfaceString}"))
        {
            // Prepare Primary
            this.Generate_PreparePrimary();

            if (this.Union != null)
            {
                this.Union.GenerateTable(ssb, info);
            }

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

                if (x.ObjectAttribute.Structural && !x.ObjectFlags.HasFlag(TinyhandObjectFlags.IStructuralObjectImplemented))
                {
                    x.GenerateIStructuralObject(ssb, info);
                }
            }

            if (this.ObjectFlags.HasFlag(TinyhandObjectFlags.UnsafeConstructor))
            {
                this.GenerateUnsafeConstructor(ssb);
            }

            /*if (this.ObjectAttribute != null && info.UseMemberNotNull)
            {// MemberNotNull
                if (this.MethodCondition_Reconstruct == MethodImplementationKind.MemberMethod)
                {
                    this.GenerateMemberNotNull_MemberMethod(ssb, info);
                }
                else if (this.MethodCondition_Serialize == MethodImplementationKind.StaticMethod)
                {
                    this.GenerateMemberNotNull_StaticMethod(ssb, info);
                }
            }*/

            // StringKey fields
            // this.GenerateStringKeyFields(ssb, info);

            // Generate accessor delegates (getter, setter, ref field)
            this.GenerateAccessorDelegate(ssb, info);

            if (this.ObjectAttribute?.AddImmutable == true)
            { // Generate immutable class
                this.GenerateImmutable(ssb, info);
            }

            if (this.Children?.Count > 0)
            {// Generate children and loader.
                ssb.AppendLine();
                foreach (var x in this.Children)
                {
                    x.Generate(ssb, info);
                }
            }
        }
    }

    internal void Generate_PreparePrimary()
    {// Prepare Primary TinyhandObject
        this.PrepareTrie();

        foreach (var member in this.GetMembersWithFlag(TinyhandObjectFlags.SerializeTarget).Where(x => x.RequiresGetAccessor || x.RequiresSetAccessor))
        {
            member.RefFieldDelegate = this.Identifier.GetIdentifier();
        }
    }

    internal void Generate_PrepareSecondary()
    {// Prepare Secondary TinyhandObject
        // this.PrepareAutomata();
        this.PrepareTrie();

        var od = this.OriginalDefinition;
        if (od == null)
        {
            return;
        }

        // Init setter delegates
        for (var n = 0; n < this.Members.Length; n++)
        {
            this.Members[n].RefFieldDelegate = od.Members[n].RefFieldDelegate;
        }
    }

    internal void GenerateImmutable(ScopingStringBuilder ssb, GenerationContext info)
    {
        this.GenerateImmutableClass(ssb, info);
        this.GenerateImmutableMethod(ssb, info);
    }

    internal void GenerateImmutableClass(ScopingStringBuilder ssb, GenerationContext info)
    {
        var underlyingClassName = this.SimpleName;

        using (var classScope = ssb.ScopeBrace($"public sealed class {TinyhandBody.ImmutableClassName} : ITinyhandSerializable<{TinyhandBody.ImmutableClassName}>, ITinyhandReconstructable<{TinyhandBody.ImmutableClassName}>, ITinyhandCloneable<{TinyhandBody.ImmutableClassName}>"))
        {
            ssb.AppendLine($"private readonly {underlyingClassName} {TinyhandBody.UnderlyingObjectName};");
            ssb.AppendLine($"public {TinyhandBody.ImmutableClassName}({underlyingClassName} obj) {{ this.{TinyhandBody.UnderlyingObjectName} = obj; }}");
            ssb.AppendLine($"public {underlyingClassName} GetUnderlyingObject() => this.{TinyhandBody.UnderlyingObjectName};");

            using (var serializeScope = ssb.ScopeBrace($"static void ITinyhandSerializable<{TinyhandBody.ImmutableClassName}>.Serialize(ref TinyhandWriter writer, scoped ref {TinyhandBody.ImmutableClassName}? value, TinyhandSerializerOptions options)"))
            {// Serialize
                ssb.AppendLine("if (value is null) writer.WriteNil();");
                ssb.AppendLine($"else TinyhandSerializer.SerializeObject(ref writer, in value.{TinyhandBody.UnderlyingObjectName}, options);");
            }

            using (var deserializeScope = ssb.ScopeBrace($"static void ITinyhandSerializable<{TinyhandBody.ImmutableClassName}>.Deserialize(ref TinyhandReader reader, scoped ref {TinyhandBody.ImmutableClassName}? value, TinyhandSerializerOptions options)"))
            {// Deserialize
                ssb.AppendLine($"if (TinyhandSerializer.DeserializeObject<{underlyingClassName}>(ref reader, options) is {{ }} obj) value = new(obj);");
            }

            using (var reconstructScope = ssb.ScopeBrace($"static void ITinyhandReconstructable<{TinyhandBody.ImmutableClassName}>.Reconstruct([NotNull] scoped ref {TinyhandBody.ImmutableClassName}? value, TinyhandSerializerOptions options)"))
            {// Reconstruct
                ssb.AppendLine($"value ??= new(TinyhandSerializer.ReconstructObject<{underlyingClassName}>(options));");
            }

            using (var cloneScope = ssb.ScopeBrace($"static {TinyhandBody.ImmutableClassName}? ITinyhandCloneable<{TinyhandBody.ImmutableClassName}>.Clone(scoped ref {TinyhandBody.ImmutableClassName}? value, TinyhandSerializerOptions options)"))
            {// Clone
                ssb.AppendLine($"if (TinyhandSerializer.CloneObject(value?.{TinyhandBody.UnderlyingObjectName}, options) is {{ }} obj) return new(obj);");
                ssb.AppendLine("else return default;");
            }

            ssb.AppendLine();

            foreach (var x in this.GetMembersWithFlag(TinyhandObjectFlags.SerializeTarget))
            {
                if (x.TypeObjectWithNullable is { } memberType)
                {
                    if (x.symbol?.GetDocumentationCommentXml() is { } xml)
                    {// Quotes the documentation comment if available.
                        /*try
                        {
                            var doc = XDocument.Parse(xml);
                            if (doc.Root?.Element("summary") is { } summary)
                            {
                                var text = summary.Value.Trim().Replace("\r", string.Empty).Replace("\n", string.Empty);
                                ssb.AppendLine($"/// <summary>{text}</summary>");
                            }
                        }
                        catch
                        {
                        }*/

                        var matchSummary = Regex.Match(xml, @"<summary>(.*?)</summary>", RegexOptions.Singleline);
                        if (matchSummary.Success)
                        {
                            var content = matchSummary.Groups[1].Value;
                            var lines = content.Split(['\r', '\n',], StringSplitOptions.RemoveEmptyEntries);
                            for (var i = 0; i < lines.Length; i++)
                            {
                                lines[i] = lines[i].Trim();
                            }

                            ssb.AppendLine($"/// <summary>{string.Join(string.Empty, lines)}</summary>");
                        }

                        /*var matchSummary = Regex.Match(xml, @"<summary>(.*?)</summary>", RegexOptions.Singleline);
                        if (matchSummary.Success)
                        {
                            var content = matchSummary.Groups[1].Value;
                            var result = Regex.Replace(content, @"(?m)^\s+|\s+$", string.Empty);
                            result = Regex.Replace(result, @"\r?\n", string.Empty);
                            ssb.AppendLine($"/// <summary>{result}</summary>");
                        }*/
                    }

                    ssb.AppendLine($"public {memberType.FullNameWithNullable} {x.SimpleName} => this.{TinyhandBody.UnderlyingObjectName}.{x.SimpleName};");
                }
            }

            ssb.AppendLine();
        }
    }

    internal void GenerateImmutableMethod(ScopingStringBuilder ssb, GenerationContext info)
    {
        ssb.AppendLine($"public {TinyhandBody.ImmutableClassName} ToImmutable() => new(this);");
        ssb.AppendLine($"public {TinyhandBody.ImmutableClassName} CloneAndToImmutable() => new(TinyhandSerializer.CloneObject(this));");
        ssb.AppendLine();
    }

    internal void GenerateAccessorDelegate(ScopingStringBuilder ssb, GenerationContext info)
    {
        // Ref field delegate
        foreach (var x in this.GetMembersWithFlag(TinyhandObjectFlags.SerializeTarget).Where(x => x.RefFieldDelegate is not null))
        {
            if (x.Kind == VisceralObjectKind.Field)
            {
                ssb.AppendLine($"[UnsafeAccessor(UnsafeAccessorKind.Field, Name = \"{x.SimpleName}\")]");
            }
            else if (x.Kind == VisceralObjectKind.Property)
            {
                ssb.AppendLine($"[UnsafeAccessor(UnsafeAccessorKind.Field, Name = \"{string.Format(TinyhandBody.BackingFieldFormat, x.SimpleName)}\")]");
            }

            ssb.AppendLine($"private static extern ref {x.TypeObjectWithNullable?.FullNameWithNullable} {x.RefFieldDelegate}({this.InModifierIfStruct}{x.ContainingObject!.FullName} obj);"); // x.TypeObject!.FullName
        }
    }

    internal void Generate_CallbackMethod(ScopingStringBuilder ssb, CallbackKind kind)
    {
        if (this.CallbackMethods is null)
        {
            return;
        }

        foreach (var x in this.CallbackMethods)
        {
            if (x.Kind == kind)
            {
                x.Generate(ssb);
            }
        }
    }

    internal void GenerateSerialize_Method2(ScopingStringBuilder ssb, GenerationContext info)
    {// static abstract
        info.GeneratingStaticMethod = true;
        var methodCode = $"static void ITinyhandSerializable<{this.RegionalName}>.Serialize(ref TinyhandWriter writer, scoped ref {this.RegionalName}{this.QuestionMarkIfReferenceType} v, TinyhandSerializerOptions options)";
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

            // LockMemberName
            ScopingStringBuilder.IScope? lockScope = null;
            if (!string.IsNullOrEmpty(this.ObjectAttribute?.LockMemberName))
            {
                ssb.AppendLine($"var {TinyhandBody.LockTakenVariable} = false;");
                lockScope = ssb.ScopeBrace("try");

                if (this.ObjectAttribute!.LockObjectType == LockObjectType.SemaphoreLock ||
                    this.ObjectAttribute!.LockObjectType == LockObjectType.Lock)
                {
                    ssb.AppendLine($"{ssb.FullObject}.{this.ObjectAttribute!.LockMemberName}!.Enter();");
                    ssb.AppendLine($"{TinyhandBody.LockTakenVariable} = true;");
                }
                else
                {
                    ssb.AppendLine($"System.Threading.Monitor.Enter({ssb.FullObject}.{this.ObjectAttribute!.LockMemberName}!, ref {TinyhandBody.LockTakenVariable});");
                }
            }

            this.Generate_CallbackMethod(ssb, CallbackKind.OnSerializing); // CallbackMethodCode

            if (this.HasAlternateKey)
            {
                using (var scopeConvertToString = ssb.ScopeBrace("if (options.HasConvertToStringFlag)"))
                {
                    this.GenerateSerializerStringKey(ssb, info, ConvertToStringMode.ConvertToString);
                }

                using (var scopeConvertToInt = ssb.ScopeBrace("else"))
                {
                    this.GenerateSerializerIntKey(ssb, info, ConvertToStringMode.NoConvertToString);
                }
            }
            else
            {
                if (this.ObjectFlags.HasFlag(TinyhandObjectFlags.StringKeyObject))
                {// String Key
                    this.GenerateSerializerStringKey(ssb, info, ConvertToStringMode.NotSpecified);
                }
                else
                {// Int Key
                    this.GenerateSerializerIntKey(ssb, info, ConvertToStringMode.NotSpecified);
                }
            }

            this.Generate_CallbackMethod(ssb, CallbackKind.OnSerialized); // CallbackMethodCode

            if (lockScope != null)
            {
                lockScope.Dispose();
                using (var finallyScope = ssb.ScopeBrace("finally"))
                {
                    if (this.ObjectAttribute!.LockObjectType == LockObjectType.SemaphoreLock ||
                        this.ObjectAttribute!.LockObjectType == LockObjectType.Lock)
                    {
                        ssb.AppendLine($"{ssb.FullObject}.{this.ObjectAttribute!.LockMemberName}!.Exit();");
                    }
                    else
                    {
                        ssb.AppendLine($"if ({TinyhandBody.LockTakenVariable}) System.Threading.Monitor.Exit({ssb.FullObject}.{this.ObjectAttribute!.LockMemberName}!);");
                    }
                }
            }
        }
    }

    /*internal void GenerateFormatter_Serialize(ScopingStringBuilder ssb, GenerationContext info)
    {
        if (this.Kind.IsReferenceType())
        {// Reference type
            ssb.AppendLine($"if ({ssb.FullObject} == null) {{ writer.WriteNil(); return; }}");
        }

        if (this.MethodCondition_Serialize == MethodImplementationKind.StaticMethod)
        {// Static method
            ssb.AppendLine($"{this.FullName}.Serialize(ref writer, {ssb.FullObject}, options);");
        }
        else if (this.MethodCondition_Serialize == MethodImplementationKind.ExplicitlyDeclared)
        {// Explicitly declared (Interface.Method())
            ssb.AppendLine($"((ITinyhandSerializable){ssb.FullObject}).Serialize(ref writer, options);");
        }
        else
        {// Member method
            ssb.AppendLine($"{ssb.FullObject}.Serialize(ref writer, options);");
        }
    }*/

    /*internal void GenerateFormatter_DeserializeCore(ScopingStringBuilder ssb, GenerationContext info, string name)
    {
        if (this.MethodCondition_Deserialize == MethodImplementationKind.StaticMethod)
        {// Static method
            // ssb.AppendLine($"{this.FullName}.Deserialize{this.GenericsNumberString}(ref {name}, ref reader, options);");
            ssb.AppendLine($"{name}.Deserialize(ref reader, options);");
        }
        else if (this.MethodCondition_Deserialize == MethodImplementationKind.ExplicitlyDeclared)
        {// Explicitly declared (Interface.Method())
            ssb.AppendLine($"((ITinyhandSerializable){name}).Deserialize(ref reader, options);");
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
        else if (this.ObjectAttribute?.UseServiceProvider == true)
        {// Service Provider
            return $"({this.FullName})TinyhandSerializer.GetService(typeof({this.FullName}))";
        }
        else if (this.OriginalDefinition?.ObjectFlags.HasFlag(TinyhandObjectFlags.UnsafeConstructor) == true)
        {
            return $"{this.FullName}.{TinyhandBody.UnsafeConstructorName}()";
        }
        else if (this.PrimaryConstructor is not null)
        {// new(default!, ..., default!)
            var sb = new StringBuilder();
            sb.Append("new ");
            sb.Append(this.FullName);
            sb.Append("(");
            for (var i = 0; i < this.PrimaryConstructor.Method_Parameters.Length; i++)
            {
                sb.Append($"({this.PrimaryConstructor.Method_Parameters[i]})default!");
                if (i != (this.PrimaryConstructor.Method_Parameters.Length - 1))
                {
                    sb.Append($", ");
                }
            }

            sb.Append(")");
            return sb.ToString();
        }
        else
        {// Default constructor. new()
            return "new " + this.FullName + "()";
        }
    }

    internal void GenerateFormatter_Deserialize2(ScopingStringBuilder ssb, TinyhandObject x)
    {// Called by GenerateDeserializeCore, GenerateDeserializeCore2
        /*if (x.DefaultValue != null)
        {
            if (this.MethodCondition_SetDefaultValue == MethodImplementationKind.Declared)
            {
                ssb.AppendLine($"vd.{TinyhandBody.SetDefaultValueMethod}({VisceralDefaultValue.DefaultValueToString(x.DefaultValue)});");
            }
            else if (this.MethodCondition_SetDefaultValue == MethodImplementationKind.ExplicitlyDeclared)
            {
                ssb.AppendLine($"(({TinyhandBody.ITinyhandDefaultName})vd).{TinyhandBody.SetDefaultValueMethod}({VisceralDefaultValue.DefaultValueToString(x.DefaultValue)});");
            }
        }*/

        if (this.ObjectFlags.HasFlag(TinyhandObjectFlags.HasIStringConvertible))
        {
            if (x.ObjectFlags.HasFlag(TinyhandObjectFlags.ReconstructTarget))
            {
                ssb.AppendLine($"TinyhandSerializer.ReadStringConvertibleOrDeserializeAndReconstructObject(ref reader, ref vd!, options);");
            }
            else
            {
                ssb.AppendLine($"TinyhandSerializer.ReadStringConvertibleOrDeserializeObject(ref reader, ref vd!, options);");
            }
        }
        else
        {
            ssb.AppendLine($"TinyhandSerializer.DeserializeObject(ref reader, ref vd!, options);");
        }
    }

    internal void GenerateFormatter_Reconstruct2(ScopingStringBuilder ssb, GenerationContext info, string originalName, object? defaultValue, bool reuseInstance)
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

        /*if (defaultValue != null)
        {
            if (this.MethodCondition_SetDefaultValue == MethodImplementationKind.Declared)
            {
                ssb.AppendLine($"v2.{TinyhandBody.SetDefaultValueMethod}({VisceralDefaultValue.DefaultValueToString(defaultValue)});");
            }
            else if (this.MethodCondition_SetDefaultValue == MethodImplementationKind.ExplicitlyDeclared)
            {
                ssb.AppendLine($"(({TinyhandBody.ITinyhandDefaultName})v2).{TinyhandBody.SetDefaultValueMethod}({VisceralDefaultValue.DefaultValueToString(defaultValue)});");
            }
        }*/

        ssb.AppendLine($"{ssb.FullObject} = v2!;");
    }

    /*internal void GenerateFormatter_Clone(ScopingStringBuilder ssb, GenerationContext info)
    {
        if (this.MethodCondition_Clone == MethodImplementationKind.StaticMethod)
        {// Static method
            ssb.AppendLine($"return {this.FullName}.DeepClone(ref value, options);"); // {this.GenericsNumberString}
        }
        else if (this.MethodCondition_Clone == MethodImplementationKind.ExplicitlyDeclared)
        {// Explicitly declared (Interface.Method())
            ssb.AppendLine($"return (value as ITinyhandCloneable<{this.FullName}>)?.DeepClone(options);");
        }
        else
        {// Member method
            ssb.AppendLine($"return value{this.QuestionMarkIfReferenceType}.DeepClone(options);");
        }
    }*/

    internal void GenerateDeserialize_Method(ScopingStringBuilder ssb, GenerationContext info)
    {
        string methodCode;
        string objectCode;

        if (this.MethodCondition_Deserialize == MethodImplementationKind.MemberMethod)
        {
            info.GeneratingStaticMethod = false;
            methodCode = $"public {this.UnsafeModifier}void Deserialize(ref TinyhandReader reader, TinyhandSerializerOptions options)";
            objectCode = "this";
        }
        else if (this.MethodCondition_Deserialize == MethodImplementationKind.StaticMethod)
        {
            info.GeneratingStaticMethod = true;
            methodCode = $"public static {this.UnsafeModifier}void Deserialize(scoped ref {this.RegionalName} v, ref TinyhandReader reader, TinyhandSerializerOptions options)"; // {this.GenericsNumberString}
            objectCode = "v";
        }
        else
        {
            return;
        }

        using (var m = ssb.ScopeBrace(methodCode))
        using (var v = ssb.ScopeObject(objectCode))
        {
            if (this.HasAlternateKey)
            {
                using (var scopeConvertToString = ssb.ScopeBrace("if (options.HasConvertToStringFlag)"))
                {
                    this.GenerateDeserializerStringKey(ssb, info);
                }

                using (var scopeConvertToInt = ssb.ScopeBrace("else"))
                {
                    this.GenerateDeserializerIntKey(ssb, info);
                }
            }
            else
            {
                if (this.ObjectFlags.HasFlag(TinyhandObjectFlags.StringKeyObject))
                {// String Key
                    this.GenerateDeserializerStringKey(ssb, info);
                }
                else
                {// Int Key
                    this.GenerateDeserializerIntKey(ssb, info);
                }
            }
        }
    }

    internal void GenerateDeserialize_Method2(ScopingStringBuilder ssb, GenerationContext info)
    {
        info.GeneratingStaticMethod = true;
        var methodCode = $"static {this.UnsafeModifier}void ITinyhandSerializable<{this.RegionalName}>.Deserialize(ref TinyhandReader reader, scoped ref {this.RegionalName}{this.QuestionMarkIfReferenceType} v, TinyhandSerializerOptions options)";
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

            // LockMemberName
            if (!string.IsNullOrEmpty(this.ObjectAttribute?.LockMemberName))
            {
                this.GenerateDeserialize_LockPrepare(ssb, info);
            }

            if (this.Kind.IsReferenceType())
            {
                ssb.AppendLine($"{ssb.FullObject} ??= {this.NewInstanceCode()};");
            }

            if (this.HasAlternateKey)
            {
                using (var scopeConvertToString = ssb.ScopeBrace("if (options.HasConvertToStringFlag)"))
                {
                    this.GenerateDeserializerStringKey(ssb, info);
                }

                using (var scopeConvertToInt = ssb.ScopeBrace("else"))
                {
                    this.GenerateDeserializerIntKey(ssb, info);
                }
            }
            else
            {
                if (this.ObjectFlags.HasFlag(TinyhandObjectFlags.StringKeyObject))
                {// String Key
                    this.GenerateDeserializerStringKey(ssb, info);
                }
                else
                {// Int Key
                    this.GenerateDeserializerIntKey(ssb, info);
                }
            }
        }
    }

    internal void GenerateDeserialize_LockPrepare(ScopingStringBuilder ssb, GenerationContext info)
    {
        var lockObject = this.ObjectAttribute?.LockMemberName;
        if (!string.IsNullOrEmpty(lockObject))
        {
            ssb.AppendLine($"var {TinyhandBody.LockObjectVariable} = {ssb.FullObject}{(this.Kind.IsReferenceType() ? "?" : string.Empty)}.{lockObject};");
            ssb.AppendLine($"var {TinyhandBody.LockTakenVariable} = false;");
        }
    }

    internal void GenerateDeserialize_LockEnter(ScopingStringBuilder ssb, GenerationContext info)
    {
        if (!string.IsNullOrEmpty(this.ObjectAttribute?.LockMemberName))
        {
            if (this.ObjectAttribute!.LockObjectType == LockObjectType.SemaphoreLock ||
                this.ObjectAttribute!.LockObjectType == LockObjectType.Lock)
            {
                ssb.AppendLine($"if ({TinyhandBody.LockObjectVariable} != null) {{ {TinyhandBody.LockObjectVariable}.Enter(); {TinyhandBody.LockTakenVariable} = true; }}");
            }
            else
            {
                ssb.AppendLine($"if ({TinyhandBody.LockObjectVariable} != null) System.Threading.Monitor.Enter({TinyhandBody.LockObjectVariable}, ref {TinyhandBody.LockTakenVariable});");
            }
        }
    }

    internal void GenerateDeserialize_LockExit(ScopingStringBuilder ssb, GenerationContext info)
    {
        if (!string.IsNullOrEmpty(this.ObjectAttribute?.LockMemberName))
        {
            if (this.ObjectAttribute!.LockObjectType == LockObjectType.SemaphoreLock ||
                this.ObjectAttribute!.LockObjectType == LockObjectType.Lock)
            {
                ssb.AppendLine($"if ({TinyhandBody.LockTakenVariable}) {TinyhandBody.LockObjectVariable}!.Exit();");
            }
            else
            {
                ssb.AppendLine($"if ({TinyhandBody.LockTakenVariable}) System.Threading.Monitor.Exit({TinyhandBody.LockObjectVariable}!);");
            }
        }
    }

    internal void GenerateReconstructRemaining(ScopingStringBuilder ssb, GenerationContext info)
    {
        /*if (this.TypeObject?.Kind == VisceralObjectKind.Struct)
        {
            foreach (var x in this.Members.Where(x => x.ReconstructMode == ReconstructMode.Always && x.KeyAttribute == null))
            {
                this.GenerateReconstructCore(ssb, info, x);
            }
        }*/
    }

    internal void GenerateJournal_SetParent(ScopingStringBuilder ssb, TinyhandObject? child, string parent, ref int count)
    {
        if (!this.SupportsStructuralObject ||
            child?.TypeObject is not { } typeObject ||
            child.KeyAttribute?.IntKey is not int key)
        {
            return;
        }

        if (typeObject.IsStructuralTarget)
        {
            var keyString = key.ToString();
            var objName = "obj" + keyString;
            ssb.AppendLine($"if ({ssb.FullObject} is {TinyhandBody.IStructuralObjectName} {objName}) {objName}.{TinyhandBody.SetupStructureMethod}({parent}, {keyString});");
            count++;
        }
    }

    internal void GenerateJournal_Delete(ScopingStringBuilder ssb, TinyhandObject? child)
    {
        if (child?.TypeObject is not { } typeObject || child.KeyAttribute?.IntKey is not int key)
        {
            return;
        }

        if (typeObject.IsStructuralTarget)
        {// IStructuralObject or unknown generated class
            var objName = $"obj{key.ToString()}";
            ssb.AppendLine($"if ({ssb.FullObject} is {TinyhandBody.IStructuralObjectName} {objName}) await {objName}.DeleteData(forceDeleteAfter, writeJournal).ConfigureAwait(false);");
        }
    }

    internal void GenerateJournal_StoreData(ScopingStringBuilder ssb, TinyhandObject? child)
    {
        if (child?.TypeObject is not { } typeObject || child.KeyAttribute?.IntKey is not int key)
        {
            return;
        }

        if (typeObject.IsStructuralTarget)
        {// IStructuralObject or unknown generated class
            var objName = $"obj{key.ToString()}";
            ssb.AppendLine($"if ({ssb.FullObject} is {TinyhandBody.IStructuralObjectName} {objName} && await {objName}.StoreData(storeMode).ConfigureAwait(false) == false) return false;");
        }
    }

    internal void GenerateJournal_Save(ScopingStringBuilder ssb, TinyhandObject? child)
    {
        if (child?.TypeObject is not { } typeObject || child.KeyAttribute?.IntKey is not int key)
        {
            return;
        }

        if (typeObject.IsStructuralTarget)
        {// IStructuralObject or unknown generated class
            var objName = $"obj{key.ToString()}";
            ssb.AppendLine($"if ({ssb.FullObject} is {TinyhandBody.IStructuralObjectName} {objName} && await {objName}.Save(unloadMode).ConfigureAwait(false) == false) return false;");
        }
    }

    /* internal void GenerateReconstruct_Method(ScopingStringBuilder ssb, GenerationContext info)
    {
        string methodCode;
        string objectCode;

        if (this.MethodCondition_Reconstruct == MethodImplementationKind.MemberMethod)
        {
            info.GeneratingStaticMethod = false;
            methodCode = $"public {this.UnsafeModifier}void Reconstruct(TinyhandSerializerOptions options)";
            objectCode = "this";
        }
        else if (this.MethodCondition_Reconstruct == MethodImplementationKind.StaticMethod)
        {
            info.GeneratingStaticMethod = true;
            methodCode = $"public static {this.UnsafeModifier}void Reconstruct(ref {this.RegionalName} v, TinyhandSerializerOptions options)"; // {this.GenericsNumberString}
            objectCode = "v";
        }
        else
        {
            return;
        }

        using (var m = ssb.ScopeBrace(methodCode))
        using (var v = ssb.ScopeObject(objectCode))
        {
            this.Generate_CallbackMethod(ssb, CallbackKind.OnReconstructing); // CallbackMethodCode

            foreach (var x in this.Members)
            {
                this.GenerateReconstructCore(ssb, info, x);
            }

            this.Generate_CallbackMethod(ssb, CallbackKind.OnReconstructed); // CallbackMethodCode
        }
    }*/

    internal void GenerateReconstruct_Method2(ScopingStringBuilder ssb, GenerationContext info)
    {
        info.GeneratingStaticMethod = true;
        var methodCode = $"static {this.UnsafeModifier}void ITinyhandReconstructable<{this.RegionalName}>.Reconstruct([NotNull] scoped ref {this.RegionalName}{this.QuestionMarkIfReferenceType} v, TinyhandSerializerOptions options)";
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

            // this.Generate_CallbackMethod(ssb, CallbackKind.OnReconstructing); // CallbackMethodCode

            if (this.TypeObject?.Kind == VisceralObjectKind.Struct)
            {// Since structs may be initialized with default instead of new(), Reconstruct processing is required.
                foreach (var x in this.Members)
                {
                    this.GenerateReconstructCore(ssb, info, x);
                }
            }

            this.Generate_CallbackMethod(ssb, CallbackKind.OnReconstructed); // CallbackMethodCode
        }
    }

    internal void GenerateClone_Method2(ScopingStringBuilder ssb, GenerationContext info)
    {
        info.GeneratingStaticMethod = true;
        var methodCode = $"static {this.UnsafeModifier}{this.RegionalName}{this.QuestionMarkIfReferenceType} ITinyhandCloneable<{this.RegionalName}>.Clone(scoped ref {this.RegionalName}{this.QuestionMarkIfReferenceType} v, TinyhandSerializerOptions options)";
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
            foreach (var x in this.GetMembersWithFlag(TinyhandObjectFlags.CloneTarget))
            {
                string sourceName;
                if (x.RefFieldDelegate is not null)
                {// Ref field or getter delegate
                    var prefix = info.GeneratingStaticMethod ? (this.RegionalName + ".") : string.Empty;
                    sourceName = $"{prefix}{x.RefFieldDelegate}({sourceObject})";
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

    internal void GenerateAddProperty(ScopingStringBuilder ssb, GenerationContext info)
    {// SetMethod IsInitOnly
        foreach (var x in this.GetMembersWithFlag(TinyhandObjectFlags.AddPropertyTarget))
        {
            if (x.TypeObjectWithNullable is not { } withNullable)
            {
                continue;
            }

            var requiredString = x.IsRequired ? "required " : string.Empty;

            if (x.KeyAttribute?.PropertyAccessibility == PropertyAccessibility.GetterOnly)
            {// getter-only
                ssb.AppendLine($"public {withNullable.FullNameWithNullable} {requiredString}{x.AddedPropertyOrPartialProperty} => {x.SimpleNameOrField};");
                continue;
            }

            var structuralEnabled = this.ObjectAttribute?.Structural == true ||
            this.ObjectFlags.HasFlag(TinyhandObjectFlags.IStructuralObjectImplemented);

            var property = x.Property_Accessibility;
            var partialProperty = "partial ";
            if (!x.IsPartialProperty)
            {
                property = new(Accessibility.Public, Accessibility.Public, false);
                partialProperty = string.Empty;

                if (x.KeyAttribute?.PropertyAccessibility == PropertyAccessibility.ProtectedSetter)
                {
                    if (this.IsSealed)
                    {
                        property.Setter = Accessibility.Private;
                    }
                    else
                    {
                        property.Setter = Accessibility.Protected;
                    }
                }
            }

            // ssb.AppendLine("[IgnoreMember]");
            using (var m = ssb.ScopeBrace($"{property.DeclarationAccessibility.AccessibilityToStringPlusSpace()}{requiredString}{partialProperty}{withNullable.FullNameWithNullable} {x.AddedPropertyOrPartialProperty}"))
            using (var scopeObject = ssb.ScopeFullObject($"{x.SimpleNameOrField}"))
            {
                ssb.AppendLine($"{property.GetterName} => {ssb.FullObject};");
                if (this.ObjectFlags.HasFlag(TinyhandObjectFlags.IsRepeatableRead))
                {// Repeatable read
                    using (var m2 = ssb.ScopeBrace($"{property.SetterName}"))
                    {// Main
                        // MaxLength
                        if (x.MaxLengthAttribute is not null)
                        {
                            JournalShared.GenerateValue_MaxLength(ssb, x, x.MaxLengthAttribute);
                        }

                        ssb.AppendLine($"{ssb.FullObject} = value;");
                    }
                }
                else
                {// Other
                    using (var m2 = ssb.ScopeBrace($"{property.SetterName}"))
                    {
                        // Compare values
                        if (withNullable.Object.IsPrimitive)
                        {
                            ssb.AppendLine($"if ({ssb.FullObject} == value) return;");
                        }
                        else
                        {
                            ssb.AppendLine($"if (EqualityComparer<{withNullable.Object.FullName}>.Default.Equals({ssb.FullObject}, value)) return;");
                        }

                        // MaxLength
                        if (x.MaxLengthAttribute is not null)
                        {
                            JournalShared.GenerateValue_MaxLength(ssb, x, x.MaxLengthAttribute);
                        }

                        // Lock
                        var lockExpression = this.GetLockExpression("this");
                        var lockScope = lockExpression is null ? null : ssb.ScopeBrace(lockExpression);

                        if (structuralEnabled)
                        {
                            x.CodeJournal(ssb, null);
                        }

                        ssb.AppendLine($"{ssb.FullObject} = value;");

                        // Update link
                        if (this.ObjectFlags.HasFlag(TinyhandObjectFlags.HasValueLinkObject) &&
                            x.AllAttributes.Any(y => y.FullName == "ValueLink.LinkAttribute"))
                        {
                            ssb.AppendLine($"this.{TinyhandBody.ValueLinkUpdateMethodPrefix}{x.SimpleName}();");
                        }

                        // Clear integrality hash
                        if (this.ObjectFlags.HasFlag(TinyhandObjectFlags.HasIIntegralityObject))
                        {
                            ssb.AppendLine($"(({TinyhandBody.IIntegralityObjectFullName})this).ClearIntegralityHash();");
                        }

                        lockScope?.Dispose();

                        if (structuralEnabled)
                        {
                            ssb.AppendLine($"(({TinyhandBody.IStructuralObjectName})this).StructuralRoot?.AddToSaveQueue();");
                        }
                    }
                }
            }
        }
    }

    internal string? GetLockExpression(string objectName)
    {
        if (string.IsNullOrEmpty(this.ObjectAttribute?.LockMemberName))
        {
            return null;
        }

        if (this.ObjectAttribute!.LockObjectType == LockObjectType.SemaphoreLock ||
            this.ObjectAttribute!.LockObjectType == LockObjectType.Lock)
        {
            return $"using ({objectName}.{this.ObjectAttribute!.LockMemberName}!.EnterScope())";
        }
        else
        {
            return $"lock ({objectName}.{this.ObjectAttribute!.LockMemberName}!)";
        }
    }

    internal string? GetAsyncLockExpression(string objectName)
    {
        if (string.IsNullOrEmpty(this.ObjectAttribute?.LockMemberName))
        {
            return null;
        }

        if (this.ObjectAttribute!.LockObjectType == LockObjectType.SemaphoreLock)
        {
            return $"await {objectName}.{this.ObjectAttribute!.LockMemberName}!.EnterAsync().ConfigureAwait(false); try";
        }
        else if (this.ObjectAttribute!.LockObjectType == LockObjectType.Object ||
            this.ObjectAttribute!.LockObjectType == LockObjectType.Lock)
        {
            this.Body.ReportDiagnostic(TinyhandBody.Warning_LockObjectSemaphoreLockRecommended, this.Location);
            // return $"{objectName}.{this.ObjectAttribute!.LockMemberName}!.Enter(); try {{";
        }

        return null;
    }

    internal void EndAsyncLockExpression(ScopingStringBuilder ssb, string objectName)
    {
        if (string.IsNullOrEmpty(this.ObjectAttribute?.LockMemberName))
        {
            return;
        }

        if (this.ObjectAttribute!.LockObjectType == LockObjectType.SemaphoreLock)
        {
            ssb.AppendLine($"finally {{ {objectName}.{this.ObjectAttribute!.LockMemberName}!.Exit(); }}");
        }

        /*else if (this.ObjectAttribute!.LockObjectType == LockObjectType.Lock)
        {
            ssb.AppendLine($"}} finally {{ {objectName}.{this.ObjectAttribute!.LockMemberName}!.Exit(); }}");
        }*/
    }

    internal void GenerateAddProperty_MaxLength(ScopingStringBuilder ssb, GenerationContext info, TinyhandObject x, MaxLengthAttributeData attribute)
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

    internal void GenerateMemberNotNull_Attribute(ScopingStringBuilder ssb, GenerationContext info)
    {
        var firstFlag = true;
        foreach (var x in this.Members.Where(x => x.ReconstructMode == ReconstructMode.Always && x.ContainingObject == this))
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

    internal void GenerateMemberNotNull_MemberMethod(ScopingStringBuilder ssb, GenerationContext info)
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

    internal void GenerateMemberNotNull_StaticMethod(ScopingStringBuilder ssb, GenerationContext info)
    {
        this.GenerateMemberNotNull_Attribute(ssb, info);
        using (var m = ssb.ScopeBrace($"public static void MemberNotNull()"))
        {
        }
    }

    internal void GenerateUnsafeConstructor(ScopingStringBuilder ssb)
    {
        var st = string.Empty;
        TinyhandObject? constructor = default;
        if (this.BaseObject is not null)
        {
            this.BaseObject.PrepareMinimumConstructor();
            constructor = this.BaseObject.PublicMinimumConstructor ?? this.BaseObject.MinimumConstructor;
        }
        else if (this.PrimaryConstructor is not null)
        {
            constructor = this.PrimaryConstructor;
        }

        if (constructor is not null)
        {
            var sb = new StringBuilder();
            if (this.PrimaryConstructor is null)
            {
                sb.Append(": base(");
            }
            else
            {
                sb.Append(": this(");
            }

            for (var i = 0; i < constructor.Method_Parameters.Length; i++)
            {
                sb.Append($"({constructor.Method_Parameters[i]})default!");
                if (i != (constructor.Method_Parameters.Length - 1))
                {
                    sb.Append($", ");
                }
            }

            sb.Append(") ");
            st = sb.ToString();
        }
        else if (this.BaseObject is not null &&
            this.BaseObject.ObjectAttribute is not null)
        {
            st = $": base({TinyhandBody.UnsafeEnumName}.Parameter)";
        }

        StringBuilder? sb2 = default;
        foreach (var x in this.GetMembersWithFlag(TinyhandObjectFlags.SerializeTarget))
        {
            if (x.IsPartialProperty && x.IsRequired && x.TypeObject?.Kind.IsReferenceType() == true && x.ContainingObject == this)
            {
                sb2 ??= new();
                // sb2.Append($"[MemberNotNull(nameof({x.SimpleName}))] ");
                sb2.Append($"this.{x.SimpleName} = default!; ");
            }
        }

        var memberNotNull = sb2?.ToString() ?? string.Empty;

        ssb.AppendLine();

        string constructorAccessibility;
        if (this.IsSealed)
        {
            constructorAccessibility = "private";
        }
        else
        {
            constructorAccessibility = "protected";
        }

        ssb.AppendLine($"[SetsRequiredMembers] {constructorAccessibility} {this.SimpleName}({TinyhandBody.UnsafeEnumName} p) {st}{{ {memberNotNull}}}");
        ssb.AppendLine($" public static {this.LocalName} {TinyhandBody.UnsafeConstructorName}() => new({TinyhandBody.UnsafeEnumName}.Parameter);");
    }

    internal void GenerateIStructuralObject(ScopingStringBuilder ssb, GenerationContext info)
    {
        ssb.AppendLine();

        if (!this.ObjectFlags.HasFlag(TinyhandObjectFlags.DerivedFromStoragePoint))
        {
            ssb.AppendLine($"[IgnoreMember] {TinyhandBody.IStructuralRootName}? {TinyhandBody.IStructuralObjectName}.StructuralRoot {{ get; set; }}");
            ssb.AppendLine($"[IgnoreMember] {TinyhandBody.IStructuralObjectName}? {TinyhandBody.IStructuralObjectName}.StructuralParent {{ get; set; }}");
            ssb.AppendLine($"[IgnoreMember] int {TinyhandBody.IStructuralObjectName}.StructuralKey {{ get; set; }} = -1;");
        }

        this.GenerateSetParent(ssb, info, out var count);
        this.GenerateReadRecord(ssb, info);

        if (count > 0)
        {
            // this.GenerateIStructuralObject_Save(ssb, info);
            this.GenerateIStructuralObject_Erase(ssb, info);
            this.GenerateIStructuralObject_StoreData(ssb, info);
        }
    }

    internal void GenerateIStructuralObject_Save(ScopingStringBuilder ssb, GenerationContext info)
    {
        using (var scopeMethod = ssb.ScopeBrace($"async Task<bool> {TinyhandBody.IStructuralObjectName}.Save(UnloadMode unloadMode)"))
        {
            if (this.IntKey_Array is not null)
            {
                using (var t = ssb.ScopeObject("this"))
                {
                    foreach (var x in this.IntKey_Array)
                    {
                        if (x is null)
                        {
                            continue;
                        }

                        using (var m = this.ScopeMember(ssb, x))
                        {
                            this.GenerateJournal_Save(ssb, x);
                        }
                    }
                }
            }

            ssb.AppendLine("return true;");
        }
    }

    internal void GenerateIStructuralObject_StoreData(ScopingStringBuilder ssb, GenerationContext info)
    {// Task<bool> StoreData(StoreMode storeMode)
        using (var scopeMethod = ssb.ScopeBrace($"async Task<bool> {TinyhandBody.IStructuralObjectName}.StoreData(StoreMode storeMode)"))
        {
            if (this.IntKey_Array is not null)
            {
                // Lock
                var lockExpression = this.GetAsyncLockExpression("this");
                ScopingStringBuilder.IScope? lockScope = default;

                using (var t = ssb.ScopeObject("this"))
                {
                    foreach (var x in this.IntKey_Array.Where(a => a?.ObjectFlags.HasFlag(TinyhandObjectFlags.DerivedFromStoragePoint) == false))
                    {// Other
                        lockScope ??= lockExpression is null ? null : ssb.ScopeBrace(lockExpression);
                        using (var m = this.ScopeMember(ssb, x!))
                        {
                            this.GenerateJournal_StoreData(ssb, x);
                        }
                    }

                    if (lockScope is not null)
                    {
                        lockScope.Dispose();
                        this.EndAsyncLockExpression(ssb, "this");
                        lockScope = default;
                    }

                    foreach (var x in this.IntKey_Array.Where(a => a?.ObjectFlags.HasFlag(TinyhandObjectFlags.DerivedFromStoragePoint) == true))
                    {// Thread-safe
                        using (var m = this.ScopeMember(ssb, x!))
                        {
                            this.GenerateJournal_StoreData(ssb, x);
                        }
                    }
                }
            }

            ssb.AppendLine("return true;");
        }
    }

    internal void GenerateIStructuralObject_Erase(ScopingStringBuilder ssb, GenerationContext info)
    {
        using (var scopeMethod = ssb.ScopeBrace($"async Task {TinyhandBody.IStructuralObjectName}.DeleteData(DateTime forceDeleteAfter, bool writeJournal)"))
        {
            if (this.IntKey_Array is not null)
            {
                using (var t = ssb.ScopeObject("this"))
                {
                    // Lock
                    var lockExpression = this.GetLockExpression("this");
                    ScopingStringBuilder.IScope? lockScope = default;

                    foreach (var x in this.IntKey_Array.Where(a => a?.ObjectFlags.HasFlag(TinyhandObjectFlags.DerivedFromStoragePoint) == false))
                    {// Other
                        lockScope ??= lockExpression is null ? null : ssb.ScopeBrace(lockExpression);
                        using (var m = this.ScopeMember(ssb, x!))
                        {
                            this.GenerateJournal_Delete(ssb, x);
                        }
                    }

                    lockScope?.Dispose();

                    foreach (var x in this.IntKey_Array.Where(a => a?.ObjectFlags.HasFlag(TinyhandObjectFlags.DerivedFromStoragePoint) == true))
                    {// Thread-safe
                        using (var m = this.ScopeMember(ssb, x!))
                        {
                            this.GenerateJournal_Delete(ssb, x);
                        }
                    }
                }
            }
        }
    }

    internal void GenerateSetParent(ScopingStringBuilder ssb, GenerationContext info, out int count)
    {// public void SetupStructure(IStructuralObject? parent, int key = -1)
        count = 0;
        using (var scopeMethod = ssb.ScopeBrace($"void {TinyhandBody.IStructuralObjectName}.{TinyhandBody.SetupStructureMethod}({TinyhandBody.IStructuralObjectName}? parent, int key)"))
        {
            ssb.AppendLine($"(({TinyhandBody.IStructuralObjectName})this).SetParentAndKey(parent, key);");

            // ssb.AppendLine($"var structuralObject = ({TinyhandBody.IStructuralObjectName})this;");
            // ssb.AppendLine($"structuralObject.StructuralRoot = parent?.StructuralRoot;");
            // ssb.AppendLine($"structuralObject.StructuralParent = parent;");
            // ssb.AppendLine($"structuralObject.StructuralKey = key;");

            if (this.IntKey_Array is not null)
            {
                using (var t = ssb.ScopeObject("this"))
                {
                    foreach (var x in this.IntKey_Array)
                    {
                        if (x is null)
                        {
                            continue;
                        }

                        using (var m = this.ScopeMember(ssb, x))
                        {
                            this.GenerateJournal_SetParent(ssb, x, "this", ref count);
                        }
                    }
                }
            }
        }
    }

    internal void GenerateReadRecord(ScopingStringBuilder ssb, GenerationContext info)
    {
        using (var scopeMethod = ssb.ScopeBrace($"{this.UnsafeModifier}bool {TinyhandBody.IStructuralObjectName}.ProcessJournalRecord(ref TinyhandReader reader)"))
        {
            // Lock
            var lockExpression = this.GetLockExpression("this");
            var lockScope = lockExpression is null ? null : ssb.ScopeBrace(lockExpression);

            // Custom read
            /*if (this.MethodCondition_ReadCustomRecord == MethodImplementationKind.Declared ||
                this.MethodCondition_ReadCustomRecord == MethodImplementationKind.ExplicitlyDeclared)
            {
                ssb.AppendLine("var fork = reader.Fork();");
                var readCustomRecord = this.MethodCondition_ReadCustomRecord == MethodImplementationKind.Declared ?
                    "if (this.ReadCustomRecord(ref fork))" : "if (((ITinyhandCustomJournal)this).ReadCustomRecord(ref fork))";
                using (var scopeCustom = ssb.ScopeBrace(readCustomRecord))
                {
                    ssb.AppendLine("return true;");
                }

                ssb.AppendLine();
            }*/

            if (this.MethodCondition_ReadCustomRecord == MethodImplementationKind.Declared ||
                this.MethodCondition_ReadCustomRecord == MethodImplementationKind.ExplicitlyDeclared ||
                this.BaseObject is not null)
            {// ITinyhandCustomJournal
                using (var scopeTry = ssb.ScopeBrace("try"))
                {
                    using (var scopeCustom = ssb.ScopeBrace("if (this is ITinyhandCustomJournal custom)"))
                    {
                        ssb.AppendLine("var fork = reader.Fork();");
                        using (var scopeCustom2 = ssb.ScopeBrace("if (custom.ReadCustomRecord(ref fork))"))
                        {
                            ssb.AppendLine("return true;");
                        }
                    }
                }

                ssb.AppendLine("catch {}");
                ssb.AppendLine();
            }

            if (this.IntKey_Count > 0 ||
                (this.StringTrie is not null && this.StringTrie.NameToNode.Count > 0))
            {
                ssb.AppendLine("KeyLoop:", false);
            }

            ssb.AppendLine("if (!reader.TryReadJournalRecord(out JournalRecordType record)) return false;");
            using (var scopeKey = ssb.ScopeBrace("if (record == JournalRecordType.Key)"))
            {
                ssb.AppendLine("var options = TinyhandSerializerOptions.Standard;");
                if (this.IntKey_Array is { } intArray)
                {// Int Key (priority)
                    var trie = new VisceralTrieInt<TinyhandObject>(this);
                    for (var i = 0; i < intArray.Length; i++)
                    {
                        if (intArray[i] is not null)
                        {
                            trie.AddNode(i, intArray[i]!);
                        }
                    }

                    using (var thisScope = ssb.ScopeObject("this"))
                    {
                        var context = new VisceralTrieInt<TinyhandObject>.VisceralTrieContext(
                            ssb,
                            (ctx, obj, node) =>
                            {
                                this.GenerateReadRecordCore(ssb, info, node.Member);
                            });

                        trie.Generate(context);
                    }
                }
                else if (this.StringTrie is not null)
                {// String Key
                    using (var thisScope = ssb.ScopeObject("this"))
                    {
                        var context = new VisceralTrieString<TinyhandObject>.VisceralTrieContext(
                            ssb,
                            (ctx, obj, node) =>
                            {
                                this.GenerateReadRecordCore(ssb, info, node.Member);
                            });

                        this.StringTrie.Generate(context);
                    }
                }
            }

            if (this.ObjectFlags.HasFlag(TinyhandObjectFlags.DerivedFromStoragePoint))
            {
                using (var scopeValue = ssb.ScopeBrace("else if (record == JournalRecordType.Value)"))
                {
                    ssb.AppendLine("this.pointId = reader.ReadUInt64();");
                    ssb.AppendLine("return true;");
                }
            }

            lockScope?.Dispose();

            ssb.AppendLine("return false;");
        }
    }

    internal void GenerateReadRecordCore(ScopingStringBuilder ssb, GenerationContext info, TinyhandObject? x)
    {
        var withNullable = x?.TypeObjectWithNullable;
        if (x == null || withNullable == null)
        {// no object
            return;
        }

        var count = 0;
        var assignment = new ValueAssignment(ssb, info, this, x);
        var destObject = ssb.FullObject; // Hidden members
        using (var m = this.ScopeSimpleMember(ssb, x))
        {
            if (x.TypeObject?.IsStructuralTarget == true)
            {
                ssb.AppendLine($"if (reader.IsNextNonValueRecord() && {ssb.FullObject} is {TinyhandBody.IStructuralObjectName} obj && obj.ProcessJournalRecord(ref reader)) return true;");
            }

            ssb.AppendLine("reader.ReadValueRecord();");

            /*if (x.ObjectFlags.HasFlag(TinyhandObjectFlags.HasIJournalObject))
            {
                ssb.AppendLine($"return (({TinyhandBody.IStructuralObjectName}){ssb.FullObject}).ProcessJournalRecord(ref reader);");
            }*/

            assignment.Start(false);

            var coder = this.Body.CoderResolver.TryGetCoder(withNullable)!;
            if (coder != null)
            {
                if (coder.RequiresRefValue)
                {
                    assignment.RefValue(true);
                }

                coder.CodeDeserialize(ssb, info, true);
            }
            else
            {
                assignment.RefValue(x.ObjectFlags.HasFlag(TinyhandObjectFlags.ReuseInstanceTarget));
                if (x.HasNullableAnnotation || withNullable.Object.Kind.IsValueType() || x.TypeObject?.IsTypeParameterWithValueTypeConstraint() == true)
                {// T?
                    ssb.AppendLine($"options.Resolver.GetFormatter<{withNullable.Object.FullName}>().Deserialize(ref reader, ref vd, options);");
                }
                else
                {// T
                    ssb.AppendLine($"var f = options.Resolver.GetFormatter<{withNullable.Object.FullName}>();");
                    ssb.AppendLine($"f.Deserialize(ref reader, ref vd!, options);");
                    ssb.AppendLine($"vd ??= f.Reconstruct(options);");
                }
            }

            assignment.End();

            this.GenerateJournal_SetParent(ssb, x, destObject, ref count);

            if (this.ObjectFlags.HasFlag(TinyhandObjectFlags.HasValueLinkObject) &&
                x.AllAttributes.Any(y => y.FullName == "ValueLink.LinkAttribute"))
            {
                ssb.AppendLine($"this.{TinyhandBody.ValueLinkUpdateMethodPrefix}{x.SimpleName}();");
            }

            ssb.AppendLine("if (reader.IsNextKeyRecord()) goto KeyLoop;");

            ssb.AppendLine("return true;");
        }
    }

    internal void GenerateMethod(ScopingStringBuilder ssb, GenerationContext info)
    {
        // Serialize/Deserialize/Reconstruct/Clone
        /*this.GenerateSerialize_Method(ssb, info);
        this.GenerateDeserialize_Method(ssb, info);
        this.GenerateReconstruct_Method(ssb, info);
        this.GenerateClone_Method(ssb, info);*/

        // Serialize/Deserialize/Reconstruct/Clone
        if (this.Generics_Kind != VisceralGenericsKind.ClosedGeneric)
        {
            if (this.MethodCondition_Serialize == MethodImplementationKind.StaticMethod)
            {
                this.GenerateSerialize_Method2(ssb, info);
            }

            if (this.MethodCondition_Deserialize == MethodImplementationKind.StaticMethod)
            {
                this.GenerateDeserialize_Method2(ssb, info);
            }

            /*if (this.MethodCondition_GetTypeIdentifier == MethodImplementationKind.StaticMethod)
            {// GetTypeIdentifierCode
                if (this.Generics_IsGeneric)
                {// Generics
                    ssb.AppendLine($"private static ulong __type_identifier__;");
                    ssb.AppendLine($"static ulong ITinyhandSerializable<{this.RegionalName}>.GetTypeIdentifier() => __type_identifier__ != 0 ? __type_identifier__ : (__type_identifier__ = Arc.Visceral.VisceralHelper.TypeToFarmHash64(typeof({this.RegionalName})));");
                }
                else
                {// Non-generics
                    ssb.AppendLine($"static ulong ITinyhandSerializable<{this.RegionalName}>.GetTypeIdentifier() => 0x{FarmHash.Hash64(this.FullName).ToString("x")}ul;");
                }
            }*/

            if (this.MethodCondition_Reconstruct == MethodImplementationKind.StaticMethod)
            {
                this.GenerateReconstruct_Method2(ssb, info);
            }

            if (this.MethodCondition_Clone == MethodImplementationKind.StaticMethod)
            {
                this.GenerateClone_Method2(ssb, info);
            }

            // ITinyhandSerializable
            ssb.AppendLine("void ITinyhandSerializable.Deserialize(ref TinyhandReader reader, TinyhandSerializerOptions options)");
            if (this.Kind.IsReferenceType())
            {
                ssb.AppendLine("{ var rt = this; TinyhandSerializer.DeserializeObject(ref reader, ref rt, options); }");
            }
            else
            {
                ssb.AppendLine("  => TinyhandSerializer.DeserializeObject(ref reader, ref Unsafe.AsRef(in this)!, options);");
            }

            ssb.AppendLine("void ITinyhandSerializable.Serialize(ref TinyhandWriter writer, TinyhandSerializerOptions options)");
            ssb.AppendLine("  => TinyhandSerializer.SerializeObject(ref writer, this, options);");
            // ssb.AppendLine($"ulong ITinyhandSerializable.GetTypeIdentifier() => TinyhandSerializer.GetTypeIdentifierObject<{this.RegionalName}>();"); // GetTypeIdentifierCode
        }

        this.GenerateAddProperty(ssb, info);

        return;
    }

    internal void PrepareTrie()
    {
        if (this.StringTrie == null)
        {
            return;
        }

        var count = 0;
        foreach (var x in this.StringTrie.NodeList)
        {
            x.SubIndex = -1;
            if (x.Member == null)
            {
                continue;
            }

            if (x.Member.NullableAnnotationIfReferenceType == Arc.Visceral.NullableAnnotation.NotAnnotated ||
                x.Member.Kind.IsValueType() ||
                x.Member.IsDefaultable ||
                x.Member.ReconstructMode == ReconstructMode.Always)
            {
                x.SubIndex = count++;
            }
        }

        this.StringTrieReconstructNumber = count;
    }

    internal void GenerateConstructor_Method(ScopingStringBuilder ssb, GenerationContext info)
    {
        // Array
        var array = this.GetMembersWithFlag(TinyhandObjectFlags.SerializeTarget).ToArray();
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

    internal void GenerateConstructorCore(ScopingStringBuilder ssb, GenerationContext info, bool isConstructor, TinyhandObject[] array)
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

    internal void GenerateSetMembers_Method(ScopingStringBuilder ssb, GenerationContext info)
    {
        // Array
        var array = this.GetMembersWithFlag(TinyhandObjectFlags.SerializeTarget).Where(x => !x.IsInitOnly).ToArray();
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

    internal ScopingStringBuilder.IScope ScopeMember(ScopingStringBuilder ssb, TinyhandObject x)
    {// ssb.ScopeObject(x.SimpleNameOrAddedProperty) -> this.ScopeMember(ssb, x)
        if (x.ObjectFlags.HasFlag(TinyhandObjectFlags.HiddenMember) &&
            x.ContainingObject is not null)
        {// ((BaseClass)v).Member
            var name = $"(({x.ContainingObject.FullName}){ssb.FullObject}).{x.SimpleNameOrAddedProperty}";
            return ssb.ScopeFullObject(name);
        }
        else
        {// v.Member
            return ssb.ScopeObject(x.SimpleNameOrAddedProperty);
        }
    }

    internal ScopingStringBuilder.IScope ScopeSimpleMember(ScopingStringBuilder ssb, TinyhandObject x)
    {// ssb.ScopeObject(x.SimpleNameOrAddedProperty) -> this.ScopeMember(ssb, x)
        if (x.ObjectFlags.HasFlag(TinyhandObjectFlags.HiddenMember) &&
            x.ContainingObject is not null)
        {// ((BaseClass)v).Member
            var name = $"(({x.ContainingObject.FullName}){ssb.FullObject}).{x.SimpleName}";
            return ssb.ScopeFullObject(name);
        }
        else
        {// v.Member
            return ssb.ScopeObject(x.SimpleName);
        }
    }

    internal string GetSourceName(string sourceObject, TinyhandObject x)
    {// ssb.ScopeObject(x.SimpleNameOrAddedProperty) -> this.ScopeMember(ssb, x)
        if (x.ObjectFlags.HasFlag(TinyhandObjectFlags.HiddenMember) &&
            x.ContainingObject is not null)
        {// ((BaseClass)v).Member
            return $"(({x.ContainingObject.FullName}){sourceObject}).{x.SimpleNameOrAddedProperty}";
        }
        else
        {// v.Member
            return sourceObject + "." + x.SimpleNameOrAddedProperty;
        }
    }

    internal void GenerateDeserializeCore(ScopingStringBuilder ssb, GenerationContext info, TinyhandObject? x)
    {// Integer key
        var withNullable = x?.TypeObjectWithNullable;
        if (x == null || withNullable == null)
        {// no object
            ssb.AppendLine("if (numberOfData-- > 0) reader.Skip();");
            return;
        }

        var count = 0;
        var assignment = new ValueAssignment(ssb, info, this, x);
        var destObject = ssb.FullObject; // Hidden members
        using (var m = this.ScopeMember(ssb, x))
        {
            var originalName = ssb.FullObject;
            var coder = this.Body.CoderResolver.TryGetCoder(withNullable);
            var exclude = x.KeyAttribute?.Exclude == true ? "!options.IsExcludeMode && " : string.Empty;
            using (var valid = ssb.ScopeBrace($"if ({exclude}numberOfData-- > 0 && !reader.TryReadNil())"))
            {
                assignment.Start(false);

                if (withNullable.Object.ObjectAttribute?.UseResolver == false &&
                    (withNullable.Object.ObjectAttribute != null || withNullable.Object.HasITinyhandSerializeConstraint()))
                {// TinyhandObject. For the purpose of default value and instance reuse.
                    assignment.RefValue(x.ObjectFlags.HasFlag(TinyhandObjectFlags.ReuseInstanceTarget));
                    withNullable.Object.GenerateFormatter_Deserialize2(ssb, x);
                }
                else if (coder != null)
                {
                    if (coder.RequiresRefValue)
                    {
                        assignment.RefValue(true);
                    }

                    coder.CodeDeserialize(ssb, info, true);
                }
                else
                {
                    if (x.TypeObject != null && (x.TypeObject.Kind != VisceralObjectKind.Error && x.TypeObject.Kind != VisceralObjectKind.TypeParameter))
                    {
                        this.Body.ReportDiagnostic(TinyhandBody.Warning_NoCoder, x.Location, withNullable.FullName);
                    }

                    assignment.RefValue(x.ObjectFlags.HasFlag(TinyhandObjectFlags.ReuseInstanceTarget));
                    if (x.HasNullableAnnotation || withNullable.Object.Kind.IsValueType() || x.TypeObject?.IsTypeParameterWithValueTypeConstraint() == true)
                    {// T?
                        ssb.AppendLine($"options.Resolver.GetFormatter<{withNullable.Object.FullName}>().Deserialize(ref reader, ref vd, options);");
                    }
                    else
                    {// T
                        ssb.AppendLine($"var f = options.Resolver.GetFormatter<{withNullable.Object.FullName}>();");
                        ssb.AppendLine($"f.Deserialize(ref reader, ref vd!, options);");
                        ssb.AppendLine($"vd ??= f.Reconstruct(options);");
                    }
                }

                assignment.End();
            }

            // AbandonReconstructCode
            /*if (x.IsDefaultable)
            {// Default
                using (var invalid = ssb.ScopeBrace("else"))
                {
                    assignment.Start(false);
                    ssb.AppendLine($"{ssb.FullObject} = {VisceralDefaultValue.DefaultValueToString(x.DefaultValue)};");
                    assignment.End();
                }
            }
            else if (x.ReconstructMode == ReconstructMode.Always)
            {
                using (var invalid = ssb.ScopeBrace("else"))
                {
                    assignment.Start(true, false);
                    if (withNullable.Object.ObjectAttribute != null)
                    {// TinyhandObject. For the purpose of default value and instance reuse.
                        withNullable.Object.GenerateFormatter_Reconstruct2(ssb, info, originalName, x.DefaultValue, x.ObjectFlags.HasFlag(TinyhandObjectFlags.ReuseInstanceTarget));
                    }
                    else if (coder != null)
                    {
                        coder.CodeReconstruct(ssb, info);
                    }
                    else
                    {
                        ssb.AppendLine($"{ssb.FullObject} = options.Resolver.GetFormatter<{withNullable.Object.FullName}>().Reconstruct(options);");
                    }

                    assignment.End();
                }
            }*/

            this.GenerateJournal_SetParent(ssb, x, destObject, ref count);
        }
    }

    internal void GenerateDeserializeCore2(ScopingStringBuilder ssb, GenerationContext info, TinyhandObject? x)
    {// String key
        var withNullable = x?.TypeObjectWithNullable;
        if (x == null || withNullable == null)
        {// no object
            ssb.GotoSkipLabel();
            return;
        }

        var assignment = new ValueAssignment(ssb, info, this, x);
        using (var m = this.ScopeMember(ssb, x))
        {
            var originalName = ssb.FullObject;
            var coder = this.Body.CoderResolver.TryGetCoder(withNullable);
            using (var valid = ssb.ScopeBrace($"if (!reader.TryReadNil())"))
            {
                assignment.Start(false);

                if (withNullable.Object.ObjectAttribute?.UseResolver == false &&
                    (withNullable.Object.ObjectAttribute != null || withNullable.Object.HasITinyhandSerializeConstraint()))
                {// TinyhandObject. For the purpose of default value and instance reuse.
                    assignment.RefValue(x.ObjectFlags.HasFlag(TinyhandObjectFlags.ReuseInstanceTarget));
                    withNullable.Object.GenerateFormatter_Deserialize2(ssb, x);
                }
                else if (coder != null)
                {
                    if (coder.RequiresRefValue)
                    {
                        assignment.RefValue(true);
                    }

                    coder.CodeDeserialize(ssb, info, true);
                }
                else
                {
                    if (x.TypeObject != null && (x.TypeObject.Kind != VisceralObjectKind.Error && x.TypeObject.Kind != VisceralObjectKind.TypeParameter))
                    {
                        this.Body.ReportDiagnostic(TinyhandBody.Warning_NoCoder, x.Location, withNullable.FullName);
                    }

                    assignment.RefValue(x.ObjectFlags.HasFlag(TinyhandObjectFlags.ReuseInstanceTarget));
                    if (x.HasNullableAnnotation || withNullable.Object.Kind.IsValueType() || x.TypeObject?.IsTypeParameterWithValueTypeConstraint() == true)
                    {// T?
                        ssb.AppendLine($"options.Resolver.GetFormatter<{withNullable.Object.FullName}>().Deserialize(ref reader, ref vd, options);");
                    }
                    else
                    {// T
                        ssb.AppendLine($"var f = options.Resolver.GetFormatter<{withNullable.Object.FullName}>();");
                        ssb.AppendLine($"f.Deserialize(ref reader, ref vd!, options);");
                        ssb.AppendLine($"vd ??= f.Reconstruct(options);");
                    }
                }

                assignment.End();
            }

            // AbandonReconstructCode
            /*if (x.IsDefaultable)
            {// Default
                using (var invalid = ssb.ScopeBrace("else"))
                {
                    assignment.Start(false);
                    ssb.AppendLine($"{ssb.FullObject} = {VisceralDefaultValue.DefaultValueToString(x.DefaultValue)};");
                    assignment.End();
                }
            }
            else if (x.ReconstructMode == ReconstructMode.Always)
            {
                using (var invalid = ssb.ScopeBrace("else"))
                {
                    assignment.Start(true, false);
                    if (withNullable.Object.ObjectAttribute != null)
                    {// TinyhandObject. For the purpose of default value and instance reuse.
                        withNullable.Object.GenerateFormatter_Reconstruct2(ssb, info, originalName, x.DefaultValue, x.ObjectFlags.HasFlag(TinyhandObjectFlags.ReuseInstanceTarget));
                    }
                    else if (coder != null)
                    {
                        coder.CodeReconstruct(ssb, info);
                    }
                    else
                    {
                        ssb.AppendLine($"{ssb.FullObject} = options.Resolver.GetFormatter<{withNullable.Object.FullName}>().Reconstruct(options);");
                    }

                    assignment.End();
                }
            }*/

            // this.GenerateJournal_SetParent(ssb, "this");
        }
    }

    internal void GenerateReconstructCore(ScopingStringBuilder ssb, GenerationContext info, TinyhandObject x)
    {// Called by GenerateReconstruct()
        var withNullable = x?.TypeObjectWithNullable;
        if (x == null || withNullable == null)
        {// no object
            return;
        }

        var count = 0;
        var assignment = new ValueAssignment(ssb, info, this, x);
        var destObject = ssb.FullObject; // Hidden members

        using (var c2 = this.ScopeSimpleMember(ssb, x))
        {
            var originalName = ssb.FullObject;
            if (x.IsDefaultable)
            {// Default
                assignment.Start(false, true);
                ssb.AppendLine($"{ssb.FullObject} = {VisceralDefaultValue.DefaultValueToString(x.DefaultValue)};");
                assignment.End();
                return;
            }

            if (x.ReconstructMode != ReconstructMode.Always)
            {
                return;
            }

            // var nullCheckCode = withNullable.Object.Kind.IsReferenceType() && !x.ObjectFlags.HasFlag(TinyhandObjectFlags.ReuseInstanceTarget) ? $"if ({ssb.FullObject} == null)" : string.Empty;

            if (withNullable.Object.ObjectAttribute != null &&
                withNullable.Object.ObjectAttribute.UseResolver == false)
            {// TinyhandObject. For the purpose of default value and instance reuse.
                using (var c = ssb.ScopeBrace(string.Empty))
                {
                    assignment.Start(true);
                    withNullable.Object.GenerateFormatter_Reconstruct2(ssb, info, originalName, x.DefaultValue, x.ObjectFlags.HasFlag(TinyhandObjectFlags.ReuseInstanceTarget));
                    assignment.End();
                }
            }
            else if (this.Body.CoderResolver.TryGetCoder(withNullable) is { } coder)
            {// Coder
                using (var c = ssb.ScopeBrace(string.Empty))
                {
                    assignment.Start(true);
                    coder.CodeReconstruct(ssb, info);
                    assignment.End();
                }
            }
            else
            {// Default constructor
                assignment.RefValue(true, true);
                // sb.AppendLine($"{ssb.FullObject} ??= {withNullable.Object.NewInstanceCode()}!;");
                assignment.End();
            }

            this.GenerateJournal_SetParent(ssb, x, destObject, ref count);
        }
    }

    internal void GenerateReconstructCore2(ScopingStringBuilder ssb, GenerationContext info, TinyhandObject x, int reconstructIndex)
    {// Called by Trie
        var withNullable = x?.TypeObjectWithNullable;
        if (x == null || withNullable == null)
        {// no object
            return;
        }

        ValueAssignment assignment = new(ssb, info, this, x);
        using (var c = ssb.ScopeObject(x.SimpleNameOrAddedProperty))
        {
            var originalName = ssb.FullObject;
            if (x.IsDefaultable)
            {
                using (var conditionDeserialized = ssb.ScopeBrace($"if ({this.GetMissingStringKeyCondition(reconstructIndex)})"))
                {
                    assignment.Start(false);
                    ssb.AppendLine($"{ssb.FullObject} = {VisceralDefaultValue.DefaultValueToString(x.DefaultValue)};");
                    assignment.End();
                }

                return;
            }

            // var nullCheckCode = withNullable.Object.Kind.IsReferenceType() && !x.ObjectFlags.HasFlag(TinyhandObjectFlags.ReuseInstanceTarget) ? $"if (!deserializedFlag[{reconstructIndex}] && {ssb.FullObject} == null)" : $"if (!deserializedFlag[{reconstructIndex}])";
            var nullCheckCode = $"if ({this.GetMissingStringKeyCondition(reconstructIndex)})";

            using (var conditionDeserialized = ssb.ScopeBrace(nullCheckCode))
            {
                assignment.Start(true);

                if (x.NullableAnnotationIfReferenceType == Arc.Visceral.NullableAnnotation.NotAnnotated || x.ReconstructMode == ReconstructMode.Always)
                {// T
                    if (withNullable.Object.ObjectAttribute != null &&
                        withNullable.Object.ObjectAttribute.UseResolver == false)
                    {// TinyhandObject. For the purpose of default value and instance reuse.
                        withNullable.Object.GenerateFormatter_Reconstruct2(ssb, info, originalName, x.DefaultValue, x.ObjectFlags.HasFlag(TinyhandObjectFlags.ReuseInstanceTarget));
                    }
                    else if (this.Body.CoderResolver.TryGetCoder(withNullable) is { } coder)
                    {
                        coder.CodeReconstruct(ssb, info);
                    }
                    else
                    {// Default constructor
                        ssb.AppendLine($"{ssb.FullObject} = {withNullable.Object.NewInstanceCode()};");
                    }
                }

                assignment.End();
            }
        }
    }

    internal void GenerateCloneCore(ScopingStringBuilder ssb, GenerationContext info, TinyhandObject x, string sourceObject)
    {// Called by GenerateClone()
        var withNullable = x?.TypeObjectWithNullable;
        if (x == null || withNullable == null)
        {// no object
            return;
        }

        var assignment = new ValueAssignment(ssb, info, this, x);
        using (var d = this.ScopeSimpleMember(ssb, x))
        {
            if (withNullable.Object.ObjectAttribute != null &&
                withNullable.Object.ObjectAttribute.UseResolver == false)
            {// TinyhandObject.
                assignment.Start(false, true);
                ssb.AppendLine($"{ssb.FullObject} = TinyhandSerializer.CloneObject({sourceObject}, options)!;");
                assignment.End(true);
            }
            else if (this.Body.CoderResolver.TryGetCoder(withNullable) is { } coder)
            {// Coder
                assignment.Start(true, true);
                coder.CodeClone(ssb, info, sourceObject);
                assignment.End(true);
            }
            else
            {// Other
                if (x.TypeObject != null && (x.TypeObject.Kind != VisceralObjectKind.Error && x.TypeObject.Kind != VisceralObjectKind.TypeParameter))
                {
                    this.Body.ReportDiagnostic(TinyhandBody.Warning_NoCoder, x.Location, withNullable.FullName);
                }

                assignment.Start(false, true);
                ssb.AppendLine($"{ssb.FullObject} = options.Resolver.GetFormatter<{withNullable.FullNameWithNullable}>().Clone({sourceObject}, options)!;");
                assignment.End(true);
            }
        }
    }

    internal void GenerateDeserializerIntKey(ScopingStringBuilder ssb, GenerationContext info)
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
            if (!string.IsNullOrEmpty(this.ObjectAttribute?.LockMemberName))
            {// LockMemberName
                this.GenerateDeserialize_LockEnter(ssb, info);
            }

            this.Generate_CallbackMethod(ssb, CallbackKind.OnDeserializing); // CallbackMethodCode
            foreach (var x in this.IntKey_Array)
            {
                this.GenerateDeserializeCore(ssb, info, x);
            }

            ssb.AppendLine("while (numberOfData-- > 0) reader.Skip();");

            this.GenerateReconstructRemaining(ssb, info);

            this.Generate_CallbackMethod(ssb, CallbackKind.OnDeserialized); // CallbackMethodCode
        }

        using (var finallyScope = ssb.ScopeBrace("finally"))
        {
            if (!string.IsNullOrEmpty(this.ObjectAttribute?.LockMemberName))
            {// LockMemberName
                this.GenerateDeserialize_LockExit(ssb, info);
            }

            ssb.AppendLine("reader.Depth--;");
        }
    }

    internal void GenerateDeserializerStringKey(ScopingStringBuilder ssb, GenerationContext info)
    {
        if (this.StringTrie is null)
        {
            return;
        }

        if (this.Kind.IsValueType())
        {// Value type
            ssb.AppendLine("if (reader.TryReadNil()) throw new TinyhandException(\"Data is Nil, struct can not be null.\");");
        }

        if (this.StringTrieReconstructNumber > 0)
        {
            // ssb.AppendLine($"var deserializedFlag = new bool[{this.StringTrieReconstructNumber}];");
            if (this.StringTrieReconstructNumber <= 64)
            {
                ssb.AppendLine("ulong deserializedFlag = 0;");
            }
            else
            {
                ssb.AppendLine($"Span<ulong> deserializedFlag = stackalloc ulong[{(this.StringTrieReconstructNumber + 63) / 64}];");
                ssb.AppendLine("deserializedFlag.Clear();");
            }
        }

        ssb.AppendLine("var numberOfData = reader.ReadMapHeaderOrEmptyArray();");

        using (var security = ssb.ScopeSecurityDepth())
        {
            if (!string.IsNullOrEmpty(this.ObjectAttribute?.LockMemberName))
            {// LockMemberName
                this.GenerateDeserialize_LockEnter(ssb, info);
            }

            this.Generate_CallbackMethod(ssb, CallbackKind.OnDeserializing); // CallbackMethodCode
            using (var loop = ssb.ScopeBrace("while (numberOfData-- > 0)"))
            {
                var context = new VisceralTrieString<TinyhandObject>.VisceralTrieContext(
                    ssb,
                    (ctx, obj, node) =>
                    {
                        if (node.SubIndex >= 0)
                        {
                            ssb.AppendLine($"{this.GetStringKeyFlag(node.SubIndex)} |= {1UL << (node.SubIndex % 64)}UL;");
                        }

                        obj?.GenerateDeserializeCore2(ssb, info, node.Member);
                    });

                this.StringTrie.Generate(context);
            }

            // Reconstruct
            if (this.StringTrieReconstructNumber > 0)
            {
                ssb.AppendLine();
                foreach (var x in this.StringTrie.NodeList)
                {
                    if (x.SubIndex < 0 || x.Member == null)
                    {
                        continue;
                    }

                    this.StringTrie.BaseObject?.GenerateReconstructCore2(ssb, info, x.Member, x.SubIndex);
                }
            }

            this.GenerateReconstructRemaining(ssb, info);

            this.Generate_CallbackMethod(ssb, CallbackKind.OnDeserialized); // CallbackMethodCode
        }

        using (var finallyScope = ssb.ScopeBrace("finally"))
        {
            if (!string.IsNullOrEmpty(this.ObjectAttribute?.LockMemberName))
            {// LockMemberName
                this.GenerateDeserialize_LockExit(ssb, info);
            }

            ssb.AppendLine("reader.Depth--;");
        }
    }

    private string GetStringKeyFlag(int index)
        => this.StringTrieReconstructNumber <= 64 ? "deserializedFlag" : $"deserializedFlag[{index / 64}]";

    private string GetMissingStringKeyCondition(int index)
        => $"({this.GetStringKeyFlag(index)} & {1UL << (index % 64)}UL) == 0";

    internal void GenerateSerializeCore(ScopingStringBuilder ssb, GenerationContext info, TinyhandObject? x, bool skipDefaultValue, ConvertToStringMode convertToStringMode)
    {
        var withNullable = x?.TypeObjectWithNullable;
        if (x == null || withNullable == null)
        {// no object
            ssb.AppendLine("writer.WriteNil();");
            return;
        }

        ScopingStringBuilder.IScope? v1 = null;
        ScopingStringBuilder.IScope v2;
        if (x.RefFieldDelegate is not null)
        {// Ref field or getter delegate
            v1 = ssb.ScopeBrace(string.Empty);
            var prefix = info.GeneratingStaticMethod ? (this.RegionalName + ".") : string.Empty;
            ssb.AppendLine($"var vd = {prefix}{x.RefFieldDelegate}({ssb.FullObject});");
            v2 = ssb.ScopeFullObject("vd");
        }
        else
        {
            v2 = this.ScopeMember(ssb, x); // Hidden members
        }

        ScopingStringBuilder.IScope? skipDefaultValueScope = null;
        if (skipDefaultValue)
        {
            if (x.DefaultValue is not null)
            {
                // var equalExpression = x.DefaultValue == "[]" ? ".SequenceEqual([])" : $" == {x.DefaultValue}";
                string equalExpression;
                if (x.DefaultValue == "[]")
                {
                    if (withNullable.Nullable == Arc.Visceral.NullableAnnotation.Annotated)
                    {
                        equalExpression = "?.Length == 0";
                    }
                    else
                    {
                        equalExpression = ".Length == 0";
                    }
                }
                else
                {
                    equalExpression = $" == {x.DefaultValue}";
                }

                using (var scopeDefault = ssb.ScopeBrace($"if ({ssb.FullObject}{equalExpression})"))
                {
                    ssb.AppendLine($"if (!options.IsSignatureMode) writer.WriteNil();");
                }

                skipDefaultValueScope = ssb.ScopeBrace("else");
            }
            else if (withNullable.Object.DefaultInterface is { } defaultInterface)
            {
                if (withNullable.Object.MethodCondition_CanSkipSerialization == MethodImplementationKind.Declared)
                {
                    using (var scopeDefault = ssb.ScopeBrace($"if ({ssb.FullObject}{withNullable.Object.QuestionMarkIfReferenceType}.{TinyhandBody.CanSkipSerializationMethod}() == true)"))
                    {
                        ssb.AppendLine($"if (!options.IsSignatureMode) writer.WriteNil();");
                    }

                    skipDefaultValueScope = ssb.ScopeBrace("else");
                }
                else if (withNullable.Object.MethodCondition_CanSkipSerialization == MethodImplementationKind.ExplicitlyDeclared)
                {
                    using (var scopeDefault = ssb.ScopeBrace($"if ((({defaultInterface.FullName}){ssb.FullObject}{withNullable.Object.QuestionMarkIfReferenceType}).{TinyhandBody.CanSkipSerializationMethod}() == true)"))
                    {
                        ssb.AppendLine($"if (!options.IsSignatureMode) writer.WriteNil();");
                    }

                    skipDefaultValueScope = ssb.ScopeBrace("else");
                }
            }
        }

        if (withNullable.Object.ObjectFlags.HasFlag(TinyhandObjectFlags.HasIStringConvertible))
        {
            if (convertToStringMode == ConvertToStringMode.ConvertToString)
            {
                ssb.AppendLine($"writer.WriteStringConvertible({ssb.FullObject});");
            }
            else if (convertToStringMode == ConvertToStringMode.NoConvertToString)
            {
                ssb.AppendLine($"TinyhandSerializer.SerializeObject(ref writer, {ssb.FullObject}, options);");
            }
            else
            {
                ssb.AppendLine($"if (options.HasConvertToStringFlag) writer.WriteStringConvertible({ssb.FullObject});");
                ssb.AppendLine($"else TinyhandSerializer.SerializeObject(ref writer, {ssb.FullObject}, options);");
            }
        }
        else
        {
            var coder = this.Body.CoderResolver.TryGetCoder(withNullable);
            if (coder != null)
            {// Coder
                coder.CodeSerialize(ssb, info);
            }
            else if (withNullable.Object.ObjectAttribute?.UseResolver == false &&
                (withNullable.Object.ObjectAttribute != null ||
                withNullable.Object.HasITinyhandSerializeConstraint()))
            {// TinyhandObject or Type parameter with ITinyhandSerializable constraint.
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
        }

        skipDefaultValueScope?.Dispose();
        v2.Dispose();
        v1?.Dispose();
    }

    /*internal void GenerateMaxLength(ScopingStringBuilder ssb, GenerationContext info, TinyhandObject typeObject, MaxLengthAttributeData attribute)
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
    }*/

    internal void GenerateSerializerIntKey(ScopingStringBuilder ssb, GenerationContext info, ConvertToStringMode convertToStringMode)
    {
        if (this.IntKey_Array == null)
        {
            return;
        }

        if (this.IntKey_Array.Length == this.IntKey_IncludedCount)
        {
            ssb.AppendLine($"if (!options.IsSignatureMode) writer.WriteArrayHeader({this.IntKey_Array.Length});");
            if (this.ObjectAttribute?.AddSignatureId == true)
            {
                ssb.AppendLine($"else writer.Write(0x{((uint)FarmHash.Hash64(this.FullName)).ToString("x")}u);");
            }
        }
        else
        {// Excluded members are omitted in exclude mode (the deserializer does not read them); every other mode except signature writes all keys.
            ssb.AppendLine($"if (options.IsExcludeMode) writer.WriteArrayHeader({this.IntKey_IncludedCount});");
            ssb.AppendLine($"else if (!options.IsSignatureMode) writer.WriteArrayHeader({this.IntKey_Array.Length});");
        }

        var skipDefaultValue = this.SkipDefaultValues;
        foreach (var x in this.IntKey_Array)
        {
            this.GenerateSerializerKey(ssb, info, x, skipDefaultValue, convertToStringMode, true);
        }
    }

    internal void GenerateSerializerKey(ScopingStringBuilder ssb, GenerationContext info, TinyhandObject? x, bool skipDefaultValue, ConvertToStringMode convertToStringMode, bool intKey)
    {
        var exclude = x?.KeyAttribute?.Exclude == true ? true : false;
        bool decrease = false;
        if (x?.KeyAttribute?.Level is not int level)
        {
            level = KeyAttributeData.DefaultLevel;
        }

        ScopingStringBuilder.IScope? scopeIf = null;
        if (level == KeyAttributeData.DefaultLevel)
        {// No level
            if (exclude)
            {// Exclude == true
                scopeIf = ssb.ScopeBrace($"if (!options.IsExcludeMode)");
            }
            else
            {// Exclude == false
            }
        }
        else
        {// Level
            decrease = x?.TypeObject?.IsPrimitive == false && level != 0;
            if (exclude)
            {// Exclude == true
                scopeIf = ssb.ScopeBrace($"if (options.IsDefaultMode || (options.IsSignatureMode && writer.Level >= {level}))");
            }
            else
            {// Exclude == false
                scopeIf = ssb.ScopeBrace($"if (writer.Level >= {level})");
            }
        }

        if (decrease)
        {
            ssb.AppendLine($"writer.Level -= {level};");
        }

        this.GenerateSerializeCore(ssb, info, x, skipDefaultValue, convertToStringMode);

        if (decrease)
        {
            ssb.AppendLine($"writer.Level += {level};");
        }

        if (scopeIf is not null)
        {
            scopeIf.Dispose();
            if (exclude && intKey)
            {// An excluded int-key member is omitted in exclude mode, and the others write nil to keep the positions.
                ssb.AppendLine($"else if (!options.IsSignatureMode && !options.IsExcludeMode) writer.WriteNil();");
            }
            else
            {// Nil keeps the position of an int-key member, or completes the key-value pair of a string-key member.
                ssb.AppendLine($"else if (!options.IsSignatureMode) writer.WriteNil();");
            }
        }
    }

    internal void GenerateSerializerStringKey(ScopingStringBuilder ssb, GenerationContext info, ConvertToStringMode convertToStringMode)
    {
        if (this.StringTrie == null)
        {
            return;
        }

        var cf = this.OriginalDefinition; // For generics. get Class<T>
        if (cf == null)
        {
            cf = this;
        }

        ssb.AppendLine($"writer.WriteMapHeader({this.StringTrie.NodeList.Count});");
        var skipDefaultValue = this.SkipDefaultValues;
        foreach (var x in this.StringTrie.NodeList)
        {
            // Include the MessagePack header in the constant span to write each key in one operation.
            var utf8 = Encoding.UTF8.GetBytes(x.Name!);
            var header = utf8.Length < 32 ? new byte[] { (byte)(0xa0 | utf8.Length) }
                : utf8.Length <= byte.MaxValue ? new byte[] { 0xd9, (byte)utf8.Length }
                : utf8.Length <= ushort.MaxValue ? new byte[] { 0xda, (byte)(utf8.Length >> 8), (byte)utf8.Length }
                : new byte[] { 0xdb, (byte)(utf8.Length >> 24), (byte)(utf8.Length >> 16), (byte)(utf8.Length >> 8), (byte)utf8.Length };
            ssb.AppendLine($"writer.WriteRaw([{string.Join(", ", header.Concat(utf8))}]);");
            if (x.Member is not null)
            {
                this.GenerateSerializerKey(ssb, info, x.Member, skipDefaultValue, convertToStringMode, false);
            }
        }
    }

    internal void GenerateStringKeyFields(ScopingStringBuilder ssb, GenerationContext info)
    {
        /*if (this.StringTrie == null || this.StringTrie.NodeList.Count == 0)
        {
            return;
        }

        ssb.AppendLine();
        foreach (var x in this.StringTrie.NodeList)
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
        }*/
    }

    internal bool HasITinyhandSerializeConstraint()
    {
        if (this.symbol is ITypeParameterSymbol tps)
        {
            return tps.ConstraintTypes.Any(x => x.Name == "ITinyhandSerializable");
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

    private bool IsPartialProperty
        => this.symbol is IPropertySymbol ps && ps.IsPartialDefinition;

    private string SimpleNameOrField
        => this.IsPartialProperty ? "field" : $"this.{this.SimpleName}";

    private string AddedPropertyOrPartialProperty
        => this.IsPartialProperty ? this.SimpleName : this.KeyAttribute?.PropertyName ?? string.Empty;

    private bool IsStructuralTarget
    {
        get
        {
            if (this.ObjectAttribute?.Structural == true)
            {
                return true;
            }
            else if (this.ObjectFlags.HasFlag(TinyhandObjectFlags.IStructuralObjectImplemented) == true)
            {
                return true;
            }
            else if (this.Kind == VisceralObjectKind.Error)
            {
                return true;
            }
            else if (this.ObjectFlags.HasFlag(TinyhandObjectFlags.ExternalObject))
            {
                return true;
            }

            return false;
        }
    }

    private bool MethodIncludesParameterWithRef()
    {
        if (this.symbol is IMethodSymbol ms)
        {
            foreach (var x in ms.Parameters)
            {
                if (x.RefKind != RefKind.None)
                {
                    return true;
                }
            }
        }

        return false;
    }
}
