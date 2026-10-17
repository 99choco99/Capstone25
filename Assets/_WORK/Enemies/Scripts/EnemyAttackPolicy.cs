using System;
using UnityEngine;

public enum EnemyReferenceExecution
{
    Pattern,
    Observe,
    Substitute
}
public enum EnemyReferenceMapping
{
    None,
    Pattern,
    Observation,
    Substitute,
    Unavailable
}

/// <summary>원본 행동과 적별 모션의 연결, 관찰과 접촉 반응 시간을 설정합니다.</summary>
[CreateAssetMenu(fileName = "EnemyAttackPolicy", menuName = "Scriptable Objects/Enemy Attack Policy")]
public sealed class EnemyAttackPolicy : ScriptableObject
{
    [Serializable]
    public sealed class ReferenceBinding
    {
        public EnemyReferenceScope scope;
        [Range(1, 50)] public int action;
        public EnemyReferenceExecution execution;
        public EnemyAttackRole role;
        public EnemyAttackRole alternativeRole;
        public int historyKey;
        [Tooltip("원작에만 있는 활·번개 등은 대체 동작임을 명시합니다.")]
        public string adaptation;
    }

    [Tooltip("710300은 고정한 원본 행동 번호와 정수 예산을 사용합니다. 지원하지 않는 값은 적별 기본 선택을 사용합니다.")]
    public int referenceVersion;
    public ReferenceBinding[] referenceBindings = Array.Empty<ReferenceBinding>();
    public bool UsesReferenceDecisions => referenceVersion == EnemyReferenceDecision.SourceVersion;

    public ReferenceBinding FindBinding(in EnemyReferenceChoice choice)
    {
        if (referenceBindings == null)
            return null;
        foreach (ReferenceBinding binding in referenceBindings)
            if (binding != null && binding.scope == choice.Scope && binding.action == choice.Action)
                return binding;
        return null;
    }

    [Min(0f)] public float minimumObservationTime = 1.5f;
    [Min(0f)] public float maximumObservationTime = 3f;

    [Header("접촉 반응 시간 (초)")]
    [Min(0f)] public float guardReactionDuration = 0.2f;
    [Min(0f)] public float parryReactionDuration = 0.13333334f;
    [Tooltip("반동 전환 이벤트를 우리 상태 복귀에 대응한 시간입니다. 원작의 모든 행동 허용 시점이 같다는 뜻은 아닙니다.")]
    [Min(0f)] public float reboundOpportunityDuration = 0.6f;
    [Tooltip("원작의 방어 판단 재시도 간격입니다. 첫 공격 인지 지연과는 별개입니다.")]
    [Min(0f)] public float defenseDecisionInterval = 0.1f;
}
