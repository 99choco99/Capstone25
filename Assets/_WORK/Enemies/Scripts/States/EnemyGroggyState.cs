using UnityEngine;

/// <summary>
/// 체력 소진, 체간 붕괴로 인살이 가능한 상태
/// </summary>
public class EnemyGroggyState : EnemyState
{
    private float PostureBreakRecoveryDelay => CombatSettings.Current.EnemyPostureBreakRecoveryDelay;

    //애니메이션 이어붙이기
    private float GroggyEnterDuration => CombatSettings.Current.EnemyGroggyEnterDuration;
    private float GroggyRecoverDuration => CombatSettings.Current.EnemyGroggyRecoverDuration;
    private bool isHoldingGroggyPose;
    private bool isRecovering;

    private float stateTimer;

    public EnemyGroggyState(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine) { }

    /// <summary>
    /// 그로기 중 추가 피격은 경직 State를 덮어쓰지 않음.
    /// </summary>
    public override bool CanInterrupted => false;
    public override bool UseRootMotion => !isHoldingGroggyPose && !isRecovering;
    public override void Enter()
    {
        stateTimer = 0f;
        isHoldingGroggyPose = false;
        isRecovering = false;

        enemy.Motor.Stop();
        enemy.Motor.StopKnockback();
        enemy.AnimationController.PlayReaction(AnimHash.GroggyEnter);
    }

    public override void Update()
    {
        stateTimer += Time.deltaTime;

        if (!isHoldingGroggyPose && stateTimer >= GroggyEnterDuration)
        {
            isHoldingGroggyPose = true;
            enemy.AnimationController.PlayAction(AnimHash.Groggy, 0.06f);
        }

        // HP 소진으로 들어온 Groggy
        if (enemy.Stats.IsHealthDepleted)
        {
            return;
        }

        if (!isRecovering && stateTimer >= PostureBreakRecoveryDelay - GroggyRecoverDuration)
        {
            isRecovering = true;
            enemy.AnimationController.PlayAction(AnimHash.GroggyRecover, 0.04f);
        }

        if (stateTimer > PostureBreakRecoveryDelay)
        {
            enemy.Stats.ResetPosture();
            stateMachine.TransitionTo(stateMachine.EnemyGroundedState);
        }
    }

    public override void Exit()
    {
        stateTimer = 0f;
        isHoldingGroggyPose = false;
        isRecovering = false;
    }
}
