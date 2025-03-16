using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Windows;

public class PlayerController : MonoBehaviour
{
    public static PlayerController player;

    public PlayerStateMachine playerStateMachine;
    Camera playerCamera;

    public float JumpTime;
    public Rigidbody rb;
    public GameObject col;
    public Animator anim;

    [Header("Player Setting")]
    public float smoothness;
    public bool isGround;
    public float AttackTime;
    public float jumpPower;
    public float moveSpeed = 5;
    public float slideSpeed = 5;
    public float InvincibleTime = 1f;

    [Header("Player Input Values")]
    public Vector3 move;
    public Vector2 look;
    public float scroll;
    public bool jump;
    public bool sprint;
    public bool toggleCameraRotation;
    public bool attack;
    public bool guard;
    public bool interaction;

    [Header("Movement Settings")]
    public bool analogMovement;


    void Awake()
    {
        if(player == null)
        {
            player = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        playerCamera = Camera.main;
        rb = GetComponent<Rigidbody>();
        anim = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        playerStateMachine.Initialized(playerStateMachine.playerIdleState);
    }
    private void FixedUpdate()
    {
        if (anim.GetCurrentAnimatorStateInfo(0).IsName("Hit"))
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
            Vector3 playerRotate = Vector3.Scale(playerCamera.transform.forward, new Vector3(1,0,1));
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(playerRotate), Time.deltaTime * smoothness);
        }


        //플레이어 점프 착지
        if (Physics.Raycast(rb.position, Vector3.down, 0.2f) && rb.linearVelocity.y <= 1)
        {
            isGround = true;
        }
    }

    // 마우스 휠
    public void OnWheel(InputValue value)
    {
        scroll = value.Get<float>();
    }

    // W A S D 키
    public void OnMove(InputValue value)
    {
        move = value.Get<Vector3>();

    }

    // 마우스 입력
    public void OnLook(InputValue value)
    {
        look = value.Get<Vector2>();
    }

    //Space Bar 입력
    public void OnJump(InputValue value)
    {
        jump = value.isPressed;
    }

    // 마우스 왼쪽 클릭
    public void OnAttack(InputValue value)
    {
        attack = value.isPressed;
    }


    //Shift 키 입력
    public void OnSprint(InputValue value)
    {
        sprint = value.isPressed;
    }

    //Alt 키 입력
    public void OnFreeCam(InputValue value)
    {
        toggleCameraRotation = value.isPressed;
    }

    // 마우스 우클릭
    public void OnGuard(InputValue value)
    {
        guard = value.isPressed;
    }


    // F 키 입력
    public void OnInteraction(InputValue value)
    {
        interaction = value.isPressed;
    }

    public void OnShowMouse(InputValue value)
    {
        Cursor.lockState = value.isPressed ? CursorLockMode.Confined : CursorLockMode.Locked;
    }

}
