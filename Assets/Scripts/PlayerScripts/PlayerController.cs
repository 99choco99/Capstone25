using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Windows;

public enum PlayerState { Idle,Move,Jump,Attack,Guard,Damaged}
public class PlayerController : NetworkBehaviour
{
    public Rigidbody playerDetectEnemy;
    public PlayerInteraction playerInteraction;
    public PlayerInput playerInput;
    public Camera playerCamera;
    public PlayerState currentState;

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
    public float pressedTime;
    public float AttackRange;
    public float sprintSpeed;

    [Header("PlayerData Input Values")]
    public Vector3 move;  // wasd 키
    public Vector2 look;  // 마우스
    public float scroll;  // 마우스 휠
    public bool jump;   // 스페이스 바 
    public bool sprint; //왼쪽 Shift
    public bool toggleCameraRotation;  // alt키
    public bool attack; // 마우싀 좌클릭
    public bool guard;  // 마우스 우클릭
    public bool interaction;  // 상호작용 F키

    public bool crouch;  // 숙이기 ctrl
    public bool isAttackPress;

    public Vector3 CameraMovement;


    void Awake()
    {
        playerDetectEnemy = GetComponent<Rigidbody>();
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
        player = GetComponent<PlayerData>();
        playerInput = GetComponent<PlayerInput>();
        playerInteraction = GetComponent<PlayerInteraction>();
        playerCamera = Camera.main;
    }

    private void Start()
    {

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

        if (context.phase == InputActionPhase.Started) { 
            attack = true;
            isAttackPress = true;
        }
        else if (context.phase == InputActionPhase.Canceled) {
            attack = false;
            isAttackPress = false;
            pressedTime = 0;
        }


    }

    // 마우스 우클릭
    public void OnGuard(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Started) { guard = true; }
        else if (context.phase == InputActionPhase.Canceled) { guard = false; }
    }

    //Shift 키 입력
    public void OnSprint(InputAction.CallbackContext context)
    {

        if (context.phase == InputActionPhase.Started) { sprint = true; }
        else if (context.phase == InputActionPhase.Canceled) { sprint = false; }
    }

    //Alt 키 입력
    public void OnFreeCam(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Started) { toggleCameraRotation = true; }
        else if (context.phase == InputActionPhase.Canceled) { toggleCameraRotation = false; }
    }


    // F 키 입력, 상호작용 버튼
    public void OnInteraction(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Started) { interaction = true; }
        else if (context.phase == InputActionPhase.Canceled) { interaction = false; }
    }

    public void OnShowMouse(InputAction.CallbackContext context)
    {

        Cursor.lockState = context.performed ? CursorLockMode.Confined : CursorLockMode.Locked;
    }

}