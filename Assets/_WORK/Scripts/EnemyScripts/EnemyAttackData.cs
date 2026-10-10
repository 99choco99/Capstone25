using UnityEngine;

[System.Flags]
public enum EnemyAttackContext
{
    Neutral = 1,
    AfterGuard = 2,
    AfterDeflect = 4
}

[CreateAssetMenu(fileName = "EnemyAttackData", menuName = "Scriptable Objects/EnemyAttackData")]
public class EnemyAttackData : AttackData
{
    [Header("다음 공격 데이터")]
    public EnemyAttackData NextComboAttack;
    public EnemyAttackData GuardComboAttack;
    public EnemyAttackData ParryComboAttack;
    [InspectorName("다음 공격 연결 프레임 (0 = 기존 후반 연결)"), Min(0f)]
    public float NextComboStartFrame;
    [InspectorName("빗나가면 연격 종료")] public bool RequireContactForCombo;

    [SerializeField, Min(0)] private int hitGroup;

    [InspectorName("준비 동작 중 피격 경직 저항")] public bool ResistWindUpHit;

    [Header("재생 속도 곡선")]
    [Tooltip("X: 공격 진행도 0~1, Y: 기본 재생 속도에 곱할 값. 판정도 같은 속도로 진행합니다.")]
    public AnimationCurve playbackSpeed = AnimationCurve.Linear(0f, 1f, 1f, 1f);

    [Header("AI 선택 조건")]
    [SerializeField, Min(0f)] private float minDistance;
    [Tooltip("선택 가중치")]
    [SerializeField, Range(0f, 100f)] private float weight = 50f;

    [Tooltip("패링 후 반격으로 선택할 수 있는 행동인지")]
    [SerializeField] private EnemyAttackContext selectionContexts = EnemyAttackContext.Neutral | EnemyAttackContext.AfterGuard | EnemyAttackContext.AfterDeflect;

    [Header("공격별 쿨다운")]
    [SerializeField, Min(0f)] private float attackCooldown = 1.5f;

    public float SelectionWeight => weight;
    public float MinDistance => minDistance;
    public float Cooldown => attackCooldown;
    public int HitGroup => hitGroup;

    public bool SupportsContext(EnemyAttackContext context)
    {
        return (selectionContexts & context) != 0;
    }

    public float GetPlaybackSpeed(float normalizedTime)
    {
        return Mathf.Max(0.05f, playbackSpeed.Evaluate(Mathf.Clamp01(normalizedTime)));
    }

    /// <summary>
    /// 거리안에 있는지
    /// </summary>
    public bool IsInRange(float distance)
    {
        return distance <= MaxDistance;
    }

    public bool IsInStartRange(float distance)
    {
        return distance >= MinDistance && IsInRange(distance);
    }
}
