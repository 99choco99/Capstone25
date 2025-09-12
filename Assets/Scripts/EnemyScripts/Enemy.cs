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
    public BehaviorGraphAgent BehaviourTree { get; private set; }

    private void Awake()
    {
        Senses = GetComponent<EnemySense>();
        Motor = GetComponent<EnemyMotor>();
        Stats = GetComponent<EnemyStats>();
        BehaviourTree = GetComponent<BehaviorGraphAgent>();
    }

}
