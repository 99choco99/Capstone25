using Unity.Netcode;
using UnityEngine;
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
        Anim = GetComponentInChildren<Animator>();
        MainCamera = Camera.main;


        if(PlayerCamera.Instance.player == null)
        {
            PlayerCamera.Instance.player = this;
            SoundManager.Instance.PlayLoopingSFX("BGM_Main");
        }

        Cursor.lockState = CursorLockMode.Locked;

    }

}