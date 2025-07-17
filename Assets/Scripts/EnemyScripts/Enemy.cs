using System;
using System.Collections;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.AI;

public class Enemy: LivingEntity
{
    protected NavMeshAgent NavAgent;
    public Animator anim;
    protected BehaviorGraphAgent BehaviourAgent;
    public EnemyAttack enemyAttack;
    [SerializeField] protected EnemyData enemyData;

    [Header("EnemyData")]
    float currentSightRange;
    public float normalSightRange;
    public float detectSightRange;
    public float sightAngle;
    public float currentAttackDamage;

    [Header("EnemyDetectData")]
    Collider[] hits;
    Transform playerTransform;
    public Vector3 directionToTarget;
    public LayerMask targetLayer = 1 << 6;
    public LayerMask obstacleLayer = 1 << 13;

    [Header("EnemyState")]
    public bool isVulnerable = true;
    public bool isTargetDetected;
    public bool canTrigger = true;

    private void Awake()
    {
        NavAgent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        BehaviourAgent = GetComponent<BehaviorGraphAgent>();
        enemyAttack = GetComponent<EnemyAttack>();
    }
    private void Start()
    {
        SetUp(enemyData);
    }

    private void Update()
    {
        DetectPlayer();
    }

    public void SetUp(EnemyData enemyData)
    {
        hits = new Collider[1];
        maxHp = enemyData.hp;
        damage = enemyData.damage;
        NavAgent.speed = enemyData.speed;
        base.OnEnable();
        OnDeath += Dead;
    }


    //플레이어 발견 로직
    public void DetectPlayer()
    {
        if (Physics.OverlapSphereNonAlloc(transform.position, currentSightRange, hits, targetLayer) > 0)
        {
            playerTransform = hits[0].transform;
            directionToTarget = (playerTransform.position - transform.position).normalized;

            float distance = Vector3.Distance(transform.position, playerTransform.position);
            BehaviourAgent.BlackboardReference.SetVariableValue<float>("CurrentDistance", distance);

            //장애물에 숨어있을 때
            if (Physics.Raycast(transform.position, directionToTarget, currentSightRange, obstacleLayer)){
                SetDetectState(false);
                return;
            }
            if (Vector3.Dot(directionToTarget, transform.forward) > Mathf.Cos(sightAngle * 0.5f * Mathf.Deg2Rad) || isTargetDetected)
            {
                BehaviourAgent.BlackboardReference.SetVariableValue<GameObject>("Target", hits[0].gameObject);
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(directionToTarget), 5 * Time.deltaTime);
                SetDetectState(true);
                currentSightRange = detectSightRange;
            }
            else
            {
                SetDetectState(false);
            }
        }
        else
        {
            SetDetectState(false);
            currentSightRange = normalSightRange;
        }

    }

    public void SetDetectState(bool isDetect)
    {
        BehaviourAgent.BlackboardReference.SetVariableValue<bool>("IsTargetDetected", isDetect);
        isTargetDetected = isDetect;
        isVulnerable = !isDetect;
    }


    public override void OnDamage(Attack currentPattern, int currentAnimationIndex, Vector3 hitNormal)
    {

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

    // 몬스터당 공격가능 시간
    public IEnumerator ResetTrigger()
    {
        yield return new WaitForSeconds(1f);
        canTrigger = true;
    }

}
