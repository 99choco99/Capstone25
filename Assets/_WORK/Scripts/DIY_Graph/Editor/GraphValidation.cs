namespace UniversalGraph.Editor
{
    /// <summary>그래프 작성 단계 진단에서 사용하는 문제 심각도입니다.</summary>
    public enum GraphValidationSeverity
    {
        Warning,
        Error
    }

    /// <summary>그래프 에셋에서 발견한 작성 문제 하나입니다.</summary>
    public sealed class GraphValidationIssue
    {
        public GraphValidationIssue(
            GraphValidationSeverity severity,
            string code,
            string message,
            string nodeGuid = null)
        {
            Severity = severity;
            Code = string.IsNullOrWhiteSpace(code) ? "GRAPH" : code.Trim();
            Message = message ?? string.Empty;
            NodeGuid = nodeGuid;
        }

        public GraphValidationSeverity Severity { get; }
        public string Code { get; }
        public string Message { get; }
        public string NodeGuid { get; }

        /// <summary>Console과 간단한 진단 화면에 표시할 문자열로 문제를 변환합니다.</summary>
        public override string ToString()
        {
            string severityLabel = Severity == GraphValidationSeverity.Error ? "오류" : "경고";
            return $"[{severityLabel}] {Code}: {Message}";
        }
    }
}
