using System;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public Transform cameraRoot;
    public bool IsLocalPlayer { get; private set; } = false;
    public static event Action<Player> OnLocalPlayerSpawned;


    // 모든 핵심 컴포넌트들에 대한 공용 참조 지점
    [field: Header("Core Systems")]
    [field: SerializeField] public PlayerInputHandler InputHandler { get; private set; }
    public PlayerInputBuffer InputBuffer => InputHandler.Buffer;
    [field: SerializeField] public PlayerMotor Motor { get; private set; }
    [field: SerializeField] public PlayerStats Stats { get; private set; }

    [field: Header("Combat")]
    [field: SerializeField] public PlayerCombat Combat { get; private set; }
    [field: SerializeField] public PlayerExecution Execution { get; private set; } 
    [field: SerializeField] public TargetingSystem TargetingSystem { get; private set; }

    [field: Header("Animation")]
    [field: SerializeField] public AnimationController AnimatorController { get; private set; }

    [field: Header("Interaction")]
    [field: SerializeField] public PlayerInteraction Interaction { get; private set; }


    public PlayerStateMachine StateMachine { get; private set; }
    public InventoryManager Inventory { get; private set; }
    public PlayerQuestController Quest { get; private set; }



    public bool IsLockOn => TargetingSystem.CurrentTarget != null;

    public void SetInvincible(bool isInvincible) { Stats.IsInvincible = isInvincible; }

    public void Init(bool isLocal)
    {
        IsLocalPlayer = isLocal;

        if (InputHandler.TryGetComponent<UnityEngine.InputSystem.PlayerInput>(out var unityInput))
        {
            unityInput.enabled = isLocal;
        }


        if (IsLocalPlayer)
        {
            CreateSystem();
            InjectBasicStats();
            WireSystem();

            OnLocalPlayerSpawned?.Invoke(this);
        }
    }


    private void CreateSystem()
    {
        StateMachine = new PlayerStateMachine(this);
        Inventory = new InventoryManager();
        Quest = new PlayerQuestController();
    }

    private void InjectBasicStats()
    {
        PlayerData playerData = DataManager.Instance.Server_PlayerData;
        if (playerData != null)
        {
            Stats.LoadPlayerData(playerData);
        }

        List<QuestProgress> data = DataManager.Instance.Server_QuestProgress;
        if (data != null)
        {
            Quest.LoadQuestData(data);
        }
    }

    private void WireSystem()
    {
        InputHandler.OnTargetPressed += TargetingSystem.ToggleTarget;
        InputHandler.OnInteractionPressed += Interaction.ExecuteInteraction;

        Stats.OnLevelUp += Quest.SyncPlayerLevel;
        Stats.OnDamaged += HandleDamageRecieved;
        Stats.OnDeath += HandleDeath;

        Inventory.OnEquipmentChanged += Stats.RecalculateEquipmentStats;

        DialogueManager.Instance.OnConversationStart += HandleConversationStart;
        DialogueManager.Instance.OnConversationEnd += HandleConversationEnd;

        Quest.OnStatRewardEarned += HandleStatReward;
        Quest.OnItemRewardEarned += HandleItemReward;
        Quest.CheckInventorySpace = HandleCheckInventorySpace;
        Quest.OnRewardFailed_InventoryFull += HandleInventoryFullWarning;
    }

    private void HandleDamageRecieved(DamageEvent damageEvent)
    {
        if (Stats.IsDead || Stats.IsInvincible) return;
        StateMachine.CurrentState.HandleDamage(damageEvent);
    }

    private void HandleDeath() => StateMachine.TransitionTo(StateMachine.PlayerDeadState);
    private void HandleConversationStart() => StateMachine.TransitionTo(StateMachine.ConversationState);
    private void HandleConversationEnd() => StateMachine.TransitionTo(StateMachine.PlayerGroundedState);

    private void HandleStatReward(int exp, int gold)
    {
        Stats.AddExp(exp);         // PlayerStats에 경험치 추가 로직
        //Inventory.AddGold(gold); // 재화를 인벤토리(또는 지갑)에 추가
    }

    private void HandleItemReward(int itemId, int count)
    {
        Inventory.AddItem(itemId, count); // 이미 검증된 아이템이므로 안전하게 들어감!
    }
    private bool HandleCheckInventorySpace(int itemId)
    {
        ItemBase data = ItemManager.Instance.GetItem(itemId);
        if (data == null) return false;
        return Inventory.FindEmptySlot(data.type) != null;
    }

    private void HandleInventoryFullWarning()
    {
        Debug.LogWarning("인벤토리가 꽉 차서 퀘스트를 완료할 수 없습니다!");
        // TODO: MainUIManager.Instance.ShowSystemMessage("인벤토리 공간이 부족합니다.");
    }


    public Vector3 GetDesiredMoveDirection()
    {
        Transform camTransform = Camera.main.transform;
        Vector3 camForward = camTransform.forward; camForward.y = 0f;
        Vector3 camRight = camTransform.right; camRight.y = 0f;

        // 카메라 기준 키보드 입력 방향 계산
        return (camForward.normalized * InputHandler.MoveInput.z + camRight.normalized * InputHandler.MoveInput.x).normalized;
    }



    private void Update()
    {
        if (!IsLocalPlayer) return; // 로컬 플레이어만 조작 가능

        if (IsLockOn)
        {
            TargetingSystem.SwitchTargetUpdates(InputHandler.LookInput.x);
        }

        StateMachine?.Update();
    }

    private void OnDestroy()
    {
        UnwireSystem();
    }
    private void UnwireSystem()
    {
        if (InputHandler != null)
        {

            InputHandler.OnTargetPressed -= TargetingSystem.ToggleTarget;
            InputHandler.OnInteractionPressed -= Interaction.ExecuteInteraction;
        }
        if (Stats != null && Combat != null && Quest != null)
        {
            Stats.OnDeath -= Combat.ForceResetAttackState;
            Stats.OnLevelUp -= Quest.SyncPlayerLevel;
            Stats.OnDamaged -= HandleDamageRecieved;
        }
        if (Inventory != null)
            Inventory.OnEquipmentChanged -= Stats.RecalculateEquipmentStats;

        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnConversationStart -= HandleConversationStart;
            DialogueManager.Instance.OnConversationEnd -= HandleConversationEnd;
        }
        if (Quest != null)
        {
            Quest.OnStatRewardEarned -= HandleStatReward;
            Quest.OnItemRewardEarned -= HandleItemReward;
            Quest.CheckInventorySpace = null;
            Quest.OnRewardFailed_InventoryFull -= HandleInventoryFullWarning;
        }
    }
}