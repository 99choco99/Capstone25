using Unity.Netcode;
using UnityEngine;
public class Player : MonoBehaviour
{
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
    }

    //public override void OnNetworkSpawn()
    //{
    //    // 내 캐릭터가 아닐 경우, 조작 관련 컴포넌트를 비활성화하여 남의 캐릭터를 조작하는 것을 방지
    //    if (!IsOwner)
    //    {
    //        InputHandler.enabled = false;
    //        StateMachine.enabled = false;
    //    }
    //}
}