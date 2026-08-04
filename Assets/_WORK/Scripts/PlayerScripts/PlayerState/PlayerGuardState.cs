using System.Collections;
using UnityEngine;

public class PlayerGuardState : PlayerState
{

    [Header("패링 시스템")]
    private float guardTimer;
    private float parryWindowDuration = 0.25f;   // 0.2초 안에는 패링
    private float normalGuardDuration = 0.6f;   // 0.6초 안에는 일반 가드

    private float currentParryWindow;           // 깎여나갈 실제 패링 시간
    private float lastGuardEnterTime;         // 가드를 푼 시간 기록
    private int spamCount = 0;                  // 연타 횟수


    public PlayerGuardState(Player player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }

    public override bool UseRootMotion => false;


    public override void Enter()
    {
        guardTimer = 0f;

        if (Time.time - lastGuardEnterTime < 0.4f) spamCount++;
        else spamCount = 0;
        lastGuardEnterTime = Time.time;
        currentParryWindow = Mathf.Max(0.04f, parryWindowDuration - (spamCount * 0.05f));



        player.Motor.SetMovement(Vector3.zero);
        player.AnimatorController.ForceStopLocomotion();

        player.AnimatorController.PlayAction(AnimHash.Guard);
        player.Combat.CurrentDefenseType = DefenseType.PerfectParry;
    }


    public override void Update()
    {
        base.HandleInput();
        if (stateMachine.CurrentState != this) return;

        guardTimer += Time.unscaledDeltaTime;
        if (guardTimer > normalGuardDuration) { player.Combat.CurrentDefenseType = DefenseType.FailedGuard; }
        else if (guardTimer > currentParryWindow) { player.Combat.CurrentDefenseType = DefenseType.NormalGuard; }


        //=========가드 키 감지 ===============


        if (guardTimer < parryWindowDuration)
        {
            HandleGuardMovement(true); // 발을 땅에 고정
            return; // 여기서 return 되므로 가드를 뗄 수 없음
        }

        if (!player.InputHandler.GuardInput && guardTimer >= parryWindowDuration)
        {
            stateMachine.TransitionTo(stateMachine.PlayerGroundedState);
            return;
        }

        HandleGuardMovement(false);
    }

    private void HandleGuardMovement(bool stop)
    {
        if (stop)
        {
            player.Motor.SetMovement(Vector3.zero);
            if (player.IsLockOn && player.TargetingSystem.CurrentTarget != null)
            {
                Vector3 directionToTarget = player.TargetingSystem.CurrentTarget.TargetTransform.position - player.transform.position;
                directionToTarget.y = 0;
                player.Motor.RotateToDirection(directionToTarget);
            }

            UpdateLocomotionAnimation(Vector3.zero);
            return; 
        }


        Vector3 moveDir = player.GetDesiredMoveDirection();
        player.Motor.SetMovement(moveDir * player.Motor.GuardSpeed);

        if (player.IsLockOn && player.TargetingSystem.CurrentTarget != null)
        {
            Vector3 directionToTarget = player.TargetingSystem.CurrentTarget.TargetTransform.position - player.transform.position;
            directionToTarget.y = 0;
            player.Motor.RotateToDirection(directionToTarget);
        }
        else if (moveDir != Vector3.zero)
        {
            player.Motor.RotateToDirection(moveDir);
        }
        UpdateLocomotionAnimation(moveDir);
    }

    private void UpdateLocomotionAnimation(Vector3 moveDir)
    {
        if (moveDir == Vector3.zero)
        {
            player.AnimatorController.UpdateLocomotion(0, 0);
        }
        else
        {
            if (player.IsLockOn)
            {
                Vector3 localMove = player.transform.InverseTransformDirection(moveDir);
                player.AnimatorController.UpdateLocomotion(localMove.x, localMove.z);
            }
            else
                player.AnimatorController.UpdateLocomotion(0, player.InputHandler.MoveAmount);
        }
    }

    protected override void OnAttackCommand()
    {
        if (player.TargetingSystem.IsCurrentTargetExecutable())
        {
            stateMachine.TransitionTo(stateMachine.PlayerExecuteState);
        }
        else
        {
            stateMachine.RequestedAttackData = player.Combat.FirstAttackData;
            stateMachine.TransitionTo(stateMachine.PlayerAttackState);
        }
    }

    protected override void OnDodgeCommand()
    {
        stateMachine.TransitionTo(stateMachine.PlayerDodgeState);
    }

    protected override void OnJumpCommand()
    {
        if (player.Motor.IsGrounded)
        {
            stateMachine.TransitionTo(stateMachine.PlayerJumpState);
        }
    }

    protected override void OnGuardCommand()
    {
        if (guardTimer >= parryWindowDuration)
        {
            stateMachine.TransitionTo(stateMachine.PlayerGuardState);
        }
    }

    public override void Exit()
    {
        player.Combat.CurrentDefenseType = DefenseType.None;
    }

}
