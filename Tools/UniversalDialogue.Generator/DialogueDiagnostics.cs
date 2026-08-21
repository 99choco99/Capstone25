using Microsoft.CodeAnalysis;

namespace UniversalGraph.Dialogue.Generator
{
    internal static class DialogueDiagnostics
    {
        private const string Category = "UniversalGraph.Dialogue.Generation";

        internal static readonly DiagnosticDescriptor InvalidKey = new DiagnosticDescriptor(
            "UDG001",
            "Invalid dialogue method key",
            "{0} '{1}' has an empty or reserved key",
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor InvalidMethod = new DiagnosticDescriptor(
            "UDG002",
            "Invalid dialogue method",
            "{0} '{1}' is not supported: {2}",
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor InvalidTarget = new DiagnosticDescriptor(
            "UDG003",
            "Invalid dialogue method target",
            "{0} '{1}' cannot use target '{2}': {3}",
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor InvalidParameter = new DiagnosticDescriptor(
            "UDG004",
            "Invalid dialogue method parameter",
            "{0} '{1}' parameter '{2}' is not supported: {3}",
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor DuplicateParameterId = new DiagnosticDescriptor(
            "UDG005",
            "Duplicate dialogue parameter id",
            "{0} '{1}' contains duplicate parameter id '{2}'",
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor DuplicateKey = new DiagnosticDescriptor(
            "UDG006",
            "Duplicate dialogue method key",
            "Duplicate {0} key '{1}' is declared in assembly '{2}'",
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);
    }
}

