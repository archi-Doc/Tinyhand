// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Arc.Visceral;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

#pragma warning disable SA1306 // Field names should begin with lower-case letter
#pragma warning disable SA1204 // Static elements should appear before instance elements

namespace Tinyhand.Generator;

[Generator]
public class TinyhandGeneratorV2 : IIncrementalGenerator, IGeneratorInformation
{
    public bool AttachDebugger { get; private set; }

    public bool GenerateToFile { get; private set; }

    public string? CustomNamespace { get; private set; }

    public IAssemblySymbol AssemblySymbol { get; private set; } = default!;

    public string? AssemblyName { get; private set; }

    public int AssemblyId { get; private set; }

    public OutputKind OutputKind { get; private set; }

    public string? TargetFolder { get; private set; }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.CompilationProvider.Combine(
            context.SyntaxProvider
            .CreateSyntaxProvider(static (s, _) => IsSyntaxTargetForGeneration(s), static (ctx, _) => GetSemanticTargetForGeneration(ctx))
            .Collect());

        context.RegisterImplementationSourceOutput(provider, this.Emit);
    }

    private static bool IsSyntaxTargetForGeneration(SyntaxNode node)
    {
        if (node is TypeDeclarationSyntax m && m.AttributeLists.Count > 0)
        {
            return true;
        }
        else if (node is GenericNameSyntax { })
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    private static CSharpSyntaxNode? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
    {
        if (context.Node is TypeDeclarationSyntax typeSyntax)
        {
            foreach (var attributeList in typeSyntax.AttributeLists)
            {
                foreach (var attribute in attributeList.Attributes)
                {
                    var name = attribute.Name.ToString();
                    if (name.EndsWith(TinyhandGeneratorOptionAttributeMock.Name) ||
                        name.EndsWith(TinyhandGeneratorOptionAttributeMock.SimpleName))
                    {
                        return typeSyntax;
                    }
                    else if (name.EndsWith(TinyhandObjectAttributeMock.Name) ||
                        name.EndsWith(TinyhandObjectAttributeMock.SimpleName) ||
                        name.EndsWith(TinyhandUnionAttributeMock.Name) ||
                        name.EndsWith(TinyhandUnionAttributeMock.SimpleName))
                    {
                        return typeSyntax;
                    }
                }
            }
        }
        else if (context.Node is GenericNameSyntax genericSyntax)
        {
            return genericSyntax;
        }

        return null;
    }

    private void Emit(SourceProductionContext context, (Compilation compilation, ImmutableArray<CSharpSyntaxNode?> types) source)
    {
        var compilation = source.compilation;
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

        this.AssemblySymbol = compilation.Assembly;
        this.AssemblyName = compilation.AssemblyName ?? string.Empty;
        this.AssemblyId = this.AssemblyName.GetHashCode();
        this.OutputKind = compilation.Options.OutputKind;

        var body = new TinyhandBody(context);
        // receiver.Generics.Prepare(compilation);
#pragma warning disable RS1024 // Symbols should be compared for equality
        var processed = new HashSet<INamedTypeSymbol?>();
#pragma warning restore RS1024 // Symbols should be compared for equality

        this.generatorOptionIsSet = false;
        var generics = new VisceralGenerics();
        foreach (var x in source.types)
        {
            if (x == null)
            {
                continue;
            }
            else if (x is GenericNameSyntax genericSyntax)
            {
                generics.Add(genericSyntax);
                continue;
            }

            context.CancellationToken.ThrowIfCancellationRequested();

            var model = compilation.GetSemanticModel(x.SyntaxTree);
            if (model.GetDeclaredSymbol(x) is INamedTypeSymbol symbol)
            {
                this.ProcessSymbol(body, processed, x.SyntaxTree, symbol);
            }
        }

        generics.Prepare(compilation);
        foreach (var ts in generics.ItemDictionary.Values.Where(a => a.GenericsKind == VisceralGenericsKind.ClosedGeneric))
        {
            if (ts.TypeSymbol != null)
            {
                this.ProcessSymbol(body, processed, ts.GenericSyntax.SyntaxTree, ts.TypeSymbol);
            }
        }

        this.SalvageCloseGeneric(body, generics, processed);

        context.CancellationToken.ThrowIfCancellationRequested();
        body.Prepare();
        if (body.Abort)
        {
            return;
        }

        context.CancellationToken.ThrowIfCancellationRequested();
        body.Generate(this, context.CancellationToken);
    }

    private void ProcessSymbol(TinyhandBody body, HashSet<INamedTypeSymbol?> processed, SyntaxTree? syntaxTree, INamedTypeSymbol symbol)
    {
        if (!SymbolEqualityComparer.Default.Equals(symbol.ContainingAssembly, this.AssemblySymbol))
        {// Different assembly
            return;
        }
        else if (processed.Contains(symbol))
        {
            return;
        }

        processed.Add(symbol);
        foreach (var y in symbol.GetAttributes())
        {
            if (SymbolEqualityComparer.Default.Equals(y.AttributeClass, this.tinyhandObjectAttributeSymbol) ||
                SymbolEqualityComparer.Default.Equals(y.AttributeClass, this.tinyhandUnionAttributeSymbol))
            { // ValueLinkObject
                body.Add(symbol);
                break;
            }
            else if (!this.generatorOptionIsSet &&
                syntaxTree != null &&
                SymbolEqualityComparer.Default.Equals(y.AttributeClass, this.tinyhandGeneratorOptionAttributeSymbol))
            {
                this.generatorOptionIsSet = true;
                var va = new VisceralAttribute(TinyhandGeneratorOptionAttributeMock.FullName, y);
                var ta = TinyhandGeneratorOptionAttributeMock.FromArray(va.ConstructorArguments, va.NamedArguments);

                this.AttachDebugger = ta.AttachDebugger;
                this.GenerateToFile = ta.GenerateToFile;
                this.CustomNamespace = ta.CustomNamespace;
                this.TargetFolder = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(syntaxTree.FilePath), "Generated");
            }
        }
    }

    private void SalvageCloseGeneric(TinyhandBody body, VisceralGenerics generics, HashSet<INamedTypeSymbol?> processed)
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

            this.ProcessSymbol(body, processed, null, ts);

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

    private bool generatorOptionIsSet;
    private INamedTypeSymbol? tinyhandObjectAttributeSymbol;
    private INamedTypeSymbol? tinyhandUnionAttributeSymbol;
    private INamedTypeSymbol? tinyhandGeneratorOptionAttributeSymbol;
}
