using System.Collections.Immutable;
using System.Linq;
using DDDAnalyzer.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DDDAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ValueObjectAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "DDDAnalyzer";
        private const string Category = "Design";

        private static readonly DiagnosticDescriptor ValueObjectMustNotUseEntityRule = new DiagnosticDescriptor(DiagnosticId,
            new LocalizableResourceString(nameof(Resources.ValueObjectUsesEntityTitle), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.ValueObjectUsesEntityMessageFormat), Resources.ResourceManager, typeof(Resources)),
            Category,
            DiagnosticSeverity.Error,
            true,
            new LocalizableResourceString(nameof(Resources.ValueObjectUsesEntityDescription), Resources.ResourceManager, typeof(Resources)));

        private static readonly DiagnosticDescriptor ValueObjectMustBeImmutable = new DiagnosticDescriptor(DiagnosticId,
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

        private void AnalyzeField(SymbolAnalysisContext context)
        {
            var fieldSymbol = (IFieldSymbol)context.Symbol;
            if (!fieldSymbol.IsReadOnly)
            {
                EmitImmutabilityViolation(context, fieldSymbol);
            }
        }

        private static void AnalyzeMethod(SymbolAnalysisContext context)
        {
            var method = (IMethodSymbol)context.Symbol;
            var type = method.ContainingType;
            if (IsValueObject(type))
            {
                CheckThatParametersAreNotEntities(context, method);
                CheckThatReturnTypeIsNotEntity(context, method);
            }
        }

        private static void CheckThatReturnTypeIsNotEntity(SymbolAnalysisContext context, IMethodSymbol method)
        {
            if (!method.ReturnsVoid)
            {
                if (IsEntity(method.ReturnType))
                {
                    EmitEntityViolation(context, method);
                }
            }
        }

        private static void CheckThatParametersAreNotEntities(SymbolAnalysisContext context, IMethodSymbol method)
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
            if (IsValueObject(classType))
            {
                if (!propertySymbol.IsWriteOnly)
                {
                    EmitImmutabilityViolation(context, propertySymbol);
                }

                if (IsEntity(propertySymbol.Type))
                {
                    EmitEntityViolation(context, propertySymbol);
                }
            }
        }

        private static bool IsEntity(ISymbol symbol)
        {
            var attributes = symbol.GetAttributes().ToArray();
            return attributes.Any(it => it.AttributeClass.Name.Equals(nameof(EntityAttribute)));
        }

        private static bool IsValueObject(INamedTypeSymbol type)
        {
            var attributes = type.GetAttributes();
            return attributes.Any(IsValueObject);
        }

        private static bool IsValueObject(AttributeData attribute)
        {
            var isValueObject = attribute.AttributeClass.Name.Equals(nameof(ValueObjectAttribute));
            return isValueObject;
        }

        private static void EmitImmutabilityViolation(SymbolAnalysisContext context, ISymbol symbol)
        {
            var diagnostic = Diagnostic.Create(ValueObjectMustBeImmutable, symbol.Locations[0]);
            context.ReportDiagnostic(diagnostic);
        }

        private static void EmitEntityViolation(SymbolAnalysisContext context, ISymbol symbol)
        {
            var diagnostic = Diagnostic.Create(ValueObjectMustNotUseEntityRule, symbol.Locations[0]);
            context.ReportDiagnostic(diagnostic);
        }
    }
}