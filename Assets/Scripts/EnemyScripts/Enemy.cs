using System;
using System.Collections;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.AI;

public class Enemy: LivingEntity
{
    protected NavMeshAgent agent;
    protected Animator anim;
    protected BehaviorGraphAgent BehaviourAgent;
    [SerializeField] protected EnemyData enemyData;

    public Vector3 directionToTarget;

    [Header("EnemyData")]
    public float sightRange;
    public float sightAngle;
    public LayerMask targetLayer = 1 << 6;
    public LayerMask obstacleLayer = 1 << 13;
    Collider[] hits;
    RaycastHit target;

    [Header("EnemyState")]
    public bool isTargetDetected;
    public bool isChasing;
    public bool isVulnerable = true;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        BehaviourAgent = GetComponent<BehaviorGraphAgent>();
    }
    private void Start()
    {
        SetUp(enemyData);
    }

    private void Update()
    {
        if (Physics.OverlapSphereNonAlloc(transform.position, sightRange, hits, targetLayer) > 0) {
            Transform playerTransform = hits[0].transform;
            directionToTarget = (playerTransform.position - transform.position).normalized;
            if(Vector3.Dot(directionToTarget, transform.forward) < sightAngle){ return; }


            if(Physics.Raycast(transform.position, directionToTarget, out target, sightRange, obstacleLayer))
            {
                Debug.Log("사라짐");
                isTargetDetected = false;
                isVulnerable = true;
            }
            else
            {
                Debug.Log("발견");
                isTargetDetected= true;
                isVulnerable = false;
            }
        }
        else
        {
            isTargetDetected = false;
            isVulnerable = true;
        }
    }

    public void SetUp(EnemyData enemyData)
    {
        hits = new Collider[1];
        maxHp = enemyData.hp;
        damage = enemyData.damage;
        agent.speed = enemyData.speed;
        base.OnEnable();
        OnDeath += Dead;
    }

    public override void OnDamage(float damage, Vector3 hitPoint, Vector3 hitNormal)
    {
        base.OnDamage(damage, hitPoint, hitNormal);
    }

    void Dead()
    {
        anim.SetTrigger("Die");
        BehaviourAgent.BlackboardReference.SetVariableValue<Boolean>("Dead", true);
        StartCoroutine("Disappear");
    }

    //죽은 후 2.5초뒤 시체 없어짐.
    IEnumerator Disappear()
    {
        yield return new WaitForSeconds(2.5f);
        Destroy(gameObject);
    }
}
