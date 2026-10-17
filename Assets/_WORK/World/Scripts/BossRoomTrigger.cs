using UnityEngine;
using UnityEngine.Playables;

public class BossRoomTrigger : MonoBehaviour
{
    private bool isLocked = false;
    private Player introPlayer;
    private PlayableDirector activeIntroDirector;

    [SerializeField] private Enemy Boss;
    [SerializeField] private GameObject[] Boundaries;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private PlayableDirector introDirector;
    [SerializeField] private PlayableDirector executionDirector;

    /// <summary>이동만 막는 투명 경계입니다. 보이는 벽처럼 락온 시야를 가리지 않습니다.</summary>
    public bool IsBoundary(Collider candidate)
    {
        if (candidate == null || Boundaries == null)
            return false;
        foreach (GameObject boundary in Boundaries)
            if (boundary != null && candidate.transform.IsChildOf(boundary.transform))
                return true;
        return false;
    }

    private void OnDisable()
    {
        if (Boss != null && Boss.Stats != null)
            Boss.Stats.OnDeath -= ClearBossRoom;

        bool wasLocked = isLocked;
        isLocked = false;
        try
        {
            CancelIntro();
        }
        finally
        {
            if (wasLocked)
                SetBlocking();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isActiveAndEnabled || isLocked || Boss == null || Boss.IsDead || !other.TryGetComponent(out Player player))
            return;
        isLocked = true;

        SetBlocking();
        introPlayer = player;
        activeIntroDirector = introDirector;
        activeIntroDirector.stopped += OnIntroStopped;
        Boss.Stats.OnDeath += ClearBossRoom;

        player.InputHandler.SetInputEnabled(false);
        player.StateMachine.TransitionTo(player.StateMachine.PlayerGroundedState);
        player.Motor.SetTransform(spawnPoint.position, spawnPoint.rotation);
        introDirector.Play();
    }

    private void OnIntroStopped(PlayableDirector director)
    {
        if (director != activeIntroDirector)
            return;
        director.stopped -= OnIntroStopped;
        activeIntroDirector = null;
        Player player = introPlayer;
        introPlayer = null;

        try
        {
            if (!isActiveAndEnabled || player == null || player != Player.LocalPlayer || Boss == null || Boss.IsDead
                || GameManager.Instance != null && GameManager.Instance.IsChangingScene)
                return;

            Boss.Sense.enabled = true;
            Boss.Sense.Alert(player.transform.position);
        }
        finally
        {
            RestoreIntroInput(player);
        }
    }

    private void CancelIntro()
    {
        PlayableDirector director = activeIntroDirector;
        Player player = introPlayer;
        activeIntroDirector = null;
        introPlayer = null;

        try
        {
            if (director != null)
            {
                director.stopped -= OnIntroStopped;
                director.Stop();
            }
        }
        finally
        {
            RestoreIntroInput(player);
        }
    }

    private static void RestoreIntroInput(Player player)
    {
        if (player != null && player.InputHandler != null)
            player.InputHandler.SetInputEnabled(true);
    }

    private void SetBlocking()
    {
        if (Boundaries == null || Boundaries.Length == 0)
        {
            Debug.LogError("설정된 블록이 없음");
            return;
        }
        foreach (var boundary in Boundaries)
        {
            if (boundary != null)
                boundary.SetActive(isLocked);
        }
    }

    private void ClearBossRoom()
    {
        if (!isActiveAndEnabled || !isLocked)
            return;

        Boss.Stats.OnDeath -= ClearBossRoom;
        isLocked = false;
        SetBlocking();
        CancelIntro();
        executionDirector.Play();
    }
}
