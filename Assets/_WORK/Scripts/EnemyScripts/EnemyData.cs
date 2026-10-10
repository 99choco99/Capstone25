using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "EnemyData", menuName = "Scriptable Objects/EnemyData")]
public class EnemyData : ScriptableObject
{
    public int id;

    [Header("생존 수치")]
    public float hp;
    public float posture;
    [Min(0f)] public float attackPower = 1f;

    public int exp;

    [Header("거리 판단")]
    [Tooltip("교전 거리")]
    [Min(0.1f)] public float combatRange = 3f;
    [Tooltip("이 배수만큼 멀어지면 추적으로 전환. 전투 경계선에서 떨림 방지")]
    [Min(1f)] public float combatExitRangeMultiplier = 1.2f;
    [Tooltip("strafe 거리")]
    [InspectorName("유지할 중심 간격"), Min(0.1f)] public float strafeDistance = 2f;
    [InspectorName("다시 접근하기 전 거리 여유"), Min(0f)] public float distanceTolerance = 0.2f;
    [Tooltip("공격을 고른 뒤 접근할 때 사용할 최소 중심 거리. 공격 전진·접촉 간격과 몸 충돌 반경을 제한하지 않습니다.")]
    [FormerlySerializedAs("minimumCombatDistance"), InspectorName("최소 접근 거리"), Min(0f)] public float minimumApproachDistance = 1.35f;

    [Header("행동 대기")]
    [Min(0f)] public float minActionDelay = 0.5f;
    [Min(0f)] public float maxActionDelay = 1.2f;

    [Header("공격 패턴")]
    public List<EnemyAttackData> pattern = new();
    [InspectorName("같은 패턴 반복 선택 완화")] public bool avoidRepeatedPatterns = true;
    [InspectorName("직전 패턴 선택 가중치 배율"), Range(0.01f, 1f)] public float repeatedPatternWeight = 0.35f;
    [InspectorName("후퇴 동작")] public EnemyAttackData retreatAttack;

    [Header("방어")]
    [Tooltip("첫 패링 확률")]
    [Range(0f, 1f)] public float parryChance = 0.1f;
    [Tooltip("일반 가드 확률")]
    [Range(0f, 1f)] public float blockChance = 0.75f;
    [Tooltip("예상 타격 보다 빠르게 가드를 올리는 시간")]
    [Min(0f)] public float guardLeadTime = 0.11f;
    [Tooltip("공격을 막을 수 있는 전체 각도")]
    [Range(0f, 180f)] public float guardAngle = 120f;

    [Header("위협 판정")]
    [Min(0.1f)] public float reactionRange = 2.5f;
    [Range(0f, 180f)] public float threatAngle = 100f;

    [Header("반격")]
    [Tooltip("패링 후 반격 확률")]
    [Range(0f, 1f)] public float counterChance = 0.45f;
    [Tooltip("패링 접촉 후 반격 전 최소 반응 시간")]
    [Min(0f)] public float counterDelay = 0.16f;

    [Header("회전")]
    [Min(0f)] public float combatRotationSpeed = 15f;
    [Min(0f)] public float chaseAngularSpeed = 400f;

    [Header("이동 속도")]
    [Min(0f)] public float patrolSpeed = 2f;
    [Min(0f)] public float chaseSpeed = 3f;
    [Min(0f)] public float strafeSpeed = 0.5f;
    [Min(0f)] public float arrivalTolerance = 0.1f;

    [Header("교전 위치 보정")]
    [Tooltip("타깃 주위를 도는 각도")]
    [Range(5f, 60f)] public float strafeArcAngle = 24f;
    [Tooltip("방향 유지 최소 시간")]
    [Range(0f, 10f)] public float minStrafeTime = 1.5f;
    [Tooltip("방향 유지 최대 시간")]
    [Range(0f, 10f)] public float maxStrafeTime = 3.5f;
    [Tooltip("strafe 목적지 계산 간격, 작을수록 촘촘한 원형")]
    [Min(0.05f)] public float strafeRefreshTime = 0.25f;
    [Min(0f)] public float strafeSampleDistance = 1.5f;
    [Min(0f)] public float strafeRetryTime = 0.5f;

    private void OnValidate()
    {
        if (parryChance + blockChance >= 1f)
        {
            blockChance = 1f - parryChance;
        }
    }
}
