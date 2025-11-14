using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;

public class PlayerInputHandler : MonoBehaviour
{
    Player player;
    [SerializeField] PlayerInput PlayerInput;

    [Header("PlayerSetting Input Values")]
    public Vector3 MoveInput { get; private set; }
    public float horizontalInput;
    public float verticalInput;
    public float moveAmount;

    public Vector2 LookInput { get; private set; }
    public bool JumpInput { get; private set; }
    public bool AttackInput { get; private set; }
    public bool GuardInput { get; private set; }
    public bool DodgeInput { get; private set; }
    public bool SprintInput { get; private set; }
    public bool IsShowMouse { get; private set; }
    public bool InteractionInput { get; private set; }
    public bool TargetInput { get; private set; }
    public bool CrouchInput { get; private set; }
    public bool IsAttackPress { get; private set; }
    public float Scroll { get; private set; }


    public void UseDodgeInput() => DodgeInput = false;
    public void UseJumpInput() => JumpInput = false;
    public void UseAttackInput() => AttackInput = false;
    public void UseInteractionInput() => InteractionInput = false;
    public void UseTargetInput() => TargetInput = false;

    bool blockInputAfterMapSwitch;

    private void Awake()
    {
        player = GetComponent<Player>();
    }

    private void Start()
    {
        if (player.IsLocalPlayer) { 
            PlayerInput.enabled = true; 
            GameManager.instance.OnGameStateChanged += HandlerGameStateChanged; 
        }
        else {  PlayerInput.enabled = false; }
    }

    private void OnDestroy()
    {
        if (!player.IsLocalPlayer) { return; }
        if (GameManager.instance != null)
        {
            GameManager.instance.OnGameStateChanged -= HandlerGameStateChanged;
        }
    }




    // 매 프레임 한번만 true가 되도록 처리하기 위함
    private void LateUpdate()
    {
        DodgeInput = false;
        TargetInput = false;
        JumpInput = false;
        AttackInput = false;
        InteractionInput = false;
    }

    void HandlerGameStateChanged(GameState state)
    {
        if(state == GameState.Gameplay)
        {
            PlayerInput.SwitchCurrentActionMap("Player");
        }
        else
        {
            PlayerInput.SwitchCurrentActionMap("UI");
        }
        blockInputAfterMapSwitch = true;
    }




    public void OnMove(InputAction.CallbackContext context)
    {
        MoveInput = context.ReadValue<Vector3>();

        horizontalInput = MoveInput.x;
        verticalInput = MoveInput.z;

        moveAmount = Mathf.Clamp01(Mathf.Abs(horizontalInput) + Mathf.Abs(verticalInput));
        if (moveAmount >= 0.5f)
        {
            moveAmount = 1f;
        }
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        if (IsShowMouse) { LookInput = default; return; }
        LookInput = context.ReadValue<Vector2>();
    }

    public void OnWheel(InputAction.CallbackContext context)
    {
        Scroll = -context.ReadValue<float>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.started)
            JumpInput = true;
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (IsShowMouse) return;

        if (context.started)
        {
            AttackInput = true;
            IsAttackPress = true;
        }
        else if (context.canceled)
        {
            IsAttackPress = false;
        }
    }

    public void OnGuard(InputAction.CallbackContext context)
    {
        if (IsShowMouse) return;
        GuardInput = context.ReadValueAsButton();
    }

    public void OnDodge(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            DodgeInput = true;
        }
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        if (context.performed) { SprintInput = true; }
        else if (context.canceled) { SprintInput = false; }
    }

    public void OnShowMouse(InputAction.CallbackContext context)
    {
        if (context.started) { IsShowMouse = true; }
        else if (context.canceled) { IsShowMouse = false; }
    }

    public void OnInteraction(InputAction.CallbackContext context)
    {
        if (blockInputAfterMapSwitch)
        {
            blockInputAfterMapSwitch = false;
            return;
        }
        if (context.started)
        {
            InteractionInput = true;
        }
    }

    public void OnMarketUIClose(InputAction.CallbackContext context)
    {
        if (context.started && player.PlayerUIManager.IsPanelOpen(UIPanelType.Market))
        {
            player.PlayerUIManager.CloseUI(UIPanelType.Market);
        }
    }

    public void OnCrouch(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            CrouchInput = !CrouchInput;
        }
    }

    public void OnChangeTarget(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Started)
        {
            TargetInput = true;
        }
    }




    public void OnEscape(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Started)
        {
            player.PlayerUIManager.CloseLastUI();
        }
    }



    public void OnInventory(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Started)
        {
            player.PlayerUIManager.ToggleUI(UIPanelType.Inventory);
        }
    }

    public void OnPlayerProfile(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Started)
        {
            player.PlayerUIManager.ToggleUI(UIPanelType.Profile);
        }
    }

    public void OnSetting(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Started)
        {
            player.PlayerUIManager.ToggleUI(UIPanelType.Setting);
        }
    }


    //J 버튼을 통해 퀘스트 UI 실행
    public void OnQuestList(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Started)
        {
            player.PlayerUIManager.ToggleUI(UIPanelType.Quest);
        }
    }
}
