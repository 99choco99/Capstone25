using Microsoft.CodeAnalysis;

namespace UniversalGraph.Generator
{
    internal static class DialogueDiagnostics
    {
        private const string Category = "UniversalGraph.Dialogue.Generation";

        internal static readonly DiagnosticDescriptor InvalidKey = new DiagnosticDescriptor(
            "UDG001",
            "올바르지 않은 Dialogue 메서드 키",
            "{0} '{1}'에 빈 키 또는 예약된 키가 있습니다.",
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor InvalidMethod = new DiagnosticDescriptor(
            "UDG002",
            "올바르지 않은 Dialogue 메서드",
            "{0} '{1}'은(는) 지원되지 않습니다: {2}",
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor InvalidTarget = new DiagnosticDescriptor(
            "UDG003",
            "올바르지 않은 Dialogue 메서드 대상",
            "{0} '{1}'은(는) 대상 '{2}'을(를) 사용할 수 없습니다: {3}",
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor InvalidParameter = new DiagnosticDescriptor(
            "UDG004",
            "올바르지 않은 Dialogue 메서드 파라미터",
            "{0} '{1}'의 파라미터 '{2}'은(는) 지원되지 않습니다: {3}",
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor DuplicateParameterId = new DiagnosticDescriptor(
            "UDG005",
            "중복된 Dialogue 파라미터 ID",
            "{0} '{1}'에 중복된 파라미터 ID '{2}'이(가) 있습니다.",
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor DuplicateKey = new DiagnosticDescriptor(
            "UDG006",
            "중복된 Dialogue 메서드 키",
            "어셈블리 '{2}'에 {0} 키 '{1}'이(가) 중복 선언되어 있습니다.",
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);
    }
}

