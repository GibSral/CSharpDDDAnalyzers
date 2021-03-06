﻿using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DDDAnalyzer.ValueObjectAnalyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace DDDAnalyzer.ValueObjectCodeFixProvider
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ValueObjectImplementsIEquatableCodeFixProvider))]
    [Shared]
    public class ValueObjectImplementsIEquatableCodeFixProvider : Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider
    {
        private const string Title = "Implement IEquatable";

        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(ClassTypeAnalyzer.ValueObjectsMustImplementIEquatableId);
        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;
        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First(it => it.Id.Equals(ClassTypeAnalyzer.ValueObjectsMustImplementIEquatableId));
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<TypeDeclarationSyntax>().First();


            var implementedIEquatable = CodeAction.Create(Title, it => ImplementIEquatable(context.Document, declaration, it), Title);
            context.RegisterCodeFix(implementedIEquatable, diagnostic);
        }

        private async Task<Document> ImplementIEquatable(Document contextDocument, TypeDeclarationSyntax declaration, CancellationToken cancellationToken)
        {
            var className = declaration.Identifier.Text;
            var syntaxGenerator = SyntaxGenerator.GetGenerator(contextDocument);
            var updated = syntaxGenerator.AddBaseType(
                declaration,
                SyntaxFactory.ParseName($"IEquatable<{className}>"));
            var syntaxRoot = await contextDocument.GetSyntaxRootAsync(cancellationToken);
            var newDocument = contextDocument.WithSyntaxRoot(syntaxRoot.ReplaceNode(declaration, updated));
            return newDocument;
        }
    }
}