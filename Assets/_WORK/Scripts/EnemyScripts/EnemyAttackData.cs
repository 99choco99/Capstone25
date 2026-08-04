using UnityEngine;


public class EnemyAttackData : AttackData
{
    [Header("AI 선택 조건")]
    [Tooltip("최소 거리")]
    [SerializeField, Min(0f)] private float minDistance;

    [Tooltip("최대 거리")]
    [SerializeField, Min(0f)] private float maxDistance = 3f;

    [Tooltip("선택 가중치")]
    [SerializeField, Range(0f, 100f)] private float weight = 50f;

    [Header("공격별 쿨다운")]
    [SerializeField, Min(0f)] private float minAttackCooldown = 1f;
    [SerializeField, Min(0f)] private float maxAttackCooldown = 2f;

    public float MinimumRange => minDistance;
    public float MaximumRange => maxDistance;
    public float SelectionWeight => weight;

    /// <summary>
    /// 거리안에 있는지
    /// </summary>
    public bool IsInRange(float distance)
    {
        return distance >= MinimumRange && distance <= MaximumRange;
    }

    /// <summary>
    /// 랜덤 쿨타임 적용
    /// </summary>
    public float GetRandomCooldown()
    {
        float min = Mathf.Min(minAttackCooldown, maxAttackCooldown);
        float max = Mathf.Max(minAttackCooldown, maxAttackCooldown);
        return Random.Range(min, max);
    }

    private void OnValidate()
    {
        minDistance = Mathf.Max(0f, minDistance);
        maxDistance = Mathf.Max(minDistance, maxDistance);
        minAttackCooldown = Mathf.Max(0f, minAttackCooldown);
        maxAttackCooldown = Mathf.Max(minAttackCooldown, maxAttackCooldown);

    }
}
