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

    [Header("방어 이펙트")]
    [SerializeField] private bool anchorDefenseEffectToWeapon;

    [Header("인살 타격")]
    [SerializeField] private Vector3 deathblowEffectOffset = new(0f, 1.1f, 0.1f);

    private CinemachineImpulseSource impulseSource;
    private Weapon[] weapons;
    private Transform postureBreakPoint;

    private void Awake()
    {
        if (entity == null)
            entity = GetComponent<LivingEntity>();
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
        CombatSettings.Reaction reaction = CombatSettings.Current.GetReaction(result.Request);
        bool isFinisher = result.Request.UseHeavyDefenseFeedback;
        Vector3 effectPoint = result.HitPoint;
        if (result.DefenseType != DefenseType.None)
            effectPoint = GetDefenseEffectPoint(result);

        Quaternion effectRotation = result.HitDirection.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(result.HitDirection) : Quaternion.identity;

        if (result.DefenseType == DefenseType.Parry)
        {
            PlaySound(SfxKeys.Parry, effectPoint);
            PlayEffect(isFinisher ? VfxKeys.FinisherParry : VfxKeys.Parry, effectPoint, effectRotation);
        }
        else if (result.DefenseType == DefenseType.NormalGuard)
        {
            PlaySound(SfxKeys.GuardHit, effectPoint);
            PlayEffect(isFinisher ? VfxKeys.FinisherGuardHit : VfxKeys.GuardHit, effectPoint, effectRotation);
        }
        else
        {
            Weapon source = result.Request.SourceWeapon;
            if (source != null)
            {
                effectPoint = source.HitEffectPoint;
                if (source.HitMotion.sqrMagnitude > 0.000001f)
                    effectRotation = Quaternion.LookRotation(source.HitMotion);
            }

            PlaySound(SfxKeys.CuttingFlesh, effectPoint);
            PlayEffect(VfxKeys.Blood, effectPoint, effectRotation);
        }

        float strength = reaction.GetShakeStrength(result);
        if (strength > 0f)
            impulseSource.GenerateImpulseAtPositionWithVelocity(effectPoint, Vector3.down * strength);
    }

    /// <summary>착지는 피해 판정과 분리합니다. 빗나가도 지면 연출은 재생하며 히트스톱은 접촉에만 적용합니다.</summary>
    public void PlayLandingImpact(EnemyAttackData attack, bool hasAcceptedContact)
    {
        Vector3 point = transform.position + transform.forward * 0.55f + Vector3.up * 0.05f;
        if (!string.IsNullOrEmpty(attack.LandingSoundKey))
            PlaySound(attack.LandingSoundKey, point);
        if (!string.IsNullOrEmpty(attack.LandingEffectKey))
            PlayEffect(attack.LandingEffectKey, point, Quaternion.FromToRotation(Vector3.forward, Vector3.up));

        // 접촉에서 이미 큰 충격을 재생했다면 착지 흔들림을 겹치지 않습니다.
        if (!hasAcceptedContact && attack.LandingShakeStrength > 0f)
            impulseSource.GenerateImpulseAtPositionWithVelocity(point, Vector3.down * attack.LandingShakeStrength);
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
            if (weapon == null)
                continue;

            Vector3 defenderPoint = GetDefenseBladePoint(weapon, result.HitPoint);
            Vector3 attackerPoint = GetDefenseBladePoint(source, defenderPoint);
            defenderPoint = GetDefenseBladePoint(weapon, attackerPoint);
            float distance = (defenderPoint - attackerPoint).sqrMagnitude;
            if (distance >= closestDistance)
                continue;

            // 무기 사이가 벌어져 있어도 방어 불꽃은 선택된 칼날 위에 표시합니다.
            closestPoint = anchorDefenseEffectToWeapon ? defenderPoint : (defenderPoint + attackerPoint) * 0.5f;
            closestDistance = distance;
        }

        return closestPoint;
    }

    private Vector3 GetDefenseBladePoint(Weapon weapon, Vector3 point)
    {
        Vector3 closestPoint = weapon.GetClosestBladePoint(point);
        if (!anchorDefenseEffectToWeapon)
            return closestPoint;

        var points = weapon.HitPoints;
        if (points == null || points.Count < 2)
            return closestPoint;

        float closestDistance = (point - closestPoint).sqrMagnitude;
        for (int i = 1; i < points.Count; i++)
        {
            if (points[i - 1] == null || points[i] == null)
                continue;

            Vector3 start = points[i - 1].position;
            Vector3 segment = points[i].position - start;
            float lengthSquared = segment.sqrMagnitude;
            if (lengthSquared <= 0.000001f)
                continue;

            float t = Mathf.Clamp01(Vector3.Dot(point - start, segment) / lengthSquared);
            Vector3 candidate = start + segment * t;
            float distance = (point - candidate).sqrMagnitude;
            if (distance >= closestDistance)
                continue;

            closestPoint = candidate;
            closestDistance = distance;
        }

        return closestPoint;
    }

    /// <summary>Timeline의 칼이 들어가는 시점에 호출하며, 피해나 목숨은 여기서 변경하지 않습니다.</summary>
    public void PlayDeathblowImpact(Vector3 hitDirection)
    {
        Vector3 point = transform.TransformPoint(deathblowEffectOffset);
        PlayEffect(VfxKeys.DeathblowBlood, point, Quaternion.LookRotation(hitDirection));
        impulseSource.GenerateImpulseAtPositionWithVelocity(
            point,
            Vector3.down * CombatSettings.Current.DeathblowShakeStrength);

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
