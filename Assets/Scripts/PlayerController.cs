using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Windows;

public class PlayerController : MonoBehaviour
{

    PlayerStateMachine playerStateMachine;
    Camera playerCamera;

    public float JumpTime;
    public float AttackTime;
    public Rigidbody rb;
    public Animator anim;
    public PlayerInputs input;


    public float smoothness;


    void Awake()
    {
        playerCamera = Camera.main;
        input = GetComponentInParent<PlayerInputs>();
        rb = GetComponent<Rigidbody>();
        anim = GetComponentInChildren<Animator>();

        playerStateMachine = new PlayerStateMachine(this);
        playerStateMachine.Initialized(playerStateMachine.playerIdleState);

    }

    private void FixedUpdate()
    {
        playerStateMachine.Update();
        if(input.move.magnitude != 0) { playerStateMachine.TransitionTo(playerStateMachine.playerMoveState); }
        else { playerStateMachine.TransitionTo(playerStateMachine.playerIdleState); }
    }

    private void LateUpdate()
    {
        if (!input.toggleCameraRotation)
        {
            Vector3 playerRotate = Vector3.Scale(playerCamera.transform.forward, new Vector3(1,0,1));
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(playerRotate), Time.deltaTime * smoothness);
        }
    }



    ////플레이어 점프 대기시간
    //private IEnumerator HandleJump()
    //{
    //    playerStateMachine.UnControllable(); // 입력을 받지 못하도록 설정
    //    yield return new WaitForSeconds(JumpTime); //  대기
    //    playerStateMachine.Controllable(); // 다시 입력을 받을 수 있도록 설정
    //}
    ////플레이어 공격 대기시간
    //public IEnumerator HandleAttack()
    //{
    //    playerStateMachine.UnControllable(); // 입력을 받지 못하도록 설정
    //    yield return new WaitForSeconds(AttackTime);
    //    playerStateMachine.Controllable(); // 다시 입력을 받을 수 있도록 설정
    //}



    //// 플레이어가 움직였을 때
    //public void OnMove(InputAction.CallbackContext context)
    //{
    //    if (!playerStateMachine.IsControll()) { return; }
    //    if (context.started) { playerStateMachine.TransitionTo(playerStateMachine.playerMoveState); }
    //    else if (context.canceled) { playerStateMachine.TransitionTo(playerStateMachine.playerIdleState); }
    //}


    //// 플레이어의 점프 입력 받기
    //public void OnJump(InputAction.CallbackContext context)
    //{
    //    if (!playerStateMachine.isTransitionPosible) { return; }
    //    if (context.started)
    //    {
    //        anim.SetBool("isJump", true);
    //        StartCoroutine(HandleJump());
    //    }
    //}

    ////플레이어의 공격 입력 받기
    //public void OnAttack(InputAction.CallbackContext context)
    //{
    //    if (!playerStateMachine.isTransitionPosible) { return; }
    //    if (context.started)
    //    {
    //        playerStateMachine.TransitionTo(playerStateMachine.playerAttackState);
    //    }
    //}

}
