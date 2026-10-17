using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class PlayerInputHandler : MonoBehaviour
{
    public const string GameplayActionMapName = "Player";
    public const string UIActionMapName = "UI";

    private PlayerInput PlayerInput;
    public PlayerInputBuffer Buffer { get; } = new PlayerInputBuffer();

    [Header("플레이어 인풋값")]
    public Vector3 MoveInput { get; private set; }
    public float MoveAmount => Mathf.Clamp01(Mathf.Abs(MoveInput.x) + Mathf.Abs(MoveInput.z));
    public Vector2 LookInput { get; private set; }
    public bool IsPointerLookInput { get; private set; }
    public bool SprintInput { get; private set; }
    public bool GuardInput { get; private set; }
    public float LastGuardPressedTime { get; private set; } = float.NegativeInfinity;
    public float LastGuardPressedGameTime { get; private set; } = float.NegativeInfinity;
    public uint InputResetVersion { get; private set; }
    public uint GuardPressVersion { get; private set; }

    public event Action OnInteractionPressed;
    public event Action<bool> OnCursorStateChanged;
    public event Action OnTargetPressed;

    //UI EnterAction
    public event Action OnUIInteractionPressed;
    public event Action OnEscapePressed;
    public event Action OnProfilePressed;
    public event Action OnSettingPressed;

    //============ 초기화 ============

    private void Awake()
    {
        PlayerInput = GetComponent<PlayerInput>();
    }

    //============ 입력 모드 전환과 초기화 ============

    public void SwitchToGameplayMode()
    {
        if (!PlayerInput.isActiveAndEnabled)
            return;
        SwitchActionMap(GameplayActionMapName, showCursor: false);
    }

    public void SwitchToUIMode()
    {
        SwitchActionMap(UIActionMapName, showCursor: true);
    }

    public void SetInputEnabled(bool isEnabled)
    {
        if (isEnabled)
        {
            PlayerInput.ActivateInput();
        }
        else
        {
            PlayerInput.DeactivateInput();
            ClearAllInputs();
        }
    }

    private void SwitchActionMap(string actionMapName, bool showCursor)
    {
        PlayerInput.SwitchCurrentActionMap(actionMapName);
        OnCursorStateChanged?.Invoke(showCursor);
        ClearAllInputs();
    }

    /// <summary>
    ///  입력값 초기화 로직
    /// </summary>
    private void ClearAllInputs()
    {
        MoveInput = Vector3.zero;
        LookInput = Vector2.zero;
        SprintInput = false;
        GuardInput = false;
        LastGuardPressedTime = float.NegativeInfinity;
        LastGuardPressedGameTime = float.NegativeInfinity;
        Buffer.Clear();
        // 상태가 보관한 입력도 전체 초기화 이후에는 사용할 수 없습니다.
        InputResetVersion = unchecked(InputResetVersion + 1u);
    }

    //============ 이동과 시점 입력 ============

    public void OnMove(InputAction.CallbackContext context)
    {
        MoveInput = context.ReadValue<Vector3>();
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        LookInput = context.ReadValue<Vector2>();
        IsPointerLookInput = context.control.device is Pointer;
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        if (context.performed)
            SprintInput = true;
        else if (context.canceled)
            SprintInput = false;
    }

    //============ 전투 입력 ============

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (context.started)
            Buffer.AddCommand(ActionCommand.Attack);
    }

    public void OnGuard(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            GuardInput = true;
            LastGuardPressedTime = Time.unscaledTime;
            LastGuardPressedGameTime = Time.time;
            GuardPressVersion++;
            Buffer.AddCommand(ActionCommand.Guard);
        }
        else if (context.canceled)
        {
            GuardInput = false;
        }
    }

    public void OnDodge(InputAction.CallbackContext context)
    {
        if (context.performed)
            Buffer.AddCommand(ActionCommand.Dodge);
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.started)
            Buffer.AddCommand(ActionCommand.Jump);
    }

    //============ 상호작용과 락온 입력 ============

    public void OnInteraction(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            // 대화를 시작한 F 입력으로 첫 대사까지 넘기지 않도록, 입력이 발생한 맵으로 구분합니다.
            if (context.action.actionMap.name == UIActionMapName)
                OnUIInteractionPressed?.Invoke();
            else
                OnInteractionPressed?.Invoke();
        }
    }

    public void OnChangeTarget(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            OnTargetPressed?.Invoke();
        }
    }

    //============ UI 입력 ============

    public void OnEscape(InputAction.CallbackContext context)
    {
        if (context.performed)
            OnEscapePressed?.Invoke();
    }

    public void OnPlayerProfile(InputAction.CallbackContext context)
    {
        if (context.performed)
            OnProfilePressed?.Invoke();
    }

    public void OnSetting(InputAction.CallbackContext context)
    {
        if (context.performed)
            OnSettingPressed?.Invoke();
    }
}
