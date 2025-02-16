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



    ////�÷��̾� ���� ���ð�
    //private IEnumerator HandleJump()
    //{
    //    playerStateMachine.UnControllable(); // �Է��� ���� ���ϵ��� ����
    //    yield return new WaitForSeconds(JumpTime); //  ���
    //    playerStateMachine.Controllable(); // �ٽ� �Է��� ���� �� �ֵ��� ����
    //}
    ////�÷��̾� ���� ���ð�
    //public IEnumerator HandleAttack()
    //{
    //    playerStateMachine.UnControllable(); // �Է��� ���� ���ϵ��� ����
    //    yield return new WaitForSeconds(AttackTime);
    //    playerStateMachine.Controllable(); // �ٽ� �Է��� ���� �� �ֵ��� ����
    //}



    //// �÷��̾ �������� ��
    //public void OnMove(InputAction.CallbackContext context)
    //{
    //    if (!playerStateMachine.IsControll()) { return; }
    //    if (context.started) { playerStateMachine.TransitionTo(playerStateMachine.playerMoveState); }
    //    else if (context.canceled) { playerStateMachine.TransitionTo(playerStateMachine.playerIdleState); }
    //}


    //// �÷��̾��� ���� �Է� �ޱ�
    //public void OnJump(InputAction.CallbackContext context)
    //{
    //    if (!playerStateMachine.isTransitionPosible) { return; }
    //    if (context.started)
    //    {
    //        anim.SetBool("isJump", true);
    //        StartCoroutine(HandleJump());
    //    }
    //}

    ////�÷��̾��� ���� �Է� �ޱ�
    //public void OnAttack(InputAction.CallbackContext context)
    //{
    //    if (!playerStateMachine.isTransitionPosible) { return; }
    //    if (context.started)
    //    {
    //        playerStateMachine.TransitionTo(playerStateMachine.playerAttackState);
    //    }
    //}

}
