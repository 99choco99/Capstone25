using System;
using UnityEngine;

/// <summary>플레이어 시작 지점에서 기존 F키 상호작용으로 체력을 회복합니다.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SphereCollider))]
public class SpawnRecoveryPoint : MonoBehaviour, IInteractable
{
    [Tooltip("일반 적이 배치된 구역에서는 상호작용 안내를 휴식으로 표시합니다.")]
    [SerializeField] private RandomEnemyPlacement enemyPlacement;

    public string InteractionPrompt => enemyPlacement != null ? "휴식" : "체력 회복";

    public async void Interact(GameObject interactor)
    {
        if (interactor == null || !interactor.TryGetComponent(out Player player)
            || player != Player.LocalPlayer || !player.IsLocalPlayer || player.Stats.IsDead
            || player.Stats.IsHealthDepleted || player.Execution.IsExecuting
            || player.StateMachine.CurrentState is not PlayerGroundedState)
            return;

        if (GameManager.Instance == null || GameManager.Instance.IsChangingScene)
            return;
        try
        {
            await GameManager.Instance.Respawn();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }
}
