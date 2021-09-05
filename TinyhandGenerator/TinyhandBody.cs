// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Arc.Visceral;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

#pragma warning disable RS2008
#pragma warning disable SA1306 // Field names should begin with lower-case letter
#pragma warning disable SA1310 // Field names should not contain underscore
#pragma warning disable SA1401 // Fields should be private
#pragma warning disable SA1117 // Parameters should be on same line or separate lines

namespace Tinyhand.Generator
{
    public class TinyhandBody : VisceralBody<TinyhandObject>
    {
        public static readonly int MaxIntegerKey = 5_000;
        public static readonly int MaxStringKeySizeInBytes = 512;
        public static readonly string SetDefaultMethod = "SetDefault";
        public static readonly string SetMembersMethod = "SetMembers";

        public static readonly DiagnosticDescriptor Error_NotPartial = new DiagnosticDescriptor(
            id: "TG001", title: "Not a partial class/struct", messageFormat: "TinyhandObject '{0}' is not a partial class/struct",
            category: "TinyhandGenerator", DiagnosticSeverity.Error, isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor Error_NotPartialParent = new DiagnosticDescriptor(
            id: "TG002", title: "Not a partial class/struct", messageFormat: "Parent object '{0}' is not a partial class/struct",
            category: "TinyhandGenerator", DiagnosticSeverity.Error, isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor Error_NoDefaultConstructor = new DiagnosticDescriptor(
            id: "TG003", title: "No default constructor", messageFormat: "TinyhandObject '{0}' must have default constructor",
            category: "TinyhandGenerator", DiagnosticSeverity.Error, isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor Error_NotSerializableMember = new DiagnosticDescriptor(
            id: "TG004", title: "Not serializable", messageFormat: "Member '{0}' is not serializable",
            category: "TinyhandGenerator", DiagnosticSeverity.Error, isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor Error_ReadonlyMember = new DiagnosticDescriptor(
            id: "TG005", title: "Not serializable", messageFormat: "Member '{0}' is readonly",
            category: "TinyhandGenerator", DiagnosticSeverity.Error, isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor Error_NotPublicMember = new DiagnosticDescriptor(
            id: "TG006", title: "Not public", messageFormat: "Member '{0}' is not public, consider setting IncludePrivateMembers property true",
            category: "TinyhandGenerator", DiagnosticSeverity.Error, isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor Error_KeyAttributeRequired = new DiagnosticDescriptor(
            id: "TG007", title: "Key attribute required", messageFormat: "Member of TinyhandObject should have KeyAttribute",
            category: "TinyhandGenerator", DiagnosticSeverity.Error, isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor Error_IntStringKeyConflict = new DiagnosticDescriptor(
            id: "TG008", title: "Key attribute conflict", messageFormat: "Integer key and String key are exclusive, use [Key(\"1\")] instead",
            category: "TinyhandGenerator", DiagnosticSeverity.Error, isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor Warning_StringKeySizeLimit = new DiagnosticDescriptor(
            id: "TG009", title: "String key limit", messageFormat: "The size of the string key exceeds the limit {0}",
            category: "TinyhandGenerator", DiagnosticSeverity.Warning, isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor Error_StringKeyConflict = new DiagnosticDescriptor(
            id: "TG010", title: "String keys conflict", messageFormat: "String keys with the same name were detected",
            category: "TinyhandGenerator", DiagnosticSeverity.Error, isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor Error_StringKeyNull = new DiagnosticDescriptor(
            id: "TG011", title: "String key null", messageFormat: "String key cannot contain null character",
            category: "TinyhandGenerator", DiagnosticSeverity.Error, isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor Error_IntKeyConflicted = new DiagnosticDescriptor(
            id: "TG012", title: "Int Key conflict", messageFormat: "Integer keys with the same number were detected",
            category: "TinyhandGenerator", DiagnosticSeverity.Error, isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor Warning_IntKeyUnused = new DiagnosticDescriptor(
            id: "TG013", title: "Unused Int Keys", messageFormat: "For better performance, try to reduce the number of unused integer keys",
            category: "TinyhandGenerator", DiagnosticSeverity.Warning, isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor Error_IntKeyOutOfRange = new DiagnosticDescriptor(
            id: "TG014", title: "Int key range", messageFormat: $"Integer key should be set to a value between 0 to {MaxIntegerKey}",
            category: "TinyhandGenerator", DiagnosticSeverity.Error, isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor Warning_KeyAndIgnoreAttribute = new DiagnosticDescriptor(
            id: "TG015", title: "Key attribute and IgnoreMember attribute", messageFormat: "Key attribute and IgnoreMember attribute are exclusive",
            category: "TinyhandGenerator", DiagnosticSeverity.Warning, isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor Error_ObjectAttributeRequired = new DiagnosticDescriptor(
            id: "TG016", title: "Key attribute required", messageFormat: "Member to be serialized must have TinyhandObjectAttribute",
            category: "TinyhandGenerator", DiagnosticSeverity.Error, isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor Error_AttributePropertyError = new DiagnosticDescriptor(
            id: "TG017", title: "Attribute property type error", messageFormat: "The argument specified does not match the type of the property",
            category: "TinyhandGenerator", DiagnosticSeverity.Error, isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor Error_KeyAttributeError = new DiagnosticDescriptor(
            id: "TG018", title: "Key attribute error", messageFormat: "KeyAttribute requires a valid int key or string key",
            category: "TinyhandGenerator", DiagnosticSeverity.Error, isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor Warning_Circular = new DiagnosticDescriptor(
            id: "TG019", title: "Circular dependency", messageFormat: "Circular dependency detected '{0}'",
            category: "TinyhandGenerator", DiagnosticSeverity.Warning, isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor Warning_NoDefaultConstructor = new DiagnosticDescriptor(
            id: "TG020", title: "No Default Constructor", messageFormat: "Reconstruct target '{0}' must have a default constructor",
            category: "TinyhandGenerator", DiagnosticSeverity.Warning, isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor Warning_NotReferenceType = new DiagnosticDescriptor(
            id: "TG021", title: "Not reference type", messageFormat: "Reconstruct target object '{0}' should be a reference type",
            category: "TinyhandGenerator", DiagnosticSeverity.Warning, isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor Warning_DefaultValueType = new DiagnosticDescriptor(
            id: "TG022", title: "Default Value Type", messageFormat: "The type of default value does not match the target type",
            category: "TinyhandGenerator", DiagnosticSeverity.Warning, isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor Warning_DefaultValueConstructor = new DiagnosticDescriptor(
            id: "TG023", title: "Default Value Constructor", messageFormat: "Constructor with an argument which type is same as the default value is required",
            category: "TinyhandGenerator", DiagnosticSeverity.Warning, isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor Warning_NoCoder = new DiagnosticDescriptor(
            id: "TG024", title: "No Coder", messageFormat: "'{0}' has passed the check, but the proper coder is not found",
            category: "TinyhandGenerator", DiagnosticSeverity.Warning, isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor Warning_KeyIgnored = new DiagnosticDescriptor(
            id: "TG025", title: "Key ignored", messageFormat: "KeyAttribute is ignored since ITinyhandSerialize is implemented",
            category: "TinyhandGenerator", DiagnosticSeverity.Warning, isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor Warning_SetDefaultMethod = new DiagnosticDescriptor(
            id: "TG026", title: "SetDefault Method", messageFormat: "To receive the default value, an implementation of 'public SetDefault({0})' method is required",
            category: "TinyhandGenerator", DiagnosticSeverity.Warning, isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor Warning_TinyhandObjectRequiredToReuse = new DiagnosticDescriptor(
            id: "TG027", title: "Reuse Instance", messageFormat: "The type of the member to be reused must have a TinyhandObject attribute",
            category: "TinyhandGenerator", DiagnosticSeverity.Warning, isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor Error_NullSubtype = new DiagnosticDescriptor(
            id: "TG028", title: "Null Subtype", messageFormat: "Could not get the subtype from the specified string or type",
            category: "TinyhandGenerator", DiagnosticSeverity.Error, isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor Error_UnionType = new DiagnosticDescriptor(
            id: "TG029", title: "Union type", messageFormat: "Union can only be interface or abstract class",
            category: "TinyhandGenerator", DiagnosticSeverity.Error, isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor Error_SubtypeConflicted = new DiagnosticDescriptor(
            id: "TG030", title: "Subtype conflict", messageFormat: "Same subtype has found",
            category: "TinyhandGenerator", DiagnosticSeverity.Error, isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor Error_UnionTargetError = new DiagnosticDescriptor(
            id: "TG031", title: "Union target error", messageFormat: "Union target type must have TinyhandObject attribute",
            category: "TinyhandGenerator", DiagnosticSeverity.Error, isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor Error_UnionNotDerived = new DiagnosticDescriptor(
            id: "TG032", title: "Union target error", messageFormat: "Union target type '{0}' is not derived from '{1}'",
            category: "TinyhandGenerator", DiagnosticSeverity.Error, isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor Error_UnionNotImplementing = new DiagnosticDescriptor(
            id: "TG033", title: "Union target error", messageFormat: "Union target type '{0}' does not implement '{1}'",
            category: "TinyhandGenerator", DiagnosticSeverity.Error, isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor Error_UnionSelf = new DiagnosticDescriptor(
            id: "TG034", title: "Union target error", messageFormat: "The type '{0}' cannot be specified as a union target",
            category: "TinyhandGenerator", DiagnosticSeverity.Error, isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor Warning_InvalidIdentifier = new DiagnosticDescriptor(
            id: "TG035", title: "Invalid identifier", messageFormat: "'{0}'is not a valid identifier, it's been replaced by '{1}'",
            category: "TinyhandGenerator", DiagnosticSeverity.Warning, isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor Warning_KeyAsNameExclusive = new DiagnosticDescriptor(
            id: "TG036", title: "KeyAsName exclusive", messageFormat: "KeyAttribute and KeyAsNameAttribute are exclusive",
            category: "TinyhandGenerator", DiagnosticSeverity.Warning, isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor Error_ImplicitExplicitKey = new DiagnosticDescriptor(
            id: "TG037", title: "Implicit Explicit conflict", messageFormat: "ImplicitKeyAsName and ExplicitKeyOnly are exclusive",
            category: "TinyhandGenerator", DiagnosticSeverity.Error, isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor Error_UnionToError = new DiagnosticDescriptor(
            id: "TG038", title: "UnionTo error", messageFormat: "The base type of TinyhandUnionToAttribute must be annotated with TinyhandObjectAttribute and must be an abstract class or interface",
            category: "TinyhandGenerator", DiagnosticSeverity.Error, isEnabledByDefault: true);

        public TinyhandBody(GeneratorExecutionContext context)
            : base(context)
        {
        }

        internal Dictionary<string, List<TinyhandObject>> Namespaces = new();

        // internal List<UnionToItem> UnionToList = new();

        public void Generate(TinyhandGenerator generator)
        {
            ScopingStringBuilder ssb = new();
            GeneratorInformation info = new()
            {
                UseMemberNotNull = generator.MemberNotNullIsAvailable,
                UseModuleInitializer = generator.ModuleInitializerIsAvailable,
            };
            List<TinyhandObject> rootObjects = new();

            // Namespace - Primary TinyhandObjects
            foreach (var x in this.Namespaces)
            {
                this.GenerateHeader(ssb);
                var ns = ssb.ScopeNamespace(x.Key);

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
                    this.Context?.AddSource($"gen.Tinyhand.{x.Key}", SourceText.From(result, Encoding.UTF8));
                }
            }

            this.GenerateLoader(generator, info, rootObjects);
            this.FlushDiagnostic();
        }

        public void Prepare()
        {
            // Configure objects.
            var array = this.FullNameToObject.ToArray();
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

            array = this.FullNameToObject.Where(x => x.Value.ObjectAttribute != null).ToArray();
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

        private void GenerateLoader(TinyhandGenerator generator, GeneratorInformation info, List<TinyhandObject> rootObjects)
        {
            var ssb = new ScopingStringBuilder();
            this.GenerateHeader(ssb);

            using (var scopeFormatter = ssb.ScopeNamespace("Tinyhand.Formatters"))
            {
                using (var methods = ssb.ScopeBrace("static class Generated"))
                {
                    info.FinalizeBlock(ssb);

                    TinyhandObject.GenerateLoader(ssb, info, rootObjects);
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
                this.Context?.AddSource($"gen.TinyhandLoader", SourceText.From(result, Encoding.UTF8));
            }
        }

        private void GenerateInitializer(TinyhandGenerator generator, ScopingStringBuilder ssb, GeneratorInformation info)
        {
            var ns = "Tinyhand"; // Namespace
            var assemblyId = string.Empty; // Assembly ID
            if (!string.IsNullOrEmpty(generator.CustomNamespace))
            {// Custom namespace.
                ns = generator.CustomNamespace;
            }
            else if (!string.IsNullOrEmpty(generator.AssemblyName) &&
                generator.OutputKind != OutputKind.ConsoleApplication &&
                generator.OutputKind != OutputKind.WindowsApplication)
            {// To avoid namespace conflicts, use assembly name for namespace.
                ns = generator.AssemblyName;
            }
            else
            {// Other (Apps)
                // assemblyId = "_" + generator.AssemblyId.ToString("x");
                if (!string.IsNullOrEmpty(generator.AssemblyName))
                {
                    assemblyId = "_" + generator.AssemblyName;
                }
            }

            info.ModuleInitializerClass.Add("Tinyhand.Formatters.Generated");

            ssb.AppendLine();
            using (var scopeTinyhand = ssb.ScopeNamespace(ns!))
            using (var scopeClass = ssb.ScopeBrace("public static class TinyhandModule" + assemblyId))
            {
                ssb.AppendLine("private static bool Initialized;");
                ssb.AppendLine();
                if (info.UseModuleInitializer)
                {
                    ssb.AppendLine("[ModuleInitializer]");
                }

                using (var scopeMethod = ssb.ScopeBrace("public static void Initialize()"))
                {
                    ssb.AppendLine($"if (Initialized) return;");
                    ssb.AppendLine($"Initialized = true;");
                    ssb.AppendLine();

                    foreach (var x in info.ModuleInitializerClass)
                    {
                        ssb.Append(x, true);
                        ssb.AppendLine(".__gen__load();", false);
                    }
                }

                // Methods for debugging
                // ssb.AppendLine($"public static string[] GetPreprocessor() => new string[] {{\"{string.Join("\", \"", generator.Context.ParseOptions.PreprocessorSymbolNames.ToArray())}\"}};");
            }
        }
    }
}
