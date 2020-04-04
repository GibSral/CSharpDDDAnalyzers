using System.Linq;
using DDDAnalyzer.Attributes;
using Microsoft.CodeAnalysis;

namespace DDDAnalyzer
{
    public static class NamedTypeSymbolExtensions
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

    }
}