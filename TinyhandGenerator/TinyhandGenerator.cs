// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Arc.Visceral;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

#pragma warning disable SA1306 // Field names should begin with lower-case letter
#pragma warning disable SA1204 // Static elements should appear before instance elements

namespace Tinyhand.Generator
{
    [Generator]
    public class TinyhandGenerator : ISourceGenerator
    {
        public bool AttachDebugger { get; private set; } = false;

        public bool GenerateToFile { get; private set; } = false;

        public string? CustomNamespace { get; private set; }

        public bool MemberNotNullIsAvailable { get; private set; } = false;

        public bool ModuleInitializerIsAvailable { get; private set; } = false;

        public string? AssemblyName { get; private set; }

        public OutputKind OutputKind { get; private set; }

        public string? TargetFolder { get; private set; }

        public GeneratorExecutionContext Context { get; private set; }

        private TinyhandBody body = default!;
        private INamedTypeSymbol? tinyhandObjectAttributeSymbol;
        private INamedTypeSymbol? tinyhandGeneratorOptionAttributeSymbol;

        static TinyhandGenerator()
        {
        }

        public void Execute(GeneratorExecutionContext context)
        {
            this.Context = context;

            if (!(context.SyntaxReceiver is TinyhandSyntaxReceiver receiver))
            {
                return;
            }

            var compilation = context.Compilation;
            this.tinyhandObjectAttributeSymbol = compilation.GetTypeByMetadataName(TinyhandObjectAttributeFake.FullName);
            if (this.tinyhandObjectAttributeSymbol == null)
            {
                return;
            }

            this.tinyhandGeneratorOptionAttributeSymbol = compilation.GetTypeByMetadataName(TinyhandGeneratorOptionAttributeFake.FullName);
            if (this.tinyhandGeneratorOptionAttributeSymbol == null)
            {
                return;
            }

            this.ProcessGeneratorOption(receiver, compilation);
            if (this.AttachDebugger)
            {
                System.Diagnostics.Debugger.Launch();
            }

            this.Prepare(context, compilation);

            this.body = new TinyhandBody(context);
            receiver.Generics.Prepare(compilation);

            // IN: type declaration
            foreach (var x in receiver.CandidateSet)
            {
                var model = compilation.GetSemanticModel(x.SyntaxTree);
                if (model.GetDeclaredSymbol(x) is INamedTypeSymbol s)
                {
                    this.ProcessSymbol(s);
                }
            }

            // IN: close generic (member, expression)
            foreach (var ts in receiver.Generics.ItemDictionary.Values.Where(a => a.GenericsKind == VisceralGenericsKind.CloseGeneric).Select(a => a.TypeSymbol))
            {
                if (ts != null)
                {
                    this.ProcessSymbol(ts);
                }
            }

            this.body.Prepare();
            if (this.body.Abort)
            {
                return;
            }

            this.body.Generate(this);
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            // System.Diagnostics.Debugger.Launch();

            context.RegisterForSyntaxNotifications(() => new TinyhandSyntaxReceiver());
        }

        private void ProcessSymbol(INamedTypeSymbol s)
        {
            foreach (var x in s.GetAttributes())
            {
                if (SymbolEqualityComparer.Default.Equals(x.AttributeClass, this.tinyhandObjectAttributeSymbol))
                { // Attribute
                    var obj = this.body.Add(s);
                    break;
                }
            }
        }

        private void Prepare(GeneratorExecutionContext context, Compilation compilation)
        {
            this.AssemblyName = compilation.AssemblyName;
            this.OutputKind = compilation.Options.OutputKind;

            if (context.ParseOptions.PreprocessorSymbolNames.Any(x => x == "NET5_0"))
            {// .NET 5
                this.MemberNotNullIsAvailable = true;
                this.ModuleInitializerIsAvailable = true;
            }
            else
            {
                if (compilation.GetTypeByMetadataName("System.Diagnostics.CodeAnalysis.MemberNotNullAttribute") is { } atr && atr.DeclaredAccessibility == Accessibility.Public)
                {// [MemberNotNull] is supported.
                    this.MemberNotNullIsAvailable = true;
                }

                if (compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.ModuleInitializerAttribute") is { } atr2 && atr2.DeclaredAccessibility == Accessibility.Public)
                {// [ModuleInitializer] is supported.
                    this.ModuleInitializerIsAvailable = true;
                }
            }
        }

        private void ProcessGeneratorOption(TinyhandSyntaxReceiver receiver, Compilation compilation)
        {
            if (receiver.GeneratorOptionSyntax == null)
            {
                return;
            }

            var model = compilation.GetSemanticModel(receiver.GeneratorOptionSyntax.SyntaxTree);
            if (model.GetDeclaredSymbol(receiver.GeneratorOptionSyntax) is INamedTypeSymbol s)
            {
                var attr = s.GetAttributes().FirstOrDefault(x => SymbolEqualityComparer.Default.Equals(x.AttributeClass, this.tinyhandGeneratorOptionAttributeSymbol));
                if (attr != null)
                {
                    var va = new VisceralAttribute(TinyhandGeneratorOptionAttributeFake.FullName, attr);
                    var ta = TinyhandGeneratorOptionAttributeFake.FromArray(va.ConstructorArguments, va.NamedArguments);

                    this.AttachDebugger = ta.AttachDebugger;
                    this.GenerateToFile = ta.GenerateToFile;
                    this.CustomNamespace = ta.CustomNamespace;
                    this.TargetFolder = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(receiver.GeneratorOptionSyntax.SyntaxTree.FilePath), "Generated");
                }
            }
        }

        internal class TinyhandSyntaxReceiver : ISyntaxReceiver
        {
            public TypeDeclarationSyntax? GeneratorOptionSyntax { get; private set; }

            public HashSet<TypeDeclarationSyntax> CandidateSet { get; } = new HashSet<TypeDeclarationSyntax>();

            public VisceralGenerics Generics { get; } = new VisceralGenerics();

            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                if (syntaxNode is TypeDeclarationSyntax typeSyntax)
                {// Our target is a type syntax.
                    if (this.CheckAttribute(typeSyntax))
                    {// If a type has the specific attribute.
                        this.CandidateSet.Add(typeSyntax);
                    }
                }
                else if (syntaxNode is GenericNameSyntax genericSyntax)
                {// Generics
                    this.Generics.Add(genericSyntax);
                }
            }

            /// <summary>
            /// Returns true if the Type Sytax contains the specific attribute.
            /// </summary>
            /// <param name="typeSyntax">A type syntax.</param>
            /// <returns>True if the Type Sytax contains the specific attribute.</returns>
            private bool CheckAttribute(TypeDeclarationSyntax typeSyntax)
            {
                foreach (var attributeList in typeSyntax.AttributeLists)
                {
                    foreach (var attribute in attributeList.Attributes)
                    {
                        var name = attribute.Name.ToString();
                        if (this.GeneratorOptionSyntax == null)
                        {
                            if (name.EndsWith(TinyhandGeneratorOptionAttributeFake.Name) || name.EndsWith(TinyhandGeneratorOptionAttributeFake.SimpleName))
                            {
                                this.GeneratorOptionSyntax = typeSyntax;
                            }
                        }

                        if (name.EndsWith(TinyhandObjectAttributeFake.Name) || name.EndsWith(TinyhandObjectAttributeFake.SimpleName))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
        }
    }
}
