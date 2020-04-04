using System.Linq;
using DDDAnalyzer.Attributes;
using Microsoft.CodeAnalysis;

namespace DDDAnalyzer
{
    public static class SymbolExtensions
    {
        public static bool IsValueObject(this INamedTypeSymbol type)
        {
            var attributes = type.GetAttributes();
            return attributes.Any(IsValueObject);
        }

        private static bool IsValueObject(AttributeData attribute)
        {
            var isValueObject = attribute.AttributeClass.Name.Equals(nameof(ValueObjectAttribute));
            return isValueObject;
        }

        public static bool IsEntity(this ITypeSymbol type)
        {
            var attributes = type.GetAttributes().ToArray();
            return attributes.Any(it => it.AttributeClass.Name.Equals(nameof(EntityAttribute)));
        }
    }
}