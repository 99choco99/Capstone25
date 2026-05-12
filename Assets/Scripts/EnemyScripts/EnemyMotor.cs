using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem.XR;

public class EnemyMotor : MonoBehaviour
{
    private NavMeshAgent navAgent;
    private Enemy enemy;

    [Header("회전 설정")]
    [SerializeField] private float combatRotationSpeed = 15f;
    [SerializeField] private float chaseRotationSpeed = 8f;

    private float combatInputX;
    private float combatInputZ;

    [Header("넉백 설정")]
    private bool isKnockingBack = false;
    private float knockbackForce;       // 넉백될 힘
    private float knockbackTimer = 0f;
    private float knockbackDuration;
    private Vector3 knockbackDirection;

    public enum MovementMode { Idle, Chase, CombatStrafe }
    public MovementMode CurrentMode { get; private set; } = MovementMode.Idle;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
        navAgent = GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
        enemy.Stats.OnDamaged += KnockBackStart;
    }
    private void OnDestroy()
    {
        if (enemy != null && enemy.Stats != null)
            enemy.Stats.OnDamaged -= KnockBackStart;
    }

    private void Update()
    {
        if (isKnockingBack)
        {
            HandleKnockBack();
            return;
        }
        if (!navAgent.isOnNavMesh) { return; }

        switch (CurrentMode)
        {
            case MovementMode.Chase:
                HandleChaseRotation();
                break;
            case MovementMode.CombatStrafe:
                HandleCombatRotation();
                break;
        }
        UpdateAnimatorParameters();
    }


    // ================== 회전 처리 ==================
    private void HandleChaseRotation()
    {
        navAgent.updateRotation = true;
        navAgent.angularSpeed = chaseRotationSpeed * 50f;
    }

    private void HandleCombatRotation()
    {
        if (enemy.Senses.CurrentTarget == null) return;

        navAgent.updateRotation = false;

        Vector3 dir = (enemy.Senses.CurrentTarget.position - transform.position).normalized;
        dir.y = 0;

        if (dir != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, combatRotationSpeed * Time.deltaTime);
        }
    }

    // ================== 모드 제어 ==================
    public void SetMovementMode(MovementMode mode) => CurrentMode = mode;
    public void SetCombatInput(float x, float z)
    {
        combatInputX = x;
        combatInputZ = z;
    }


    // ================== 이동 명령 (BT) ==================

    public void MoveTo(Vector3 destination)
    {
        if (isKnockingBack || !navAgent.isOnNavMesh) { return; }
        navAgent.SetDestination(destination);
        navAgent.isStopped = false;
    }

    public void Chase(Vector3 destination)
    {
        if (isKnockingBack || !navAgent.isOnNavMesh) return;

        SetMovementMode(MovementMode.Chase);
        navAgent.SetDestination(destination);
        navAgent.isStopped = false;
        navAgent.updateRotation = true;
    }

    public void Stop()
    {
        if (!navAgent.isOnNavMesh) return;
        navAgent.isStopped = true;
        navAgent.ResetPath();

        SetCombatInput(0, 0);
        SetMovementMode(MovementMode.Idle);
    }

    // ================== 애니메이션 동기화 =================

    private void UpdateAnimatorParameters()
    {
        Vector3 worldVelocity = navAgent.velocity;
        Vector3 localVelocity = transform.InverseTransformDirection(worldVelocity);

        float forward = localVelocity.z / navAgent.speed;
        float right = localVelocity.x / navAgent.speed;

        enemy.AnimationManager.UpdateLocomotion(forward, right, worldVelocity.magnitude);
    }



    // ================== 넉백 처리 ==================
    public void KnockBackStart(DamageInfo damageInfo)
    {
        if (enemy == null || navAgent == null)
        {
            return;
        }

        knockbackDirection = damageInfo.hitDirection;
        knockbackForce = damageInfo.knockbackForce;
        knockbackDuration = damageInfo.knockbackDuration;
        knockbackTimer = 0f;

        isKnockingBack = true;

        // NavMeshAgent 움직임 멈춤
        navAgent.isStopped = true;
        navAgent.ResetPath();
    }

    private void HandleKnockBack()
    {
        if (isKnockingBack)
        {
            knockbackTimer += Time.fixedDeltaTime;
            float deceleration = 1f - (knockbackTimer / knockbackDuration);
            deceleration = Mathf.Clamp01(deceleration);

            Vector3 moveOffset = knockbackDirection * (knockbackForce * deceleration * Time.deltaTime);
            navAgent.Move(moveOffset);

            if (knockbackTimer >= knockbackDuration)
            {
                isKnockingBack = false;
            }
        }
    }
}
