using System;
using System.Collections;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.AI;

public class Enemy: LivingEntity
{
    protected NavMeshAgent NavAgent;
    protected Animator anim;
    protected BehaviorGraphAgent BehaviourAgent;
    [SerializeField] protected EnemyData enemyData;

    public Vector3 directionToTarget;

    [Header("EnemyData")]
    public float normalSightRange;
    public float detectSightRange;
    public float sightAngle;
    public LayerMask targetLayer = 1 << 6;
    public LayerMask obstacleLayer = 1 << 13;

    [Header("EnemyState")]
    Collider[] hits;
    RaycastHit target;
    Transform playerTransform;
    public bool isVulnerable = true;
    public bool isTargetDetected;
    float currentSightRange;
    public bool canTrigger = true;

    private void Awake()
    {
        NavAgent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        BehaviourAgent = GetComponent<BehaviorGraphAgent>();
    }
    private void Start()
    {
        SetUp(enemyData);
    }

    private void Update()
    {
        DetectPlayer();
        if (!anim.GetCurrentAnimatorStateInfo(0).IsName("Start"))
        {
            anim.SetInteger("pattern", 0);
        }
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


    public void DetectPlayer()
    {
        if (Physics.OverlapSphereNonAlloc(transform.position, currentSightRange, hits, targetLayer) > 0)
        {
            playerTransform = hits[0].transform;
            directionToTarget = (playerTransform.position - transform.position).normalized;

            float distance = Vector3.Distance(transform.position, playerTransform.position);
            BehaviourAgent.BlackboardReference.SetVariableValue<float>("CurrentDistance", distance);

            //장애물에 숨어있을 때
            if (Physics.Raycast(transform.position, directionToTarget, out target, currentSightRange, obstacleLayer)){
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

    // 몬스터당 공격가능 시간
    public IEnumerator ResetTrigger()
    {
        yield return new WaitForSeconds(0.5f);
        canTrigger = true;
    }

}
