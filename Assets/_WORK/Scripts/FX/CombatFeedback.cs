using Unity.Cinemachine;
using UnityEngine;

using SoundEffectManager;

/// <summary>확정된 피격 결과에 맞춰 소리·파티클·카메라 흔들림을 함께 재생합니다.</summary>
[RequireComponent(typeof(LivingEntity), typeof(CinemachineImpulseSource))]
public class CombatFeedback : MonoBehaviour
{
    [SerializeField] private LivingEntity entity;

    [Header("방어 이펙트 위치")]
    [SerializeField] private Transform guardEffectPoint;
    [SerializeField] private Vector3 guardEffectOffset = new Vector3(0f, 1.2f, 0.6f);

    [Header("인살 타격")]
    [SerializeField] private Vector3 deathblowEffectOffset = new Vector3(0f, 1.1f, 0.1f);

    private CinemachineImpulseSource impulseSource;

    private void Awake()
    {
        if (entity == null) entity = GetComponent<LivingEntity>();
        impulseSource = GetComponent<CinemachineImpulseSource>();
    }

    private void OnEnable()
    {
        entity.OnDamage += PlayDamageFeedback;
    }

    private void OnDisable()
    {
        entity.OnDamage -= PlayDamageFeedback;
    }

    /// <summary>
    /// DamageResult 하나당 한 번 호출합니다.
    /// 패링 반환 체간은 전용 경로를 사용해 DamageResult를 만들지 않으므로 검 충돌이 중복 재생되지 않습니다.
    /// </summary>
    private void PlayDamageFeedback(DamageResult result)
    {
        CombatSettings.Reaction reaction = CombatSettings.Current.GetReaction(result.Request.KnockBackLevel);
        float strength;
        Vector3 effectPoint = result.HitPoint;
        // 몸에 맞은 위치는 그대로 사용하고, 검으로 막았을 때만 방어 지점에서 스파크를 냅니다.
        if (result.DefenseType != DefenseType.None)
            effectPoint = guardEffectPoint != null ? guardEffectPoint.position : transform.TransformPoint(guardEffectOffset);
        Quaternion effectRotation = result.HitDirection.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(result.HitDirection)
            : Quaternion.identity;

        if (result.DefenseType == DefenseType.Parry)
        {
            PlaySound(SfxKeys.Parry, effectPoint);
            PlayEffect(VfxKeys.Parry, effectPoint, effectRotation);
            strength = reaction.ParryShake;
        }
        else if (result.DefenseType == DefenseType.NormalGuard)
        {
            PlaySound(SfxKeys.GuardHit, effectPoint);
            PlayEffect(VfxKeys.GuardHit, effectPoint, effectRotation);
            strength = reaction.GuardShake;
        }
        else
        {
            // Special도 실제로 몸에 닿았을 때는 별도의 경고음을 재사용하지 않고,
            // 일반 베기보다 강한 히트 스톱·카메라 반응과 살점 타격음을 조합합니다.
            PlaySound(SfxKeys.Hit, effectPoint);
            PlaySound(SfxKeys.CuttingFlesh, effectPoint);
            PlayEffect(VfxKeys.Blood, effectPoint, effectRotation);
            strength = result.Request.CanGuard ? reaction.DirectHitShake : reaction.SpecialHitShake;
        }

        // 같은 피격·가드·패링이라도 묵직한 공격은 화면 충격을 더 크게 줍니다.
        // 등급별 최종 강도는 CombatSettings에 저장되어 있습니다.
        if (strength > 0f)
            impulseSource.GenerateImpulseAtPositionWithVelocity(effectPoint, Vector3.down * strength);
    }

    /// <summary>Timeline의 칼이 들어가는 시점에 호출하며, 피해나 목숨은 여기서 변경하지 않습니다.</summary>
    public void PlayDeathblowImpact(Vector3 hitDirection)
    {
        Vector3 point = transform.TransformPoint(deathblowEffectOffset);
        PlaySound(SfxKeys.DeathblowImpact, point);
        PlayEffect(VfxKeys.Blood, point, Quaternion.LookRotation(hitDirection));
        impulseSource.GenerateImpulseAtPositionWithVelocity(point, Vector3.down * CombatSettings.Current.DeathblowShakeStrength);

        if (HitStopManager.Instance != null)
            HitStopManager.Instance.TriggerHitStop(CombatSettings.Current.DeathblowHitStopDuration);
    }

    private static void PlaySound(string key, Vector3 worldPosition)
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.Play(key, worldPosition);
    }

    private static void PlayEffect(string key, Vector3 worldPosition, Quaternion rotation)
    {
        // 전투 파티클은 월드에 남아 있어야 피격자가 넉백돼도 함께 끌려가지 않습니다.
        if (EffectManager.Instance != null)
            EffectManager.Instance.Play(key, worldPosition, rotation);
    }
}
