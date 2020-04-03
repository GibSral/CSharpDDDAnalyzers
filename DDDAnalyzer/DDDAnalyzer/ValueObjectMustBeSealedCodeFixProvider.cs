using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;

namespace DDDAnalyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ValueObjectMustBeSealedCodeFixProvider)), Shared]
    public class ValueObjectMustBeSealedCodeFixProvider : CodeFixProvider
    {
        private string title;
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(ValueObjectAnalyzer.ValueObjectsMustBeSealedId);

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First(it => it.Id.Equals(ValueObjectAnalyzer.ValueObjectsMustBeSealedId));
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the type declaration identified by the diagnostic.
            var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<TypeDeclarationSyntax>().First();

            // Register a code action that will invoke the fix.
            title = "Make value object sealed";
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: title,
                    it => MakeClassSealed(context.Document, declaration, it),
                    equivalenceKey: title),
                diagnostic);
        }

        private async Task<Document> MakeClassSealed(Document document, TypeDeclarationSyntax typeDecl, CancellationToken cancellationToken)
        {
            var newModifiers = typeDecl.AddModifiers(SyntaxFactory.Token(SyntaxKind.SealedKeyword));
            var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken);
            var syntaxRootWithReplacedModifiers = syntaxRoot.ReplaceNode(typeDecl, newModifiers);
            return document.WithSyntaxRoot(syntaxRootWithReplacedModifiers);
        }
    }
}
