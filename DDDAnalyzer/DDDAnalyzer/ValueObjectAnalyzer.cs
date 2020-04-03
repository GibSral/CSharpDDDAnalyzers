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

        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.ValueObjectAnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.ValueObjectAnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.ValueObjectAnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Design";

        private static readonly DiagnosticDescriptor ValueObjectMustNotUseEntityRule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(ValueObjectMustNotUseEntityRule); } }

        public override void Initialize(AnalysisContext context)
        {
            //context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);
            context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);
            context.RegisterSymbolAction(AnalyzeProperty, SymbolKind.Property);
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

        private static void EmitEntityViolation(SymbolAnalysisContext context, ISymbol symbol)
        {
            var diagnostic = Diagnostic.Create(ValueObjectMustNotUseEntityRule, symbol.Locations[0]);
            context.ReportDiagnostic(diagnostic);
        }

        private static void AnalyzeProperty(SymbolAnalysisContext context)
        {
            var propertySymbol = (IPropertySymbol)context.Symbol;
            var classType = propertySymbol.ContainingType;
            if (IsValueObject(classType))
            {
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
    }
}
