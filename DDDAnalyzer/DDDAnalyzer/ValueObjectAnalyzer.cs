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

        private static readonly DiagnosticDescriptor ValueObjectMustImplementIEquatable = new DiagnosticDescriptor(DiagnosticId,
            new LocalizableResourceString(nameof(Resources.ValueObjectMustImplementIEquatableTitle), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.ValueObjectMustImplementIEquatableMessageFormat), Resources.ResourceManager, typeof(Resources)),
            Category,
            DiagnosticSeverity.Error,
            true,
            new LocalizableResourceString(nameof(Resources.ValueObjectMustImplementIEquatableDescription), Resources.ResourceManager, typeof(Resources)));

        private static readonly DiagnosticDescriptor ValueObjectMustBeSealed = new DiagnosticDescriptor(DiagnosticId,
            new LocalizableResourceString(nameof(Resources.ValueObjectMustBeSealedTitle), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.ValueObjectMustBeSealedMessageFormat), Resources.ResourceManager, typeof(Resources)),
            Category,
            DiagnosticSeverity.Error,
            true,
            new LocalizableResourceString(nameof(Resources.ValueObjectMustBeSealedDescription), Resources.ResourceManager, typeof(Resources)));


        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(ValueObjectMustNotUseEntityRule, ValueObjectMustBeImmutable, ValueObjectMustImplementIEquatable, ValueObjectMustBeSealed);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSymbolAction(AnalyzeType, SymbolKind.NamedType);
            context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);
            context.RegisterSymbolAction(AnalyzeProperty, SymbolKind.Property);
            context.RegisterSymbolAction(AnalyzeField, SymbolKind.Field);
        }

        private static void AnalyzeType(SymbolAnalysisContext context)
        {
            var namedTypeSymbol = (INamedTypeSymbol)context.Symbol;
            if (IsValueObject(namedTypeSymbol))
            {
                if (!namedTypeSymbol.IsSealed)
                {
                    EmitSealedViolation(context, namedTypeSymbol);
                }

                var implementsIEquatable = namedTypeSymbol.AllInterfaces.Any(it =>
                {
                    var implements = it.Name.Equals("IEquatable");
                    implements &= it.TypeArguments.Any(tp => tp.Name.Equals(namedTypeSymbol.Name) && tp.ContainingNamespace.Equals(namedTypeSymbol.ContainingNamespace));
                    return implements;
                });
                if (!implementsIEquatable)
                {
                    EmitIEquatableViolation(context, namedTypeSymbol);
                }
            }
        }

        private static void AnalyzeField(SymbolAnalysisContext context)
        {
            var fieldSymbol = (IFieldSymbol)context.Symbol;
            if (IsValueObject(fieldSymbol.ContainingType))
            {
                if (!fieldSymbol.IsReadOnly)
                {
                    EmitImmutabilityViolation(context, fieldSymbol);
                }
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
                CheckForPrivateConstructor(method, context.);
            }
        }

        private static void CheckForPrivateConstructor(IMethodSymbol method)
        {
            if (method.MethodKind == MethodKind.Constructor)
            {
                if(method.)
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
                if (!propertySymbol.IsReadOnly)
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

        private static void EmitIEquatableViolation(SymbolAnalysisContext context, INamedTypeSymbol symbol) =>
            EmitViolation(context, symbol, ValueObjectMustImplementIEquatable, symbol.Name);

        private static void EmitSealedViolation(SymbolAnalysisContext context, INamedTypeSymbol symbol) => EmitViolation(context, symbol, ValueObjectMustBeSealed);

        private static void EmitImmutabilityViolation(SymbolAnalysisContext context, ISymbol symbol) => EmitViolation(context, symbol, ValueObjectMustBeImmutable);

        private static void EmitEntityViolation(SymbolAnalysisContext context, ISymbol symbol) => EmitViolation(context, symbol, ValueObjectMustNotUseEntityRule);

        private static void EmitViolation(SymbolAnalysisContext context, ISymbol symbol, DiagnosticDescriptor valueObjectMustBeSealed, params object[] parameters)
        {
            var diagnostic = Diagnostic.Create(valueObjectMustBeSealed, symbol.Locations[0], parameters);
            context.ReportDiagnostic(diagnostic);
        }
    }
}