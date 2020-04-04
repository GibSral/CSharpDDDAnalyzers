using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DDDAnalyzer.ValueObjectAnalyzers
{
    public static class FieldAnalyzer
    {
        public static void AnalyzeField(SymbolAnalysisContext context, Action<ISymbol> emitImmutabilityViolation)
        {
            var fieldSymbol = (IFieldSymbol)context.Symbol;
            if (fieldSymbol.ContainingType.IsValueObject())
            {
                EnsureFieldIsReadonly(fieldSymbol, emitImmutabilityViolation);
            }
        }

        private static void EnsureFieldIsReadonly(IFieldSymbol fieldSymbol, Action<ISymbol> emitImmutabilityViolation)
        {
            if (!fieldSymbol.IsReadOnly)
            {
                emitImmutabilityViolation(fieldSymbol);
            }
        }
    }
}