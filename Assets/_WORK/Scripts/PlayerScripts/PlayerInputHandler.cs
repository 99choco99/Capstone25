using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class PlayerInputHandler : MonoBehaviour
{
    [SerializeField] PlayerInput PlayerInput;
    public PlayerInputBuffer Buffer { get; } = new PlayerInputBuffer();

    [Header("플레이어 인풋값")]
    public Vector3 MoveInput { get; private set; }
    public float MoveAmount;
    public Vector2 LookInput { get; private set; }
    public float Scroll { get; private set; }
    public bool SprintInput { get; private set; }
    public bool GuardInput { get; private set; }


    public event Action OnInteractionPressed;
    public event Action<bool> OnCursorStateChanged;
    public event Action OnTargetPressed;

    //UI Event
    public event Action OnEscapePressed;
    public event Action OnInventoryPressed;
    public event Action OnProfilePressed;
    public event Action OnSettingPressed;
    public event Action OnQuestPressed;
    public event Action OnDialogueNextPressed;

    private void Awake()
    {
        PlayerInput = GetComponent<PlayerInput>();
    }

    public void SwitchToGameplayMode()
    {
        PlayerInput.SwitchCurrentActionMap("Player");
        OnCursorStateChanged?.Invoke(false);
        ClearAllInputs();
    }

    public void SwitchToUIMode()
    {
        PlayerInput.SwitchCurrentActionMap("UI");
        OnCursorStateChanged?.Invoke(true);
        ClearAllInputs();
    }

    /// <summary>
    ///  입력값 초기화 로직
    /// </summary>
    private void ClearAllInputs()
    {
        MoveInput = Vector3.zero;
        MoveAmount = 0f;
        SprintInput = false; GuardInput = false;
    }


    /*=============== Input System Callbacks ===================*/
    public void OnMove(InputAction.CallbackContext context)
    {
        MoveInput = context.ReadValue<Vector3>();
        MoveAmount = Mathf.Clamp01(Mathf.Abs(MoveInput.x) + Mathf.Abs(MoveInput.z));
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        LookInput = context.ReadValue<Vector2>();
    }

    public void OnWheel(InputAction.CallbackContext context)
    {
        Scroll = -context.ReadValue<float>();
    }




    public void OnGuard(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            GuardInput = true;
            Buffer.AddCommand(ActionCommand.Guard);
        }
        else if (context.canceled)
        {
            GuardInput = false;
        }

    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (context.started) Buffer.AddCommand(ActionCommand.Attack);
    }
    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.started) Buffer.AddCommand(ActionCommand.Jump);
    }
    public void OnInteraction(InputAction.CallbackContext context) { 
        if (context.started)
        {
            OnInteractionPressed?.Invoke();
        }
    }
    public void OnChangeTarget(InputAction.CallbackContext context) { 
        if (context.started)
        {
            OnTargetPressed?.Invoke();
        }

    }


    public void OnDodge(InputAction.CallbackContext context)
    {
        if (context.performed) Buffer.AddCommand(ActionCommand.Dodge);
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        if (context.performed) SprintInput = true;
        else if (context.canceled) SprintInput = false;
    }


    /* ==================== UI Events =================*/
    public void OnEscape(InputAction.CallbackContext context) { if (context.performed) OnEscapePressed?.Invoke(); }
    public void OnInventory(InputAction.CallbackContext context) { if (context.performed) OnInventoryPressed?.Invoke(); }
    public void OnPlayerProfile(InputAction.CallbackContext context) { if (context.performed) OnProfilePressed?.Invoke(); }
    public void OnSetting(InputAction.CallbackContext context) { if (context.performed) OnSettingPressed?.Invoke(); }
    public void OnQuestList(InputAction.CallbackContext context) { if (context.performed) OnQuestPressed?.Invoke(); }

    public void OnDialogueNext(InputAction.CallbackContext context) { if (context.performed) OnDialogueNextPressed?.Invoke(); }
}
