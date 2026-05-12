using System;
using UnityEngine;

public class Player : MonoBehaviour
{
    public bool IsLocalPlayer { get; private set; } = false;
    public bool IsLockOn;

    public static event Action<Transform> OnLocalPlayerSpawned;

    // 모든 핵심 컴포넌트들에 대한 공용 참조 지점
    [field: Header("Core Systems")]
    [field: SerializeField] public PlayerInputHandler InputHandler { get; private set; }
    [field: SerializeField] public PlayerStateMachine StateMachine { get; private set; }
    [field: SerializeField] public PlayerMotor Motor { get; private set; }
    [field: SerializeField] public PlayerStats Stats { get; private set; }


    [field: Header("Combat & Interaction")]
    [field: SerializeField] public PlayerCombat Combat { get; private set; }
    [field: SerializeField] public PlayerExecution Execution { get; private set; } 
    [field: SerializeField] public TargetingSystem TargetingSystem { get; private set; }
    [field: SerializeField] public PlayerInteraction Interaction { get; private set; }
    [field: SerializeField] public PlayerAnimatorManager AnimatorManager { get; private set; }
    [field: SerializeField] public Animator Anim { get; private set; }

    public void Init(bool isLocal)
    {
        IsLocalPlayer = isLocal;
        if (IsLocalPlayer)
        {
            OnLocalPlayerSpawned?.Invoke(this.transform);
            Cursor.lockState = CursorLockMode.Locked;
        }
    }
}