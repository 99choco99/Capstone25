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
    [SerializeReference] public BlackboardVariable<float> Speed;
    [SerializeReference] public BlackboardVariable<float> StrafeDuration; // 스트레이프 지속 시간
    [SerializeReference] public BlackboardVariable<float> StrafeMagnitude; // 좌우로 움직일 힘의 크기

    private NavMeshAgent navMeshAgent;
    private Vector3 currentDestination;
    private float strafeEndTime; // 스트레이프를 멈출 시간
    private int strafeDirectionMultiplier;
    private bool destinationSet = false;

    protected override Status OnStart()
    {

        navMeshAgent = Agent.Value.GetComponent<NavMeshAgent>();
        navMeshAgent.speed = Speed.Value;
        navMeshAgent.updateRotation = false; // NavMeshAgent의 회전을 직접 제어하기 위해 자동 회전 기능 끄기

        SetNewDestination(); // 초기 목적지 설정
        strafeEndTime = Time.time + StrafeDuration.Value; // 스트레이프 종료 시간 설정

        return destinationSet ? Status.Running : Status.Failure;
    }

    protected override Status OnUpdate()
    {
        if (Target.Value == null || navMeshAgent == null || !destinationSet)
        {
            return Status.Failure;
        }

        // 적이 플레이어를 계속 바라보도록 회전
        LookAtTarget();

        if (Time.time >= strafeEndTime)
        {
            SetNewDestination();
            return Status.Success;
        }

        // 새로운 목적지가 유효하게 설정되었으면 이동 명령
        if (destinationSet)
        {
            navMeshAgent.SetDestination(currentDestination);
        }

        return Status.Running;
    }

    protected override void OnEnd()
    {
        // 액션이 종료될 때 NavMeshAgent를 정지시키고 자동 회전을 원래대로 돌려놓음
        if (navMeshAgent != null && navMeshAgent.isOnNavMesh)
        {
            navMeshAgent.ResetPath();
            navMeshAgent.updateRotation = true;
        }
    }

    private void SetNewDestination()
    {
        if (Target.Value == null || Agent.Value == null)
        {
            destinationSet = false;
            return;
        }

        Vector3 agentPosition = Agent.Value.transform.position;
        Vector3 targetPosition = Target.Value.transform.position;


        Vector3 directionFromTarget = (agentPosition - targetPosition).normalized;
        directionFromTarget.y = 0;


        Vector3 rightDirection = Vector3.Cross(directionFromTarget, Vector3.up).normalized;

        //왼쪽 , 오른쪽, 가만히
        strafeDirectionMultiplier = UnityEngine.Random.Range(-1, 2);
        Vector3 strafeVector = strafeDirectionMultiplier * StrafeMagnitude.Value * rightDirection;

        Vector3 newDestination = agentPosition; // 현재 위치를 기준으로 시작

        // 플레이어로부터의 거리 조절 로직
        float currentDistanceFromTarget = Vector3.Distance(agentPosition, targetPosition);
        if (currentDistanceFromTarget > Distance.Value + 0.5f) // Distance보다 약간 멀어지면 플레이어 쪽으로
        {
            newDestination += (targetPosition - agentPosition).normalized * (currentDistanceFromTarget - Distance.Value);
        }
        else if (currentDistanceFromTarget < Distance.Value - 0.5f) // Distance보다 약간 가까워지면 플레이어로부터 멀리
        {
            newDestination -= (targetPosition - agentPosition).normalized * (Distance.Value - currentDistanceFromTarget);
        }

        // 좌우 이동 벡터 추가
        newDestination += strafeVector;


        NavMeshHit hit;
        if (NavMesh.SamplePosition(newDestination, out hit, StrafeMagnitude.Value * 2 + 5f, NavMesh.AllAreas))
        {
            currentDestination = hit.position;
            destinationSet = true;
        }
        else
        {
            // 유효한 NavMesh 위치를 찾지 못했을 경우, 현재 위치 근처에서 다시 시도 (안정성 강화)
            if (NavMesh.SamplePosition(agentPosition, out hit, 2f, NavMesh.AllAreas)) // 현재 위치 근처 2m 반경에서 유효한 NavMesh 찾기
            {
                currentDestination = hit.position;
                destinationSet = true;
            }
            else
            {
                destinationSet = false;
            }
        }
        strafeEndTime = Time.time + StrafeDuration.Value;
    }

    private void LookAtTarget()
    {
        if (Agent.Value == null || Target.Value == null) return;

        Vector3 direction = (Target.Value.transform.position - Agent.Value.transform.position).normalized;
        direction.y = 0;

        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            Agent.Value.transform.rotation = Quaternion.Slerp(Agent.Value.transform.rotation, lookRotation, Time.deltaTime * navMeshAgent.angularSpeed);
        }
    }
}

