using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

namespace UniversalGraph.Generator
{
    internal static class DialogueSymbolUtility
    {
        internal static bool IsOrDerivesFrom(ITypeSymbol type, INamedTypeSymbol baseType)
        {
            if (type == null || baseType == null)
                return false;

            ITypeSymbol current = type;
            while (current != null)
            {
                if (SymbolEqualityComparer.Default.Equals(current, baseType))
                    return true;

                current = (current as INamedTypeSymbol)?.BaseType;
            }

            return false;
        }

        internal static bool IsSupportedSerializedType(
            ITypeSymbol type,
            INamedTypeSymbol unityObjectType)
        {
            if (type == null)
                return false;

            switch (type.SpecialType)
            {
                case SpecialType.System_String:
                case SpecialType.System_Boolean:
                case SpecialType.System_Int32:
                case SpecialType.System_Single:
                    return true;
            }

            if (type.TypeKind == TypeKind.Enum)
                return true;

            return IsOrDerivesFrom(type, unityObjectType);
        }

        internal static bool HasOpenGenericContainingType(INamedTypeSymbol type)
        {
            INamedTypeSymbol current = type;
            while (current != null)
            {
                if (current.Arity != 0)
                    return true;

                current = current.ContainingType;
            }

            return false;
        }

        internal static bool CanEmitDirectCall(IMethodSymbol method)
        {
            if (!IsAccessibleFromGeneratedNamespace(method.DeclaredAccessibility))
                return false;

            INamedTypeSymbol containingType = method.ContainingType;
            while (containingType != null)
            {
                if (!IsAccessibleFromGeneratedNamespace(containingType.DeclaredAccessibility))
                    return false;

                containingType = containingType.ContainingType;
            }

            foreach (IParameterSymbol parameter in method.Parameters)
            {
                if (!IsTypeNameAccessible(parameter.Type))
                    return false;
            }

            return true;
        }

        internal static string GetReflectionTypeName(INamedTypeSymbol type)
        {
            var containingNames = new Stack<string>();
            INamedTypeSymbol current = type;
            while (current != null)
            {
                containingNames.Push(current.MetadataName);
                current = current.ContainingType;
            }

            var builder = new StringBuilder();
            if (!type.ContainingNamespace.IsGlobalNamespace)
            {
                builder.Append(type.ContainingNamespace.ToDisplayString());
                builder.Append('.');
            }

            bool isFirst = true;
            foreach (string name in containingNames)
            {
                if (!isFirst)
                    builder.Append('+');

                builder.Append(name);
                isFirst = false;
            }

            return builder.ToString();
        }

        internal static string GetReflectionTypeName(ITypeSymbol type)
        {
            var namedType = type as INamedTypeSymbol;
            return namedType == null
                ? type?.ToDisplayString() ?? string.Empty
                : GetReflectionTypeName(namedType);
        }

        internal static string GetSourceTypeName(ITypeSymbol type)
        {
            return type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        }

        internal static string EscapeIdentifier(string identifier)
        {
            // @ is valid for every C# identifier, not only keywords.
            return "@" + identifier;
        }

        internal static string EscapeString(string value)
        {
            if (value == null)
                return "null";

            var builder = new StringBuilder(value.Length + 2);
            builder.Append('"');
            foreach (char character in value)
            {
                switch (character)
                {
                    case '\\':
                        builder.Append("\\\\");
                        break;
                    case '"':
                        builder.Append("\\\"");
                        break;
                    case '\r':
                        builder.Append("\\r");
                        break;
                    case '\n':
                        builder.Append("\\n");
                        break;
                    case '\t':
                        builder.Append("\\t");
                        break;
                    default:
                        if (char.IsControl(character) || character == '\u2028' || character == '\u2029')
                            builder.Append("\\u").Append(((int)character).ToString("x4"));
                        else
                            builder.Append(character);
                        break;
                }
            }

            builder.Append('"');
            return builder.ToString();
        }

        internal static string SanitizeIdentifier(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "Assembly";

            var builder = new StringBuilder(text.Length);
            foreach (char character in text)
            {
                builder.Append(
                    character == '_' || char.IsLetterOrDigit(character)
                        ? character
                        : '_');
            }

            if (builder.Length == 0 ||
                (builder[0] != '_' && !char.IsLetter(builder[0])))
                builder.Insert(0, '_');

            return builder.ToString();
        }

        internal static uint GetStableHash(string text)
        {
            unchecked
            {
                uint hash = 2166136261;
                if (text != null)
                {
                    foreach (char character in text)
                    {
                        hash ^= character;
                        hash *= 16777619;
                    }
                }

                return hash;
            }
        }

        private static bool IsAccessibleFromGeneratedNamespace(Accessibility accessibility)
        {
            return accessibility == Accessibility.Public ||
                   accessibility == Accessibility.Internal ||
                   accessibility == Accessibility.ProtectedOrInternal;
        }

        private static bool IsTypeNameAccessible(ITypeSymbol type)
        {
            if (type == null)
                return false;

            if (type is IArrayTypeSymbol arrayType)
                return IsTypeNameAccessible(arrayType.ElementType);

            if (type is IPointerTypeSymbol pointerType)
                return IsTypeNameAccessible(pointerType.PointedAtType);

            if (!(type is INamedTypeSymbol namedType))
                return type.TypeKind != TypeKind.Error;

            INamedTypeSymbol current = namedType;
            while (current != null)
            {
                if (!IsAccessibleFromGeneratedNamespace(current.DeclaredAccessibility))
                    return false;

                current = current.ContainingType;
            }

            foreach (ITypeSymbol typeArgument in namedType.TypeArguments)
            {
                if (!IsTypeNameAccessible(typeArgument))
                    return false;
            }

            return true;
        }
    }
}

