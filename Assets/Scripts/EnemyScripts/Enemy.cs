using System;
using System.Collections;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.AI;

public class Enemy: LivingEntity
{
    public Rigidbody rb;
    public NavMeshAgent NavAgent;
    public Animator anim;
    protected BehaviorGraphAgent BehaviourAgent;
    public EnemyAttack enemyAttack;
    EnemyKnockBack enemyGuard;
    [SerializeField] protected EnemyData enemyData;

    [Header("EnemyData")]
    [SerializeField] private float guardChance = 0.9f;
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

    [Header("EnemyBoolState")]
    public bool freezeRotation = false;
    public bool isVulnerable = true;
    public bool isTargetDetected;
    public Coroutine knockbackCoroutine;

    private void Awake()
    {
        NavAgent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        BehaviourAgent = GetComponent<BehaviorGraphAgent>();
        enemyAttack = GetComponent<EnemyAttack>();
        enemyGuard = GetComponent<EnemyKnockBack>();
        rb = GetComponent<Rigidbody>();
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
                if (!freezeRotation)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(directionToTarget), 5 * Time.deltaTime);
                }
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


    public override void OnDamage(Attack currentPattern, int currentAnimationIndex, Vector3 hitDirection)
    {
        this.hitDirection = hitDirection;
        //뒤로 공격 받음
        if (Vector3.Dot(hitDirection, transform.forward) > 0)
        {
            anim.SetTrigger("BackHit");
            //base.OnDamage(currentPattern, currentAnimationIndex, hitDirection);
            return;
        }
        Debug.Log("정면");
        //정면 공격 받음
        float randomValue = UnityEngine.Random.value;
        if (randomValue <= guardChance)
        {
            anim.SetTrigger("Guard");
        }
        else
        {
            anim.SetTrigger("Hit");
            //base.OnDamage(currentPattern,currentAnimationIndex,this.hitDirection);
        }
        //enemyGuard.KnockBack(); 넉백 문제 해결 필요
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
