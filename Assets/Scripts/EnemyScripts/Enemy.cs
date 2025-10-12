using System;
using System.Collections;
using Unity.Behavior;

using UnityEngine;
using UnityEngine.AI;

public class Enemy: MonoBehaviour
{
    public EnemySense Senses { get; private set; }
    public EnemyMotor Motor { get; private set; }
    public EnemyStats Stats { get; private set; }
    public EnemyCombat Combat { get; private set; }
    public Animator Anim { get; private set; }
    public EnemyAnimationManager AnimationManager { get; private set; }
    public BehaviorGraphAgent BehaviourTree { get; private set; }

    private void Awake()
    {
        Stats = GetComponent<EnemyStats>();
        Senses = GetComponent<EnemySense>();
        Motor = GetComponent<EnemyMotor>();
        Combat = GetComponent<EnemyCombat>();
        Anim = GetComponent<Animator>();
        BehaviourTree = GetComponent<BehaviorGraphAgent>();
        AnimationManager = GetComponent<EnemyAnimationManager>();
    }


    public void AE_PlaySFX(string name)
    {
        SoundManager.Instance.PlaySFX(name);
    }
}
