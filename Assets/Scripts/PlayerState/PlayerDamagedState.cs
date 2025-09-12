using UnityEngine;

public class PlayerDamagedState : State
{

    private float exitTimer; // 피격 상태에서 머무를 시간
    public PlayerDamagedState(Player player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }


    public override void Enter()
    {
        exitTimer = 0.5f;
        player.Combat.ResetCombo(); // 피격 시 콤보 강제 초기화
    }

    public override void Update()
    {
        exitTimer -= Time.deltaTime;

        if (exitTimer <= 0f)
        {
            stateMachine.TransitionTo(stateMachine.PlayerIdleState);
        }
    }
}
