// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace XUnitTest
{
    /// <summary>
    /// Parse a text file and get SyntaxTree, Compilation, SemanticModel.
    /// </summary>
    public class RoslynUnit
    {
        public RoslynUnit([CallerFilePath] string fileName = "")
        {
            this.SourceText = File.ReadAllText(fileName);
            this.Tree = CSharpSyntaxTree.ParseText(this.SourceText);
            this.Root = (CompilationUnitSyntax)this.Tree.GetRoot();
            this.Compilation = CSharpCompilation.Create(Path.GetFileName(fileName))
                              .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
                              .AddSyntaxTrees(this.Tree);
            this.Model = this.Compilation.GetSemanticModel(this.Tree);
        }

        public ITypeSymbol GetTypeSymbol(string typeName)
        {
            INamedTypeSymbol? symbol = null;

            var syntax = this.Root.DescendantNodes().OfType<TypeDeclarationSyntax>().FirstOrDefault(x => x.Identifier.ValueText == typeName);
            if (syntax != null)
            {
                symbol = this.Model.GetDeclaredSymbol(syntax) as INamedTypeSymbol;
            }

            if (symbol == null)
            {
                throw new System.Exception($"Could not find the type '{typeName}'.");
            }

            return symbol;
        }

        public ITypeSymbol GetEnumSymbol(string enumName)
        {
            INamedTypeSymbol? symbol = null;

            var syntax = this.Root.DescendantNodes().OfType<EnumDeclarationSyntax>().FirstOrDefault(x => x.Identifier.ValueText == enumName);
            if (syntax != null)
            {
                symbol = this.Model.GetDeclaredSymbol(syntax) as INamedTypeSymbol;
            }

            if (symbol == null)
            {
                throw new System.Exception($"Could not find the eum '{enumName}'.");
            }

            return symbol;
        }

        public string SourceText { get; }

        public SyntaxTree Tree { get; }

        public CompilationUnitSyntax Root { get; }

        public CSharpCompilation Compilation { get; }

        public SemanticModel Model { get; }
    }
}
