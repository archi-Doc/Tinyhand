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

        private TinyhandBody body = default!;
        private INamedTypeSymbol? tinyhandObjectAttributeSymbol;
        private INamedTypeSymbol? tinyhandGeneratorOptionAttributeSymbol;

        static TinyhandGenerator()
        {
        }

        public void Execute(GeneratorExecutionContext context)
        {
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

            this.body.Generate(this.GenerateToFile);
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
