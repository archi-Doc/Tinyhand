// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Arc.Visceral;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Tinyhand.Generator;

/// <summary>
/// Holds the state of one run of <see cref="TinyhandGenerator"/>.<br/>
/// A new instance is created for every run, because the generator instance is shared by runs that may execute concurrently.
/// </summary>
internal sealed class TinyhandGeneratorRun : IGeneratorInformation
{
    public bool AttachDebugger { get; private set; }

    public bool GenerateToFile { get; private set; }

    public string? CustomNamespace { get; private set; }

    public IAssemblySymbol AssemblySymbol { get; private set; } = default!;

    public string? AssemblyName { get; private set; }

    public int AssemblyId { get; private set; }

    public OutputKind OutputKind { get; private set; }

    public string? TargetFolder { get; private set; }

    public void Emit(SourceProductionContext context, Compilation compilation, ImmutableArray<CSharpSyntaxNode?> types)
    {
        this.tinyhandObjectAttributeSymbol = compilation.GetTypeByMetadataName(TinyhandObjectAttributeData.FullName);
        if (this.tinyhandObjectAttributeSymbol == null)
        {
            return;
        }

        this.tinyhandUnionAttributeSymbol = compilation.GetTypeByMetadataName(TinyhandUnionAttributeData.FullName);
        if (this.tinyhandUnionAttributeSymbol == null)
        {
            return;
        }

        this.tinyhandGeneratorOptionAttributeSymbol = compilation.GetTypeByMetadataName(TinyhandGeneratorOptionAttributeData.FullName);
        if (this.tinyhandGeneratorOptionAttributeSymbol == null)
        {
            return;
        }

        this.tinyhandGenerateMemberAttributeSymbol = compilation.GetTypeByMetadataName(TinyhandGenerateMemberAttributeData.FullName);
        if (this.tinyhandGenerateMemberAttributeSymbol == null)
        {
            return;
        }

        this.tinyhandGenerateHashAttributeSymbol = compilation.GetTypeByMetadataName(TinyhandGenerateHashAttributeData.FullName);
        if (this.tinyhandGenerateHashAttributeSymbol == null)
        {
            return;
        }

        this.AssemblySymbol = compilation.Assembly;
        this.AssemblyName = compilation.AssemblyName ?? string.Empty;
        this.AssemblyId = this.AssemblyName.GetHashCode();
        this.OutputKind = compilation.Options.OutputKind;

        var body = new TinyhandBody(compilation, context, this.AssemblySymbol);
        var generateMemberBody = new TinyhandGenerateMemberBody(context);
        // receiver.Generics.Prepare(compilation);
#pragma warning disable RS1024 // Symbols should be compared for equality
        var processed = new HashSet<INamedTypeSymbol?>();
#pragma warning restore RS1024 // Symbols should be compared for equality

        var generics = new VisceralGenerics();
        foreach (var x in types)
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

            var model = compilation.GetSemanticModel(x.SyntaxTree);
#pragma warning disable RS1039 // This call to 'SemanticModel.GetDeclaredSymbol()' will always return 'null'
            if (model.GetDeclaredSymbol(x) is INamedTypeSymbol symbol)
            {
                this.ProcessSymbol(body, generateMemberBody, processed, x.SyntaxTree, symbol);
            }
#pragma warning restore RS1039 // This call to 'SemanticModel.GetDeclaredSymbol()' will always return 'null'
        }

        generics.Prepare(compilation);
        foreach (var ts in generics.ItemDictionary.Values.Where(a => a.GenericsKind == VisceralGenericsKind.ClosedGeneric))
        {
            if (ts.TypeSymbol != null)
            {
                this.ProcessSymbol(body, generateMemberBody, processed, ts.GenericSyntax.SyntaxTree, ts.TypeSymbol);
            }
        }

        // this.SalvageCloseGeneric(body, generics, processed);

        body.Prepare();
        if (body.Abort)
        {
            return;
        }

        if (compilation.Options is CSharpCompilationOptions csharpCompilationOptions &&
            !csharpCompilationOptions.AllowUnsafe &&
            body.RequiresUnsafeBlocks)
        {
            body.ReportDiagnostic(TinyhandBody.Error_UnsafeRequired, default);
        }

        generateMemberBody.Prepare();
        if (generateMemberBody.Abort)
        {
            return;
        }

        body.Generate(this, context.CancellationToken);
        generateMemberBody.Generate(this, context.CancellationToken);
    }

    private void ProcessSymbol(TinyhandBody body, TinyhandGenerateMemberBody? generateMemberBody, HashSet<INamedTypeSymbol?> processed, SyntaxTree? syntaxTree, INamedTypeSymbol symbol)
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
            { // TinyhandObject or TinyhandUnion
                body.Add(symbol);
                break;
            }
            else if (generateMemberBody != null &&
                (SymbolEqualityComparer.Default.Equals(y.AttributeClass, this.tinyhandGenerateMemberAttributeSymbol) ||
                SymbolEqualityComparer.Default.Equals(y.AttributeClass, this.tinyhandGenerateHashAttributeSymbol)))
            { // TinyhandGenerateMember
                generateMemberBody.Add(symbol);
            }
            else if (!this.generatorOptionIsSet &&
                syntaxTree != null &&
                SymbolEqualityComparer.Default.Equals(y.AttributeClass, this.tinyhandGeneratorOptionAttributeSymbol))
            {
                this.generatorOptionIsSet = true;
                var va = new VisceralAttribute(TinyhandGeneratorOptionAttributeData.FullName, y);
                var ta = TinyhandGeneratorOptionAttributeData.FromArray(va.ConstructorArguments, va.NamedArguments);

                this.AttachDebugger = ta.AttachDebugger;
                this.GenerateToFile = ta.GenerateToFile;
                this.CustomNamespace = ta.CustomNamespace;

                // A syntax tree without a file (e.g. an in-memory compilation) has no folder to write to.
                var directory = string.IsNullOrEmpty(syntaxTree.FilePath) ? null : System.IO.Path.GetDirectoryName(syntaxTree.FilePath);
                this.TargetFolder = string.IsNullOrEmpty(directory) ? null : System.IO.Path.Combine(directory, "Generated");
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

            this.ProcessSymbol(body, null, processed, null, ts);

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
    private INamedTypeSymbol? tinyhandGenerateMemberAttributeSymbol;
    private INamedTypeSymbol? tinyhandGenerateHashAttributeSymbol;
}
