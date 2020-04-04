using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DDDAnalyzer.ValueObjectAnalyzers
{
    public class PropertyAnalyzer
    {
        public static void AnalyzerProperty(SymbolAnalysisContext context, Action<ISymbol> emitEntityViolation, Action<ISymbol> emitImmutabilityViolation)
        {
            var propertySymbol = (IPropertySymbol)context.Symbol;
            var classType = propertySymbol.ContainingType;
            if (classType.IsValueObject())
            {
                EnsureThatPropertyIsReadonly(propertySymbol, emitImmutabilityViolation);
                EnsureThatPropertyIsNotOfAnEntityType(propertySymbol, emitEntityViolation);
            }
        }

        private static void EnsureThatPropertyIsNotOfAnEntityType(IPropertySymbol propertySymbol, Action<ISymbol> emitEntityViolation)
        {
            if (propertySymbol.Type.IsEntity())
            {
                emitEntityViolation(propertySymbol);
            }
        }

        private static void EnsureThatPropertyIsReadonly(IPropertySymbol propertySymbol, Action<ISymbol> emitImmutabilityViolation)
        {
            if (!propertySymbol.IsReadOnly)
            {
                emitImmutabilityViolation(propertySymbol);
            }
        }
    }
}