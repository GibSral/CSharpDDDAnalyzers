using System.Collections.Immutable;
using System.Linq;
using DDDAnalyzer.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using static DDDAnalyzer.ValueObjectAnalyzers.PropertyAnalyzer;
using static DDDAnalyzer.ValueObjectAnalyzers.MethodAnalyzer;
using static DDDAnalyzer.ValueObjectAnalyzers.FieldAnalyzer;

namespace DDDAnalyzer.ValueObjectAnalyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ValueObjectAnalyzer : DiagnosticAnalyzer
    {
        public const string NoEntitiesInValueObjectsId = nameof(NoEntitiesInValueObjectsId);
        public const string ValueObjectsMustBeImmutableId = nameof(ValueObjectsMustBeImmutableId);
        private const string Category = "Design";

        private static readonly DiagnosticDescriptor ValueObjectMustNotUseEntityRule = new DiagnosticDescriptor(NoEntitiesInValueObjectsId,
            new LocalizableResourceString(nameof(Resources.ValueObjectUsesEntityTitle), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.ValueObjectUsesEntityMessageFormat), Resources.ResourceManager, typeof(Resources)),
            Category,
            DiagnosticSeverity.Error,
            true,
            new LocalizableResourceString(nameof(Resources.ValueObjectUsesEntityDescription), Resources.ResourceManager, typeof(Resources)));

        private static readonly DiagnosticDescriptor ValueObjectMustBeImmutable = new DiagnosticDescriptor(ValueObjectsMustBeImmutableId,
            new LocalizableResourceString(nameof(Resources.ValueObjectMustBeImmutableTitle), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.ValueObjectMustBeImmutableMessageFormat), Resources.ResourceManager, typeof(Resources)),
            Category,
            DiagnosticSeverity.Error,
            true,
            new LocalizableResourceString(nameof(Resources.ValueObjectMustBeImmutableDescription), Resources.ResourceManager, typeof(Resources)));


        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(ValueObjectMustNotUseEntityRule, ValueObjectMustBeImmutable);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSymbolAction(analysisContext => AnalyzeMethod(analysisContext, symbol => EmitEntityViolation(analysisContext, symbol)), SymbolKind.Method);
            context.RegisterSymbolAction(
                analysisContext => AnalyzerProperty(analysisContext,
                    symbol => EmitEntityViolation(analysisContext, symbol),
                    symbol => EmitImmutabilityViolation(analysisContext, symbol)),
                SymbolKind.Property);
            context.RegisterSymbolAction(analysisContext => AnalyzeField(analysisContext, symbol => EmitImmutabilityViolation(analysisContext, symbol)), SymbolKind.Field);
        }

        private static void EmitImmutabilityViolation(SymbolAnalysisContext context, ISymbol symbol) => EmitViolation(context, symbol, ValueObjectMustBeImmutable);

        private static void EmitEntityViolation(SymbolAnalysisContext context, ISymbol symbol) => EmitViolation(context, symbol, ValueObjectMustNotUseEntityRule);

        private static void EmitViolation(SymbolAnalysisContext context, ISymbol symbol, DiagnosticDescriptor valueObjectMustBeSealed, params object[] parameters)
        {
            var diagnostic = Diagnostic.Create(valueObjectMustBeSealed, symbol.Locations[0], parameters);
            context.ReportDiagnostic(diagnostic);
        }
    }
}