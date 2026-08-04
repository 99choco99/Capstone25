using UnityEngine;

public interface ITargetable
{
    /// <summary>거리와 방향 계산의 기준이 되는 대상의 루트 Transform</summary>
    Transform TargetTransform { get; }

    /// <summary>락온 마커와 카메라가 바라볼 지점. 없으면 TargetTransform을 사용</summary>
    Transform LockOnPoint { get; }

    /// <summary>죽은 대상이 락온 후보와 현재 타겟에서 제외되도록 제공</summary>
    bool IsDead { get; }
}
