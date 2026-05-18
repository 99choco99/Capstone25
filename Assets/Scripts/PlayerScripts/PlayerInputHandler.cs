using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;

public class PlayerInputHandler : MonoBehaviour
{
    Player player;
    [SerializeField] PlayerInput PlayerInput;

    [Header("선입력 시간")]
    [SerializeField] private float inputBufferTime = 0.2f; // 입력이 유지되는 시간 (0.2초)

    private float lastJumpTime = -1f;
    private float lastAttackTime = -1f;
    private float lastDodgeTime = -1f;
    private float lastInteractionTime = -1f;
    private float lastTargetTime = -1f;

    [Header("플레이어 인풋값")]
    public Vector3 MoveInput { get; private set; }
    public Vector2 LookInput { get; private set; }
    public float MoveAmount;
    public float Scroll { get; private set; }


    [Header("단발성 인풋")]
    public bool JumpInput => Time.time - lastJumpTime <= inputBufferTime;
    public bool AttackInput => Time.time - lastAttackTime <= inputBufferTime;
    public bool DodgeInput => Time.time - lastDodgeTime <= inputBufferTime;
    public bool InteractionInput => Time.time - lastInteractionTime <= inputBufferTime;
    public bool TargetInput => Time.time - lastTargetTime <= inputBufferTime;



    [Header("지속성 인풋")]
    public bool SprintInput { get; private set; }
    public bool IsAttackPress { get; private set; }
    public bool GuardInput { get; private set; }
    public bool IsShowMouse { get; private set; }
    public bool CrouchInput { get; private set; }


    //단발성 인풋 소모
    public void UseJumpInput() => lastJumpTime = float.MinValue;
    public void UseAttackInput() => lastAttackTime = float.MinValue;
    public void UseDodgeInput() => lastDodgeTime = float.MinValue;
    public void UseInteractionInput() => lastInteractionTime = float.MinValue;
    public void UseTargetInput() => lastTargetTime = float.MinValue;

    //UI Event
    public event Action OnEscapePressed;
    public event Action OnInventoryPressed;
    public event Action OnProfilePressed;
    public event Action OnSettingPressed;
    public event Action OnQuestPressed;

    public event Action<bool> OnCursorStateChanged;

    private void Awake()
    {
        player = GetComponent<Player>();
    }

    private void Start()
    {
        PlayerInput.enabled = player.IsLocalPlayer;
    }


    public void SetCursorState(bool isUnlocked)
    {
        IsShowMouse = isUnlocked;
        Cursor.visible = isUnlocked;
        Cursor.lockState = isUnlocked? CursorLockMode.Confined : CursorLockMode.Locked;

        if (isUnlocked)
        {
            MoveInput = Vector3.zero;
            MoveAmount = 0f;
            LookInput = Vector2.zero;

            SprintInput = false;
            IsAttackPress = false;
            GuardInput = false;
            CrouchInput = false;

            UseJumpInput();
            UseAttackInput();
            UseDodgeInput();
            UseInteractionInput();
        }

        OnCursorStateChanged?.Invoke(isUnlocked);
    }


    /*=============== Input System Callbacks ===================*/
    public void OnMove(InputAction.CallbackContext context)
    {
        if (IsShowMouse) {return; }
        MoveInput = context.ReadValue<Vector3>();
        MoveAmount = Mathf.Clamp01(Mathf.Abs(MoveInput.x) + Mathf.Abs(MoveInput.z));
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        if (IsShowMouse) {return; }
        LookInput = context.ReadValue<Vector2>();
    }

    public void OnWheel(InputAction.CallbackContext context)
    {
        Scroll = -context.ReadValue<float>();
    }



    public void OnAttack(InputAction.CallbackContext context)
    {
        if (IsShowMouse) { return; }
        if (context.started)
        {
            lastAttackTime = Time.time;
            IsAttackPress = true;
        }
        else if (context.canceled)
        {
            IsAttackPress = false;
        }
    }

    public void OnGuard(InputAction.CallbackContext context)
    {
        if (IsShowMouse) { return; }
        GuardInput = context.ReadValueAsButton();
    }

    public void OnDodge(InputAction.CallbackContext context) { if (IsShowMouse) { return; } if (context.performed) lastDodgeTime = Time.time; }
    public void OnJump(InputAction.CallbackContext context) { if (IsShowMouse) { return; } if (context.started) lastJumpTime = Time.time; }
    public void OnInteraction(InputAction.CallbackContext context) { if (IsShowMouse) { return; } if (context.started) lastInteractionTime = Time.time; }
    public void OnChangeTarget(InputAction.CallbackContext context) { if (IsShowMouse) { return; } if (context.started) lastTargetTime = Time.time; }


    public void OnSprint(InputAction.CallbackContext context)
    {
        if (IsShowMouse) { return; }
        if (context.performed) { SprintInput = true; }
        else if (context.canceled) { SprintInput = false; }
    }

    public void OnShowMouse(InputAction.CallbackContext context)
    {
        if (IsShowMouse) { return; }
        if (context.started) { IsShowMouse = true; }
        else if (context.canceled) { IsShowMouse = false; }
    }

    public void OnCrouch(InputAction.CallbackContext context)
    {
        if (IsShowMouse) { return; }
        if (context.started) CrouchInput = !CrouchInput;
    }


    /* ==================== UI Events =================*/
    public void OnEscape(InputAction.CallbackContext context) { if (context.started) OnEscapePressed?.Invoke(); }
    public void OnInventory(InputAction.CallbackContext context) { if (context.started) OnInventoryPressed?.Invoke(); }
    public void OnPlayerProfile(InputAction.CallbackContext context) { if (context.started) OnProfilePressed?.Invoke(); }
    public void OnSetting(InputAction.CallbackContext context) { if (context.started) OnSettingPressed?.Invoke(); }
    public void OnQuestList(InputAction.CallbackContext context) { if (context.started) OnQuestPressed?.Invoke(); }
}
