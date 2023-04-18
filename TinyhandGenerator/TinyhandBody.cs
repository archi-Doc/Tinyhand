// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Arc.Visceral;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

#pragma warning disable RS2008
#pragma warning disable SA1310 // Field names should not contain underscore
#pragma warning disable SA1401 // Fields should be private
#pragma warning disable SA1117 // Parameters should be on same line or separate lines

namespace Tinyhand.Generator;

public class TinyhandBody : VisceralBody<TinyhandObject>
{
    public const string GeneratorName = "TinyhandGenerator";
    public static readonly int MaxIntegerKey = 5_000;
    public static readonly int MaxStringKeySizeInBytes = 512;
    public static readonly string SetMembersMethod = "SetMembers";
    public static readonly string Namespace = "Tinyhand";
    public static readonly string ITinyhandDefault = "ITinyhandDefault";
    public static readonly string SetDefaultValueMethod = "SetDefaultValue";
    public static readonly string CanSkipSerializationMethod = "CanSkipSerialization";
    public static readonly string LockObject = "__lockObject__";
    public static readonly string LockTaken = "__lockTaken__";
    public static readonly string ILockable = "Arc.Threading.ILockable";
    public static readonly string LockStruct = "Arc.Threading.LockStruct";

    public static readonly DiagnosticDescriptor Error_NotPartial = new DiagnosticDescriptor(
        id: "TG001", title: "Not a partial class/struct", messageFormat: "'{0}' must be a partial class/struct",
        category: GeneratorName, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_NotPartialParent = new DiagnosticDescriptor(
        id: "TG002", title: "Not a partial class/struct", messageFormat: "Parent object '{0}' is not a partial class/struct",
        category: GeneratorName, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_NoDefaultConstructor = new DiagnosticDescriptor(
        id: "TG003", title: "No default constructor", messageFormat: "TinyhandObject '{0}' must have default constructor",
        category: GeneratorName, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_NotSerializableMember = new DiagnosticDescriptor(
        id: "TG004", title: "Not serializable", messageFormat: "Member '{0}' is not serializable",
        category: GeneratorName, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_ReadonlyMember = new DiagnosticDescriptor(
        id: "TG005", title: "Not serializable", messageFormat: "Member '{0}' is readonly",
        category: GeneratorName, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_NotPublicMember = new DiagnosticDescriptor(
        id: "TG006", title: "Not public", messageFormat: "Member '{0}' is not public, consider setting IncludePrivateMembers property true",
        category: GeneratorName, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_KeyAttributeRequired = new DiagnosticDescriptor(
        id: "TG007", title: "Key attribute required", messageFormat: "Member of TinyhandObject should have KeyAttribute",
        category: GeneratorName, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_IntStringKeyConflict = new DiagnosticDescriptor(
        id: "TG008", title: "Key attribute conflict", messageFormat: "Integer key and String key are exclusive",
        category: GeneratorName, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Warning_StringKeySizeLimit = new DiagnosticDescriptor(
        id: "TG009", title: "String key limit", messageFormat: "The size of the string key exceeds the limit {0}",
        category: GeneratorName, DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_StringKeyConflict = new DiagnosticDescriptor(
        id: "TG010", title: "String keys conflict", messageFormat: "String keys with the same name were detected",
        category: GeneratorName, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_StringKeyNull = new DiagnosticDescriptor(
        id: "TG011", title: "String key null", messageFormat: "String key cannot contain null character",
        category: GeneratorName, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_IntKeyConflicted = new DiagnosticDescriptor(
        id: "TG012", title: "Int Key conflict", messageFormat: "Integer keys with the same number were detected",
        category: GeneratorName, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Warning_IntKeyUnused = new DiagnosticDescriptor(
        id: "TG013", title: "Unused Int Keys", messageFormat: "For better performance, try to reduce the number of unused integer keys",
        category: GeneratorName, DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_IntKeyOutOfRange = new DiagnosticDescriptor(
        id: "TG014", title: "Int key range", messageFormat: $"Integer key should be set to a value between 0 to {MaxIntegerKey}",
        category: GeneratorName, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Warning_KeyAndIgnoreAttribute = new DiagnosticDescriptor(
        id: "TG015", title: "Key attribute and IgnoreMember attribute", messageFormat: "Key attribute and IgnoreMember attribute are exclusive",
        category: GeneratorName, DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_ObjectAttributeRequired = new DiagnosticDescriptor(
        id: "TG016", title: "Key attribute required", messageFormat: "Member to be serialized must have TinyhandObjectAttribute '{0}'",
        category: GeneratorName, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_AttributePropertyError = new DiagnosticDescriptor(
        id: "TG017", title: "Attribute property type error", messageFormat: "The specified argument does not match the property type",
        category: GeneratorName, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_KeyAttributeError = new DiagnosticDescriptor(
        id: "TG018", title: "Key attribute error", messageFormat: "KeyAttribute requires a valid int key or string key",
        category: GeneratorName, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Warning_Circular = new DiagnosticDescriptor(
        id: "TG019", title: "Circular dependency", messageFormat: "Circular dependency detected '{0}'",
        category: GeneratorName, DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Warning_NoDefaultConstructor = new DiagnosticDescriptor(
        id: "TG020", title: "No Default Constructor", messageFormat: "Reconstruct target '{0}' must have a default constructor",
        category: GeneratorName, DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Warning_NotReferenceType = new DiagnosticDescriptor(
        id: "TG021", title: "Not reference type", messageFormat: "Reconstruct target object '{0}' should be a reference type",
        category: GeneratorName, DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Warning_DefaultValueType = new DiagnosticDescriptor(
        id: "TG022", title: "Default Value Type", messageFormat: "The type of default value does not match the target type",
        category: GeneratorName, DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Warning_DefaultValueConstructor = new DiagnosticDescriptor(
        id: "TG023", title: "Default Value Constructor", messageFormat: "Constructor with an argument which type is same as the default value is required",
        category: GeneratorName, DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Warning_NoCoder = new DiagnosticDescriptor(
        id: "TG024", title: "No Coder", messageFormat: "'{0}' has passed the check, but the proper coder is not found",
        category: GeneratorName, DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Warning_KeyIgnored = new DiagnosticDescriptor(
        id: "TG025", title: "Key ignored", messageFormat: "KeyAttribute is ignored since ITinyhandSerialize is implemented",
        category: GeneratorName, DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Warning_DefaultInterface = new DiagnosticDescriptor(
        id: "TG026", title: "ITinyhandDefault", messageFormat: "To receive the default value, an implementation of 'ITinyhandDefault' interface is required",
        category: GeneratorName, DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Warning_TinyhandObjectRequiredToReuse = new DiagnosticDescriptor(
        id: "TG027", title: "Reuse Instance", messageFormat: "The type of the member to be reused must have a TinyhandObject attribute",
        category: GeneratorName, DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_NullSubtype = new DiagnosticDescriptor(
        id: "TG028", title: "Null Subtype", messageFormat: "Could not get the subtype from the specified string or type",
        category: GeneratorName, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_UnionType = new DiagnosticDescriptor(
        id: "TG029", title: "Union type", messageFormat: "Union can only be interface or abstract class",
        category: GeneratorName, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_SubtypeConflicted = new DiagnosticDescriptor(
        id: "TG030", title: "Subtype conflict", messageFormat: "Same subtype has found",
        category: GeneratorName, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_UnionTargetError = new DiagnosticDescriptor(
        id: "TG031", title: "Union target error", messageFormat: "Union target type must have TinyhandObject attribute",
        category: GeneratorName, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_UnionNotDerived = new DiagnosticDescriptor(
        id: "TG032", title: "Union target error", messageFormat: "Union target type '{0}' is not derived from '{1}'",
        category: GeneratorName, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_UnionNotImplementing = new DiagnosticDescriptor(
        id: "TG033", title: "Union target error", messageFormat: "Union target type '{0}' does not implement '{1}'",
        category: GeneratorName, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_UnionSelf = new DiagnosticDescriptor(
        id: "TG034", title: "Union target error", messageFormat: "The type '{0}' cannot be specified as a union target",
        category: GeneratorName, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Warning_InvalidIdentifier = new DiagnosticDescriptor(
        id: "TG035", title: "Invalid identifier", messageFormat: "'{0}' is not a valid identifier, it's been replaced by '{1}'",
        category: GeneratorName, DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Warning_KeyAsNameExclusive = new DiagnosticDescriptor(
        id: "TG036", title: "KeyAsName exclusive", messageFormat: "KeyAttribute and KeyAsNameAttribute are exclusive",
        category: GeneratorName, DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_ImplicitExplicitKey = new DiagnosticDescriptor(
        id: "TG037", title: "Implicit Explicit conflict", messageFormat: "ImplicitKeyAsName and ExplicitKeyOnly are exclusive",
        category: GeneratorName, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_UnionToError = new DiagnosticDescriptor(
        id: "TG038", title: "UnionTo error", messageFormat: "The base type of TinyhandUnionToAttribute must be annotated with TinyhandObjectAttribute and must be an abstract class or interface",
        category: GeneratorName, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_IntKeyReserved = new DiagnosticDescriptor(
        id: "TG039", title: "Int Key reserved", messageFormat: "Integer keys from 0 to {0} are reserved in base class",
        category: GeneratorName, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Warning_InvalidKeyMarker = new DiagnosticDescriptor(
        id: "TG040", title: "Key marker", messageFormat: "'Key marker is only valid for integer keys",
        category: GeneratorName, DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_GenericType = new DiagnosticDescriptor(
        id: "TG041", title: "Generic type", messageFormat: "Generic type is not supported",
        category: GeneratorName, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_NoTinyhandFile = new DiagnosticDescriptor(
        id: "TG042", title: "No Tinyhand", messageFormat: "Could not load tinyhand file '{0}'",
        category: GeneratorName, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_ParseTinyhandFile = new DiagnosticDescriptor(
        id: "TG043", title: "Parse Tinyhand", messageFormat: "Could not parse tinyhand file '{0}'",
        category: GeneratorName, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Warning_InvalidIdentifier2 = new DiagnosticDescriptor(
        id: "TG044", title: "Invalid identifier", messageFormat: "'{0}' is not valid identifier ({1})",
        category: GeneratorName, DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Warning_MaxLengthAttribute = new DiagnosticDescriptor(
        id: "TG045", title: "Max length", messageFormat: "MaxLengthAttribute is valid only for string, array, List<T>",
        category: GeneratorName, DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_AddProperty = new DiagnosticDescriptor(
        id: "TG046", title: "Add property", messageFormat: "You can only add properties to fields",
        category: GeneratorName, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_DuplicateKeyword = new DiagnosticDescriptor(
        id: "TG047", title: "Duplicate keyword", messageFormat: "The type '{0}' already contains a definition for '{1}'",
        category: GeneratorName, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Warning_MaxLengthAttribute2 = new DiagnosticDescriptor(
        id: "TG048", title: "Max length2", messageFormat: "To enable MaxLengthAttribute, AddProperty must be specified",
        category: GeneratorName, DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_LockObject = new DiagnosticDescriptor(
        id: "TG049", title: "LockObject", messageFormat: "Member specified in LockObject is not found",
        category: GeneratorName, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_LockObject2 = new DiagnosticDescriptor(
        id: "TG050", title: "LockObject2", messageFormat: "Member specified in LockObject must be a reference type",
        category: GeneratorName, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_LockObject3 = new DiagnosticDescriptor(
        id: "TG051", title: "LockObject3", messageFormat: "Member specified in LockObject is not accessible",
        category: GeneratorName, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public TinyhandBody(GeneratorExecutionContext context, IAssemblySymbol assemblySymbol)
        : base(context)
    {
        this.AssemblySymbol = assemblySymbol;
    }

    public TinyhandBody(SourceProductionContext context, IAssemblySymbol assemblySymbol)
        : base(context)
    {
        this.AssemblySymbol = assemblySymbol;
    }

    internal Dictionary<string, List<TinyhandObject>> Namespaces = new();

    internal IAssemblySymbol AssemblySymbol;

    // internal List<UnionToItem> UnionToList = new();

    public void Generate(IGeneratorInformation generator, CancellationToken cancellationToken)
    {
        ScopingStringBuilder ssb = new();
        GeneratorInformation info = new();
        List<TinyhandObject> rootObjects = new();

        // Namespace - Primary TinyhandObjects
        foreach (var x in this.Namespaces)
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.GenerateHeader(ssb);
            ssb.AppendNamespace(x.Key);

            rootObjects.AddRange(x.Value); // For loader generation

            var firstFlag = true;
            foreach (var y in x.Value)
            {
                if (!firstFlag)
                {
                    ssb.AppendLine();
                }

                firstFlag = false;

                y.Generate(ssb, info); // Primary TinyhandObject
            }

            var result = ssb.Finalize();

            if (generator.GenerateToFile && generator.TargetFolder != null && Directory.Exists(generator.TargetFolder))
            {
                this.StringToFile(result, Path.Combine(generator.TargetFolder, $"gen.Tinyhand.{x.Key}.cs"));
            }
            else
            {
                var hintName = $"gen.Tinyhand.{x.Key}";
                var sourceText = SourceText.From(result, Encoding.UTF8);
                this.Context?.AddSource(hintName, sourceText);
                this.Context2?.AddSource(hintName, sourceText);
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
        this.GenerateLoader(generator, info, rootObjects, this.Namespaces);
        this.FlushDiagnostic();
    }

    public void Prepare()
    {
        // Configure objects.
        var array = this.FullNameToObject.Where(x => x.Value.Generics_Kind != VisceralGenericsKind.ClosedGeneric).ToArray();
        foreach (var x in array)
        {
            x.Value.Configure();
        }

        /*foreach (var x in this.UnionToList)
        {
            TinyhandUnion.ProcessUnionTo(x);
        }*/

        this.FlushDiagnostic();
        if (this.Abort)
        {
            return;
        }

        array = this.FullNameToObject.Where(x => x.Value.ObjectAttribute != null && x.Value.IsSameAssembly(this.AssemblySymbol)).ToArray();
        foreach (var x in array)
        {
            x.Value.ConfigureRelation();
        }

        // Check
        foreach (var x in array)
        {
            x.Value.Check();
        }

        this.FlushDiagnostic();
        if (this.Abort)
        {
            return;
        }
    }

    private void GenerateHeader(ScopingStringBuilder ssb)
    {
        ssb.AddHeader("// <auto-generated/>");
        ssb.AddUsing("System");
        ssb.AddUsing("System.Collections.Generic");
        ssb.AddUsing("System.Diagnostics.CodeAnalysis");
        ssb.AddUsing("System.Linq.Expressions");
        ssb.AddUsing("System.Runtime.CompilerServices");
        ssb.AddUsing("FastExpressionCompiler");
        ssb.AddUsing("Tinyhand");
        ssb.AddUsing("Tinyhand.IO");
        ssb.AddUsing("Tinyhand.Resolvers");
        ssb.AppendLine("#nullable enable", false);
        ssb.AppendLine("#pragma warning disable CS0219", false);
        ssb.AppendLine("#pragma warning disable CS0108", false); // Hides inherited member
        ssb.AppendLine("#pragma warning disable CS0162", false); // Unreachable code detected
        ssb.AppendLine("#pragma warning disable CS0168", false);
        ssb.AppendLine("#pragma warning disable CS1591", false);
        ssb.AppendLine("#pragma warning disable CS8618", false);
        ssb.AppendLine("#pragma warning disable CS8714", false); // Ignore Generic type constraints
        ssb.AppendLine();
    }

    private void GenerateLoader(IGeneratorInformation generator, GeneratorInformation info, List<TinyhandObject> rootObjects, Dictionary<string, List<TinyhandObject>> namespaces)
    {
        var ssb = new ScopingStringBuilder();
        this.GenerateHeader(ssb);

        using (var scopeFormatter = ssb.ScopeNamespace("Tinyhand.Formatters"))
        {
            using (var methods = ssb.ScopeBrace("static class Generated"))
            {
                info.FinalizeBlock(ssb);

                if (!info.FlatLoader)
                {
                    TinyhandObject.GenerateLoader(ssb, info, rootObjects);
                }
                else
                {// FlatLoader
                    using (var m = ssb.ScopeBrace("internal static void __gen__th()"))
                    {
                        foreach (var x in namespaces.Values)
                        {
                            foreach (var y in x)
                            {
                                y.GenerateFlatLoader(ssb, info);
                            }
                        }
                    }
                }
            }
        }

        this.GenerateInitializer(generator, ssb, info);

        var result = ssb.Finalize();

        if (generator.GenerateToFile && generator.TargetFolder != null && Directory.Exists(generator.TargetFolder))
        {
            this.StringToFile(result, Path.Combine(generator.TargetFolder, "gen.TinyhandGenerated.cs"));
        }
        else
        {
            var hintName = "gen.TinyhandLoader";
            var sourceText = SourceText.From(result, Encoding.UTF8);
            this.Context?.AddSource(hintName, sourceText);
            this.Context2?.AddSource(hintName, sourceText);
        }
    }

    private void GenerateInitializer(IGeneratorInformation generator, ScopingStringBuilder ssb, GeneratorInformation info)
    {
        var ns = "Tinyhand"; // Namespace
        var assemblyId = string.Empty; // Assembly ID
        if (!string.IsNullOrEmpty(generator.CustomNamespace))
        {// Custom namespace.
            ns = generator.CustomNamespace;
        }
        else
        {// Other (Apps)
            // assemblyId = "_" + generator.AssemblyId.ToString("x");
            if (!string.IsNullOrEmpty(generator.AssemblyName))
            {
                assemblyId = VisceralHelper.AssemblyNameToIdentifier("_" + generator.AssemblyName);
            }
        }

        info.ModuleInitializerClass.Add("Tinyhand.Formatters.Generated");

        ssb.AppendLine();
        using (var scopeTinyhand = ssb.ScopeNamespace(ns!))
        using (var scopeClass = ssb.ScopeBrace("public static class TinyhandModule" + assemblyId))
        {
            ssb.AppendLine("private static bool Initialized;");
            ssb.AppendLine();
            ssb.AppendLine("[ModuleInitializer]");

            using (var scopeMethod = ssb.ScopeBrace("public static void Initialize()"))
            {
                ssb.AppendLine($"if (Initialized) return;");
                ssb.AppendLine($"Initialized = true;");
                ssb.AppendLine();

                foreach (var x in info.ModuleInitializerClass)
                {
                    ssb.Append(x, true);
                    ssb.AppendLine(".__gen__th();", false);
                }
            }

            // Methods for debugging
            // ssb.AppendLine($"public static string[] GetPreprocessor() => new string[] {{\"{string.Join("\", \"", generator.Context.ParseOptions.PreprocessorSymbolNames.ToArray())}\"}};");
        }
    }
}
