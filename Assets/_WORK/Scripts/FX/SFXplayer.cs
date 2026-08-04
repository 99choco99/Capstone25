using UnityEngine;

[RequireComponent(typeof(LivingEntity))]
public class SFXplayer : MonoBehaviour
{
    // 두 검이 이 거리보다 멀면 실제 칼끼리 맞닿은 것으로 보지 않습니다.
    private const float MaximumBladeClashDistance = 0.6f;

    // 계산한 검 접촉점이 물리 충돌점에서 지나치게 멀면 모델 정렬 오류로 보고 원래 충돌점을 사용합니다.
    private const float MaximumEffectPointCorrection = 0.75f;

    [SerializeField] private LivingEntity entity;
    private Weapon[] defendingWeapons;

    private void Awake()
    {
        if (entity == null) entity = GetComponent<LivingEntity>();
        defendingWeapons = GetComponentsInChildren<Weapon>(includeInactive: true);
    }

    private void OnEnable()
    {
        if (entity != null) entity.OnDamage += PlayDamageFeedback;
    }

    private void OnDisable()
    {
        if (entity != null) entity.OnDamage -= PlayDamageFeedback;
    }

    private void PlayDamageFeedback(DamageResult result)
    {
        if (!result.IsAccepted) return;

        Vector3 effectPoint = ResolveEffectPoint(result);
        Quaternion effectRotation = result.HitDirection.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(result.HitDirection)
            : Quaternion.identity;

        if (result.DefenseType == DefenseType.Parry)
        {
            PlaySound(SfxKeys.Parry, effectPoint);
            PlayEffect(VfxKeys.Parry, effectPoint, effectRotation);
        }
        else if (result.DefenseType == DefenseType.NormalGuard)
        {
            PlaySound(SfxKeys.GuardHit, effectPoint);
            PlayEffect(VfxKeys.GuardHit, effectPoint, effectRotation);
        }
        else
        {
            // Special도 실제로 몸에 닿았을 때는 별도의 경고음을 재사용하지 않고,
            // 일반 베기보다 강한 히트 스톱·카메라 반응과 살점 타격음을 조합합니다.
            PlaySound(SfxKeys.Hit, effectPoint);
            PlaySound(SfxKeys.CuttingFlesh, effectPoint);
            PlayEffect(VfxKeys.Blood, effectPoint, effectRotation);
        }

        CombatImpactFeedback.Trigger(result);
    }

    /// <summary>
    /// 가드·패링이면 공격 무기와 방어 무기의 최근접점을 우선 사용합니다.
    /// 모델의 칼 정렬이 크게 어긋난 경우에는 원래 충돌점으로 돌아가 잘못된 위치의 스파크를 막습니다.
    /// </summary>
    private Vector3 ResolveEffectPoint(in DamageResult result)
    {
        // DamageResult는 이제 실제 무기 충돌에서만 생성되므로 별도의 피해 원인 검사가 필요 없습니다.
        bool isBladeClash =
            result.DefenseType == DefenseType.Parry
            || result.DefenseType == DefenseType.NormalGuard;

        Weapon attackingWeapon = result.Request.SourceWeapon;
        if (!isBladeClash
            || attackingWeapon == null
            || !attackingWeapon.IsAttackActive
            || defendingWeapons == null)
        {
            return result.HitPoint;
        }

        Vector3 closestPoint = result.HitPoint;
        float closestSqrDistance = MaximumBladeClashDistance * MaximumBladeClashDistance;
        float maximumCorrectionSqr =
            MaximumEffectPointCorrection * MaximumEffectPointCorrection;
        bool found = false;

        foreach (Weapon defendingWeapon in defendingWeapons)
        {
            if (defendingWeapon == null
                || !attackingWeapon.TryGetClosestPointTo(
                    defendingWeapon,
                    out Vector3 candidatePoint,
                    out float candidateSqrDistance)
                || candidateSqrDistance >= closestSqrDistance
                || (candidatePoint - result.HitPoint).sqrMagnitude > maximumCorrectionSqr)
            {
                continue;
            }

            closestSqrDistance = candidateSqrDistance;
            closestPoint = candidatePoint;
            found = true;
        }

        return found ? closestPoint : result.HitPoint;
    }

    private static void PlaySound(string key, Vector3 worldPosition)
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySFXAtPoint(key, worldPosition);
    }

    private static void PlayEffect(string key, Vector3 worldPosition, Quaternion rotation)
    {
        // 전투 파티클은 월드에 남아 있어야 피격자가 넉백돼도 함께 끌려가지 않습니다.
        if (EffectManager.Instance != null)
            EffectManager.Instance.PlayEffect(key, worldPosition, rotation, parent: null);
    }
}
