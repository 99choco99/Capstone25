using Unity.Netcode;
using UnityEngine;
using UnityEngine.Playables;
public class Player : MonoBehaviour
{
    public bool IsLocalPlayer { get; set; } = false;
    public bool isLockOn;

    // 모든 핵심 컴포넌트들에 대한 공용 참조 지점
    public PlayerInputHandler InputHandler { get; private set; }   //플레이어의 입력
    public PlayerStateMachine StateMachine { get; private set; }   //플레이어의 논리적인 상태전환 정의
    public PlayerMotor Motor { get; private set; }                 //플레이어의 시각적 움직임 정의
    public PlayerStats Stats { get; private set; }                 //플레이어의 기본적인 스탯 정의
    public PlayerInteraction Interaction { get; private set; }     //플레이어의 상호작용 정의
    public TargetingSystem TargetingSystem { get; private set; }   //플레이어의 타겟 선정을 정리
    public PlayerCombat Combat { get; private set; }               //플레이어의 전투 관련 정의.
    public PlayerAnimatorManager animatorManager {get;private set;}//플레이어의 애니메이션 정의.
    public InventoryManager Inventory { get; private set; }        //플레이어의 인벤토리 매니저
    public QuestManager Quest { get; private set; }
    public DialogueManager Dialogue { get; private set; }
    public EquipmentManager Equipment { get; private set; }
    public LocalAPIManager localAPI { get; private set; }
    
    public Animator Anim { get; private set; }
    public Camera MainCamera { get; private set; }

    void Awake()
    {
        InputHandler = GetComponent<PlayerInputHandler>();
        StateMachine = GetComponent<PlayerStateMachine>();
        animatorManager = GetComponent<PlayerAnimatorManager>();
        Motor = GetComponent<PlayerMotor>();
        Stats = GetComponent<PlayerStats>();
        Combat = GetComponent<PlayerCombat>();
        TargetingSystem = GetComponent<TargetingSystem>();
        Interaction = GetComponent<PlayerInteraction>();
        localAPI = GetComponent<LocalAPIManager>();
        Inventory = GetComponentInChildren<InventoryManager>();
        Quest = GetComponentInChildren<QuestManager>();
        Dialogue = GetComponentInChildren<DialogueManager>();
        Equipment = GetComponentInChildren<EquipmentManager>();
        Anim = GetComponentInChildren<Animator>();
        MainCamera = Camera.main;

        
    }

    private void Start()
    {
        if (IsLocalPlayer)
        {
            if (PlayerCamera.Instance != null && PlayerCamera.Instance.player == null)
            {
                PlayerCamera.Instance.player = this;
            }

            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayBGM("BGM_Main");
            }

            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    private void OnDestroy()
    {
        // 내가 로컬 플레이어일 경우에만
        if (IsLocalPlayer)
        {
            if (DataManager.Instance != null)
            {
                // DataManager에게 내 참조를 모두 null로 만들라고 알림
                DataManager.Instance.Unregister();
            }
        }
    }

}