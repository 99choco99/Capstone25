using UnityEngine;
using UnityEngine.AI;

/// <summary>한 걸음의 목적지를 유지하며, 횡이동은 도착 후 잠시 대치한 뒤 이어갑니다.</summary>
public sealed class EnemyCombatPositioning
{
    private readonly NavMeshAgent agent;
    private readonly Transform body;
    private readonly EnemyData data;
    private readonly NavMeshPath path = new();
    private bool planned;
    private bool completed;
    private bool sideways;
    private int strafeDirection;
    private Vector3 targetAtPlan;
    private Vector3 radialAtPlan;
    private float radiusAtPlan;
    private float requestedDistance;
    private float angleAtPlan;
    private float nextRepathTime;
    private float deadline;

    public EnemyCombatPositioning(NavMeshAgent agent, Transform body, EnemyData data)
    {
        this.agent = agent;
        this.body = body;
        this.data = data;
    }

    public void Reset()
    {
        planned = completed = false;
        strafeDirection = 0;
        nextRepathTime = 0f;
    }

    public void Move(Vector3 target, float distance, bool moveAroundTarget)
    {
        if (planned
            && (sideways != moveAroundTarget
            || Mathf.Abs(distance - requestedDistance) > data.distanceTolerance))
            Reset();
        if (!planned)
        {
            if (Time.time < nextRepathTime)
            {
                Hold();
                return;
            }
            Plan(target, distance, moveAroundTarget);
            return;
        }
        Vector3 targetMovement = Flat(target - targetAtPlan);
        float threshold = Mathf.Max(data.observationDistanceTolerance, agent.radius * 2f);
        if (completed)
        {
            // 횡이동은 짧게 쉰 뒤 이어가고, 후퇴는 상대가 다시 붙을 때만 새로 준비합니다.
            if (sideways && Time.time >= nextRepathTime)
                Plan(target, distance, moveAroundTarget);
            else if (!sideways && Time.time >= nextRepathTime && targetMovement.sqrMagnitude > threshold * threshold
                && Flat(body.position - target).magnitude < distance - data.observationDistanceTolerance)
                Plan(target, distance, moveAroundTarget);
            else
                Hold();
            return;
        }

        bool arrived = !agent.pathPending && agent.hasPath
            && agent.remainingDistance <= agent.stoppingDistance + 0.02f;
        if (Time.time >= deadline || !agent.pathPending && (!agent.hasPath || arrived))
        {
            CompleteStep(blocked: !arrived);
            return;
        }

        if (Time.time >= nextRepathTime && targetMovement.sqrMagnitude > threshold * threshold)
        {
            // Translate the same committed step when the opponent actually moves.
            // Never rotate a fresh waypoint around our new position every frame.
            targetAtPlan = target;
            nextRepathTime = Time.time + Mathf.Max(0.05f, data.strafeRefreshTime);
            if (!TryDestination(target, angleAtPlan))
                CompleteStep(blocked: true);
        }
    }

    private void Plan(Vector3 target, float distance, bool moveAroundTarget)
    {
        sideways = moveAroundTarget;
        requestedDistance = distance;
        targetAtPlan = target;
        Vector3 offset = Flat(body.position - target);
        if (!moveAroundTarget && offset.magnitude >= distance)
        {
            planned = completed = true;
            Hold();
            return;
        }
        radialAtPlan = offset.sqrMagnitude > 0.0001f ? offset.normalized : -body.forward;
        radiusAtPlan = moveAroundTarget ? Mathf.Max(distance, offset.magnitude)
            : distance + Mathf.Max(0.02f, data.arrivalTolerance);
        agent.stoppingDistance = Mathf.Max(0.02f, data.arrivalTolerance);
        agent.speed = moveAroundTarget ? data.strafeSpeed : data.EffectiveSpacingSpeed;

        // One short sidestep leaves time to watch the opponent. The original arc
        // setting is an upper bound, not an endlessly advancing orbit waypoint.
        float stepLength = Mathf.Max(agent.radius * 2f, data.strafeStepDistance);
        float angle = Mathf.Min(data.strafeArcAngle,
            2f * Mathf.Asin(Mathf.Clamp01(stepLength / Mathf.Max(0.01f, 2f * radiusAtPlan))) * Mathf.Rad2Deg);
        int direction = moveAroundTarget && strafeDirection != 0
            ? strafeDirection : Random.value < 0.5f ? -1 : 1;
        angleAtPlan = moveAroundTarget ? angle * direction : 0f;
        bool found = TryDestination(target, angleAtPlan);
        if (!found)
        {
            angleAtPlan = moveAroundTarget ? -angleAtPlan : 35f * direction;
            found = TryDestination(target, angleAtPlan);
        }
        if (!found && !moveAroundTarget)
        {
            angleAtPlan = -angleAtPlan;
            found = TryDestination(target, angleAtPlan);
        }
        nextRepathTime = Time.time + Mathf.Max(0.05f, found ? data.strafeRefreshTime : data.strafeRetryTime);
        planned = found;
        completed = false;
        if (found)
        {
            if (moveAroundTarget)
                strafeDirection = angleAtPlan < 0f ? -1 : 1;
            float travel = Flat(agent.destination - body.position).magnitude / Mathf.Max(0.1f, agent.speed);
            deadline = Time.time + Mathf.Max(data.maxStrafeTime, travel + data.approachRetryDelay);
        }
        else
            Hold();
    }

    private bool TryDestination(Vector3 target, float angle)
    {
        Vector3 destination = target + Quaternion.AngleAxis(angle, Vector3.up) * radialAtPlan * radiusAtPlan;
        destination.y = body.position.y;
        var filter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };
        if (!NavMesh.SamplePosition(destination, out var hit, data.strafeSampleDistance, filter)
            || Mathf.Abs(hit.position.y - body.position.y) > agent.height * 0.5f
            || agent.Raycast(hit.position, out _)
            || !agent.CalculatePath(hit.position, path) || path.status != NavMeshPathStatus.PathComplete)
            return false;

        Vector3 start = Flat(body.position - target);
        Vector3 end = Flat(hit.position - target);
        float radialFloor = Mathf.Min(start.magnitude, radiusAtPlan - data.observationDistanceTolerance);
        // A projected waypoint must not cut through the opponent or turn a retreat
        // into an approach. Both the endpoint and the straight travel corridor count.
        Vector3 segment = end - start;
        float t = segment.sqrMagnitude > 0.0001f
            ? Mathf.Clamp01(-Vector3.Dot(start, segment) / segment.sqrMagnitude)
            : 0f;
        if (end.magnitude + 0.02f < radialFloor
            || (start + segment * t).magnitude + 0.02f < radialFloor)
            return false;

        agent.isStopped = false;
        return agent.SetPath(path);
    }

    private void CompleteStep(bool blocked)
    {
        completed = true;
        if (sideways)
        {
            float pause = blocked ? Mathf.Max(data.strafePauseTime, data.strafeRetryTime) : data.strafePauseTime;
            nextRepathTime = Time.time + Mathf.Max(0.05f, pause);
        }
        Hold();
    }

    private void Hold()
    {
        if (!agent.isStopped)
            agent.isStopped = true;
    }

    private static Vector3 Flat(Vector3 value)
    {
        value.y = 0f;
        return value;
    }
}
