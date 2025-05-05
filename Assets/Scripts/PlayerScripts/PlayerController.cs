using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Windows;

public class PlayerController : NetworkBehaviour
{
    public PlayerStateMachine playerStateMachine;
    public PlayerInteraction playerInteraction;
    public PlayerInput playerInput;
    public Camera playerCamera;

    public float JumpTime;
    public Rigidbody rb;
    public Animator anim;
    public GameObject col;
    public PlayerData player;

    [Header("PlayerData Setting")]
    public float smoothness; // alt시 카메라 회전 속도
    public bool isGround; // 땅에 착지 했는가
    public float AttackTime;  // 공격 간격
    public float jumpPower;  // 점프 힘
    public float moveSpeed = 5;  // 이동속도
    public float slideSpeed = 5;  // 슬라이딩 속도
    public float InvincibleTime = 1f;  // 피격시 무적 시간

    [Header("PlayerData Input Values")]
    public Vector3 move;  // wasd 키
    public Vector2 look;  // 마우스
    public float scroll;  // 마우스 휠
    public bool jump;   // 스페이스 바 
    public bool sprint; //슬라이딩 왼쪽 Shift
    public bool toggleCameraRotation;  // alt키
    public bool attack; // 마우싀 좌클릭
    public bool guard;  // 마우스 우클릭
    public bool interaction;  // 상호작용 F키
    public bool isShowMouse;  // 마우스 보임, ctrl 키

    [Header("Movement Settings")]
    public bool analogMovement;

    const float SENSEGROUND = 0.4f;

    void Awake()
    {
        playerStateMachine = GetComponent<PlayerStateMachine>();
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
        player = GetComponent<PlayerData>();
        playerInput = GetComponent<PlayerInput>();
        playerInteraction = GetComponent<PlayerInteraction>();
        playerCamera = Camera.main;
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        playerStateMachine.Initialized(playerStateMachine.playerMoveState);
    }

    private void FixedUpdate()
    {
        //플레이어 점프 착지
        if (Physics.Raycast(rb.position, Vector3.down, SENSEGROUND) && rb.linearVelocity.y <= 1)
        {
            isGround = true;
        }
        if (player.dead)
        {
            playerStateMachine.TransitionTo(playerStateMachine.playerDeadState);
        }
        else if (!guard && player.Ishit)
        {
            playerStateMachine.TransitionTo(playerStateMachine.playerDamagedState);
        }
        playerStateMachine.StateUpdate();
    }

    private void LateUpdate()
    {
        // alt키 누르면 카메라 자유 회전
        if (!toggleCameraRotation)
        {
            Vector3 playerRotate = Vector3.Scale(playerCamera.transform.forward, new Vector3(1, 0, 1));
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(playerRotate), Time.deltaTime * smoothness);
        }
    }

    // 마우스 휠
    public void OnWheel(InputAction.CallbackContext context)
    {

        scroll = -context.ReadValue<float>();
    }

    // W A S D 키
    public void OnMove(InputAction.CallbackContext context)
    {
        move = context.ReadValue<Vector3>();
    }


    // 마우스 입력
    public void OnLook(InputAction.CallbackContext context)
    {
        if (isShowMouse) { return; } // ctrl키 누를 시 캐릭터 회전 안함.
        look = context.ReadValue<Vector2>();
    }

    //Space Bar 입력
    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.started) { jump = true; }
        if (context.canceled) { jump = false; }
    }

    // 마우스 왼쪽 클릭
    public void OnAttack(InputAction.CallbackContext context)
    {
        if (isShowMouse) { return; }
        if (context.phase == InputActionPhase.Started) { attack = true; }
        else if(context.phase == InputActionPhase.Canceled) { attack = false; }

    }

    //Shift 키 입력
    public void OnSprint(InputAction.CallbackContext context)
    {
        if (isShowMouse) { return; }
        if (context.phase == InputActionPhase.Started) { sprint = true; }
        else if (context.phase == InputActionPhase.Canceled) { sprint = false; }
    }

    //Alt 키 입력
    public void OnFreeCam(InputAction.CallbackContext context)
    {
        if(playerStateMachine.CurrentState == playerStateMachine.playerConversationState) { return; }
        if (context.phase == InputActionPhase.Started) { toggleCameraRotation = true; }
        else if (context.phase == InputActionPhase.Canceled) { toggleCameraRotation = false; }
    }

    // 마우스 우클릭
    public void OnGuard(InputAction.CallbackContext context)
    {
        if (isShowMouse || toggleCameraRotation) { return; }
        if (context.phase == InputActionPhase.Started) { guard = true; }
        else if (context.phase == InputActionPhase.Canceled) { guard = false; }
    }

    // F 키 입력, 상호작용 버튼
    public void OnInteraction(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Started) { interaction = true; }
        else if (context.phase == InputActionPhase.Canceled) { interaction = false; }
    }

    public void OnShowMouse(InputAction.CallbackContext context)
    {
        if (context.started) { isShowMouse = true; } if(context.canceled){ isShowMouse = false; }
        Cursor.lockState = context.performed ? CursorLockMode.Confined : CursorLockMode.Locked;
    }

}