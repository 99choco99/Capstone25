using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class EnemyStats : LivingEntity
{
    public override Faction TargetFaction => Faction.EnemyTeam;
    [SerializeField] EnemyData enemyData;

    public event Action<DamageEvent> OnDamaged;

    public bool IsPlayingDeathBlow;


    public int MaxLives = 1;
    public int CurrentLives { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        SetUp(enemyData);
    }

    public void SetUp(EnemyData data)
    {
        MaxHp.AddBaseValue(data.hp - MaxHp.Value);
        Defense.AddBaseValue(data.defense - Defense.Value);
        MaxPosture.AddBaseValue(data.posture - MaxPosture.Value);
        CurrentHp = MaxHp.Value;
        CurrentPosture = 0f;
        CurrentLives = MaxLives;
    }

    public override void TakeDamage(ref DamageEvent result)
    {
        if (IsDead) return;

        base.TakeDamage(ref result);

        OnDamaged?.Invoke(result);
    }

    public void LoseLife()
    {
        CurrentLives--;
        if (CurrentLives > 0)
        {
            // ∫Œ»∞
            CurrentHp = MaxHp.Value;
            CurrentPosture = 0f;
        }
    }


    public void ExecuteDeathBlow(GameObject executor)
    {
        DamageEvent damage = new DamageEvent()
        {
            attacker = executor,
            currentDamage = MaxHp.Value * 10f
        };

        TakeDamage(ref damage);
    }
}
