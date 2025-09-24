using Unity.Netcode;
using UnityEngine;
public class Player : MonoBehaviour
{
    public bool IsLocalPlayer { get; set; } = false;

    // 모든 핵심 컴포넌트들에 대한 공용 참조 지점
    public PlayerInputHandler InputHandler { get; private set; }   //플레이어의 입력
    public PlayerStateMachine StateMachine { get; private set; }   //플레이어의 논리적인 상태전환 정의
    public PlayerMotor Motor { get; private set; }                 //플레이어의 시각적 움직임 정의
    public PlayerStats Stats { get; private set; }                 //플레이어의 기본적인 스탯 정의
    public PlayerInteraction Interaction { get; private set; }
    public TargetingSystem TargetingSystem { get; private set; }
    public PlayerCombat Combat { get; private set; }
    public Animator Anim { get; private set; }
    public Camera MainCamera { get; private set; }

    void Awake()
    {
        InputHandler = GetComponent<PlayerInputHandler>();
        StateMachine = GetComponent<PlayerStateMachine>();
        Motor = GetComponent<PlayerMotor>();
        Stats = GetComponent<PlayerStats>();
        Combat = GetComponent<PlayerCombat>();
        TargetingSystem = GetComponent<TargetingSystem>();
        Anim = GetComponentInChildren<Animator>();
        MainCamera = Camera.main;

        PlayerCamera.Instance.player = this;
    }


}