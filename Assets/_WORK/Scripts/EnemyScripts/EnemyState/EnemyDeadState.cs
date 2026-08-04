using UnityEngine;

/// <summary>
/// 적의 사망 상태
/// </summary>
public class EnemyDeadState : EnemyState
{
    private bool playDeathAnimation = true;

    public EnemyDeadState(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine) { }

    /// <summary>DeadState 진입 전에 호출. false면 Death 애니를 재생하지 않음(인살 사망)</summary>
    public void SetPlayDeathAnimation(bool play) => playDeathAnimation = play;

    public override void Enter()
    {
        if (playDeathAnimation)
            enemy.AnimationController.PlayAction(AnimHash.Death);

        enemy.Motor.Stop();
        enemy.Motor.StopKnockback();
        enemy.Motor.DisableAgent();
        enemy.Combat.CancelAttack();
        enemy.Combat.ClearDefense();


        foreach (Collider col in enemy.GetComponentsInChildren<Collider>())
            col.enabled = false;
    }
}
