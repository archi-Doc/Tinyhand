// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Arc.Visceral;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

#pragma warning disable SA1306 // Field names should begin with lower-case letter
#pragma warning disable SA1204 // Static elements should appear before instance elements

namespace Tinyhand.Generator
{
    [Generator]
    public class TinyhandGenerator : ISourceGenerator, IGeneratorInformation
    {
        public bool AttachDebugger { get; private set; } = false;

        public bool GenerateToFile { get; private set; } = false;

        public string? CustomNamespace { get; private set; }

        public string? AssemblyName { get; private set; }

        public int AssemblyId { get; private set; }

        public OutputKind OutputKind { get; private set; }

        public string? TargetFolder { get; private set; }

        public GeneratorExecutionContext Context { get; private set; }

        private TinyhandBody body = default!;
        private INamedTypeSymbol? tinyhandObjectAttributeSymbol;
        private INamedTypeSymbol? tinyhandUnionAttributeSymbol;
        private INamedTypeSymbol? tinyhandGeneratorOptionAttributeSymbol;
#pragma warning disable RS1024
        private HashSet<INamedTypeSymbol?> processedSymbol = new();
#pragma warning restore RS1024

        static TinyhandGenerator()
        {
        }

        public void Execute(GeneratorExecutionContext context)
        {
            try
            {
                this.Context = context;
                context.CancellationToken.ThrowIfCancellationRequested();

                if (!(context.SyntaxReceiver is TinyhandSyntaxReceiver receiver))
                {
                    return;
                }

                var compilation = context.Compilation;
                /*var options = context.Compilation.Options.WithMetadataImportOptions(MetadataImportOptions.All);
                var topLevelBinderFlagsProperty = typeof(CSharpCompilationOptions).GetProperty("TopLevelBinderFlags", BindingFlags.Instance | BindingFlags.NonPublic);
                topLevelBinderFlagsProperty.SetValue(options, 1U  << 22);
                var compilation = context.Compilation.WithOptions(options);*/

                this.tinyhandObjectAttributeSymbol = compilation.GetTypeByMetadataName(TinyhandObjectAttributeMock.FullName);
                if (this.tinyhandObjectAttributeSymbol == null)
                {
                    return;
                }

                this.tinyhandUnionAttributeSymbol = compilation.GetTypeByMetadataName(TinyhandUnionAttributeMock.FullName);
                if (this.tinyhandUnionAttributeSymbol == null)
                {
                    return;
                }

                this.tinyhandGeneratorOptionAttributeSymbol = compilation.GetTypeByMetadataName(TinyhandGeneratorOptionAttributeMock.FullName);
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
                context.CancellationToken.ThrowIfCancellationRequested();

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
                foreach (var ts in receiver.Generics.ItemDictionary.Values.Where(a => a.GenericsKind == VisceralGenericsKind.ClosedGeneric).Select(a => a.TypeSymbol))
                {
                    if (ts != null)
                    {
                        this.ProcessSymbol(ts);
                    }
                }

                this.SalvageCloseGeneric(receiver.Generics);
                context.CancellationToken.ThrowIfCancellationRequested();

                this.body.Prepare();
                context.CancellationToken.ThrowIfCancellationRequested();
                if (this.body.Abort)
                {
                    return;
                }

                this.body.Generate(this, context.CancellationToken);
            }
            catch
            {
            }
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            // System.Diagnostics.Debugger.Launch();

            context.RegisterForSyntaxNotifications(() => new TinyhandSyntaxReceiver());
        }

        private void SalvageCloseGeneric(VisceralGenerics generics)
        {
            var stack = new Stack<INamedTypeSymbol>();
            foreach (var x in generics.ItemDictionary.Values.Where(a => a.GenericsKind == VisceralGenericsKind.ClosedGeneric))
            {
                SalvageCloseGenericCore(stack, x.TypeSymbol);
            }

            void SalvageCloseGenericCore(Stack<INamedTypeSymbol> stack, INamedTypeSymbol? ts)
            {
                if (ts == null || stack.Contains(ts))
                {// null or already exists.
                    return;
                }
                else if (ts.TypeKind != TypeKind.Class && ts.TypeKind != TypeKind.Struct)
                {// Not type
                    return;
                }
                else if (VisceralHelper.TypeToGenericsKind(ts) != VisceralGenericsKind.ClosedGeneric)
                {// Not close generic
                    return;
                }

                this.ProcessSymbol(ts);

                stack.Push(ts);
                try
                {
                    foreach (var y in ts.GetBaseTypesAndThis().SelectMany(x => x.GetMembers()))
                    {
                        INamedTypeSymbol? nts = null;
                        if (y is IFieldSymbol fs)
                        {
                            nts = fs.Type as INamedTypeSymbol;
                        }
                        else if (y is IPropertySymbol ps)
                        {
                            nts = ps.Type as INamedTypeSymbol;
                        }

                        // not primitive
                        if (nts != null && nts.SpecialType == SpecialType.None)
                        {
                            SalvageCloseGenericCore(stack, nts);
                        }
                    }
                }
                finally
                {
                    stack.Pop();
                }
            }
        }

        private void ProcessSymbol(INamedTypeSymbol s)
        {
            if (this.processedSymbol.Contains(s))
            {
                return;
            }

            this.processedSymbol.Add(s);
            foreach (var x in s.GetAttributes())
            {
                if (SymbolEqualityComparer.Default.Equals(x.AttributeClass, this.tinyhandObjectAttributeSymbol) ||
                    SymbolEqualityComparer.Default.Equals(x.AttributeClass, this.tinyhandUnionAttributeSymbol))
                { // TinyhandObject or TinyhandUnion
                    var obj = this.body.Add(s);
                    break;
                }
            }
        }

        private void Prepare(GeneratorExecutionContext context, Compilation compilation)
        {
            this.AssemblyName = compilation.AssemblyName ?? string.Empty;
            this.AssemblyId = this.AssemblyName.GetHashCode();
            this.OutputKind = compilation.Options.OutputKind;
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
                    var va = new VisceralAttribute(TinyhandGeneratorOptionAttributeMock.FullName, attr);
                    var ta = TinyhandGeneratorOptionAttributeMock.FromArray(va.ConstructorArguments, va.NamedArguments);

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
                            if (name.EndsWith(TinyhandGeneratorOptionAttributeMock.Name) || name.EndsWith(TinyhandGeneratorOptionAttributeMock.SimpleName))
                            {
                                this.GeneratorOptionSyntax = typeSyntax;
                            }
                        }

                        if (name.EndsWith(TinyhandObjectAttributeMock.Name) ||
                            name.EndsWith(TinyhandObjectAttributeMock.SimpleName) ||
                            name.EndsWith(TinyhandUnionAttributeMock.Name) ||
                            name.EndsWith(TinyhandUnionAttributeMock.SimpleName))
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
