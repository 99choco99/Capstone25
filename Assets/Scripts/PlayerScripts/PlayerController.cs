using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Windows;

public class PlayerController : MonoBehaviour
{

    public PlayerStateMachine playerStateMachine;
    Camera playerCamera;

    public float JumpTime;
    public Rigidbody rb;
    public Animator anim;

    [Header("Player Setting")]
    public float smoothness;
    public bool isGround;
    public float AttackTime;
    public float jumpPower;
    public float moveSpeed = 5;
    public float slideSpeed = 5;

    [Header("Player Input Values")]
    public Vector3 move;
    public Vector2 look;
    public bool jump;
    public bool sprint;
    public bool toggleCameraRotation;
    public bool attack;
    public bool guard;
    public bool interaction;

    [Header("Movement Settings")]
    public bool analogMovement;

    [Header("Mouse Cursor Settings")]
    public bool cursorLocked = true;
    public bool cursorInputForLook = true;

    void Awake()
    {
        playerCamera = Camera.main;
        rb = GetComponent<Rigidbody>();
        anim = GetComponentInChildren<Animator>();

        playerStateMachine = new PlayerStateMachine(this);
        playerStateMachine.Initialized(playerStateMachine.playerIdleState);

    }

    private void FixedUpdate()
    {
        playerStateMachine.Update();

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

    private void OnCollisionEnter(Collision collision)
    {
        //충돌?
    }


    public void OnMove(InputValue value)
    {
        move = value.Get<Vector3>();

    }

    public void OnLook(InputValue value)
    {
        if (cursorInputForLook)
        {
            look = value.Get<Vector2>();
        }
    }

    public void OnJump(InputValue value)
    {
        jump = value.isPressed;
    }

    public void OnAttack(InputValue value)
    {
        attack = value.isPressed;
    }

    public void OnSprint(InputValue value)
    {
        sprint = value.isPressed;
    }

    public void OnFreeCam(InputValue value)
    {
        toggleCameraRotation = value.isPressed;
    }

    public void OnGuard(InputValue value)
    {
        guard = value.isPressed;
    }

    public void OnInteraction(InputValue value)
    {
        interaction = value.isPressed;
    }


    private void OnApplicationFocus(bool hasFocus)
    {
        SetCursorState(cursorLocked);
    }

    private void SetCursorState(bool newState)
    {
        Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
    }
}
