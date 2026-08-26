using Microsoft.CodeAnalysis;

namespace UniversalGraph.Generator
{
    internal static class QuestDiagnostics
    {
        private const string Category = "UniversalGraph.Quest.Generation";

        internal static readonly DiagnosticDescriptor InvalidKey = new DiagnosticDescriptor(
            "UQG001", "올바르지 않은 Quest 메서드 키", "{0} '{1}'에 빈 키 또는 예약된 키가 있습니다.",
            Category, DiagnosticSeverity.Error, true);

        internal static readonly DiagnosticDescriptor InvalidMethod = new DiagnosticDescriptor(
            "UQG002", "올바르지 않은 Quest 메서드", "{0} '{1}'은(는) 지원되지 않습니다: {2}",
            Category, DiagnosticSeverity.Error, true);

        internal static readonly DiagnosticDescriptor InvalidTarget = new DiagnosticDescriptor(
            "UQG003", "올바르지 않은 Quest 메서드 대상", "{0} '{1}'은(는) 대상 '{2}'을(를) 사용할 수 없습니다: {3}",
            Category, DiagnosticSeverity.Error, true);

        internal static readonly DiagnosticDescriptor InvalidParameter = new DiagnosticDescriptor(
            "UQG004", "올바르지 않은 Quest 메서드 파라미터", "{0} '{1}'의 파라미터 '{2}'은(는) 지원되지 않습니다: {3}",
            Category, DiagnosticSeverity.Error, true);

        internal static readonly DiagnosticDescriptor DuplicateParameterId = new DiagnosticDescriptor(
            "UQG005", "중복된 Quest 파라미터 ID", "{0} '{1}'에 중복된 파라미터 ID '{2}'이(가) 있습니다.",
            Category, DiagnosticSeverity.Error, true);

        internal static readonly DiagnosticDescriptor DuplicateKey = new DiagnosticDescriptor(
            "UQG006", "중복된 Quest 메서드 키", "어셈블리 '{2}'에 {0} 키 '{1}'이(가) 중복 선언되어 있습니다.",
            Category, DiagnosticSeverity.Error, true);
    }
}
