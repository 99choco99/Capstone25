using System;
using UnityEngine;

public class Player : MonoBehaviour
{
    [field :SerializeField] public bool IsLocalPlayer { get; private set; } = false;
    public bool IsLockOn;

    public static event Action<Transform> OnLocalPlayerSpawned;

    // 모든 핵심 컴포넌트들에 대한 공용 참조 지점
    [field: Header("Core Systems")]
    [field: SerializeField] public PlayerInputHandler InputHandler { get; private set; }
    [field: SerializeField] public PlayerMotor Motor { get; private set; }
    [field: SerializeField] public PlayerStats Stats { get; private set; }


    [field: Header("Combat")]
    [field: SerializeField] public PlayerCombat Combat { get; private set; }
    [field: SerializeField] public PlayerExecution Execution { get; private set; } 
    [field: SerializeField] public TargetingSystem TargetingSystem { get; private set; }

    [field: Header("Animation")]
    [field: SerializeField] public PlayerAnimationController AnimatorController { get; private set; }
    [field: SerializeField] public Animator Anim { get; private set; }

    [field: Header("Interaction")]
    [field: SerializeField] public PlayerInteraction Interaction { get; private set; }


    public PlayerStateMachine StateMachine { get; private set; }
    public InventoryManager Inventory { get; private set; } 
    public QuestManager Quest { get; private set; }

    public void Init(bool isLocal)
    {
        IsLocalPlayer = isLocal;
        Anim = GetComponentInChildren<Animator>();
        if (IsLocalPlayer)
        {
            InjectBasicStats();
            CreateSystem();
            InjectProgressData();
            WireSystem();

            OnLocalPlayerSpawned?.Invoke(transform);
            Cursor.lockState = CursorLockMode.Locked;
        }
    }


    private void InjectBasicStats()
    {
        PlayerData playerData = DataManager.Instance.Server_PlayerData;
        if (playerData != null)
        {
            Stats.LoadPlayerData(playerData);
        }
    }

    private void CreateSystem()
    {
        StateMachine = new PlayerStateMachine(this);
        Inventory = new InventoryManager(this);
        Quest = new QuestManager(Stats.Level);
    }
    private void InjectProgressData()
    {
        QuestData questData = DataManager.Instance.Server_QuestData;
        if (questData != null)
        {
            Quest.LoadQuestData(questData.questTemplate, questData.questProgress, null);
        }
    }

    private void WireSystem()
    {

        Stats.OnLevelUp += Quest.SyncPlayerLevel;

        //Quest.OnItemRewardEarned += (itemId) => Inventory.AddItem(itemId);
        //Inventory.OnItemCollected += (itemId, amount) => Quest.HandleCollectItem(itemId, amount);
    }


    private void Update()
    {
        if (!IsLocalPlayer) return; // 로컬 플레이어만 조작 가능
        StateMachine?.Update();
    }

    private void FixedUpdate()
    {
        if (!IsLocalPlayer) return;
        StateMachine?.FixedUpdate();
    }
}