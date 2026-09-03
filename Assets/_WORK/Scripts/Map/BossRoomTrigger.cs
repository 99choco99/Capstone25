using System;
using UnityEngine;
using UnityEngine.Timeline;

public class BossRoomTrigger : MonoBehaviour
{
    private event Action OnEnterRoom;
    private bool isLocked = false;


    [SerializeField] private Enemy Boss;
    [SerializeField] private GameObject[] Boundaries;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private TimelineAsset timeline;

    private void OnTriggerEnter(Collider other)
    {
        if (isLocked) { return; }
        isLocked = true;

        SetBlocking();

        if (other.TryGetComponent<Player>(out Player player))
        {
            player.Motor.SetTransform(spawnPoint.position, default);
        }

        Boss.Stats.OnDeath += ClearBossRoom;
        OnEnterRoom?.Invoke();
    }


    private void SetBlocking()
    {
        if(Boundaries == null || Boundaries.Length == 0) { Debug.LogError("설정된 블록이 없음"); return; }
        foreach (var boundary in Boundaries)
        {
            boundary.SetActive(isLocked);
        }
    }

    private void ClearBossRoom()
    {
        Boss.Stats.OnDeath -= ClearBossRoom;
        isLocked = false;
        SetBlocking();
    }

}
