using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using UnityEngine.AI;
using Action = Unity.Behavior.Action;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "StrafeForDuration", story: "[Agent] Strafes [strafeDuration] seconds for [strafeMagnitude] around [target] at [speed] , maintaining [distance] from target.", category: "Action", id: "a7eed1bf67535922f40ac505a3987665")]
public partial class StrafeForDurationAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<float> Distance; // 플레이어로부터 떨어진 목표 평균 거리
    [SerializeReference] public BlackboardVariable<GameObject> Target;
    [SerializeReference] public BlackboardVariable<float> Speed; // NavMeshAgent의 최대 속도
    [SerializeReference] public BlackboardVariable<float> StrafeDuration; // 스트레이프 총 지속 시간
    [SerializeReference] public BlackboardVariable<float> StrafeMagnitude; // 좌우로 움직일 힘의 크기

    private NavMeshAgent navMeshAgent;
    private Animator anim;
    private Vector3 currentDestination;
    private float actionEndTime; // 스트레이프 액션 전체의 종료 시간
    private int strafeDirectionMultiplier;

    // 다음 목적지까지의 최소 이동 거리
    private const float MIN_STRAFE_MOVE_DISTANCE = 5.0f;

    // 새로운 목적지를 미리 계산할 거리 임계값
    private const float REPLAN_THRESHOLD_DISTANCE = 1.5f;

    // 새로운 경로 계획 중인지 여부 플래그
    private bool isPlanningNewPath = false;

    // 마지막으로 성공적으로 설정된 목적지 (NavMesh 접근 불가능 시 폴백용)
    private Vector3 lastValidDestination;
    private bool hasLastValidDestination = false;

    // --- SINGLE_MOVE_TIMEOUT 관련 변수 다시 추가 ---
    private float currentDestinationSetTime; // 현재 목적지가 설정된 시간
    private const float SINGLE_MOVE_TIMEOUT = 5.0f; // 단일 목적지까지 이동 시간 초과 임계값 (조절 가능)
    // --- SINGLE_MOVE_TIMEOUT 관련 변수 다시 추가 끝 ---


    protected override Status OnStart()
    {
        if (Agent.Value == null || Target.Value == null)
        {
            Debug.LogError("StrafeForDurationAction: Agent or Target is not assigned. Returning Failure.");
            return Status.Failure;
        }

        navMeshAgent = Agent.Value.GetComponent<NavMeshAgent>();
        anim = Agent.Value.GetComponent<Animator>();

        if (navMeshAgent == null)
        {
            Debug.LogError("StrafeForDurationAction: NavMeshAgent component not found on Agent. Returning Failure.");
            return Status.Failure;
        }
        if (anim == null)
        {
            Debug.LogWarning("StrafeForDurationAction: Animator component not found on Agent. Animation will not work.");
        }

        navMeshAgent.speed = Speed.Value;
        navMeshAgent.stoppingDistance = 0.1f; // 매우 작게 유지하여 거의 정확히 도달하도록 유도
        navMeshAgent.updateRotation = false; // 직접 회전 제어
        navMeshAgent.autoBraking = false; // 자동 제동 끄기

        // 초기화
        hasLastValidDestination = false;
        isPlanningNewPath = false;

        SetNewDestination(); // 첫 목적지 설정 시도 (currentDestinationSetTime도 여기서 초기화됨)

        actionEndTime = Time.time + StrafeDuration.Value; // 액션 전체의 종료 시간 설정

        if (anim != null)
        {
            anim.SetFloat("moveDirX", 0);
            anim.SetFloat("moveDirZ", 0);
            anim.SetFloat("Speed", 0); // 초기에는 멈춰있으므로 Speed 0
        }

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (Target.Value == null || navMeshAgent == null)
        {
            return Status.Failure;
        }

        LookAtTarget();

        // 1. 전체 스트레이프 액션의 지속 시간 확인 (액션 종료 조건)
        if (Time.time >= actionEndTime)
        {
            if (navMeshAgent.isOnNavMesh)
            {
                navMeshAgent.ResetPath();
            }
            if (anim != null)
            {
                anim.SetFloat("moveDirZ", 0);
                anim.SetFloat("moveDirX", 0);
                anim.SetFloat("Speed", 0); // 액션 종료 시 멈추므로 Speed 0
            }
            // Debug.Log("StrafeForDurationAction: Total strafe duration ended. Returning Success.");
            return Status.Success;
        }

        // 2. 경로 갱신 조건:
        //    a) 현재 NavMeshAgent가 경로를 가지고 있지 않거나 (NavMesh 접근 불가능 지점 진입 등)
        //    b) 현재 목표까지 남은 거리가 REPLAN_THRESHOLD_DISTANCE 이내이거나
        //    c) 현재 목표까지 이동하는 데 SINGLE_MOVE_TIMEOUT을 초과한 경우
        //    그리고 새로운 경로를 계획 중이 아닐 때만 SetNewDestination 시도
        if (!isPlanningNewPath &&
            (!navMeshAgent.hasPath ||
             navMeshAgent.remainingDistance <= REPLAN_THRESHOLD_DISTANCE ||
             (navMeshAgent.hasPath && (Time.time - currentDestinationSetTime >= SINGLE_MOVE_TIMEOUT)))) // SINGLE_MOVE_TIMEOUT 사용
        {
            // Debug.Log($"[OnUpdate] Replan condition met. hasPath: {navMeshAgent.hasPath}, remaining: {navMeshAgent.remainingDistance:F2}, timeout: {Time.time - currentDestinationSetTime:F2} / {SINGLE_MOVE_TIMEOUT:F2}");
            SetNewDestination();
        }

        // 애니메이션 파라미터 업데이트
        if (anim != null)
        {
            Vector3 worldVelocity = navMeshAgent.velocity;
            Vector3 localVelocity = Agent.Value.transform.InverseTransformDirection(worldVelocity);

            float targetMoveX = localVelocity.x;
            float targetMoveZ = localVelocity.z;
            float currentSpeedNormalized = 0f;

            if (worldVelocity.magnitude < 0.1f) // 아주 작은 속도는 0으로 간주 (거의 정지 상태)
            {
                targetMoveX = 0;
                targetMoveZ = 0;
                currentSpeedNormalized = 0; // 멈춰있으면 Speed 0
            }
            else
            {
                targetMoveX = Mathf.Clamp(localVelocity.x / Speed.Value, -1f, 1f);
                targetMoveZ = Mathf.Clamp(localVelocity.z / Speed.Value, -1f, 1f);

                // NavMeshAgent의 현재 속도 (magnitude)를 MaxSpeed로 정규화
                currentSpeedNormalized = Mathf.Clamp01(worldVelocity.magnitude / Speed.Value);
            }

            anim.SetFloat("moveDirZ", targetMoveZ);
            anim.SetFloat("moveDirX", targetMoveX);
            anim.SetFloat("Speed", currentSpeedNormalized); // "Speed" 파라미터 업데이트
        }

        return Status.Running;
    }

    protected override void OnEnd()
    {
        if (navMeshAgent != null && navMeshAgent.isOnNavMesh)
        {
            navMeshAgent.ResetPath();
            navMeshAgent.updateRotation = true;
            navMeshAgent.autoBraking = true;
        }
        if (anim != null)
        {
            anim.SetFloat("moveDirZ", 0);
            anim.SetFloat("moveDirX", 0);
            anim.SetFloat("Speed", 0); // 종료 시 Speed 0
        }
        // 모든 상태 초기화
        isPlanningNewPath = false;
        hasLastValidDestination = false;
        // currentDestinationSetTime은 액션 종료 후 재시작 시 OnStart에서 초기화될 것이므로 여기서 필요 없음
    }

    private void SetNewDestination()
    {
        if (isPlanningNewPath)
        {
            return;
        }
        isPlanningNewPath = true; // 새로운 경로 계획 시작
        currentDestinationSetTime = Time.time; // 새로운 목적지 설정 시점 기록 (SINGLE_MOVE_TIMEOUT 체크용)

        if (Target.Value == null || Agent.Value == null)
        {
            isPlanningNewPath = false;
            Debug.LogError("StrafeForDurationAction: Target or Agent is null during SetNewDestination. Cannot set destination.");
            return;
        }

        Vector3 agentPosition = Agent.Value.transform.position;
        Vector3 targetPosition = Target.Value.transform.position;

        Vector3 currentVectorFromTarget = (agentPosition - targetPosition);
        currentVectorFromTarget.y = 0;

        strafeDirectionMultiplier = UnityEngine.Random.Range(0, 2) == 0 ? -1 : 1;

        float baseRotationAngle = 30f;
        float rotationAngle = baseRotationAngle;

        Quaternion rotation = Quaternion.Euler(0, rotationAngle * strafeDirectionMultiplier, 0);
        Vector3 rotatedVectorFromTarget = rotation * currentVectorFromTarget.normalized;

        Vector3 potentialDestination = targetPosition + rotatedVectorFromTarget * Distance.Value;

        Vector3 directPathToPotential = potentialDestination - agentPosition;
        if (directPathToPotential.magnitude < MIN_STRAFE_MOVE_DISTANCE)
        {
            potentialDestination = agentPosition + directPathToPotential.normalized * MIN_STRAFE_MOVE_DISTANCE;
            // Debug.Log($"Adjusted new destination to ensure min distance: {directPathToPotential.magnitude:F2} -> {MIN_STRAFE_MOVE_DISTANCE:F2}");
        }

        NavMeshHit hit;
        bool foundValidDestination = false;
        Vector3 chosenDestination = Vector3.zero;

        // --- 유효한 목적지 찾기 시도 (우선순위 순) ---
        // 1. 원하는 스트레이프 목적지 시도
        if (NavMesh.SamplePosition(potentialDestination, out hit, MIN_STRAFE_MOVE_DISTANCE * 2 + 5f, NavMesh.AllAreas))
        {
            chosenDestination = hit.position;
            foundValidDestination = true;
            // Debug.Log($"[SetNewDestination] Found desired strafe point: {chosenDestination}");
        }
        // 2. 현재 에이전트 위치 근처의 NavMesh 지점 시도 (가장 안전한 폴백)
        else if (NavMesh.SamplePosition(agentPosition, out hit, 2f, NavMesh.AllAreas))
        {
            chosenDestination = hit.position;
            foundValidDestination = true;
            Debug.LogWarning("StrafeForDurationAction: Failed to find desired strafe point. Falling back to agent's current NavMesh point.");
        }
        // 3. 타겟(플레이어) 위치 근처의 NavMesh 지점 시도
        else if (Target.Value != null && NavMesh.SamplePosition(Target.Value.transform.position, out hit, 5f, NavMesh.AllAreas))
        {
            chosenDestination = hit.position;
            foundValidDestination = true;
            Debug.LogWarning("StrafeForDurationAction: Failed to find agent's current NavMesh point. Falling back to target's NavMesh point.");
        }
        else
        {
            Debug.LogError("StrafeForDurationAction: Failed to find any valid NavMesh position (desired, agent's, or target's). AI might be stuck.");
        }

        // 최종 목적지 설정 시도
        if (foundValidDestination)
        {
            currentDestination = chosenDestination;
            lastValidDestination = currentDestination;
            hasLastValidDestination = true;

            // NavMeshAgent에 목적지 설정 및 결과 확인
            if (!navMeshAgent.SetDestination(currentDestination))
            {
                Debug.LogError($"[SetNewDestination] Failed to set path to {currentDestination}. Path is invalid or agent disabled. Keeping last valid destination.");
                if (navMeshAgent.isOnNavMesh) navMeshAgent.ResetPath(); // 경로 설정 실패 시 현재 경로 리셋
            }
            // else { Debug.Log($"[SetNewDestination Final] Path successfully set to {currentDestination}."); }
        }
        else
        {
            // 아무 유효한 목적지도 찾지 못했고, 이전에 유효한 목적지가 있었다면 그곳으로 다시 시도
            if (hasLastValidDestination)
            {
                currentDestination = lastValidDestination;
                if (!navMeshAgent.SetDestination(currentDestination))
                {
                    Debug.LogError($"[SetNewDestination] Failed to set path to last valid destination {currentDestination}. AI might be stuck.");
                    if (navMeshAgent.isOnNavMesh) navMeshAgent.ResetPath();
                }
                else
                {
                    Debug.LogWarning($"[SetNewDestination] Reverting to last valid destination: {currentDestination}.");
                }
            }
            else
            {
                // 정말 아무것도 할 수 없을 때: 경로 리셋하고 움직임 멈춤
                if (navMeshAgent.isOnNavMesh) navMeshAgent.ResetPath();
                Debug.LogError("StrafeForDurationAction: No valid destination found and no last valid destination to fall back on. AI is stuck!");
            }
        }

        isPlanningNewPath = false; // 경로 계획 완료 (성공/실패와 무관하게)
    }

    private void LookAtTarget()
    {
        if (Agent.Value == null || Target.Value == null) return;

        Vector3 direction = (Target.Value.transform.position - Agent.Value.transform.position).normalized;
        direction.y = 0;

        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            float rotationLerpSpeed = 5f;
            Agent.Value.transform.rotation = Quaternion.Slerp(Agent.Value.transform.rotation, lookRotation, Time.deltaTime * rotationLerpSpeed);
        }
    }
}