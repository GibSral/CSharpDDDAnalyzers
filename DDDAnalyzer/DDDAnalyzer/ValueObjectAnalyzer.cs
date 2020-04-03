using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using DDDAnalyzer.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DDDAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ValueObjectAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "DDDAnalyzer";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.ValueObjectAnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.ValueObjectAnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.ValueObjectAnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Design";

        private static DiagnosticDescriptor ValueObjectMustNotUseEntityRule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(ValueObjectMustNotUseEntityRule); } }

        public override void Initialize(AnalysisContext context)
        {
            //context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);
            context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);
            context.RegisterSymbolAction(AnalyzeProperty, SymbolKind.Property);
        }

        private void AnalyzeMethod(SymbolAnalysisContext context)
        {
            var method = (IMethodSymbol)context.Symbol;
            var type = method.ContainingType;
            if (IsValueObject(type))
            {
                CheckThatParametersAreNotEntities(context, method);
                CheckThatReturnTypeIsNotEntity(context, method);
            }
        }

        private void CheckThatReturnTypeIsNotEntity(SymbolAnalysisContext context, IMethodSymbol method)
        {
            if (!method.ReturnsVoid)
            {
                if (IsEntity(method.ReturnType))
                {
                    EmitEntityViolation(context, method.ReturnType);
                }
            }
        }

        private void CheckThatParametersAreNotEntities(SymbolAnalysisContext context, IMethodSymbol method)
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

        private static void EmitEntityViolation(SymbolAnalysisContext context, ISymbol parameter)
        {
            var diagnostic = Diagnostic.Create(ValueObjectMustNotUseEntityRule, parameter.Locations[0]);
            context.ReportDiagnostic(diagnostic);
        }

        private void AnalyzeProperty(SymbolAnalysisContext context)
        {
        }

        private bool IsEntity(ITypeSymbol parameterType)
        {
            var attributes = parameterType.GetAttributes().ToArray();
            return attributes.Any(it => it.AttributeClass.Name.Equals(nameof(EntityAttribute)));
        }

        private bool IsValueObject(INamedTypeSymbol type)
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
