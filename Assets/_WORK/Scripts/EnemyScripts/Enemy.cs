using System;
using System.Collections;
using Unity.Behavior;

using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Playables;

public class Enemy: MonoBehaviour, ITargetable
{
    public Transform TargetTransform => transform;

    [SerializeField] private Transform lockOnPoint;
    public Transform LockOnPoint => lockOnPoint;

    public bool IsTargetableDead => Stats.IsDead;


    [field: Header("Core Systems")]
    [field: SerializeField] public EnemyAIController AIController { get; private set; } // Input 대신 뇌가 들어감
    [field: SerializeField] public EnemyMotor Motor { get; private set; }
    [field: SerializeField] public EnemyStats Stats { get; private set; }


    [field: Header("Combat")]
    [field: SerializeField] public EnemyCombat Combat { get; private set; }
    [field: SerializeField] public EnemySense Sense { get; private set; }

    [field: SerializeField] public AnimationController AnimationController { get; private set; }


    public EnemyStateMachine StateMachine { get; private set; }

    private void Awake()
    {
        StateMachine = new EnemyStateMachine(this);
        AIController.Init(this);

        Stats.OnDamaged += StateMachine.CurrentState.HandleDamage;
    }

    private void Update()
    {
        StateMachine.Update();
    }

    [Header("인살(Deathblow) 타임라인 데이터")]
    public PlayableAsset frontDeathblowTimeline;
    public PlayableAsset behindDeathblowTimeline;

    // 플레이어가 타임라인을 요구할 때 내어주는 함수
    public PlayableAsset GetExecutionTimeline(bool isFront)
    {
        return isFront ? frontDeathblowTimeline : behindDeathblowTimeline;
    }

}
