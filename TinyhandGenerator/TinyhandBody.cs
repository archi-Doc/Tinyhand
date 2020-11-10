// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Arc.Visceral;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

#pragma warning disable RS2000
#pragma warning disable SA1306 // Field names should begin with lower-case letter
#pragma warning disable SA1310 // Field names should not contain underscore
#pragma warning disable SA1401 // Fields should be private
#pragma warning disable SA1117 // Parameters should be on same line or separate lines

namespace Tinyhand.Generator
{
    public class TinyhandBody : VisceralBody<TinyhandObject>
    {
        public static readonly string StringKeyFieldFormat = "__gen_utf8_key_{0:D4}";
        public static readonly int MaxIntegerKey = 5_000;
        public static readonly int MaxStringKeySizeInBytes = 256;

        public static readonly DiagnosticDescriptor Error_NotPartial = new DiagnosticDescriptor(
            id: "TG001", title: "Not a partial class/struct", messageFormat: "TinyhandObject '{0}' is not a partial class/struct",
            category: "TinyhandGenerator", DiagnosticSeverity.Error, isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor Error_NotPartialParent = new DiagnosticDescriptor(
            id: "TG002", title: "Not a partial class/struct", messageFormat: "Parent object '{0}' is not a partial class/struct",
            category: "TinyhandGenerator", DiagnosticSeverity.Error, isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor Error_NoDefaultConstructor = new DiagnosticDescriptor(
            id: "TG003", title: "No default constructor", messageFormat: "TinyhandObject '{0}' should have default constructor",
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
            id: "TG016", title: "Key attribute required", messageFormat: "Member to be serialized should have TinyhandObjectAttribute",
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
            id: "TG020", title: "No Default Constructor", messageFormat: "Reconstruct target object '{0}' should have default constructor",
            category: "TinyhandGenerator", DiagnosticSeverity.Warning, isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor Warning_NotReferenceType = new DiagnosticDescriptor(
            id: "TG021", title: "Not reference type", messageFormat: "Reconstruct target object '{0}' should be a reference type",
            category: "TinyhandGenerator", DiagnosticSeverity.Warning, isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor Warning_DefaultValueType = new DiagnosticDescriptor(
            id: "TG022", title: "Default Value Type", messageFormat: "The type of default value does not match the target type",
            category: "TinyhandGenerator", DiagnosticSeverity.Warning, isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor Warning_DefaultValueConstructor = new DiagnosticDescriptor(
            id: "TG023", title: "Default Value Constructor", messageFormat: "You need a constructor with an argument which type is same as the default value",
            category: "TinyhandGenerator", DiagnosticSeverity.Warning, isEnabledByDefault: true);

        public TinyhandBody(GeneratorExecutionContext context)
            : base(context)
        {
        }

        internal Dictionary<string, List<TinyhandObject>> Namespaces = new();

        public void Generate(bool generateToFile, string? targetFolder)
        {
            ScopingStringBuilder ssb = new();
            GeneratorInformation info = new();
            List<TinyhandObject> rootObjects = new();

            // Namespace
            foreach (var x in this.Namespaces)
            {
                ssb.AddUsing("System");
                ssb.AddUsing("System.Collections.Generic");
                ssb.AddUsing("System.Runtime.CompilerServices");
                ssb.AddUsing("Tinyhand");
                ssb.AddUsing("Tinyhand.IO");
                ssb.AddUsing("Tinyhand.Resolvers");
                ssb.AppendLine("// <auto-generated/>", false);
                ssb.AppendLine("#nullable enable", false);
                ssb.AppendLine("#pragma warning disable CS1591", false);
                ssb.AppendLine();

                var ns = ssb.ScopeNamespace(x.Key);

                rootObjects.AddRange(x.Value);

                var firstFlag = true;
                foreach (var y in x.Value)
                {
                    if (!firstFlag)
                    {
                        ssb.AppendLine();
                    }

                    firstFlag = false;

                    y.Generate(ssb, info);
                }

                var result = ssb.Finalize();

                if (generateToFile && targetFolder != null && Directory.Exists(targetFolder))
                {
                    this.StringToFile(result, Path.Combine(targetFolder, $"gen.Tinyhand.{x.Key}.cs"));
                }
                else
                {
                    this.Context?.AddSource($"gen.Tinyhand.{x.Key}", SourceText.From(result, Encoding.UTF8));
                }
            }

            this.GenerateLoader(generateToFile, targetFolder, info, rootObjects);
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

        private void GenerateLoader(bool generateToFile, string? targetFolder, GeneratorInformation info, List<TinyhandObject> rootObjects)
        {
            var ssb = new ScopingStringBuilder();
            ssb.AddUsing("System");
            ssb.AddUsing("System.Runtime.CompilerServices");
            ssb.AddUsing("Tinyhand");
            ssb.AddUsing("Tinyhand.IO");
            ssb.AddUsing("Tinyhand.Resolvers");
            ssb.AppendLine("// <auto-generated/>", false);
            ssb.AppendLine("#nullable enable", false);
            ssb.AppendLine("#pragma warning disable CS1591", false);
            ssb.AppendLine();

            ssb.ScopeNamespace("Tinyhand.Formatters");

            using (var methods = ssb.ScopeBrace("static class Generated"))
            {
                info.FinalizeBlock(ssb);

                TinyhandObject.GenerateLoader(ssb, info, rootObjects, true);
            }

            var result = ssb.Finalize();

            if (generateToFile && targetFolder != null && Directory.Exists(targetFolder))
            {
                this.StringToFile(result, Path.Combine(targetFolder, "gen.TinyhandGenerated.cs"));
            }
            else
            {
                this.Context?.AddSource($"gen.TinyhandLoader", SourceText.From(result, Encoding.UTF8));
            }
        }
    }
}
