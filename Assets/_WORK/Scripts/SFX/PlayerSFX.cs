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

    private void PlayDamageFeedback(DamageEvent result)
    {

        Quaternion effectRotation = Quaternion.LookRotation(result.hitDirection);

        if (!result.attackData.CanGuard)
        {
            SoundManager.Instance.PlaySFX("HeavyHit");
            EffectManager.Instance.PlayEffect("Blood", result.hitPoint, Quaternion.identity, transform);
        }
        else if (result.defenseResult == DefenseType.PerfectParry)
        {
            SoundManager.Instance.PlaySFX("Parry");
            EffectManager.Instance.PlayEffect("Parry", result.hitPoint, effectRotation, transform);
        }
        else if (result.defenseResult == DefenseType.NormalGuard)
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
