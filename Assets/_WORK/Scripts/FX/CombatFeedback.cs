using System;
using Unity.Cinemachine;
using UnityEngine;
using SoundEffectManager;

/// <summary>확정된 피격 결과에 맞춰 소리, 파티클, 카메라 흔들림을 함께 재생</summary>
[RequireComponent(typeof(LivingEntity), typeof(CinemachineImpulseSource))]
public class CombatFeedback : MonoBehaviour
{
    public static event Action<LivingEntity, Vector3> OnPostureBreak;

    [SerializeField] private LivingEntity entity;

    [Header("인살 타격")]
    [SerializeField] private Vector3 deathblowEffectOffset = new(0f, 1.1f, 0.1f);

    private CinemachineImpulseSource impulseSource;
    private Weapon[] weapons;
    private Transform postureBreakPoint;

    private void Awake()
    {
        if (entity == null) entity = GetComponent<LivingEntity>();
        impulseSource = GetComponent<CinemachineImpulseSource>();
        weapons = GetComponentsInChildren<Weapon>();
        Enemy enemy = GetComponent<Enemy>();
        postureBreakPoint = enemy != null ? enemy.LockOnPoint : null;
    }

    private void OnEnable()
    {
        entity.OnDamage += PlayDamageFeedback;
        entity.OnPostureBroken += PlayPostureBreakFeedback;
        if (entity is EnemyStats)
            entity.OnHealthDepleted += PlayPostureBreakFeedback;
    }

    private void OnDisable()
    {
        entity.OnDamage -= PlayDamageFeedback;
        entity.OnPostureBroken -= PlayPostureBreakFeedback;
        if (entity is EnemyStats)
            entity.OnHealthDepleted -= PlayPostureBreakFeedback;
    }

    /// <summary>
    /// DamageResult 하나당 한 번 호출합니다.
    /// </summary>
    private void PlayDamageFeedback(DamageResult result)
    {
        CombatSettings.Reaction reaction = CombatSettings.Current.GetReaction(result.Request.KnockBackLevel);
        bool isFinisher = result.Request.KnockBackLevel == KnockBackLevel.Heavy && result.Request.DeflectResponse == DeflectResponse.Rebound;
        float strength;
        Vector3 effectPoint = result.HitPoint;
        if (result.DefenseType != DefenseType.None)
            effectPoint = GetDefenseEffectPoint(result);

        Quaternion effectRotation = result.HitDirection.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(result.HitDirection) : Quaternion.identity;

        if (result.DefenseType == DefenseType.Parry)
        {
            PlaySound(SfxKeys.Parry, effectPoint);
            PlayEffect(isFinisher ? VfxKeys.FinisherParry : VfxKeys.Parry, effectPoint, effectRotation);
            strength = reaction.ParryShake;
        }
        else if (result.DefenseType == DefenseType.NormalGuard)
        {
            PlaySound(SfxKeys.GuardHit, effectPoint);
            PlayEffect(isFinisher ? VfxKeys.FinisherGuardHit : VfxKeys.GuardHit, effectPoint, effectRotation);
            strength = reaction.GuardShake;
        }
        else
        {
            PlaySound(SfxKeys.CuttingFlesh, effectPoint);
            PlayEffect(VfxKeys.Blood, effectPoint, effectRotation);
            strength = result.Request.CanGuard ? reaction.DirectHitShake : reaction.SpecialHitShake;
        }

        if (strength > 0f)
            impulseSource.GenerateImpulseAtPositionWithVelocity(effectPoint, Vector3.down * strength);
    }

    private Vector3 GetDefenseEffectPoint(in DamageResult result)
    {
        Weapon source = result.Request.SourceWeapon;
        if (source == null)
            return result.HitPoint;

        Vector3 closestPoint = result.HitPoint;
        float closestDistance = float.PositiveInfinity;
        foreach (Weapon weapon in weapons)
        {
            if (weapon == null) continue;

            Vector3 defenderPoint = weapon.GetClosestBladePoint(result.HitPoint);
            Vector3 attackerPoint = source.GetClosestBladePoint(defenderPoint);
            defenderPoint = weapon.GetClosestBladePoint(attackerPoint);
            float distance = (defenderPoint - attackerPoint).sqrMagnitude;
            if (distance >= closestDistance) continue;

            closestPoint = (defenderPoint + attackerPoint) * 0.5f;
            closestDistance = distance;
        }

        return closestPoint;
    }

    /// <summary>Timeline의 칼이 들어가는 시점에 호출하며, 피해나 목숨은 여기서 변경하지 않습니다.</summary>
    public void PlayDeathblowImpact(Vector3 hitDirection)
    {
        Vector3 point = transform.TransformPoint(deathblowEffectOffset);
        PlayEffect(VfxKeys.DeathblowBlood, point, Quaternion.LookRotation(hitDirection));
        impulseSource.GenerateImpulseAtPositionWithVelocity(point, Vector3.down * CombatSettings.Current.DeathblowShakeStrength);

        if (HitStopManager.Instance != null)
            HitStopManager.Instance.HitStop(CombatSettings.Current.DeathblowHitStopDuration);
    }

    private void PlayPostureBreakFeedback()
    {
        Vector3 point = postureBreakPoint != null
            ? postureBreakPoint.position : transform.TransformPoint(deathblowEffectOffset);
        CombatSettings settings = CombatSettings.Current;
        PlayEffect(VfxKeys.PostureBreak, point, Quaternion.identity);
        impulseSource.GenerateImpulseAtPositionWithVelocity(point, Vector3.down * settings.PostureBreakShakeStrength);

        if (HitStopManager.Instance != null)
            HitStopManager.Instance.HitStop(settings.PostureBreakHitStopDuration);

        OnPostureBreak?.Invoke(entity, point);
    }

    private static void PlaySound(string key, Vector3 worldPosition)
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.Play(key, worldPosition);
    }

    private static void PlayEffect(string key, Vector3 worldPosition, Quaternion rotation)
    {
        if (EffectManager.Instance != null)
            EffectManager.Instance.Play(key, worldPosition, rotation);
    }
}
