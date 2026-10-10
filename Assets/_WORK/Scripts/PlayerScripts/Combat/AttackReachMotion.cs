using UnityEngine;

public sealed class AttackReachMotion
{
    private Transform contactTarget;
    private float contactGap;
    private int contactHitGroup;
    private Vector3 approachDirection;
    private Vector3 targetStartPosition;
    private float referenceGap;
    private float remainingDistance;

    public Transform Target { get; private set; }
    public Transform ContactTarget => contactTarget;
    public Transform TrackedTarget => Target != null ? Target : contactTarget;

    public bool HasContact(Transform target, int hitGroup)
    {
        return target != null && contactTarget == target && contactHitGroup == hitGroup && contactGap > 0f && !float.IsInfinity(contactGap);
    }

    public void RecordContact(Vector3 sourcePosition, Transform target, float gap, int hitGroup)
    {
        Stop();
        contactTarget = target;
        contactGap = gap;
        contactHitGroup = hitGroup;
        if (target != null)
        {
            Vector3 direction = target.position - sourcePosition;
            direction.y = 0f;
            approachDirection = direction.normalized;
        }
    }

    public bool ValidateContact(Vector3 sourcePosition, float corridorWidth, float maxExtraDistance)
    {
        if (contactTarget == null) return true;

        Vector3 direction = contactTarget.position - sourcePosition;
        direction.y = 0f;
        if (direction.magnitude > contactGap + maxExtraDistance || Vector3.Dot(direction, approachDirection) <= 0f
            || Vector3.ProjectOnPlane(direction, approachDirection).magnitude > corridorWidth)
        {
            Clear();
            return false;
        }
        return true;
    }

    public bool CanBegin(Vector3 sourcePosition, Transform target, int hitGroup, float startRange, float maxExtraDistance, float fallbackGap = 0f)
    {
        float gap = HasContact(target, hitGroup) ? contactGap : fallbackGap;
        if (target == null || gap <= 0f || float.IsNaN(gap) || float.IsInfinity(gap) || maxExtraDistance <= 0f) return false;

        Vector3 direction = target.position - sourcePosition;
        direction.y = 0f;
        float range = startRange > 0f ? startRange : gap;
        return direction.magnitude <= range + maxExtraDistance;
    }

    public void Begin(Vector3 sourcePosition, Transform target, int hitGroup, float startRange, float maxExtraDistance, bool preserveContact = false, float fallbackGap = 0f)
    {
        Stop();
        if (CanBegin(sourcePosition, target, hitGroup, startRange, maxExtraDistance, fallbackGap))
        {
            Vector3 direction = target.position - sourcePosition;
            direction.y = 0f;
            float gap = HasContact(target, hitGroup) ? contactGap : fallbackGap;
            remainingDistance = Mathf.Min(maxExtraDistance, Mathf.Max(0f, direction.magnitude - gap));
            if (remainingDistance > 0f)
            {
                Target = target;
                referenceGap = gap;
                approachDirection = direction.normalized;
                targetStartPosition = target.position;
            }
        }
        if (!preserveContact) contactTarget = null;
    }

    public Vector3 Apply(Vector3 deltaPosition, Vector3 sourcePosition, float corridorWidth, float maxForwardMultiplier)
    {
        if (Target == null || remainingDistance <= 0f) return deltaPosition;

        Vector3 direction = Target.position - sourcePosition;
        direction.y = 0f;
        Vector3 targetMovement = Target.position - targetStartPosition;
        targetMovement.y = 0f;
        float currentGap = direction.magnitude;
        if (currentGap <= referenceGap)
        {
            Stop();
            return deltaPosition;
        }
        if (Vector3.Dot(direction, approachDirection) <= 0f
            || Vector3.ProjectOnPlane(direction, approachDirection).magnitude > corridorWidth
            || Vector3.Dot(targetMovement, approachDirection) > corridorWidth)
        {
            Clear();
            return deltaPosition;
        }

        direction /= currentGap;
        float forwardDistance = Vector3.Dot(deltaPosition, direction);
        if (forwardDistance <= 0f) return deltaPosition;

        float availableDistance = Mathf.Max(0f, currentGap - referenceGap - forwardDistance);
        float extraDistance = Mathf.Min(remainingDistance, Mathf.Min(availableDistance, forwardDistance * Mathf.Max(0f, maxForwardMultiplier - 1f)));
        remainingDistance -= extraDistance;
        return deltaPosition + direction * extraDistance;
    }

    public void Stop()
    {
        Target = null;
        remainingDistance = 0f;
    }

    public void Clear()
    {
        Stop();
        contactTarget = null;
    }
}
