using System.Collections.Immutable;
using System.Linq;
using DDDAnalyzer.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

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
            context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);
            context.RegisterSymbolAction(AnalyzeProperty, SymbolKind.Property);
            context.RegisterSymbolAction(AnalyzeField, SymbolKind.Field);
        }

        private static void AnalyzeField(SymbolAnalysisContext context)
        {
            var fieldSymbol = (IFieldSymbol)context.Symbol;
            if (fieldSymbol.ContainingType.IsValueObject())
            {
                EnsureFieldIsReadonly(context, fieldSymbol);
            }
        }

        private static void EnsureFieldIsReadonly(SymbolAnalysisContext context, IFieldSymbol fieldSymbol)
        {
            if (!fieldSymbol.IsReadOnly)
            {
                EmitImmutabilityViolation(context, fieldSymbol);
            }
        }

        private static void AnalyzeMethod(SymbolAnalysisContext context)
        {
            var method = (IMethodSymbol)context.Symbol;
            var type = method.ContainingType;
            if (type.IsValueObject())
            {
                EnsureThatEntitiesAreNotUsedAsParameters(context, method);
                EnsureThatEntityIsNotUsedAsReturnValue(context, method);
            }
        }

        private static void EnsureThatEntityIsNotUsedAsReturnValue(SymbolAnalysisContext context, IMethodSymbol method)
        {
            if (!method.ReturnsVoid)
            {
                if (IsEntity(method.ReturnType))
                {
                    EmitEntityViolation(context, method);
                }
            }
        }

        private static void EnsureThatEntitiesAreNotUsedAsParameters(SymbolAnalysisContext context, IMethodSymbol method)
        {
            foreach (var parameter in method.Parameters)
            {
                var parameterType = parameter.Type;
                if (IsEntity(parameterType))
                {
                    EmitEntityViolation(context, parameter);
                }
            }
        }

        private static void AnalyzeProperty(SymbolAnalysisContext context)
        {
            var propertySymbol = (IPropertySymbol)context.Symbol;
            var classType = propertySymbol.ContainingType;
            if (classType.IsValueObject())
            {
                EnsureThatPropertyIsReadonly(context, propertySymbol);
                EnsureThatPropertyIsNotOfAnEntityType(context, propertySymbol);
            }
        }

        private static void EnsureThatPropertyIsNotOfAnEntityType(SymbolAnalysisContext context, IPropertySymbol propertySymbol)
        {
            if (IsEntity(propertySymbol.Type))
            {
                EmitEntityViolation(context, propertySymbol);
            }
        }

        private static void EnsureThatPropertyIsReadonly(SymbolAnalysisContext context, IPropertySymbol propertySymbol)
        {
            if (!propertySymbol.IsReadOnly)
            {
                EmitImmutabilityViolation(context, propertySymbol);
            }
        }

        private static bool IsEntity(ISymbol symbol)
        {
            var attributes = symbol.GetAttributes().ToArray();
            return attributes.Any(it => it.AttributeClass.Name.Equals(nameof(EntityAttribute)));
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