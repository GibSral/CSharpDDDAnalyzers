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
                    createChangedSolution: it => MakeUppercaseAsync(context.Document, declaration, it),
                    equivalenceKey: title),
                diagnostic);
        }

        //private Task<Solution> MakeClassSealed(Document document, TypeDeclarationSyntax typeDecl, CancellationToken cancellationToken)
        //{
        //}
        
        private async Task<Solution> MakeUppercaseAsync(Document document, TypeDeclarationSyntax typeDecl, CancellationToken cancellationToken)
        {
            // Compute new uppercase name.
            var identifierToken = typeDecl.Identifier;
            var newName = identifierToken.Text.ToUpperInvariant();

            // Get the symbol representing the type to be renamed.
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            var typeSymbol = semanticModel.GetDeclaredSymbol(typeDecl, cancellationToken);

            // Produce a new solution that has all references to that type renamed, including the declaration.
            var originalSolution = document.Project.Solution;
            var optionSet = originalSolution.Workspace.Options;
            var newSolution = await Renamer.RenameSymbolAsync(document.Project.Solution, typeSymbol, newName, optionSet, cancellationToken).ConfigureAwait(false);

            // Return the new solution with the now-uppercase type name.
            return newSolution;
        }
    }
}
