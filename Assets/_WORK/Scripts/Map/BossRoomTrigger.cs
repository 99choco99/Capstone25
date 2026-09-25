using System;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Playables;

public class BossRoomTrigger : MonoBehaviour
{
    private event Action OnEnterRoom;
    private bool isLocked = false;


    [SerializeField] private Enemy Boss;
    [SerializeField] private GameObject[] Boundaries;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private PlayableDirector introDirector;
    [SerializeField] private PlayableDirector executionDirector;

    private void OnTriggerEnter(Collider other)
    {
        if (isLocked || Boss.IsDead || !other.TryGetComponent(out Player player))
            return;
        isLocked = true;

        SetBlocking();
        introDirector.stopped += OnIntroStopped;

        player.InputHandler.SetInputEnabled(false);
        player.Motor.SetTransform(spawnPoint.position, spawnPoint.rotation);
        introDirector.Play();
        Boss.Stats.OnDeath += ClearBossRoom;
        OnEnterRoom?.Invoke();
    }

    private void OnIntroStopped(PlayableDirector director)
    {
        director.stopped -= OnIntroStopped;

        Boss.Sense.enabled = true;
        Boss.Sense.Alert(Player.LocalPlayer.transform.position);
        Player.LocalPlayer.InputHandler.SetInputEnabled(true);
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
        executionDirector.Play();
    }

}
