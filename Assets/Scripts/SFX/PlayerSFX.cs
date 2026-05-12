using UnityEngine;

public class PlayerSFX : MonoBehaviour
{
    private Player player;

    private void Awake()
    {
        player = GetComponent<Player>();
    }

    private void Start()
    {
        player.Stats.OnDamaged += PlayDamageFeedback;
    }

    private void OnDestroy()
    {
        if (player != null && player.Stats != null)
            player.Stats.OnDamaged -= PlayDamageFeedback;
    }

    private void PlayDamageFeedback(DamageInfo result)
    {
        if (result.amount <= 0 && !result.wasParried && !result.wasGuarded) return;

        Quaternion effectRotation = Quaternion.LookRotation(result.hitDirection);

        if (result.attackType == AttackType.Heavy)
        {
            SoundManager.Instance.PlaySFX("HeavyHit");
            EffectManager.Instance.PlayEffect("Blood", result.hitPoint, Quaternion.identity, transform);
        }
        else if (result.wasParried)
        {
            SoundManager.Instance.PlaySFX("Parry");
            EffectManager.Instance.PlayEffect("Parry", result.hitPoint, effectRotation, transform);
        }
        else if (result.wasGuarded)
        {
            SoundManager.Instance.PlaySFX("GuardHit");
            EffectManager.Instance.PlayEffect("GuardHit", result.hitPoint, effectRotation, transform);
        }
        else
        {
            SoundManager.Instance.PlaySFX("Hit");
            SoundManager.Instance.PlaySFX("Cutting flesh");
            EffectManager.Instance.PlayEffect("Blood", result.hitPoint, Quaternion.identity, transform);
        }
    }
}
