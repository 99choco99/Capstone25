using UnityEngine;

public interface ITargetable
{
    Transform TargetTransform { get; }
    Transform LockOnPoint { get; }
    bool IsTargetableDead {  get; }
}
