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
        private INamedTypeSymbol? tinyhandGeneratorTestAttributeSymbol;

        private static readonly string AttributeName = typeof(TinyhandObjectAttribute).Name;
        private static readonly string AttributeNameSimple;
        private static readonly string AttributeNameFull = typeof(TinyhandObjectAttribute).FullName;
        private static readonly string TestAttributeName = typeof(TinyhandGeneratorTestAttribute).Name;
        private static readonly string TestAttributeNameSimple;
        private static readonly string TestAttributeNameFull = typeof(TinyhandGeneratorTestAttribute).FullName;

        static TinyhandGenerator()
        {
            AttributeNameSimple = AttributeName.Remove(AttributeName.LastIndexOf("Attribute"));
            TestAttributeNameSimple = TestAttributeName.Remove(TestAttributeName.LastIndexOf("Attribute"));
        }

        public void Execute(GeneratorExecutionContext context)
        {
            if (!(context.SyntaxReceiver is TinyhandSyntaxReceiver receiver))
            {
                return;
            }

            var compilation = context.Compilation;
            this.tinyhandObjectAttributeSymbol = compilation.GetTypeByMetadataName(AttributeNameFull);
            if (this.tinyhandObjectAttributeSymbol == null)
            {
                return;
            }

            this.tinyhandGeneratorTestAttributeSymbol = compilation.GetTypeByMetadataName(TestAttributeNameFull);
            if (this.tinyhandGeneratorTestAttributeSymbol == null)
            {
                return;
            }

            this.ProcessGeneratorTest(receiver, compilation);
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

        private void ProcessGeneratorTest(TinyhandSyntaxReceiver receiver, Compilation compilation)
        {
            if (receiver.GeneratorTestSyntax == null)
            {
                return;
            }

            var model = compilation.GetSemanticModel(receiver.GeneratorTestSyntax.SyntaxTree);
            if (model.GetDeclaredSymbol(receiver.GeneratorTestSyntax) is INamedTypeSymbol s)
            {
                var attr = s.GetAttributes().FirstOrDefault(x => x.AttributeClass?.Name == TestAttributeName);
                if (attr != null)
                {
                    var va = new VisceralAttribute(TestAttributeNameFull, attr);
                    var ta = TinyhandGeneratorTestAttribute.FromArray(va.ConstructorArguments, va.NamedArguments);

                    this.AttachDebugger = ta.AttachDebugger;
                    this.GenerateToFile = ta.GenerateToFile;
                }
            }
        }

        internal class TinyhandSyntaxReceiver : ISyntaxReceiver
        {
            public TypeDeclarationSyntax? GeneratorTestSyntax { get; private set; }

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
                        if (this.GeneratorTestSyntax == null)
                        {
                            if (name.EndsWith(TestAttributeName) || name.EndsWith(TestAttributeNameSimple))
                            {
                                this.GeneratorTestSyntax = typeSyntax;
                            }
                        }

                        if (name.EndsWith(AttributeName) || name.EndsWith(AttributeNameSimple))
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
